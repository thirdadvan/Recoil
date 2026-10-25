Shader "Recoil/Effects/Indoor Vision Fog"
{
    Properties { _MainTex("Camera image",2D)="white"{} }
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
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _MainTex_TexelSize;
            float4x4 _InverseViewProjection;
            float4 _Observer; // xyz, depth distance weight
            float4 _Vision; // clear radius, feather width, far brightness, light reveal strength
            float4 _LightPosition[12],_LightDirection[12],_LightData[12];
            int _LightCount;
            float _PreviewMask;
            float4 frag(v2f_img i):SV_Target
            {
                float2 uv=i.uv;
                float2 depthUV=uv;
                #if UNITY_UV_STARTS_AT_TOP
                    if(_MainTex_TexelSize.y<0)depthUV.y=1-depthUV.y;
                #endif
                float raw=SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,depthUV);
                float depth=raw;
                #if !defined(UNITY_REVERSED_Z)
                    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float4 h=mul(_InverseViewProjection,float4(depthUV*2-1,depth,1));
                float3 position=h.xyz/h.w;
                float3 delta=position-_Observer.xyz;delta.z*=_Observer.w;
                float distance=length(delta);
                float nearVision=1-smoothstep(_Vision.x,_Vision.x+_Vision.y,distance);
                if(Linear01Depth(raw)>.9999)nearVision=0;
                float exposure=0;
                [loop]for(int n=0;n<12;n++)
                {
                    if(n>=_LightCount)break;
                    float3 fromLight=position-_LightPosition[n].xyz;
                    float squared=dot(fromLight,fromLight);
                    float radius=_LightPosition[n].w;
                    if(squared>radius*radius)continue;
                    float d=sqrt(max(squared,.00001));
                    float attenuation=saturate(1-d/max(.01,radius));attenuation*=attenuation;
                    if(_LightData[n].y>.5)
                    {
                        float angle=dot(fromLight/d,_LightDirection[n].xyz);
                        attenuation*=smoothstep(_LightDirection[n].w,min(.9999,_LightDirection[n].w+.08),angle);
                    }
                    exposure=max(exposure,attenuation*_LightData[n].x);
                }
                float4 colour=tex2D(_MainTex,uv);
                float brightness=max(colour.r,max(colour.g,colour.b));
                // A lamp volume can preserve an already lit pixel, never brighten a dark surface.
                float litSurface=smoothstep(.025,.16,brightness);
                float lampVision=saturate(exposure*_Vision.w)*litSurface;
                // Small emissive lenses and bright window slits stay readable in the darkness.
                float luminous=smoothstep(.30,.80,brightness)*.85;
                float reveal=max(nearVision,max(lampVision,luminous));
                float transmission=lerp(_Vision.z,1,reveal);
                if(_PreviewMask>.5)return float4(transmission.xxx,1);
                return float4(colour.rgb*transmission,colour.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
