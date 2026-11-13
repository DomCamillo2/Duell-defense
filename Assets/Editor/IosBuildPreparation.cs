// Produces an unsigned Xcode project, never an installable IPA or a store upload.
using System;
using System.IO;
using System.Linq;
using System.Xml;
using DefenseDuell.Content;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseDuell.Editor
{
    public static class IosBuildPreparation
    {
        [Serializable] public sealed class Compatibility
        {
            public string status="xcode_export_not_signed_or_device_tested";
            public string platform="iOS", applicationVersion, unityVersion, bundleIdentifier, baseCatalogHash, simulationRulesVersion;
            public int wireVersion;
            public string[] mapContentHashes;
        }

        // Root invokes with -buildTarget iOS -executeMethod ...Export and a NEW output directory.
        public static void Export()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("Install iOS Build Support for this exact Unity version first.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.iOS)
                throw new BuildFailedException("Start this export with -buildTarget iOS.");
            string output=Environment.GetEnvironmentVariable("DEFENSE_IOS_OUTPUT");
            if(string.IsNullOrWhiteSpace(output))throw new BuildFailedException("DEFENSE_IOS_OUTPUT must name a new export directory.");
            output=Path.GetFullPath(output);
            if(Directory.Exists(output)||File.Exists(output))throw new BuildFailedException("Refusing to overwrite an existing export: "+output);
            var content=AssetDatabase.LoadAssetAtPath<GameContentAsset>("Assets/Content/Standard.asset");
            if(content==null)throw new BuildFailedException("Prepare the versioned project with the integration owner first.");
            var catalog=content.CreateCatalog();string catalogHash=WireProtocol.CatalogHash(catalog);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if(!scenes.Contains(BuildEntry.BootScene)||!scenes.Contains(StudioIntroSetup.ScenePath)||scenes.Any(s=>!File.Exists(s)))
                throw new BuildFailedException("Expected prepared intro + boot scenes.");
            ModelPortraitBaker.Verify();

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"de.defenseduell.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS,ManagedStrippingLevel.Low);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS,1); // ARM64
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{GraphicsDeviceType.Metal});
            PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.targetOSVersionString="15.0";
            // App version/build stay on the same release line as Android; no separate gameplay branch.
            PlayerSettings.iOS.buildNumber=PlayerSettings.Android.bundleVersionCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // Do not set or clear a user's team ID/profile. Xcode selects the user's team later.
            PlayerSettings.iOS.appleEnableAutomaticSigning=true;
            AssetDatabase.SaveAssets();

            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=scenes, locationPathName=output, target=BuildTarget.iOS, options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("iOS export failed: "+report.summary.result);
            if(!Directory.Exists(Path.Combine(output,"Unity-iPhone.xcodeproj")))
                throw new BuildFailedException("Expected Xcode project was not produced.");
            PrepareExportPlist(Path.Combine(output,"Info.plist"));
            var evidence=new Compatibility{
                applicationVersion=PlayerSettings.bundleVersion,unityVersion=Application.unityVersion,
                bundleIdentifier=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS),
                wireVersion=WireProtocol.Version,baseCatalogHash=catalogHash,simulationRulesVersion=GameContent.SimulationRulesVersion,
                mapContentHashes=catalog.Select(m=>m.ContentHash).ToArray()};
            File.WriteAllText(Path.Combine(output,"defense-crossplay.json"),JsonUtility.ToJson(evidence,true));
            Debug.Log("DEFENSE_IOS_XCODE_EXPORTED "+output+"; local-network description included; unsigned and not device tested.");
        }

        public const string LocalNetworkDescription="Defense Duell verbindet sich mit Mitspielern in deinem WLAN, auch auf Android.";

        // Uses framework XML only: compiles without the optional Unity iOS/Xcode assembly.
        // Kept independently callable for verification without launching Unity or Xcode.
        public static void PrepareExportPlist(string path)
        {
            var document=new XmlDocument { XmlResolver=null };
            var settings=new XmlReaderSettings { DtdProcessing=DtdProcessing.Ignore, XmlResolver=null };
            using(var reader=XmlReader.Create(path,settings))document.Load(reader);
            var dict=document.SelectSingleNode("/plist/dict");
            var identifier=dict?.SelectSingleNode("key[text()='CFBundleIdentifier']/following-sibling::*[1]");
            if(identifier==null || identifier.Name!="string" || string.IsNullOrWhiteSpace(identifier.InnerText))
                throw new InvalidDataException("Expected an exported application Info.plist with bundle identifier.");
            foreach(var key in dict.SelectNodes("key[text()='NSLocalNetworkUsageDescription']").Cast<XmlNode>().ToArray())
            {
                var value=key.NextSibling;
                if(value==null || value.Name=="key")throw new InvalidDataException("Malformed local-network plist entry.");
                dict.RemoveChild(value);dict.RemoveChild(key);
            }
            var addedKey=document.CreateElement("key");addedKey.InnerText="NSLocalNetworkUsageDescription";
            var addedValue=document.CreateElement("string");addedValue.InnerText=LocalNetworkDescription;
            dict.AppendChild(addedKey);dict.AppendChild(addedValue);
            document.Save(path);
        }
    }
}
