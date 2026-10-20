using UnityEngine;

// One pendulum degree of freedom; the existing player controller retains collision authority.
[ExecuteAlways,DisallowMultipleComponent,AddComponentMenu("Recoil/Swinging Chain")]
public sealed class SwingingChain : MonoBehaviour
{
    [Min(1)] public float length=3.8f;
    [Min(.1f)] public float climbSpeed=2.4f, grabRadius=.22f;
    [Min(0)] public float gravity=9.81f, damping=.18f, inputAcceleration=7, shotImpulse=4;
    [Range(10,89)] public float maximumAngle=80;
    [Range(-45,45)] public float initialAngle=5;
    [Min(.08f)] public float linkSpacing=.18f;
    public Color chainColor=new Color(.43f,.47f,.48f,1);
    public LineRenderer visual;
    RecoilPrototype world;
    RecoilPlayerController rider;
    float angle,angularVelocity,grip;
    public bool Occupied {get{return rider;}}
    public Vector2 Anchor {get{return transform.position;}}
    Vector2 Direction {get{return new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));}}
    public Vector2 RiderPosition {get{return Anchor+Direction*grip;}}
    public Vector2 CameraPosition {get{return Anchor+Vector2.down*grip;}}
    public Vector2 RiderVelocity {get{return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*grip*angularVelocity;}}
    void OnEnable()
    {
        world=GetComponentInParent<RecoilPrototype>();angle=initialAngle*Mathf.Deg2Rad;angularVelocity=0;rider=null;
        if(!visual)visual=GetComponent<LineRenderer>();
        if(!visual)visual=gameObject.AddComponent<LineRenderer>();
        visual.useWorldSpace=true;visual.loop=false;visual.startWidth=visual.endWidth=.032f;visual.sortingOrder=12;
        if(!visual.sharedMaterial && world)visual.sharedMaterial=world.SprayMaterial;
        visual.enabled=true;Draw();
    }
    public bool Contains(Vector2 p,float height)
    {
        if(!isActiveAndEnabled || Occupied)return false;
        for(int n=-1;n<=1;n++)
        {
            Vector2 sample=p+Vector2.up*(height*.4f*n);
            var nearest=Anchor+Direction*Mathf.Clamp(Vector2.Dot(sample-Anchor,Direction),.9f,length);
            if(Vector2.Distance(sample,nearest)<=grabRadius+.32f)return true;
        }
        return false;
    }
    public void Attach(RecoilPlayerController player,Vector2 position)
    {rider=player;grip=Mathf.Clamp(Vector2.Dot(position-Anchor,Direction),.95f,length);}
    public void Release(){rider=null;}
    public void Drive(float horizontal,float vertical,float dt,float speedMultiplier)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(dt*120));float h=dt/steps;
        for(int n=0;n<steps;n++)
        {
            float oldGrip=grip;grip=Mathf.Clamp(grip-vertical*climbSpeed*speedMultiplier*h,.95f,length);
            float radialSpeed=(grip-oldGrip)/Mathf.Max(.00001f,h);
            Integrate(h,grip,horizontal*inputAcceleration*speedMultiplier,radialSpeed);
        }
    }
    void Integrate(float dt,float radius,float acceleration,float radialSpeed=0)
    {
        angularVelocity+=(-gravity/radius*Mathf.Sin(angle)+acceleration/radius*Mathf.Cos(angle)
            -(damping+2*radialSpeed/radius)*angularVelocity)*dt;
        angle+=angularVelocity*dt;
        float limit=maximumAngle*Mathf.Deg2Rad;
        if(Mathf.Abs(angle)>limit){angle=Mathf.Clamp(angle,-limit,limit);angularVelocity*=-.15f;}
    }
    public void ApplyShot(Vector2 aim)
    {angularVelocity+=Vector2.Dot(-aim*shotImpulse,new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)))/Mathf.Max(.95f,grip);}
    public void StopAt(Vector2 position)
    {angle=Mathf.Clamp(Mathf.Atan2(position.x-Anchor.x,Anchor.y-position.y),-maximumAngle*Mathf.Deg2Rad,maximumAngle*Mathf.Deg2Rad);angularVelocity=0;grip=Mathf.Clamp(Vector2.Distance(position,Anchor),.95f,length);}
    void Update()
    {
        if(Application.isPlaying && world && world.Playing && !Occupied)
        {float dt=world.FrameDelta;int steps=Mathf.Max(1,Mathf.CeilToInt(dt*120));for(int n=0;n<steps;n++)Integrate(dt/steps,length,0);}
    }
    void LateUpdate(){if(!Application.isPlaying)angle=initialAngle*Mathf.Deg2Rad;Draw();}
    void Draw()
    {
        if(!visual)return;
        int links=Mathf.Clamp(Mathf.CeilToInt(length/Mathf.Max(.08f,linkSpacing)),2,128),points=13;
        visual.positionCount=links*points;visual.startColor=visual.endColor=chainColor;
        Vector2 down=Direction,tangent=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));float spacing=length/links;
        for(int i=0;i<links;i++)for(int j=0;j<points;j++)
        {float a=j*Mathf.PI*2/(points-1);Vector2 p=Anchor+down*((i+.5f)*spacing+Mathf.Cos(a)*spacing*.48f)+tangent*(Mathf.Sin(a)*(i%2==0?.075f:.04f));visual.SetPosition(i*points+j,new Vector3(p.x,p.y,transform.position.z));}
    }
    void OnDisable(){rider=null;if(visual)visual.enabled=false;}
    void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(Anchor,.12f);Gizmos.DrawLine(Anchor,Anchor+Direction*length);}
}
