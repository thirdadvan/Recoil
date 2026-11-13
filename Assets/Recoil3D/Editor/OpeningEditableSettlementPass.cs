using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Recover the four authored houses from the old material batches, without rebuilding the forecourt.
public static class OpeningEditableSettlementPass
{
    const string Folder = "Assets/Recoil3D/EditableSettlement";
    const string Marker = "Distant settlement - individually editable houses";
    static Transform Group(string name, Transform parent, Vector3 position)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = position; return t;
    }
    static void Box(Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.transform.SetParent(parent, false); g.transform.localPosition = position; g.transform.localScale = size;
        Object.DestroyImmediate(g.GetComponent<Collider>()); g.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    static string VertexKey(Vector3 p)
    {
        return Mathf.RoundToInt(p.x * 1000) + "," + Mathf.RoundToInt(p.y * 1000) + "," + Mathf.RoundToInt(p.z * 1000);
    }
    static string Key(Vector3 a, Vector3 b, Vector3 c)
    {
        var keys = new[] { VertexKey(a), VertexKey(b), VertexKey(c) };
        Array.Sort(keys, StringComparer.Ordinal); return string.Join(";", keys);
    }
    static Material Mat(string name)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Opening/Materials/" + name + ".mat");
        if (!m) throw new Exception("Missing material: " + name); return m;
    }
    static Mesh Save(string name, Mesh mesh)
    {
        mesh.name = name; AssetDatabase.CreateAsset(mesh, Folder + "/" + name + ".asset"); return mesh;
    }
    static Transform House(Transform parent, Transform original, int index, Material masonry, Material roof, Material window)
    {
        var house = Group("Distant house " + (index + 1).ToString("00") + " - individually editable", parent, Vector3.zero);
        house.position = original.position;
        Box(house, new Vector3(0, 1.55f, 0), new Vector3(5.6f, 3.1f, 5), masonry);
        Box(house, new Vector3(1.2f, 4.25f, .8f), new Vector3(.5f, 1.5f, .6f), masonry);
        for (int k = 0; k < 3; k++)
            Box(house, new Vector3(-1.65f + k * 1.65f, 1.9f, -2.52f), new Vector3(.57f, .76f, .03f), k == 1 && index == 2 ? window : roof);
        var r = Group("Roof temporary geometry", house, new Vector3(0, 3.1f, 0));
        var size = new Vector3(6.1f, 1.5f, 5.6f);
        var mesh = new Mesh();
        mesh.vertices = new[] {
            new Vector3(-size.x/2,0,-size.z/2), new Vector3(size.x/2,0,-size.z/2), new Vector3(0,size.y,-size.z/2),
            new Vector3(-size.x/2,0,size.z/2), new Vector3(size.x/2,0,size.z/2), new Vector3(0,size.y,size.z/2) };
        mesh.triangles = new[] {0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4,0,1,4,0,4,3};
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        r.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        r.gameObject.AddComponent<MeshRenderer>().sharedMaterial = roof;
        return house;
    }
    static void Bake(Transform house)
    {
        var filters = house.GetComponentsInChildren<MeshFilter>();
        var groups = filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
        var temporary = new List<Mesh>();
        foreach (var group in groups)
        {
            var m = new Mesh();
            m.CombineMeshes(group.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = house.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray(), true, true);
            temporary.Add(m);
        }
        var result = new Mesh();
        result.CombineMeshes(temporary.Select(m => new CombineInstance { mesh = m, transform = Matrix4x4.identity }).ToArray(), false, false);
        result.RecalculateBounds();
        foreach (var child in house.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        house.gameObject.AddComponent<MeshFilter>().sharedMesh = Save(house.name, result);
        var renderer = house.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = groups.Select(g => g.Key).ToArray(); renderer.shadowCastingMode = ShadowCastingMode.Off;
        foreach (var m in temporary) Object.DestroyImmediate(m);
    }
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");
        if (!scene.isLoaded) throw new Exception("Open the outdoor scene first");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var country = all.Single(t => t.name == "Countryside - low hills woods and mining settlement");
        if (country.Find(Marker)) return "Already applied; independent house positions preserved.";
        var road = all.Single(t => t.name == "Road - foreground to misty horizon");
        var collisionBefore = all.SelectMany(t => t.GetComponents<Collider2D>()).Select(c => c.GetInstanceID() + "|" + c.bounds).ToArray();
        var originals = Enumerable.Range(0, 4).Select(i => country.Find("Low roadside settlement house " + i)).ToArray();
        if (originals.Any(t => !t)) throw new Exception("Missing original house transforms");
        Directory.CreateDirectory("Backups/EditableSettlement");
        string backup = "Backups/EditableSettlement/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
        EditorSceneManager.SaveScene(scene, backup, true);
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var sources = country.GetComponentsInChildren<MeshFilter>(true);
        var root = Group(Marker, country, Vector3.zero);
        var houses = Enumerable.Range(0, 4).Select(i => House(root, originals[i], i, Mat("Near countryside atmosphere"), Mat("Dark tree line"), Mat("Distant isolated window"))).ToArray();
        var keys = new HashSet<string>(); int expected = 0;
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
        {
            var v = f.sharedMesh.vertices; var t = f.sharedMesh.triangles;
            for (int i = 0; i < t.Length; i += 3) { keys.Add(Key(f.transform.TransformPoint(v[t[i]]), f.transform.TransformPoint(v[t[i+1]]), f.transform.TransformPoint(v[t[i+2]]))); expected++; }
        }
        var plans = new List<KeyValuePair<MeshFilter, Mesh>>(); int removed = 0;
        foreach (var f in sources)
        {
            var v = f.sharedMesh.vertices; var t = f.sharedMesh.triangles; var keep = new List<int>(); int count = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                if (keys.Contains(Key(f.transform.TransformPoint(v[t[i]]), f.transform.TransformPoint(v[t[i+1]]), f.transform.TransformPoint(v[t[i+2]])))) count++;
                else keep.AddRange(new[] {t[i], t[i+1], t[i+2]});
            }
            removed += count; if (count == 0) continue;
            var mesh = Object.Instantiate(f.sharedMesh); mesh.SetTriangles(keep, 0); mesh.RecalculateBounds();
            plans.Add(new KeyValuePair<MeshFilter, Mesh>(f, mesh));
        }
        if (removed != expected)
        {
            Object.DestroyImmediate(root.gameObject);
            throw new Exception("House extraction mismatch; original geometry retained: " + removed + "/" + expected);
        }
        int n = 0;
        foreach (var plan in plans) plan.Key.sharedMesh = Save("Countryside without houses " + n++, plan.Value);
        foreach (var original in originals) Object.DestroyImmediate(original.gameObject);
        foreach (var house in houses) Bake(house);
        var delta = road.position.x - country.position.x;
        country.position += Vector3.right * delta;
        AssetDatabase.SaveAssets();
        var forest = ForecourtServiceYardPass.GroundExistingForest();
        var collisionAfter = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider2D>(true)).Select(c => c.GetInstanceID() + "|" + c.bounds).ToArray();
        if (!collisionBefore.SequenceEqual(collisionAfter)) throw new Exception("Unexpected collision change");
        if (houses.Any(h => h.childCount != 0 || h.GetComponentsInChildren<MeshRenderer>().Length != 1)) throw new Exception("House still contains separate parts");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        var report = "PASS: countryside aligned to road, delta X=" + delta + "; four single-mesh houses, no child parts; extracted " + removed + " triangles; collision unchanged. " + forest + "\nBackup: " + backup + "\n" + string.Join("\n", houses.Select(h => h.name + " position=" + h.position).ToArray());
        Directory.CreateDirectory("output/editable-settlement"); File.WriteAllText("output/editable-settlement/verification.txt", report);
        return report;
    }
}
