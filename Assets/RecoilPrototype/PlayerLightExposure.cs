using UnityEngine;

// Gameplay illumination, sampled from environmental lights only. No GPU readback.
[DisallowMultipleComponent,RequireComponent(typeof(RecoilPlayerController))]
[AddComponentMenu("Recoil/Player Light Exposure")]
public sealed class PlayerLightExposure : MonoBehaviour
{
    public const float MinimumCoefficient=.4f;
    [Min(.001f),Tooltip("环境亮度达到此值时视为完全照亮。与视觉光强独立标定。")]
    public float fullyLitBrightness=.45f;
    [SerializeField,Range(MinimumCoefficient,1)] float illumination=MinimumCoefficient;
    RecoilPrototype world;
    RecoilPlayerController player;
    float sampledClock=float.NegativeInfinity,sampledHeight;
    Vector2 sampledPosition;
    bool sampledAlarm,sampledIndoor;int sampledLampRevision=-1,sampledEnvironmentRevision=-1;
    public float Coefficient
    {
        get
        {
            if(!world)world=GetComponentInParent<RecoilPrototype>();
            if(!player)player=GetComponent<RecoilPlayerController>();
            if(!world || !player)return MinimumCoefficient;
            Vector2 position=player.Position;
            float half=player.Crouched?.35f:.65f;
            bool alarm=world.SoundWaves && world.SoundWaves.RedAlert;
            if(sampledClock==world.Clock && sampledPosition==position && sampledHeight==half && sampledAlarm==alarm && sampledIndoor==world.IndoorDarknessActive && sampledLampRevision==RecoilStreetLamp.Revision && sampledEnvironmentRevision==RecoilLight2D.Revision)return illumination;
            bool inCone;
            float brightness=world.EnvironmentBrightness(position,out inCone);
            if(inCone)illumination=1;
            else
            {
                brightness+=world.EnvironmentBrightness(position-Vector2.up*half);
                brightness+=world.EnvironmentBrightness(position+Vector2.up*half);
                illumination=Mathf.Lerp(MinimumCoefficient,1,Mathf.Clamp01(brightness/(3*Mathf.Max(.001f,fullyLitBrightness))));
            }
            sampledIndoor=world.IndoorDarknessActive;sampledLampRevision=RecoilStreetLamp.Revision;sampledEnvironmentRevision=RecoilLight2D.Revision;sampledClock=world.Clock;sampledPosition=position;sampledHeight=half;sampledAlarm=alarm;
            return illumination;
        }
    }
    void OnValidate(){fullyLitBrightness=Mathf.Max(.001f,fullyLitBrightness);sampledClock=float.NegativeInfinity;}
}
