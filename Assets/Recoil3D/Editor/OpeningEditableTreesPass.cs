using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class OpeningEditableTreesPass
{
    const string Folder = "Assets/Recoil3D/EditableOpeningTrees";
    static string Point(Vector3 p) { return Mathf.RoundToInt(p.x * 1000) + "," + Mathf.RoundToInt(p.y * 1000) + "," + Mathf.RoundToInt(p.z * 1000); }
    static string Key(Vector3 a, Vector3 b, Vector3 c)
    {
        var keys = new[] {Point(a), Point(b), Point(c)}; Array.Sort(keys, StringComparer.Ordinal); return string.Join(";", keys);
    }
    static void Register(Dictionary<string, Transform> owners, List<KeyValuePair<Transform, Vector3[]>> precise, Transform part, Mesh mesh, Transform tree)
    {
        var v = mesh.vertices; var t = mesh.triangles;
        for (int i = 0; i < t.Length; i += 3)
        {
            var points = new[] {part.TransformPoint(v[t[i]]), part.TransformPoint(v[t[i+1]]), part.TransformPoint(v[t[i+2]])};
            owners.Add(Key(points[0], points[1], points[2]), tree);
            precise.Add(new KeyValuePair<Transform, Vector3[]>(tree, points));
        }
    }
    // Compact each extracted section so bounds and pivots belong to this tree only.
    static Mesh Section(MeshFilter source, List<int> indices, Transform tree)
    {
        var old = source.sharedMesh; var v = old.vertices; var normals = old.normals;
        var uv = old.uv; var uv2 = old.uv2; var colors = old.colors; var tangents = old.tangents;
        var used = indices.Distinct().ToArray(); var map = new Dictionary<int, int>();
        for (int i = 0; i < used.Length; i++) map.Add(used[i], i);
        var matrix = tree.worldToLocalMatrix * source.transform.localToWorldMatrix;
        var normalMatrix = matrix.inverse.transpose;
        var m = new Mesh(); m.vertices = used.Select(i => matrix.MultiplyPoint3x4(v[i])).ToArray();
        if (normals.Length == v.Length) m.normals = used.Select(i => normalMatrix.MultiplyVector(normals[i]).normalized).ToArray();
        if (uv.Length == v.Length) m.uv = used.Select(i => uv[i]).ToArray();
        if (uv2.Length == v.Length) m.uv2 = used.Select(i => uv2[i]).ToArray();
        if (colors.Length == v.Length) m.colors = used.Select(i => colors[i]).ToArray();
        if (tangents.Length == v.Length) m.tangents = used.Select(i => { var t = tangents[i]; var d = matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized; return new Vector4(d.x,d.y,d.z,t.w); }).ToArray();
        m.triangles = indices.Select(i => map[i]).ToArray(); m.RecalculateBounds(); return m;
    }
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        var selected = Selection.activeTransform;
        if (!selected || selected.name != "Static detail - Nearby spruce and birch leaves") throw new Exception("Select the requested nearby leaf group");
        var root = selected.parent;
        if (root.name != "Left forest - same spruce family as factory right") throw new Exception("Unexpected forest group");
        var trees = root.Cast<Transform>().Where(t => t.name.StartsWith("Spruce matching right-hand forest ")).ToArray();
        var sources = root.GetComponentsInChildren<MeshFilter>(true);
        if (trees.Length != 38 || sources.Length != 3) throw new Exception("Unexpected forest layout; no changes made");
        var scene = root.gameObject.scene;
        Directory.CreateDirectory("Backups/EditableOpeningTrees");
        var backup = "Backups/EditableOpeningTrees/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
        EditorSceneManager.SaveScene(scene, backup, true);
        var cylinderObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var cylinder = cylinderObject.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(cylinderObject);
        var cone = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Recoil3D/Residential/Meshes/Spruce cone.asset");
        var owners = new Dictionary<string, Transform>();
        var precise = new List<KeyValuePair<Transform, Vector3[]>>();
        foreach (var tree in trees)
            foreach (Transform part in tree) Register(owners, precise, part, part.name == "Trunk" ? cylinder : cone, tree);
        var sections = trees.ToDictionary(t => t, t => new List<KeyValuePair<MeshFilter,List<int>>>());
        int triangleCount = 0;
        foreach (var source in sources)
        {
            var mesh = source.sharedMesh; var v = mesh.vertices; var indices = mesh.triangles;
            var perTree = new Dictionary<Transform,List<int>>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                Transform owner;
                if (!owners.TryGetValue(Key(source.transform.TransformPoint(v[indices[i]]), source.transform.TransformPoint(v[indices[i+1]]), source.transform.TransformPoint(v[indices[i+2]])), out owner))
                {
                    // Matrix roundoff can cross a millimetre rounding boundary. Require all three real vertices to match within 0.1 mm.
                    var points = new[] { source.transform.TransformPoint(v[indices[i]]), source.transform.TransformPoint(v[indices[i+1]]), source.transform.TransformPoint(v[indices[i+2]]) };
                    var matches = precise.Where(p => points.All(a => p.Value.Any(b => (a-b).sqrMagnitude < .00000001f))).Select(p => p.Key).Distinct().ToArray();
                    if (matches.Length != 1) throw new Exception("Unmatched geometry; original forest retained: " + source.name + " triangle=" + i/3);
                    owner = matches[0];
                }
                List<int> list; if (!perTree.TryGetValue(owner, out list)) perTree.Add(owner, list = new List<int>());
                list.AddRange(new[] {indices[i], indices[i+1], indices[i+2]}); triangleCount++;
            }
            foreach (var entry in perTree) sections[entry.Key].Add(new KeyValuePair<MeshFilter,List<int>>(source, entry.Value));
        }
        if (triangleCount != owners.Count || sections.Values.Any(parts => parts.Count != 2)) throw new Exception("Tree extraction mismatch; original forest retained");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var positions = trees.ToDictionary(t => t, t => t.position);
        foreach (var tree in trees)
        {
            var parts = sections[tree]; var meshes = parts.Select(p => Section(p.Key, p.Value, tree)).ToArray();
            var mesh = new Mesh(); mesh.name = "Complete spruce " + tree.name.Split(' ').Last();
            mesh.CombineMeshes(meshes.Select(m => new CombineInstance {mesh=m,transform=Matrix4x4.identity}).ToArray(), false, false);
            var path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + mesh.name + ".asset"); AssetDatabase.CreateAsset(mesh, path);
            var foliage = parts.Single(p => p.Key.name != "Static detail - Weathered timber").Key;
            foreach (var child in tree.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            tree.SetParent(foliage.transform, true);
            tree.name = "Tree - spruce " + tree.name.Split(' ').Last();
            tree.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = tree.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = parts.Select(p => p.Key.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
            renderer.shadowCastingMode = foliage.GetComponent<MeshRenderer>().shadowCastingMode;
            renderer.receiveShadows = foliage.GetComponent<MeshRenderer>().receiveShadows;
            foreach (var m in meshes) Object.DestroyImmediate(m);
        }
        foreach (var source in sources)
        {
            var go = source.gameObject; Object.DestroyImmediate(go.GetComponent<MeshRenderer>()); Object.DestroyImmediate(source);
            if (go.transform.childCount == 0) Object.DestroyImmediate(go);
        }
        if (trees.Any(t => t.position != positions[t] || t.childCount != 0)) throw new Exception("Unexpected tree transform change");
        var oldPosition = trees[0].position; var center = trees[0].GetComponent<Renderer>().bounds.center;
        trees[0].position += Vector3.right;
        bool moves = (trees[0].GetComponent<Renderer>().bounds.center - center - Vector3.right).sqrMagnitude < .000001f;
        trees[0].position = oldPosition;
        if (!moves || trees.Skip(1).Any(t => t.position != positions[t])) throw new Exception("Independent movement check failed");
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = selected.gameObject;
        var report = "PASS " + trees.Length + " whole-tree objects; " + selected.childCount + " under requested nearby foliage group; exact " + triangleCount + " triangles retained; positions, materials, tree shapes retained; independent movement verified. Backup: " + backup;
        Directory.CreateDirectory("output/editable-opening-trees"); File.WriteAllText("output/editable-opening-trees/verification.txt", report); return report;
    }
}
