Shader "Recoil/Weathered Pump Paint"
{
    Properties
    {
        _Color ("Paint colour", Color) = (0.5,0.48,0.4,1)
        _MainTex ("Original paint texture", 2D) = "white" {}
        _RustNoise ("Shared rust noise", 2D) = "gray" {}
        _RustColor ("Rust colour", Color) = (0.21,0.085,0.031,1)
        _RustAmount ("Corrosion threshold", Range(0.2,0.8)) = 0.54
        _RustSeed ("Pattern offset", Vector) = (0,0,0,0)
        _Glossiness ("Paint smoothness", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.5
        sampler2D _MainTex, _RustNoise;
        fixed4 _Color, _RustColor;
        float _RustAmount, _Glossiness;
        float4 _RustSeed;
        struct Input { float2 uv_MainTex; float3 localPos; float3 localNormal; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.localPos=v.vertex.xyz; o.localNormal=v.normal;
        }
        float3 noiseAt(float3 p,float3 w)
        {
            return tex2D(_RustNoise,p.zy).rgb*w.x+tex2D(_RustNoise,p.xz).rgb*w.y+tex2D(_RustNoise,p.xy).rgb*w.z;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 w=pow(abs(normalize(IN.localNormal)),4);w/=max(dot(w,float3(1,1,1)),0.001);
            float3 p=IN.localPos+_RustSeed.xyz;
            float3 n=noiseAt(p*0.93,w);
            float3 fine=noiseAt(p*8.7,w);
            // Independent broader islands, fine chipped edges, and narrow downward streaks.
            float3 drip=noiseAt(float3(p.x*5.3,p.y*0.24,p.z*5.3),w);
            float baseWet=saturate((0.55-IN.localPos.y)*2.0)*0.1;
            float mask=smoothstep(_RustAmount-0.035,_RustAmount+0.035,n.r+baseWet+(fine.g-0.5)*0.12);
            float trails=smoothstep(0.65,0.76,drip.b)*smoothstep(0.39,0.53,n.r)*0.55;
            mask=saturate(mask+trails);
            fixed3 paint=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
            fixed3 rust=_RustColor.rgb*lerp(0.58,1.45,fine.g);
            float darkRim=smoothstep(_RustAmount-0.06,_RustAmount-0.025,n.r+baseWet)*(1-mask);
            o.Albedo=lerp(paint*(1-darkRim*0.25),rust,mask);
            o.Metallic=0;
            o.Smoothness=lerp(_Glossiness,0.025,mask);
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
