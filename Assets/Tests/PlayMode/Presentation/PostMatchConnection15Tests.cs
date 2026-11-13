using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class PostMatchConnection15Tests
    {
        const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        [UnityTest] public IEnumerator PreservedResultDisablesRematchWhenTheRoomConnectionHasEnded()
        {
            yield return SceneManager.LoadSceneAsync("Boot");yield return null;yield return null;
            var session=Object.FindFirstObjectByType<SessionCoordinator>();var ui=Object.FindFirstObjectByType<GameUI>();
            var progress=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Single(c=>c.GetType().FullName=="DefenseDuell.Progression.LocalProgress");
            string folder=Path.Combine(Path.GetTempPath(),"DefenseDuell-result15-"+Guid.NewGuid().ToString("N"));
            progress.GetType().GetMethod("Initialize").Invoke(progress,new object[]{session,Path.Combine(folder,"profile.save")});
            try
            {
                ushort port;using(var socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp))
                {socket.Bind(new IPEndPoint(IPAddress.Loopback,0));port=(ushort)((IPEndPoint)socket.LocalEndPoint).Port;}
                session.StartLanHost(port);yield return null;yield return null;
                // Arranged result presentation fixture; this is not a played-match or transport claim.
                session.State.Phase=MatchPhase.Finished;session.State.Outcome=MatchOutcome.Player0;session.State.WinningTeamId=0;
                typeof(SessionCoordinator).GetMethod("LoseConnection",Hidden).Invoke(session,new object[]{"Room ended"});
                Assert.That(session.Status,Is.EqualTo(ConnectionState.Finished));
                typeof(GameUI).GetMethod("Rebuild",Hidden).Invoke(ui,null);
                typeof(GameUI).GetMethod("RefreshLive",Hidden).Invoke(ui,null);yield return null;
                var button=(Button)typeof(GameUI).GetField("rematchButton",Hidden).GetValue(ui);
                var status=(Text)typeof(GameUI).GetField("statusText",Hidden).GetValue(ui);
                Assert.That(button.interactable,Is.False);Assert.That(button.GetComponentInChildren<Text>().text,Is.EqualTo("Raum beendet"));
                Assert.That(status.text,Does.Contain("Verbindung beendet"));
                Assert.That(session.State.Phase,Is.EqualTo(MatchPhase.Finished));
            }
            finally
            {
                session.Shutdown();Object.Destroy(ui.gameObject);Object.Destroy(progress);
            }
            yield return null;if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
    }
}
