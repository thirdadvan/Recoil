Shader "Recoil/Effects/Broadcast Refraction"
{
    Properties { _MainTex("Camera image",2D)="white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _WorldRect;
            float4 _PulseFront[4]; // origin.xy, current radius, amplitude envelope
            float4 _PulseShape[4]; // packet width, wavelength, displacement in metres, propagation sign
            float4 _PulseTint[4];
            float _RippleTime,_Irregularity,_ChromaticAberration;
            int _PulseCount;
            float4 frag(v2f_img i):SV_Target
            {
                float2 world=_WorldRect.xy+i.uv*_WorldRect.zw;
                float2 worldOffset=0;
                float3 tint=0;
                float tintWeight=0;
                [unroll] for(int n=0;n<4;n++)
                {
                    if(n>=_PulseCount)break;
                    float2 delta=world-_PulseFront[n].xy;
                    float distance=max(length(delta),.0001);
                    float2 normal=delta/distance;
                    float width=max(.05,_PulseShape[n].x);
                    float noise=sin(world.y*1.43+world.x*.37+_RippleTime*1.1)
                              +.45*sin(world.y*3.2-world.x*1.7-_RippleTime*.83);
                    float q=distance-_PulseFront[n].z+noise*_Irregularity*width*.22;
                    // A smooth wave packet, not a coloured or opaque circle.
                    float envelope=exp(-q*q/(width*width*.24))*_PulseFront[n].w;
                    float phase=q*6.283185/max(.08,_PulseShape[n].y);
                    float bend=sin(phase)*envelope*_PulseShape[n].z;
                    float2 tangent=float2(-normal.y,normal.x);
                    worldOffset+=(normal*bend+tangent*cos(phase*.65)*noise*envelope*_PulseShape[n].z*.12*_Irregularity);
                    tint+=_PulseTint[n].rgb*_PulseTint[n].a*envelope;
                    tintWeight+=_PulseTint[n].a*envelope;
                }
                // Fade at image edges and clamp samples so screen borders do not smear.
                float edge=saturate(min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y))/.025);
                float2 offset=worldOffset/max(_WorldRect.zw,float2(.01,.01))*edge;
                float2 border=abs(_MainTex_TexelSize.xy)*1.5;
                float2 uv=clamp(i.uv+offset,border,1-border);
                float4 color=tex2D(_MainTex,uv);
                if(_ChromaticAberration>.0001)
                {
                    float2 separation=offset*_ChromaticAberration;
                    color.r=tex2D(_MainTex,clamp(uv+separation,border,1-border)).r;
                    color.b=tex2D(_MainTex,clamp(uv-separation,border,1-border)).b;
                }
                // Very subtle warning tint. Geometry/light bending remains the primary cue.
                color.rgb=lerp(color.rgb,color.rgb*(1+tint),saturate(tintWeight));
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
