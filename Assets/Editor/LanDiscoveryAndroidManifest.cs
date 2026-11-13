using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace DefenseDuell.Editor
{
    /// <summary>Merge LAN discovery permissions without replacing Unity's activity manifest.</summary>
    public sealed class LanDiscoveryAndroidManifest : IPostGenerateGradleAndroidProject
    {
        const string Android="http://schemas.android.com/apk/res/android";
        public int callbackOrder=>100;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifest=Path.Combine(path,"src/main/AndroidManifest.xml");
            var document=new XmlDocument();document.Load(manifest);
            AddPermission(document,"android.permission.ACCESS_WIFI_STATE");
            AddPermission(document,"android.permission.CHANGE_WIFI_MULTICAST_STATE");
            document.Save(manifest);
        }
        public static void AddPermission(XmlDocument document,string permission)
        {
            var root=document.DocumentElement;
            foreach(XmlNode node in root.ChildNodes)
                if(node.Name=="uses-permission"&&node.Attributes?["name",Android]?.Value==permission)return;
            var entry=document.CreateElement("uses-permission");
            var name=document.CreateAttribute("android","name",Android);name.Value=permission;entry.Attributes.Append(name);
            root.AppendChild(entry);
        }
    }
}
