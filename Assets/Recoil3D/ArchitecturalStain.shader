Shader "Recoil/3D/Architectural Runoff"
{
 Properties { _MainTex("UV",2D)="white"{} _Tint("Tint",Color)=(.08,.07,.04,.45) _Shape("0 runoff, 1 rising damp, 2 puddle",Float)=0 }
 SubShader { Tags {"Queue"="Transparent-5" "RenderType"="Transparent"} Cull Off ZWrite Off Offset -1,-1
 CGPROGRAM
 #pragma surface surf Standard alpha:fade
 #pragma target 3.0
 fixed4 _Tint;float _Shape;
 struct Input {float2 uv_MainTex;};sampler2D _MainTex;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float2 uv=IN.uv_MainTex;float stripe=.35+.65*hash(float2(floor(uv.x*23),0));float a;
 if(_Shape>1.5){float d=length((uv-.5)*2);a=(1-smoothstep(.55,1,d))*(.72+.28*stripe);}
 else if(_Shape>.5){a=(1-smoothstep(.03,.95,uv.y))*(.6+.4*stripe)*smoothstep(0,.04,uv.x)*smoothstep(0,.04,1-uv.x);}
 else{float width=lerp(.07,.48,uv.y);a=(1-smoothstep(width*.65,width,abs(uv.x-.5)))*smoothstep(0,.22,uv.y)*stripe;}
 o.Albedo=_Tint.rgb;o.Metallic=_Shape>1.5?.15:0;o.Smoothness=_Shape>1.5?.72:.08;o.Alpha=a*_Tint.a;
 }
 ENDCG
 } Fallback Off
}
