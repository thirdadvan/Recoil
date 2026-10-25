using UnityEngine;

// Actor-owned timing for footsteps, landings and recoil use. Shared waves handle propagation.
[DefaultExecutionOrder(-250),DisallowMultipleComponent,AddComponentMenu("Recoil/Player Noise Emitter")]
public sealed class PlayerNoiseEmitter : MonoBehaviour
{
    public event System.Action<Vector3,float> FootstepEmitted;
    [Header("Player sounds")]
    [Min(.01f)] public float footstepRadius=4.16f,footstepInterval=.45f;
    [Header("Sound propagation and visuals")]
    [Min(.01f)] public float propagationSpeed=10;
    [Min(.001f)] public float rippleWidth=.025f;
    public Color rippleColor=new Color(.5411765f,.8980392f,1,.3f);
    [SerializeField] Material rippleMaterial;
    RecoilPrototype world;
    float nextFootstep,nextFlightNoise;
    bool flightNoiseStarted,wasUsingRecoil;
    LocalSoundWaveEmitter waves;
    public void Initialize(RecoilPrototype context){ResetNoise();world=context;waves=new LocalSoundWaveEmitter(world,transform);}
    public void ResetNoise()
    {if(waves!=null)waves.Clear();nextFootstep=nextFlightNoise=0;flightNoiseStarted=wasUsingRecoil=false;}
    public void UpdateFootsteps(Vector2 position,bool grounded,bool quietWalking,float horizontalIntent)
    {
        if(!isActiveAndEnabled || !world || !grounded || quietWalking || Mathf.Abs(horizontalIntent)<=.1f || world.Clock<=nextFootstep)return;
        Emit(position,footstepRadius);
        if(FootstepEmitted!=null)FootstepEmitted(new Vector3(position.x,position.y,transform.position.z),footstepRadius);
        nextFootstep=world.Clock+Mathf.Max(.01f,footstepInterval);
    }
    public void EmitLanding(Vector2 position)
    {
        if(!isActiveAndEnabled || !world)return;
        Emit(position,footstepRadius);
        // Landing replaces this frame's walking footstep, without suppressing later landings.
        nextFootstep=world.Clock+Mathf.Max(.01f,footstepInterval);
    }
    public void UpdateFlightNoise(Vector2 position,bool flying,bool usingRecoil)
    {
        if(!isActiveAndEnabled || !world)return;
        if(!flying){flightNoiseStarted=wasUsingRecoil=false;return;}
        bool beginsUse=usingRecoil && !wasUsingRecoil;
        if(beginsUse || (flightNoiseStarted && world.Clock>=nextFlightNoise))
        {
            Emit(position,footstepRadius);flightNoiseStarted=true;
            nextFlightNoise=world.Clock+Mathf.Max(.01f,footstepInterval);
        }
        wasUsingRecoil=usingRecoil;
    }
    public void Emit(Vector2 position,float radius)
    {
        if(!isActiveAndEnabled || !world || radius<=0)return;
        if(waves==null)waves=new LocalSoundWaveEmitter(world,transform);
        waves.Emit(position,radius,propagationSpeed,rippleWidth,rippleColor,rippleMaterial);
    }
    void Update(){if(world && world.Playing)Simulate(world.FrameDelta);}
    public void Simulate(float dt){if(waves!=null)waves.Simulate(dt);}
    void OnDisable(){ResetNoise();}
}
