using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Tower Siren Trigger")]
public sealed class TowerSirenTrigger : MonoBehaviour
{
    public Transform highestPlatform, waveOrigin;
    public SpriteRenderer beacon;
    [Min(.1f)] public float horizontalRange=3.5f, blinkPeriod=.65f;
    public string progressId="station_tower_siren_triggered";
    RecoilPrototype world;
    Color restingColor;
    void OnEnable(){world=GetComponentInParent<RecoilPrototype>();if(beacon)restingColor=beacon.color;}
    void Update()
    {
        if(!world || !world.SoundWaves)return;
        var waves=world.SoundWaves;
        if(world.Playing && highestPlatform && waveOrigin && !world.HasItem(progressId))
        {
            var collider=highestPlatform.GetComponent<BoxCollider2D>();
            float top=collider?collider.bounds.max.y:highestPlatform.position.y;
            if(Mathf.Abs(world.Player.Position.x-highestPlatform.position.x)<=horizontalRange &&
                world.Player.Position.y-world.Player.BodyHeight*.5f>top &&
                waves.StartBroadcastAt(waveOrigin.position))world.RememberProgress(progressId);
        }
        if(beacon)beacon.color=world.HasItem(progressId) && waves.RedAlert
            ? Color.Lerp(new Color(.12f,.01f,.005f,1),new Color(1,.08f,.025f,1),Mathf.Pow(.5f+.5f*Mathf.Sin(world.Clock*Mathf.PI*2/Mathf.Max(.1f,blinkPeriod)),3))
            :restingColor;
    }
    void OnDisable(){if(beacon)beacon.color=restingColor;}
}
