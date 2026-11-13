using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DefenseDuell.Editor
{
    /// <summary>Explicit install and build-time enforcement of the versioned launcher artwork.</summary>
    public sealed class AppIconSetup : IPreprocessBuildWithReport
    {
        const string Folder="Assets/Art/AppIcon/v1";
        const string OpaquePath=Folder+"/defense-duell-icon.png";
        const string ForegroundPath=Folder+"/defense-duell-adaptive-foreground-v2.png";
        const string BackgroundPath=Folder+"/defense-duell-adaptive-background.png";
        const int BackgroundSize=1024;
        const int MaxImportSize=4096;
        public int callbackOrder=>int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform==BuildTarget.iOS)ApplyDefaultIcon();
            else Apply();
        }

        // iOS exports use the opaque default source; Android-specific density slots
        // are enforced only on the established Android/desktop preparation path.
        static void ApplyDefaultIcon()
        {
            ValidateSource(OpaquePath,false);
            var opaque=ImportIcon(OpaquePath,false);
            var sizes=PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown,IconKind.Any);
            Require(sizes!=null&&sizes.Length>0,"Unity returned no default icon slots.");
            foreach(int size in sizes)CheckSize(opaque,size,size,"Default");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,Enumerable.Repeat(opaque,sizes.Length).ToArray(),IconKind.Any);
            AssetDatabase.SaveAssets();
            var actual=PlayerSettings.GetIcons(NamedBuildTarget.Unknown,IconKind.Any);
            Require(actual!=null&&actual.Length==sizes.Length,"Default icon readback has the wrong slot count.");
            foreach(var icon in actual)CheckReference(icon,opaque,"Default iOS export source");
        }

        [MenuItem("Defense Duell/Install app icons")]
        public static void Apply()
        {
            // Check the authored inputs before generating or changing anything.
            ValidateSource(OpaquePath,false);
            ValidateSource(ForegroundPath,true);
            WriteSolidBackground();
            var opaque=ImportIcon(OpaquePath,false);
            var foreground=ImportIcon(ForegroundPath,true);
            var background=ImportIcon(BackgroundPath,false);

            var defaultSizes=PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown,IconKind.Any);
            Require(defaultSizes!=null&&defaultSizes.Length>0,"Unity returned no default icon slots.");
            foreach(int size in defaultSizes)CheckSize(opaque,size,size,"Default");
            // Keep explicitly requested compatibility icons as well as the preferred Adaptive kind.
#pragma warning disable CS0618
            var legacyKind=AndroidPlatformIconKind.Legacy;
            var roundKind=AndroidPlatformIconKind.Round;
