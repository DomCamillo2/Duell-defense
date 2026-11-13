using System.Linq;
using System.Xml;
using NUnit.Framework;
using DefenseDuell.Editor;

namespace DefenseDuell.Tests
{
    public sealed class LanDiscoveryManifest15Tests
    {
        [Test] public void DiscoveryPermissionsMergeIdempotentlyAndPreserveUnityActivity()
        {
            const string ns="http://schemas.android.com/apk/res/android";
            var document=new XmlDocument();
            document.LoadXml("<manifest xmlns:android='"+ns+"'><uses-permission android:name='android.permission.INTERNET'/><application><activity android:name='UnityActivity'/></application></manifest>");
            foreach(var permission in new[]{"android.permission.ACCESS_WIFI_STATE","android.permission.CHANGE_WIFI_MULTICAST_STATE"})
            {LanDiscoveryAndroidManifest.AddPermission(document,permission);LanDiscoveryAndroidManifest.AddPermission(document,permission);}
            var permissions=document.DocumentElement.SelectNodes("uses-permission").Cast<XmlNode>().Select(p=>p.Attributes["name",ns].Value).ToArray();
            Assert.That(permissions.Length,Is.EqualTo(3));Assert.That(permissions.Distinct().Count(),Is.EqualTo(3));
            Assert.That(document.SelectSingleNode("/manifest/application/activity").Attributes["name",ns].Value,Is.EqualTo("UnityActivity"));
        }
    }
}
