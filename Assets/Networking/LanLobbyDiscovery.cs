using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace DefenseDuell.Networking
{
    public sealed class LanLobbyInfo
    {
        public string LobbyId { get; }
        public string HostAddress { get; }
        public ushort Port { get; }
        public string MapName { get; }
        public string FormatName { get; }
        public int PlayerCount { get; }
        public int Capacity { get; }
        public double LastSeen { get; }
        internal ulong Sequence { get; }
        internal LanLobbyInfo(LanLobbyAdvertisement value,IPAddress address,double now)
        {LobbyId=value.LobbyId;HostAddress=address.ToString();Port=value.Port;MapName=value.MapName;FormatName=value.FormatName;
            PlayerCount=value.PlayerCount;Capacity=value.Capacity;LastSeen=now;Sequence=value.Sequence;}
    }
    public sealed class LanLobbyAdvertisement
    {
        public Guid ProcessId;
        public ulong Epoch,Sequence;
        public string CatalogHash,MapName,FormatName;
        public ushort Port;
        public int PlayerCount,Capacity;
        public bool Joinable;
        public string LobbyId=>ProcessId.ToString("N")+":"+Epoch.ToString("x16");
    }
    public static class LanDiscoveryProtocol
    {
        public const int Port=19175,MaxBytes=512;
        const uint Magic=0x314C4444;
        static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        static bool Label(string value,int maxBytes)
        {
            if(string.IsNullOrWhiteSpace(value))return false;
            foreach(char c in value)if(char.IsControl(c)||c=='<'||c=='>')return false;
            return Utf8.GetByteCount(value)<=maxBytes;
        }
        static bool Valid(LanLobbyAdvertisement value)
        {
            if(value==null||value.ProcessId==Guid.Empty||value.Epoch==0||value.Sequence==0||value.Port==0
                || (value.Capacity!=2&&value.Capacity!=4&&value.Capacity!=6)||value.PlayerCount<1||value.PlayerCount>value.Capacity
                || value.Joinable&&value.PlayerCount>=value.Capacity||!Label(value.MapName,128)||!Label(value.FormatName,32)
                ||value.CatalogHash==null||value.CatalogHash.Length!=64)return false;
            foreach(char c in value.CatalogHash)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;
            return true;
        }
        public static byte[] Encode(LanLobbyAdvertisement value)
        {
            if(!Valid(value))throw new ArgumentException("Invalid LAN announcement.");
            using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
            writer.Write(Magic);writer.Write(WireProtocol.Version);writer.Write(value.ProcessId.ToByteArray());writer.Write(value.Epoch);writer.Write(value.Sequence);
            writer.Write(Encoding.ASCII.GetBytes(value.CatalogHash));writer.Write(value.Port);writer.Write((byte)value.PlayerCount);writer.Write((byte)value.Capacity);writer.Write(value.Joinable);
            WriteLabel(writer,value.MapName);WriteLabel(writer,value.FormatName);return stream.ToArray();
        }
        static void WriteLabel(BinaryWriter writer,string value){var bytes=Utf8.GetBytes(value);writer.Write((ushort)bytes.Length);writer.Write(bytes);}
        static string ReadLabel(BinaryReader reader,int max)
        {int count=reader.ReadUInt16();if(count<1||count>max||count>reader.BaseStream.Length-reader.BaseStream.Position)throw new IOException();return Utf8.GetString(reader.ReadBytes(count));}
        public static bool TryDecode(byte[] bytes,string catalogHash,out LanLobbyAdvertisement value)
        {
            value=null;if(bytes==null||bytes.Length>MaxBytes||bytes.Length<109)return false;
            try
            {
                using var stream=new MemoryStream(bytes,false);using var reader=new BinaryReader(stream);
                if(reader.ReadUInt32()!=Magic||reader.ReadUInt16()!=WireProtocol.Version)return false;
                var next=new LanLobbyAdvertisement{ProcessId=new Guid(reader.ReadBytes(16)),Epoch=reader.ReadUInt64(),Sequence=reader.ReadUInt64(),
                    CatalogHash=Encoding.ASCII.GetString(reader.ReadBytes(64)),Port=reader.ReadUInt16(),PlayerCount=reader.ReadByte(),Capacity=reader.ReadByte()};
                byte flag=reader.ReadByte();if(flag>1)return false;next.Joinable=flag==1;next.MapName=ReadLabel(reader,128);next.FormatName=ReadLabel(reader,32);
                if(stream.Position!=stream.Length||next.CatalogHash!=catalogHash||!Valid(next))return false;
                value=next;return true;
            }
            catch(IOException){return false;}catch(ArgumentException){return false;}
        }
    }
    /// <summary>Bounded directory, independent of sockets so packet/clock/lifecycle rules are testable.</summary>
    public sealed class LanLobbyDirectory
    {
        public const double TtlSeconds=4.5;
        public const int Capacity=64;
        readonly Dictionary<string,LanLobbyInfo> entries=new Dictionary<string,LanLobbyInfo>();
        readonly List<string> expired=new List<string>();
        readonly Dictionary<string,(ulong sequence,double seen)> announcements=new Dictionary<string,(ulong,double)>();
        readonly Guid ownProcess;
        readonly string catalogHash;
        public LanLobbyDirectory(Guid ownProcess,string catalogHash){this.ownProcess=ownProcess;this.catalogHash=catalogHash;}
        public IReadOnlyList<LanLobbyInfo> Snapshot()
        {var values=new List<LanLobbyInfo>(entries.Values);values.Sort((a,b)=>string.CompareOrdinal(a.LobbyId,b.LobbyId));return values.AsReadOnly();}
        public bool Receive(byte[] bytes,IPAddress source,double now)
        {
            if(double.IsNaN(now)||double.IsInfinity(now)||source==null||source.AddressFamily!=AddressFamily.InterNetwork
                ||IPAddress.Any.Equals(source)||IPAddress.Broadcast.Equals(source)||source.GetAddressBytes()[0]==0||source.GetAddressBytes()[0]>=224
                ||!LanDiscoveryProtocol.TryDecode(bytes,catalogHash,out var next)||next.ProcessId==ownProcess)return false;
            bool known=announcements.TryGetValue(next.LobbyId,out var previous);
            if(known&&next.Sequence<=previous.sequence)return false;
            if(!known&&announcements.Count>=Capacity)return false;
            announcements[next.LobbyId]=(next.Sequence,now);
            entries.TryGetValue(next.LobbyId,out var old);
            if(!next.Joinable)return entries.Remove(next.LobbyId);
            entries[next.LobbyId]=new LanLobbyInfo(next,source,now);
            // TTL renewals do not rebuild the UI unless visible metadata changes.
            return old==null||old.HostAddress!=source.ToString()||old.Port!=next.Port||old.PlayerCount!=next.PlayerCount
                ||old.Capacity!=next.Capacity||old.MapName!=next.MapName||old.FormatName!=next.FormatName;
        }
        public bool Expire(double now)
        {
            expired.Clear();foreach(var pair in announcements)if(now-pair.Value.seen>TtlSeconds||now<pair.Value.seen)expired.Add(pair.Key);
            bool changed=false;foreach(var key in expired){changed|=entries.Remove(key);announcements.Remove(key);}return changed;
        }
        public bool Clear(){bool changed=entries.Count>0;entries.Clear();announcements.Clear();return changed;}
        public bool TryGet(string id,double now,out LanLobbyInfo lobby)
        {lobby=null;return id!=null&&entries.TryGetValue(id,out lobby)&&now>=lobby.LastSeen&&now-lobby.LastSeen<=TtlSeconds;}
    }
    /// <summary>Real UDP broadcast, polled on the Unity owner thread; never initiates a game join.</summary>
    public sealed class LanLobbyDiscovery : IDisposable
    {
        public Guid ProcessId { get; }=Guid.NewGuid();
        public event Action Changed;
        public string Status {get;private set;}="";
        readonly LanLobbyDirectory directory;
        readonly byte[] receiveBuffer=new byte[LanDiscoveryProtocol.MaxBytes+1];
        Socket socket;
        ulong sequence;
        double nextAnnounce,nextRetry,nextInterfaceCheck;
        string interfaces="";
        bool disposed,wasAdvertising,socketBrowsing;
        LanLobbyAdvertisement lastAnnouncement;
        public LanLobbyDiscovery(string catalogHash){directory=new LanLobbyDirectory(ProcessId,catalogHash);}
        public IReadOnlyList<LanLobbyInfo> Lobbies=>directory.Snapshot();
        public bool TryGet(string id,double now,out LanLobbyInfo lobby)=>directory.TryGet(id,now,out lobby);
        public void Tick(double now,bool browse,LanLobbyAdvertisement announcement)
        {
            if(disposed)return;
            if(!browse&&directory.Clear())Changed?.Invoke();
            if(!browse&&announcement==null){Suspend();return;}
            // Advertising is outbound-only. Do not occupy the shared discovery receive port.
            if(socketBrowsing!=browse){Withdraw();CloseSocket();socketBrowsing=browse;nextRetry=nextAnnounce=0;}
            if(now>=nextInterfaceCheck)
            {
                nextInterfaceCheck=now+2;var current=InterfaceSignature();
                if(current!=interfaces){interfaces=current;CloseSocket();if(directory.Clear())Changed?.Invoke();nextRetry=0;}
            }
            if(socket==null&&now>=nextRetry)
            {
                try
                {
                    socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);
                    socket.EnableBroadcast=true;socket.Blocking=false;socket.Bind(new IPEndPoint(IPAddress.Any,browse?LanDiscoveryProtocol.Port:0));socketBrowsing=browse;Status="";
                }
                catch(SocketException){CloseSocket();nextRetry=now+5;Status="WLAN-Suche derzeit nicht verfügbar. Direkte IP bleibt möglich.";}
            }
            if(socket==null)return;
            bool changed=false;
            for(int received=0;browse&&received<16;received++)
            {
                try
                {
                    EndPoint source=new IPEndPoint(IPAddress.Any,0);
                    int count=socket.ReceiveFrom(receiveBuffer,0,receiveBuffer.Length,SocketFlags.None,ref source);
                    if(!browse||count>LanDiscoveryProtocol.MaxBytes)continue;
                    var packet=new byte[count];Buffer.BlockCopy(receiveBuffer,0,packet,0,count);
                    changed|=directory.Receive(packet,((IPEndPoint)source).Address,now);
                }
                catch(SocketException error)
                {if(error.SocketErrorCode==SocketError.MessageSize)continue;if(error.SocketErrorCode==SocketError.WouldBlock||error.SocketErrorCode==SocketError.IOPending)break;CloseSocket();nextRetry=now+5;break;}
            }
            changed|=directory.Expire(now);if(changed)Changed?.Invoke();
            if(announcement!=null&&now>=nextAnnounce)
            {
                nextAnnounce=now+1;announcement.ProcessId=ProcessId;announcement.Sequence=++sequence;
                SendAnnouncement(announcement);lastAnnouncement=announcement;wasAdvertising=true;
            }
            else if(announcement==null&&wasAdvertising)Withdraw();
        }
        void SendAnnouncement(LanLobbyAdvertisement value)
        {
            if(socket==null)return;
            try{var packet=LanDiscoveryProtocol.Encode(value);socket.SendTo(packet,new IPEndPoint(IPAddress.Broadcast,LanDiscoveryProtocol.Port));}
            catch(SocketException){Status="WLAN-Suche derzeit nicht verfügbar. Direkte IP bleibt möglich.";}
        }
        void Withdraw()
        {if(wasAdvertising&&lastAnnouncement!=null){lastAnnouncement.Joinable=false;lastAnnouncement.Sequence=++sequence;SendAnnouncement(lastAnnouncement);}wasAdvertising=false;lastAnnouncement=null;}
        public void Suspend(){Withdraw();CloseSocket();nextRetry=nextAnnounce=nextInterfaceCheck=0;if(directory.Clear())Changed?.Invoke();}
        void CloseSocket(){socket?.Dispose();socket=null;}
        static string InterfaceSignature()
        {
            try
            {
                var addresses=new List<string>();foreach(var item in NetworkInterface.GetAllNetworkInterfaces())
                    if(item.OperationalStatus==OperationalStatus.Up)foreach(var address in item.GetIPProperties().UnicastAddresses)
                        if(address.Address.AddressFamily==AddressFamily.InterNetwork)addresses.Add(address.Address.ToString());
                addresses.Sort(StringComparer.Ordinal);return string.Join(";",addresses);
            }
            catch(NetworkInformationException){return "unavailable";}catch(PlatformNotSupportedException){return "unsupported";}
        }
        public void Dispose(){if(disposed)return;Suspend();disposed=true;}
    }
}
