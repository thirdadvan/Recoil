using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Visual cues only: never adds a light or changes the source prop's materials.
[DisallowMultipleComponent]
public sealed class PickupVisibility : MonoBehaviour
{
    public float discoveryRange = 8f, outlineRange = 3f, glintInterval = 3.2f;
    ContinuousInteractionItem item;
    RecoilPrototype world;
    Renderer source;
    GameObject outline, glint;
    Material outlineMaterial, glintMaterial;
    Mesh outlineMesh;
    bool promptVisible;
    Vector3 anchor;
    GUIStyle promptStyle;

    void Start()
    {
        item = GetComponent<ContinuousInteractionItem>();
        world = GetComponentInParent<RecoilPrototype>();
        var glasses = GetComponent<ManagerGlassesPickup>();
        source = glasses && glasses.model ? glasses.model.GetComponentInChildren<Renderer>() : GetComponentInChildren<MeshRenderer>();
        var crowbar = GetComponent<CrowbarPickup>();
        if (crowbar && crowbar.model) foreach (var candidate in crowbar.model.GetComponentsInChildren<MeshRenderer>()) if (candidate.enabled) { source = candidate; break; }
        if (!source || !source.enabled) foreach (var candidate in GetComponentsInChildren<MeshRenderer>()) if (candidate.enabled) { source = candidate; break; }
        var filter = source ? source.GetComponent<MeshFilter>() : null;
        var outlineShader = Resources.Load<Shader>("PickupVisibility/Outline");
        var glintShader = Resources.Load<Shader>("PickupVisibility/Glint");
        if (!item || !world || !filter || !filter.sharedMesh || !outlineShader || !glintShader) { enabled = false; return; }

        // The source mesh has separate triangle vertices. Average coincident normals so
        // the outline expands continuously instead of splitting along every triangle.
        outlineMesh = Instantiate(filter.sharedMesh);
        var vertices = outlineMesh.vertices; var normals = outlineMesh.normals;
        var sums = new Dictionary<Vector3, Vector3>();
        for (int i = 0; i < vertices.Length; i++)
        { Vector3 sum; sums.TryGetValue(vertices[i], out sum); sums[vertices[i]] = sum + normals[i]; }
        for (int i = 0; i < vertices.Length; i++) normals[i] = sums[vertices[i]].normalized;
        outlineMesh.normals = normals;
        outlineMaterial = new Material(outlineShader);
        outline = new GameObject("Pickup proximity outline");
        outline.layer = source.gameObject.layer;
        outline.transform.SetParent(source.transform, false);
        outline.AddComponent<MeshFilter>().sharedMesh = outlineMesh;
        var renderer = outline.AddComponent<MeshRenderer>(); renderer.sharedMaterial = outlineMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;

        glintMaterial = new Material(glintShader);
        glint = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glint.name = "Pickup occasional reflection"; glint.layer = source.gameObject.layer;
        glint.transform.SetParent(transform, false);
        var collider = glint.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        renderer = glint.GetComponent<MeshRenderer>(); renderer.sharedMaterial = glintMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        Hide();
    }

    void LateUpdate()
    {
        Hide();
        if (!world || !world.Playing || !world.PlayerControlsAvailable || !source || !source.enabled ||
            !source.gameObject.activeInHierarchy || item.Completed || world.HasItem(item.resultId)) return;
        var camera = world.ViewCamera;
        if (!camera) return;
        float distance = Vector2.Distance(world.Player.Position, transform.position);
        if (distance > discoveryRange || !world.Clear(world.Player.Position, transform.position)) return;
        anchor = source.bounds.center;
        var screen = camera.WorldToViewportPoint(anchor);
        if (screen.z <= 0 || screen.x < 0 || screen.x > 1 || screen.y < 0 || screen.y > 1) return;

        // Mesh cues use depth testing. Collider checks also suppress floating labels behind walls.
        var ray = anchor - camera.transform.position;
        foreach (var hit in Physics.RaycastAll(camera.transform.position, ray.normalized, ray.magnitude - .03f, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(world.Player.transform)) return;

        outline.SetActive(distance <= outlineRange);
        promptVisible = world.NearbyInteraction == item && item.InRange(world);
        float phase = Mathf.Repeat(world.Clock, Mathf.Max(1, glintInterval));
        if (phase < .55f)
        {
            float pulse = Mathf.Sin(phase / .55f * Mathf.PI);
            glintMaterial.SetFloat("_Strength", pulse * pulse * .85f);
            glint.transform.position = anchor + Vector3.up * source.bounds.extents.y - camera.transform.forward * .015f;
            glint.transform.rotation = camera.transform.rotation;
            float worldPerPixel = camera.orthographic ? 2 * camera.orthographicSize / camera.pixelHeight :
                2 * screen.z * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) / camera.pixelHeight;
            glint.transform.localScale = Vector3.one * Mathf.Clamp(worldPerPixel * 22, .025f, .24f);
            glint.SetActive(true);
        }
    }

    void OnGUI()
    {
        if (!promptVisible || !world || !world.Playing || Event.current.type != EventType.Repaint) return;
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            promptStyle.normal.textColor = new Color(1, .91f, .7f);
        }
        var point = world.ViewCamera.WorldToScreenPoint(anchor);
        string text = (item.interactionDuration > 0 ? "HOLD E / " : "E / PICK UP / ") + item.displayName;
        float width = Mathf.Min(Screen.width - 16, promptStyle.CalcSize(new GUIContent(text)).x + 24);
        var rect = new Rect(Mathf.Clamp(point.x - width * .5f, 8, Screen.width - width - 8),
            Mathf.Clamp(Screen.height - point.y - 54, 8, Screen.height - 40), width, 32);
        var matrix = GUI.matrix; var color = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        GUI.color = new Color(.025f, .035f, .035f, .9f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white; GUI.Label(rect, text, promptStyle);
        GUI.color = color; GUI.matrix = matrix;
    }

    void Hide() { promptVisible = false; if (outline) outline.SetActive(false); if (glint) glint.SetActive(false); }
    void OnDisable() { Hide(); }
    void OnDestroy()
    {
        if (outline) Destroy(outline); if (glint) Destroy(glint);
        if (outlineMaterial) Destroy(outlineMaterial); if (glintMaterial) Destroy(glintMaterial);
        if (outlineMesh) Destroy(outlineMesh);
    }
}
