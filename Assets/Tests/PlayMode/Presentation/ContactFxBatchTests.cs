// Candidate native PlayMode tests. Copy only after Root's explicit runtime handoff.
using System.Collections;
using DefenseDuell.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class ContactFxBatchTests
    {
        [UnityTest]
        public IEnumerator StackedUnitsUseOneShadowWithoutPerEntityObjectsAndReducedMotionDropsFx()
        {
            var root=new GameObject("Contact FX native fixture");var cameraObject=new GameObject("Contact FX native camera",typeof(Camera));
            var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=8;camera.aspect=1.5f;
            camera.transform.position=new Vector3(0,12,-12);camera.transform.LookAt(Vector3.zero);camera.nearClipPlane=.1f;camera.farClipPlane=100;
            var shader=Shader.Find("DefenseDuell/GraphicsContactFx");Assert.That(shader,Is.Not.Null,"Root must import the candidate shader first");
            var batch=new ContactFxBatch(root.transform,0,shader);
            try
            {
                Assert.That(batch.BeginShadows(0,camera),Is.True);
                for(int i=0;i<100000;i++)batch.OfferShadow(i,new Vector3(0,ContactFxBatch.RouteGroundPlane,0),new Vector2(.4f,.3f),.16f);
                batch.EndShadows();Assert.That(batch.ShadowCount,Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<MeshFilter>(true).Length,Is.EqualTo(2));
                Assert.That(batch.BeginShadows(.001,camera),Is.False,"Shadows cannot rebuild every frame without a rate limit");
                Assert.That(batch.Trigger(ContactFxKind.Build,new Vector3(0,ContactFxBatch.OpenGroundPlane,0),Color.yellow,0),Is.True);
                Assert.That(batch.UpdateEffects(0,camera),Is.True);
                Assert.That(batch.ActiveEffectCount,Is.EqualTo(1));
                batch.SetOptions(true,false);Assert.That(batch.ActiveEffectCount,Is.Zero);
                Assert.That(batch.Trigger(ContactFxKind.Upgrade,Vector3.zero,Color.yellow,.01),Is.False);
                batch.Clear();foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))Assert.That(renderer.enabled,Is.False);
            }
            finally{batch.Dispose();Object.Destroy(cameraObject);Object.Destroy(root);}
            yield return null;yield return null;
        }

        [UnityTest]
        public IEnumerator TranslatedBoardRespectsCameraAndDisposesItsOnlyOwnedMeshesAndMaterial()
        {
            var root=new GameObject("Translated contact FX fixture");root.transform.position=new Vector3(80,0,0);
            var cameraObject=new GameObject("Translated contact FX camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.orthographicSize=8;camera.aspect=1.5f;camera.farClipPlane=100;
            camera.transform.position=root.transform.position+new Vector3(0,12,-12);camera.transform.LookAt(root.transform.position);
            var shader=Shader.Find("DefenseDuell/GraphicsContactFx");Assert.That(shader,Is.Not.Null);var batch=new ContactFxBatch(root.transform,0,shader);
            Mesh[] owned=null;Material material=null;
            try
            {
                batch.BeginShadows(0,camera);batch.OfferShadow(1,new Vector3(0,.184f,0),new Vector2(.62f,.5f),.18f,1);
                batch.OfferShadow(2,new Vector3(1000,.184f,1000),new Vector2(.62f,.5f),.18f,1);batch.EndShadows();
                Assert.That(batch.ShadowCount,Is.EqualTo(1),"Board-local centers must be projected through the translated world transform");
                var filters=root.GetComponentsInChildren<MeshFilter>(true);owned=new[]{filters[0].sharedMesh,filters[1].sharedMesh};
                material=root.GetComponentsInChildren<MeshRenderer>(true)[0].sharedMaterial;
                Assert.That(root.GetComponentsInChildren<MeshRenderer>(true)[1].sharedMaterial,Is.SameAs(material));
                batch.Dispose();batch.Dispose(); // Idempotent; owner may destroy after a failed initialization.
            }
            finally{batch.Dispose();Object.Destroy(cameraObject);Object.Destroy(root);}
            yield return null;yield return null;
            foreach(var mesh in owned)Assert.That(mesh==null,Is.True);Assert.That(material==null,Is.True);
        }
    }
}
