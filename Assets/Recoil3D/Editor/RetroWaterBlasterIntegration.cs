using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RetroWaterBlasterIntegration
{
    public const string Root = "Assets/Art/Weapons/RetroWaterBlaster";
    public const string Assembly = Root + "/Prefabs/RetroWaterBlaster.prefab";
    public const float BodyLength = .5f;
    public const float TankDiameter = .084f;
    static readonly float[] TankLengths = { .22f, .36f, .5f };
    static readonly string[] TankNames = { "Small", "Medium", "Large" };

    [MenuItem("Recoil/Water Blaster/Build Meshy modular prefabs")]
    static void BuildMenu() { Debug.Log(BuildAssets()); }

    public static string BuildAssets()
    {
        using (var preview = new PreviewScope())
        {
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Meshes");
        AssetDatabase.Refresh();
        var bodySource = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/RetroWaterBlasterBody.glb");
        var tankSource = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/RetroWaterTank.glb");
        Need(bodySource && tankSource, "Meshy body and reservoir GLB assets must be imported first.");
        Directory.CreateDirectory(Root + "/Materials");
        string materialPath = Root + "/Materials/Opaque teal tank.mat";
        var tankMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!tankMaterial) { tankMaterial = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(tankMaterial, materialPath); }
        tankMaterial.color = new Color(.16f, .46f, .41f);
        tankMaterial.SetFloat("_Metallic", 0); tankMaterial.SetFloat("_Glossiness", .32f);
        EditorUtility.SetDirty(tankMaterial);
        for (int i = 0; i < TankNames.Length; i++)
        {
            // Meshy source is retained. Re-surface the reservoir as a true
            // lathed cylinder, removing the generated facets and baked facet lines.
            var mesh = CylindricalTank(TankLengths[i]);
            mesh.name = "Round Water Tank " + TankNames[i];
            string meshPath = Root + "/Meshes/Tank" + TankNames[i] + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing)
            {
                // Use the mesh API so Unity refreshes native/GPU buffers too.
                existing.Clear(); existing.indexFormat = mesh.indexFormat;
                existing.vertices = mesh.vertices; existing.uv = mesh.uv;
                existing.triangles = mesh.triangles; existing.normals = mesh.normals;
                existing.tangents = mesh.tangents; existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh); mesh = existing;
            }
            else AssetDatabase.CreateAsset(mesh, meshPath);
            var tank = new GameObject("Water Tank " + TankNames[i]);
            SceneManager.MoveGameObjectToScene(tank, preview.scene);
            tank.AddComponent<MeshFilter>().sharedMesh = mesh;
            tank.AddComponent<MeshRenderer>().sharedMaterial = tankMaterial;
            PrefabUtility.SaveAsPrefabAsset(tank, Root + "/Prefabs/Tank" + TankNames[i] + ".prefab");
            UnityEngine.Object.DestroyImmediate(tank);
        }
        var root = new GameObject("Retro Water Blaster");
        SceneManager.MoveGameObjectToScene(root, preview.scene);
        try
        {
            var assembly = root.AddComponent<RetroWaterBlaster>();
            var pivot = Child(root.transform, "Facing pivot", Vector3.zero);
            assembly.facingPivot = pivot;
            var body = (GameObject)PrefabUtility.InstantiatePrefab(bodySource, preview.scene);
            body.name = "Meshy gun body"; body.transform.SetParent(pivot, false);
            float scale = BodyLength / bodySource.GetComponentInChildren<MeshFilter>(true).sharedMesh.bounds.size.x;
            body.transform.localScale = Vector3.one * scale;
            assembly.mainGrip = Child(pivot, "MainGrip", new Vector3(-.19f, -.09f, -.045f));
            assembly.mainGrip.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.forward);
            assembly.supportGrip = Child(pivot, "SupportGrip", new Vector3(.075f, -.095f, .045f));
            assembly.supportGrip.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            assembly.muzzle = Child(pivot, "Muzzle", new Vector3(.25f, -.008f, 0));
            assembly.tankSocket = Child(pivot, "TankSocket", new Vector3(-.151f, .088f, 0));
            for (int i = 0; i < TankNames.Length; i++)
            {
                var tank = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Tank" + TankNames[i] + ".prefab"), preview.scene);
                tank.transform.SetParent(assembly.tankSocket, false);
                // Recess the short neck inside the smooth socket collar.
                tank.transform.localPosition = new Vector3(-.002f, 0, 0);
                assembly.tanks[i] = tank;
            }
            assembly.SetTankSize(RetroWaterBlaster.TankSize.Small);
            PrefabUtility.SaveAsPrefabAsset(root, Assembly);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        return "Built Meshy body assembly and independent 0.22 / 0.36 / 0.50 m reservoirs; small is default.";
        }
    }

    sealed class PreviewScope : IDisposable
    {
        public readonly Scene scene = EditorSceneManager.NewPreviewScene();
        public void Dispose() { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static Mesh CylindricalTank(float length)
    {
        const int sides = 96;
        float r = TankDiameter * .5f;
        var profile = new[] {
            new Vector2(0, 0), new Vector2(0, .015f), new Vector2(.012f, .015f),
            new Vector2(.016f, .0165f), new Vector2(.023f, .030f),
            new Vector2(.030f, .039f), new Vector2(.035f, r),
            new Vector2(.190f, r), new Vector2(.205f, r),
            new Vector2(.211f, .0408f), new Vector2(.216f, .035f),
            new Vector2(.219f, .022f), new Vector2(.220f, 0) };
        var vertices = new Vector3[profile.Length * (sides + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>();
        for (int i = 0; i < profile.Length; i++)
        {
            float x = profile[i].x;
            // Insert additional length only into the straight wall. Both ends
            // and their circular cross-sections are identical for every size.
            if (x >= .190f) x += length - .22f;
            else if (x > .035f) x = .035f + (x - .035f) * (length - .22f + .155f) / .155f;
            for (int j = 0; j <= sides; j++)
            {
                float angle = j * Mathf.PI * 2 / sides;
                int n = i * (sides + 1) + j;
                vertices[n] = new Vector3(x, Mathf.Cos(angle) * profile[i].y, Mathf.Sin(angle) * profile[i].y);
                uv[n] = new Vector2(x / length, (float)j / sides);
                if (i < profile.Length - 1 && j < sides)
                {
                    int next = n + sides + 1;
                    triangles.Add(n); triangles.Add(n + 1); triangles.Add(next);
                    triangles.Add(n + 1); triangles.Add(next + 1); triangles.Add(next);
                }
            }
        }
        var mesh = new Mesh { vertices = vertices, uv = uv, triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        var normals = mesh.normals;
        for (int i = 0; i < profile.Length; i++)
        {
            int first = i * (sides + 1), last = first + sides;
            var normal = (normals[first] + normals[last]).normalized;
            normals[first] = normals[last] = normal;
        }
        mesh.normals = normals; mesh.RecalculateBounds(); mesh.RecalculateTangents();
        return mesh;
    }

    static Transform Child(Transform parent, string name, Vector3 position)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = position; return t;
    }

    public static RetroWaterBlaster Install(Transform weapon)
    {
        Need(weapon, "Missing weapon transform.");
        var existing = weapon.GetComponentInChildren<RetroWaterBlaster>(true);
        if (existing) return existing;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Assembly);
        Need(asset, "Build modular prefabs first.");
        foreach (var v in weapon.GetComponentsInChildren<Recoil3DSpriteVisual>(true)) { v.suppressed = true; v.enabled = false; }
        foreach (var r in weapon.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        var legacy = weapon.Find("Dawn water gun visual");
        if (legacy) legacy.gameObject.SetActive(false);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, weapon.gameObject.scene);
        go.transform.SetParent(weapon, false);
        var size = weapon.lossyScale;
        go.transform.localScale = new Vector3(1 / Mathf.Max(.001f, Mathf.Abs(size.x)), 1 / Mathf.Max(.001f, Mathf.Abs(size.y)), 1 / Mathf.Max(.001f, Mathf.Abs(size.z)));
        var result = go.GetComponent<RetroWaterBlaster>();
        result.SetTankSize(RetroWaterBlaster.TankSize.Small);
        EditorUtility.SetDirty(weapon); return result;
    }

    [MenuItem("Recoil/Water Blaster/Replace in 3D game scenes")]
    static void ReplaceMenu() { Debug.Log(ReplaceScenes()); }

    public static string ReplaceScenes()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before replacing assets.");
        for (int n = 0; n < SceneManager.sceneCount; n++)
            Need(!SceneManager.GetSceneAt(n).isDirty, "Save your open scene edits before running this installer.");
        var active = SceneManager.GetActiveScene();
        var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/3D" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.Contains("/Tests/")).ToArray();
        string backupRoot = "Backups/RetroWaterBlaster/2026-11-07/Scenes/";
        int weapons = 0, pickups = 0, changed = 0;
        try
        {
            foreach (string path in paths)
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var drivers = roots.SelectMany(g => g.GetComponentsInChildren<DawnPlayerAnimation>(true)).ToArray();
                    var items = roots.SelectMany(g => g.GetComponentsInChildren<ContinuousInteractionItem>(true)).Where(t => t.name == "Water gun pickup").ToArray();
                    if (!drivers.Any(d => d.weapon && !d.weapon.GetComponentInChildren<RetroWaterBlaster>(true)) && !items.Any(t => !t.GetComponentInChildren<RetroWaterBlaster>(true))) continue;
                    string backup = backupRoot + path.Substring("Assets/Scenes/3D/".Length);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    if (!File.Exists(backup)) File.Copy(path, backup);
                    foreach (var d in drivers)
                    {
                        if (!d.weapon || d.weapon.GetComponentInChildren<RetroWaterBlaster>(true)) continue;
                        Install(d.weapon); weapons++;
                    }
                    foreach (var item in items)
                    {
                        if (item.GetComponentInChildren<RetroWaterBlaster>(true)) continue;
                        Install(item.transform); pickups++;
                    }
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); changed++;
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        AssetDatabase.SaveAssets();
        string result = "Updated " + weapons + " held weapons and " + pickups + " pickups in " + changed + " scenes. Backups: " + backupRoot;
        Directory.CreateDirectory("Captures/RetroWaterBlaster");
        File.WriteAllText("Captures/RetroWaterBlaster/Scene-Integration.txt", result);
        return result;
    }

    public static string VerifyAndRender()
    {
        using (var preview = new PreviewScope())
        {
            var points = new List<Vector3[]>();
            var report = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var tank = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Tank" + TankNames[i] + ".prefab");
                var mesh = tank.GetComponent<MeshFilter>().sharedMesh;
                Need(Mathf.Abs(mesh.bounds.size.x - TankLengths[i]) < .001f, "Incorrect reservoir length: " + TankNames[i]);
                Need(Mathf.Abs(mesh.bounds.size.y - TankDiameter) < .002f && Mathf.Abs(mesh.bounds.size.z - TankDiameter) < .002f, "Incorrect round tank diameter.");
                points.Add(mesh.vertices);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Assembly), preview.scene);
                go.transform.position = new Vector3(0, .37f * (1 - i), 0);
                var a = go.GetComponent<RetroWaterBlaster>();
                a.SetTankSize((RetroWaterBlaster.TankSize)i);
                Need(a.tanks.Count(t => t.activeSelf) == 1 && a.tanks[i].activeSelf, "Reservoir switch failed.");
                Need(Vector3.Distance(a.mainGrip.position, a.supportGrip.position) > .25f, "Hands are too close together.");
                var barrel = go.transform.Find("Facing pivot/Meshy gun body").GetComponentInChildren<Renderer>();
                Need(Mathf.Abs(barrel.bounds.size.x - BodyLength) < .001f, "Incorrect body length.");
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    Need(r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.isSupported, "Missing or unsupported material.");
                report.Add("PASS " + TankNames[i] + ": length " + mesh.bounds.size.x.ToString("F3") + " m; diameter " + mesh.bounds.size.y.ToString("F3") + " m; one active tank; common grip spacing and supported materials.");
            }
            int neckVertices = 0;
            for (int n = 0; n < points[0].Length; n++)
            {
                Need(Mathf.Abs(points[0][n].y - points[1][n].y) < .000001f && Mathf.Abs(points[0][n].y - points[2][n].y) < .000001f && Mathf.Abs(points[0][n].z - points[1][n].z) < .000001f && Mathf.Abs(points[0][n].z - points[2][n].z) < .000001f, "Tank thickness changed between sizes.");
                if (points[0][n].x <= .017f)
                {
                    Need((points[0][n] - points[1][n]).sqrMagnitude < 1e-10f && (points[0][n] - points[2][n]).sqrMagnitude < 1e-10f, "Reservoir connector changed size.");
                    neckVertices++;
                }
            }
            Need(neckVertices > 10, "Connector geometry was not found.");
            report.Add("PASS: all three tanks have identical Y/Z cross-sections and " + neckVertices + " identical connector vertices. Largest tank equals body length.");
            var cg = new GameObject("Preview camera"); SceneManager.MoveGameObjectToScene(cg, preview.scene);
            var camera = cg.AddComponent<Camera>(); camera.scene = preview.scene;
            camera.transform.position = new Vector3(.65f, 0, -3); camera.transform.LookAt(new Vector3(.04f, 0, 0)); camera.orthographic = true; camera.orthographicSize = .60f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f, .18f, .20f);
            var lg = new GameObject("Preview key"); SceneManager.MoveGameObjectToScene(lg, preview.scene);
            var light = lg.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = .8f; light.transform.rotation = Quaternion.Euler(25, -25, 0);
            var rt = new RenderTexture(1300, 1300, 24); var previous = RenderTexture.active;
            var texture = new Texture2D(1300, 1300, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1300, 1300), 0, 0); texture.Apply();
                Directory.CreateDirectory("Captures/RetroWaterBlaster");
                File.WriteAllBytes("Captures/RetroWaterBlaster/03-Cylindrical-Tanks.png", texture.EncodeToPNG());
                File.WriteAllLines("Captures/RetroWaterBlaster/Asset-Verification.txt", report);
            }
            finally { RenderTexture.active = previous; camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture); }
            return string.Join("\n", report);
        }
    }

    static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
