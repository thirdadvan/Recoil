using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ManagerGlassesPickupSetup
{
    const string Folder = "Assets/Art/Props/ManagerGlasses";
    const string ScenePath = "Assets/Scenes/3D/RecoilPrototype.unity";
    [Serializable] class Geometry { public Vector3[] vertices; public int[] triangles; }

    public static string Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var data = JsonUtility.FromJson<Geometry>(File.ReadAllText("ArtSource/FactoryManager_Glasses_2026-11-07/GlassesGeometry.json"));
        var vertices = data.vertices.Select(v => new Vector3(v.x, v.z, -v.y)).ToArray();
        var bounds = new Bounds(vertices[0], Vector3.zero);
        foreach (var v in vertices) bounds.Encapsulate(v);
        // Preserve the manager's frame and temples; recenter and enlarge the loose prop for readability.
        var flat = data.triangles.Select(i => (vertices[i] - bounds.center) * (.38f / bounds.size.x)).ToArray();
        var mesh = new Mesh { name = "Manager square glasses - pickup" };
        mesh.vertices = flat;
        mesh.triangles = Enumerable.Range(0, flat.Length).ToArray();
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/ManagerGlasses.asset");
        if (existing) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
        else AssetDatabase.CreateAsset(mesh, Folder + "/ManagerGlasses.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Characters/FactoryManagerMeshy/Dark brown glasses.mat");
        if (!material) throw new InvalidOperationException("Manager glasses material missing");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var world = scene.GetRootGameObjects().Single(g => g.GetComponent<RecoilPrototype>());
        var old = world.GetComponentsInChildren<ManagerGlassesPickup>(true).SingleOrDefault();
        if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Manager glasses - warehouse roof pickup");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.SetParent(world.transform, false);
        root.transform.position = new Vector3(124.2f, 7.39f, 0);
        var model = new GameObject("Dark brown square glasses");
        model.transform.SetParent(root.transform, false);
        model.transform.localRotation = Quaternion.Euler(-78, -18, 8);
        model.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = model.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        model.transform.position += Vector3.up * (root.transform.position.y - renderer.bounds.min.y + .008f);
        var pickup = root.AddComponent<ManagerGlassesPickup>();
        pickup.itemId = pickup.resultId = ManagerGlassesPickup.InventoryId;
        pickup.displayName = "Manager's glasses";
        pickup.description = "Pick up the manager's dark brown square glasses.";
        pickup.interactionDuration = 0; pickup.interactionRange = 1.5f;
        pickup.resultMessage = "Manager's glasses added to inventory."; pickup.model = model;
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/ManagerGlassesPickup.prefab");
        RenderIcon(mesh, material);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Installed manager glasses at " + root.transform.position + ", instant E pickup and inventory icon.";
    }

    static void RenderIcon(Mesh mesh, Material material)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var rt = new RenderTexture(512, 384, 24); var previous = RenderTexture.active;
        var image = new Texture2D(512, 384, TextureFormat.RGBA32, false);
        try
        {
            var model = new GameObject("Icon model"); SceneManager.MoveGameObjectToScene(model, scene);
            model.AddComponent<MeshFilter>().sharedMesh = mesh; model.AddComponent<MeshRenderer>().sharedMaterial = material;
            var co = new GameObject("Icon camera"); SceneManager.MoveGameObjectToScene(co, scene);
            var camera = co.AddComponent<Camera>(); camera.scene = scene;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.orthographic = true; camera.orthographicSize = .18f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            co.transform.position = new Vector3(.32f, .3f, .75f); co.transform.LookAt(Vector3.zero);
            foreach (var direction in new[] { new Vector3(-2, 3, 4), new Vector3(3, 1, -2) })
            {
                var lo = new GameObject("Softbox"); SceneManager.MoveGameObjectToScene(lo, scene);
                var light = lo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f;
                lo.transform.rotation = Quaternion.LookRotation(-direction);
            }
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 512, 384), 0, 0); image.Apply();
            const string path = "Assets/Resources/RecoilInventory/ManagerGlasses.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, image.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
        }
        finally
        {
            RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
