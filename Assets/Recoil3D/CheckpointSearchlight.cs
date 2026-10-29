using UnityEngine;
using UnityEngine.Events;

// One sweep drives both the visible lamp and detection, including actual 3D shelter.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class CheckpointSearchlight : MonoBehaviour
{
    public RecoilPrototype world;
    public Light searchlight;
    public Transform lampHead;
    public Vector3 leftTarget, rightTarget;
    [Min(2)] public float sweepPeriod = 30;
    [Range(0, 1)] public float previewPhase = .25f;
    [Min(0)] public float exposureDelay = .45f;
    public Collider[] shelters;
    public UnityEvent onPlayerDetected = new UnityEvent();
    public bool PlayerExposed { get; private set; }
    float exposure, nextWarning;

    public void Pose(float phase)
    {
        if (!searchlight) return;
        float t = .5f - .5f * Mathf.Cos(phase * Mathf.PI * 2);
        Vector3 direction = Vector3.Lerp(leftTarget, rightTarget, t) - searchlight.transform.position;
        if (direction.sqrMagnitude > .001f) searchlight.transform.rotation = Quaternion.LookRotation(direction);
        if (lampHead) lampHead.rotation = searchlight.transform.rotation;
    }
    public bool Illuminates(Vector3 point, bool includeShelters = true)
    {
        if (!searchlight || !searchlight.enabled || !searchlight.gameObject.activeInHierarchy) return false;
        Vector3 delta = point - searchlight.transform.position;
        if (delta.sqrMagnitude > searchlight.range * searchlight.range || delta.sqrMagnitude < .001f) return false;
        if (Vector3.Angle(searchlight.transform.forward, delta) > searchlight.spotAngle * .5f) return false;
        if (includeShelters && shelters != null)
        {
            var ray = new Ray(searchlight.transform.position, delta.normalized);
            foreach (var c in shelters)
                if (c && c.enabled && c.gameObject.activeInHierarchy && c.Raycast(ray, out _, delta.magnitude - .03f)) return false;
        }
        return true;
    }
    void OnEnable() { if (!world) world = GetComponentInParent<RecoilPrototype>(); }
    void Update()
    {
        Pose(Application.isPlaying && world ? world.Clock / Mathf.Max(2, sweepPeriod) : previewPhase);
        if (!Application.isPlaying || !world || !world.Player || !world.Playing) return;
        PlayerExposed = Illuminates(world.Player.WorldPosition);
        exposure = PlayerExposed ? exposure + world.FrameDelta : 0;
        if (exposure < exposureDelay || world.Clock < nextWarning) return;
        nextWarning = world.Clock + 5;
        world.Say("The searchlight has found me. I need cover.");
        onPlayerDetected.Invoke();
    }
}