#pragma warning restore CS0618
            var legacy=ReadSlots(legacyKind,1);
            var round=ReadSlots(roundKind,1);
            var adaptive=ReadSlots(AndroidPlatformIconKind.Adaptive,2);
            CheckSlotSizes(legacy,opaque);CheckSlotSizes(round,opaque);
            CheckSlotSizes(adaptive,background);CheckSlotSizes(adaptive,foreground);

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,Enumerable.Repeat(opaque,defaultSizes.Length).ToArray(),IconKind.Any);
            SetSlots(legacyKind,legacy,opaque);
            SetSlots(roundKind,round,opaque);
            // Verified against Unity 6000.3.28f1 AndroidPlatformIconKind's layer labels:
            // index 0 = Background, index 1 = Foreground. Not the reverse.
            SetSlots(AndroidPlatformIconKind.Adaptive,adaptive,background,foreground);
            AssetDatabase.SaveAssets();

            var defaults=PlayerSettings.GetIcons(NamedBuildTarget.Unknown,IconKind.Any);
            Require(defaults!=null&&defaults.Length==defaultSizes.Length,"Default icon readback has the wrong slot count.");
            for(int i=0;i<defaults.Length;i++)CheckReference(defaults[i],opaque,"Default slot "+i);
            VerifySlots(legacyKind,legacy,opaque);
            VerifySlots(roundKind,round,opaque);
            VerifySlots(AndroidPlatformIconKind.Adaptive,adaptive,background,foreground);
            Debug.Log("[AppIconSetup] Verified default="+defaults.Length+", Android legacy="+legacy.Length+
                ", round="+round.Length+", adaptive="+adaptive.Length+" (background[0], foreground[1]); all slots use persistent source assets.");
        }

        static void ValidateSource(string path,bool transparent)
        {
            Require(File.Exists(path),"Missing authored icon: "+path+". Supply both v1 images before Apply/build.");
            var image=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Require(image.LoadImage(File.ReadAllBytes(path),false),"Cannot decode PNG: "+path);
                Require(image.width==image.height&&image.width>=432&&image.width<=MaxImportSize,
                    path+" must be square, at least 432 px and at most "+MaxImportSize+" px; got "+image.width+"x"+image.height+".");
                var pixels=image.GetPixels32();
                if(transparent)
                    Require(pixels.Any(p=>p.a==0)&&pixels.Any(p=>p.a>0),path+" needs both transparent space and visible foreground artwork.");
                else Require(pixels.All(p=>p.a==255),path+" must be fully opaque for default/legacy/round icons.");
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }

        static void WriteSolidBackground()
        {
            // The only generated artwork: opaque sRGB navy #06142F, no resampling of authored images.
            var image=new Texture2D(BackgroundSize,BackgroundSize,TextureFormat.RGB24,false);
            try
            {
                var pixels=new Color32[BackgroundSize*BackgroundSize];
                var navy=new Color32(0x06,0x14,0x2F,255);
                for(int i=0;i<pixels.Length;i++)pixels[i]=navy;
                image.SetPixels32(pixels);image.Apply(false,false);
                byte[] bytes=image.EncodeToPNG();Require(bytes!=null&&bytes.Length>0,"Solid navy background PNG encoding failed.");
                Directory.CreateDirectory(Folder);
                if(!File.Exists(BackgroundPath)||!File.ReadAllBytes(BackgroundPath).SequenceEqual(bytes))File.WriteAllBytes(BackgroundPath,bytes);
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }

        static Texture2D ImportIcon(string path,bool transparent)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer!=null,"No TextureImporter for "+path);
            importer.textureType=TextureImporterType.Default;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.sRGBTexture=true;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;
            importer.maxTextureSize=MaxImportSize;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=transparent;
            importer.isReadable=false;
            // Prevent stale per-platform compression/size overrides from defeating these settings.
            importer.ClearPlatformTextureSettings("Android");importer.ClearPlatformTextureSettings("Standalone");
            importer.SaveAndReimport();
            importer=AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer!=null&&importer.textureType==TextureImporterType.Default&&importer.textureShape==TextureImporterShape.Texture2D
                &&importer.sRGBTexture&&!importer.mipmapEnabled&&!importer.crunchedCompression
                &&importer.textureCompression==TextureImporterCompression.Uncompressed&&importer.wrapMode==TextureWrapMode.Clamp
                &&importer.maxTextureSize==MaxImportSize&&importer.npotScale==TextureImporterNPOTScale.None
                &&importer.alphaSource==TextureImporterAlphaSource.FromInput&&importer.alphaIsTransparency==transparent
                &&!importer.GetPlatformTextureSettings("Android").overridden&&!importer.GetPlatformTextureSettings("Standalone").overridden,
                "Icon importer settings did not persist: "+path);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Require(texture!=null&&AssetDatabase.Contains(texture),"Icon did not import as a persistent Texture2D: "+path);
            return texture;
        }

        static PlatformIcon[] ReadSlots(PlatformIconKind kind,int layers)
        {
            var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
            Require(icons!=null&&icons.Length>0,"No Android "+kind+" slots. Install Android Build Support for this Unity Editor; no fallback icon will be used.");
            foreach(var icon in icons)
                Require(icon!=null&&icon.minLayerCount<=layers&&icon.maxLayerCount==layers,
                    "Unexpected Android "+kind+" layer contract; expected "+layers+" layers.");
            return icons;
        }
        static void CheckSlotSizes(PlatformIcon[] icons,Texture2D texture)
        {foreach(var icon in icons)CheckSize(texture,icon.width,icon.height,"Android "+icon.kind);}
        static void CheckSize(Texture2D texture,int width,int height,string slot)
        {Require(width>0&&height>0&&texture.width>=width&&texture.height>=height,slot+" requires "+width+"x"+height+"; source "+texture.name+" is only "+texture.width+"x"+texture.height+".");}
        static void SetSlots(PlatformIconKind kind,PlatformIcon[] slots,params Texture2D[] layers)
        {
            foreach(var slot in slots)slot.SetTextures(layers);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,slots);
        }
        static void VerifySlots(PlatformIconKind kind,PlatformIcon[] expectedSlots,params Texture2D[] layers)
        {
            var actual=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
            Require(actual!=null&&actual.Length==expectedSlots.Length,"Android "+kind+" readback lost icon densities.");
            for(int i=0;i<actual.Length;i++)
            {
                Require(actual[i]!=null&&actual[i].width==expectedSlots[i].width&&actual[i].height==expectedSlots[i].height
                    &&actual[i].layerCount==layers.Length,"Android "+kind+" slot "+i+" changed size/layers on readback.");
                for(int layer=0;layer<layers.Length;layer++)CheckReference(actual[i].GetTexture(layer),layers[layer],"Android "+kind+" slot "+i+" layer "+layer);
            }
        }
        static void CheckReference(Texture2D actual,Texture2D expected,string slot)
        {Require(actual!=null&&ReferenceEquals(actual,expected),slot+" did not retain the exact imported texture reference; refusing silent fallback.");}
        static void Require(bool condition,string message)
        {if(!condition)throw new BuildFailedException("[AppIconSetup] "+message);}
    }
}
