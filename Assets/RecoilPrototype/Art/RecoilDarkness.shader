Shader "Recoil/Soft 2D Darkness"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white" {}
        _OutdoorAmbient("Outdoor ambient",Range(0,1))=.28
        _UnlitAmbientScale("Unlit ambient scale",Range(0,1))=.2
        _AlarmRed("Red alarm lighting",Range(0,1))=0
        _AlarmAmbient("Flashing red indoor ambient",Range(0,1))=0
        _FactoryEnabled("Indoor darkness enabled",Float)=1
        _FactoryMin("Factory minimum",Vector)=(16,0,0,0)
        _FactoryMax("Factory maximum",Vector)=(39,6,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend DstColor Zero
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _OutdoorAmbient,_UnlitAmbientScale,_AlarmRed,_AlarmAmbient,_FactoryEnabled;
            float4 _FactoryMin,_FactoryMax,_PointLights[32],_ConeLights[16],_ConeDirections[16],_IndustrialLightShapes[32],_IndustrialLightDirections[32];
            int _ConeCount,_PointCount;
            struct appdata {float4 vertex:POSITION;};
            struct v2f {float4 vertex:SV_POSITION;float2 world:TEXCOORD0;};
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                bool indoors=_FactoryEnabled>.5 && all(i.world>=_FactoryMin.xy) && all(i.world<=_FactoryMax.xy);
                float brightness=indoors?0:_OutdoorAmbient*_UnlitAmbientScale;
                for(int n=0;n<_PointCount;n++)
                {
                    float4 light=_PointLights[n];
                    float d=saturate(distance(i.world,light.xy)/max(light.z,.0001));
                    float amount=light.w*(1-smoothstep(0,1,d));
                    float4 shape=_IndustrialLightShapes[n];
                    if(shape.x>0)
                    {
                        float2 delta=i.world-light.xy,dir=_IndustrialLightDirections[n].xy;
                        float depth=dot(delta,dir),side=abs(delta.x*dir.y-delta.y*dir.x);
                        float main=0;
                        if(depth>=0 && depth<=shape.x)
                        {
                            float width=max(.05,shape.y*depth/max(.1,shape.x));
                            float edge=1-smoothstep(width*(1-shape.z),width,side);
                            float end=1-smoothstep(shape.x*.85,shape.x,depth);
                            main=light.w*edge*end*(1-.35*depth/max(.1,shape.x));
                        }
                        amount=max(main,amount*shape.w);
                    }
                    brightness=max(brightness,amount);
                }
                for(int c=0;c<_ConeCount;c++)
                {
                    float4 cone=_ConeLights[c];float2 dir=_ConeDirections[c].xy,delta=i.world-cone.xy;
                    float depth=dot(delta,dir),side=abs(delta.x*dir.y-delta.y*dir.x);
                    if(depth>=0 && depth<=cone.z && side<=cone.w*depth/max(cone.z,.1))brightness=1;
                }
                float3 lightColor=saturate(brightness)*lerp(float3(1,1,1),float3(1,.18,.12),_AlarmRed);
                if(indoors)lightColor=max(lightColor,_AlarmAmbient*float3(1,.45,.3));
                return fixed4(lightColor,1);
            }
            ENDCG
        }
    }
}
