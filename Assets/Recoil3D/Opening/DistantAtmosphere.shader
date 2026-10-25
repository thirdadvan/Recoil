Shader "Recoil/3D/Distant Atmosphere"
{
    Properties { _Color("Horizon silhouette",Color)=(.012,.017,.02,1) _Contrast("Atmospheric contrast",Range(0,1))=.5 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }
        Pass
        {
            Cull Off ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;float _Contrast;
            struct appdata {float4 vertex:POSITION;};
            struct v2f {float4 vertex:SV_POSITION;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);return o;}
            // Far silhouettes merge with the current fog colour without lighting the whole environment.
            fixed4 frag(v2f i):SV_Target{return fixed4(lerp(unity_FogColor.rgb,_Color.rgb,_Contrast),1);}
            ENDCG
        }
    }
}
