using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DefenseDuell.Editor
{
    /// <summary>Imports our original, colored mesh data; the field and portrait baker share these assets.</summary>
    public static class UnlockModelImporter
    {
        public const string Destination = "Assets/Art/UnlockModels/Resources/DefenseUnlockModels";
        [Serializable] sealed class ModelData { public string name; public float[] headPivot, muzzle; public PartData[] parts; }
        [Serializable] sealed class PartData { public string name; public float[] vertices, normals, colors, uv; }
        static Vector3 V(float[] a, int i) => new Vector3(a[i], a[i+1], a[i+2]);
        public static void Import()
        {
            Directory.CreateDirectory(Destination);
            AssetDatabase.Refresh();
            foreach (string kind in new[] { "tesla", "ballista" }) for (int variant = 0; variant < 6; variant++)
            {
                string name = kind + "_v" + variant;
                var data = JsonUtility.FromJson<ModelData>(File.ReadAllText("Assets/Art/UnlockModels/Source/" + name + ".mesh.json"));
                if (data == null || data.name != name || data.parts == null || data.parts.Length != 2
                    || data.headPivot == null || data.headPivot.Length != 3 || V(data.headPivot,0) != new Vector3(0,.42f,0))
                    throw new InvalidDataException("Invalid unlock model " + name);
                for (int partIndex = 0; partIndex < 2; partIndex++)
                {
                    var part = data.parts[partIndex];
                    string expected = partIndex == 0 ? "Base" : "Head";
                    if (part == null || part.name != expected || part.vertices == null || part.vertices.Length == 0 || part.vertices.Length % 9 != 0)
                        throw new InvalidDataException("Invalid topology " + name);
                    int count = part.vertices.Length / 3;
                    if (count > 12000 || part.normals?.Length != count * 3 || part.colors?.Length != count * 4 || part.uv?.Length != count * 2)
                        throw new InvalidDataException("Invalid attributes " + name);
                    foreach (var values in new[] { part.vertices, part.normals, part.colors, part.uv })
                        foreach (float value in values) if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Non-finite mesh attribute");
                    var vertices = new Vector3[count]; var normals = new Vector3[count]; var colors = new Color[count];
                    var uv = new Vector2[count]; var indices = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        vertices[i] = V(part.vertices,i*3); normals[i] = V(part.normals,i*3);
                        colors[i] = new Color(part.colors[i*4],part.colors[i*4+1],part.colors[i*4+2],part.colors[i*4+3]);
                        uv[i] = new Vector2(part.uv[i*2],part.uv[i*2+1]); indices[i] = i;
                    }
                    var mesh = new Mesh { name = name + "_" + expected, vertices = vertices, normals = normals, colors = colors, uv = uv, triangles = indices };
                    mesh.RecalculateBounds();
                    string path = Destination + "/" + mesh.name + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing == null) AssetDatabase.CreateAsset(mesh,path);
                    else { EditorUtility.CopySerialized(mesh,existing); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(mesh); }
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("DEFENSE_UNLOCK_MESHES_IMPORTED count=24");
        }
    }
}
