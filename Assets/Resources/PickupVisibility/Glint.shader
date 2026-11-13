Shader "Recoil/Pickup/Glint"
{
    Properties { _Strength("Strength",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Strength;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=abs(i.uv*2-1);
                float core=exp(-dot(p,p)*65);
                float rays=exp(-p.x*65)*pow(saturate(1-p.y),3)+exp(-p.y*65)*pow(saturate(1-p.x),3);
                return fixed4(float3(1,.89,.66)*saturate(core+rays*.65)*_Strength,0);
            }
            ENDCG
        }
    }
}
