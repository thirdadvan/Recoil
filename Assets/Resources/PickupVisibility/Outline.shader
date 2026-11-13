Shader "Recoil/Pickup/Outline"
{
    Properties { _Color("Color",Color)=(.85,.76,.55,.85) _Pixels("Width in pixels",Float)=1.3 }
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Transparent" }
        Pass
        {
            Cull Front ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color; float _Pixels;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            float4 vert(appdata v):SV_POSITION
            {
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 normal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 direction = mul((float2x2)UNITY_MATRIX_P, normal.xy);
                direction /= max(length(direction), .0001);
                clip.xy += direction * (2 * _Pixels / _ScreenParams.xy) * clip.w;
                return clip;
            }
            fixed4 frag():SV_Target { return _Color; }
            ENDCG
        }
    }
}
