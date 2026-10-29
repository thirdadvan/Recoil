Shader "Recoil/Art Tests/Warehouse Atmosphere"
{
 Properties{_MainTex("Image",2D)="white"{}}
 SubShader{
 Cull Off ZWrite Off ZTest Always
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_Scattering,_BroadGlow;
 float4 _MainTex_TexelSize;
 UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
 float3 _CameraPosition;float3 _Ray00,_Ray10,_Ray01,_Ray11;
 float4 _LampPos[3],_LampDir[3],_LampColour[3];int _LampCount,_VolumeSteps;float _Glow;
 float4 _LampVolumeProfile[3],_VolumeShelterHalfSize[6];
 float4x4 _VolumeShelterWorldToLocal[6];
 // Trace the light-to-sample segment through authored shelter boxes. This is independent
 // of the viewing ray, so the same protected space stays dark while the camera moves.
 float ShelterVisibility(int lamp,float3 samplePosition){
 float visible=1;
 [unroll]for(int j=0;j<2;j++){
 int slot=clamp(lamp*2+j,0,5);if(_VolumeShelterHalfSize[slot].w<.5)continue;
 float3 a=mul(_VolumeShelterWorldToLocal[slot],float4(_LampPos[lamp].xyz,1)).xyz;
 float3 b=mul(_VolumeShelterWorldToLocal[slot],float4(samplePosition,1)).xyz;
 float3 delta=b-a,extent=_VolumeShelterHalfSize[slot].xyz;
 float3 inverse=(step(0,delta)*2-1)/max(abs(delta),.00001);
 float3 ta=(-extent-a)*inverse,tb=(extent-a)*inverse;
 float3 nearT=min(ta,tb),farT=max(ta,tb);
 float enter=max(nearT.x,max(nearT.y,nearT.z)),exit=min(farT.x,min(farT.y,farT.z));
 if(exit>max(enter,0)&&enter<.999&&exit>0)visible=0;
 }return visible;
 }
 float _SkyEnabled,_SkyFeather;float3 _SkyTop;float4 _SkyShape,_SkyColour;
 // A rectangular roof aperture, widening gently downward. World-space bounds keep
 // the shaft anchored to the architecture when the camera pans or changes pitch.
 float3 Skylight(float3 direction,float sceneDistance,float jitter){
 if(_SkyEnabled<.5)return 0;
 float3 extent=float3(_SkyShape.x+_SkyShape.z*_SkyShape.w,_SkyShape.z*.5,_SkyShape.y+_SkyShape.z*_SkyShape.w);
 float3 centre=_SkyTop-float3(0,_SkyShape.z*.5,0);
 float3 safeDir=direction+float3(abs(direction.x)<.00001?.00001:0,abs(direction.y)<.00001?.00001:0,abs(direction.z)<.00001?.00001:0);
 float3 ta=(centre-extent-_CameraPosition)/safeDir,tb=(centre+extent-_CameraPosition)/safeDir;
 float3 nearT=min(ta,tb),farT=max(ta,tb);
 float start=max(0,max(nearT.x,max(nearT.y,nearT.z)));
 float end=min(sceneDistance,min(farT.x,min(farT.y,farT.z)));
 if(end<=start)return 0;
 float stepSize=(end-start)/16;float3 total=0;
 [unroll]for(int k=0;k<16;k++){
 float3 p=_CameraPosition+direction*(start+(k+jitter)*stepSize);
 float down=_SkyTop.y-p.y;
 float2 halfSize=_SkyShape.xy+down*_SkyShape.w;
 float2 uv=abs(p.xz-_SkyTop.xz)/halfSize;
 float edge=1-smoothstep(1-_SkyFeather,1,max(uv.x,uv.y));
 float vertical=saturate(down/.12)*lerp(1,.4,saturate(down/_SkyShape.z));
 vertical*=smoothstep(0,.24,_SkyShape.z-down);
 float dust=.96+.04*sin(p.x*2.3+p.z*1.7+p.y*.8);
 total+=_SkyColour.rgb*(edge*vertical*dust*stepSize);
 }return total;
 }
 float4 Volume(v2f_img i):SV_Target{
 float2 depthUV=i.uv;
 #if UNITY_UV_STARTS_AT_TOP
 if(_MainTex_TexelSize.y<0)depthUV.y=1-depthUV.y;
 #endif
 float raw=SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,depthUV);
 // Ray interpolation uses unflipped viewport UV; only depth sampling needs RT correction.
 float3 viewRay=lerp(lerp(_Ray00,_Ray10,i.uv.x),lerp(_Ray01,_Ray11,i.uv.x),i.uv.y);
 float3 ray=viewRay*LinearEyeDepth(raw);
 float len=min(length(ray),70);float3 direction=normalize(ray);
 float jitter=frac(52.9829189*frac(dot(i.pos.xy,float2(.06711056,.00583715))));
 float3 sum=0;
 int steps=max(8,_VolumeSteps);
 [loop]for(int s=0;s<steps;s++){
 float3 p=_CameraPosition+direction*(len*(s+jitter)/steps);
 [loop]for(int n=0;n<3;n++){
 if(n>=_LampCount)break;
 float3 v=p-_LampPos[n].xyz;float distance=length(v);
 float cone=smoothstep(_LampDir[n].w,min(.999,_LampDir[n].w+.12),dot(v/max(.001,distance),_LampDir[n].xyz));
 float fade=saturate(1-distance/_LampPos[n].w);
 float4 profile=_LampVolumeProfile[n];
 float heightMask=profile.w>.5?1-smoothstep(profile.y-profile.z,profile.y,p.y):1;
 if(cone<=0||fade<=0||heightMask<=0||profile.x<=0)continue;
 float visibility=ShelterVisibility(n,p);
 float dust=.88+.12*sin(p.x*2.3+p.z*1.7+p.y*.8);
 sum+=_LampColour[n].rgb*(cone*fade*fade*dust*len/steps*.045*profile.x*heightMask*visibility);
 }}
 sum+=Skylight(direction,len,jitter);
 return float4(sum,1);}
 float4 Blur(v2f_img i):SV_Target{
 float2 t=_MainTex_TexelSize.xy*1.7;float4 c=tex2D(_MainTex,i.uv)*.2;
 c+=tex2D(_MainTex,i.uv+t)*.2;c+=tex2D(_MainTex,i.uv-t)*.2;
 c+=tex2D(_MainTex,i.uv+float2(t.x,-t.y))*.2;c+=tex2D(_MainTex,i.uv+float2(-t.x,t.y))*.2;return c;}
 float4 Composite(v2f_img i):SV_Target{
 float4 c=tex2D(_MainTex,i.uv);float3 fog=tex2D(_Scattering,i.uv).rgb;
 c.rgb+=fog*.35+tex2D(_BroadGlow,i.uv).rgb*(.65+_Glow);
 // Output-only dither protects dark gradients; does not alter gameplay exposure.
 float a=frac(sin(dot(i.pos.xy,float2(12.9898,78.233)))*43758.5453);
 float b=frac(sin(dot(i.pos.yx,float2(39.346,11.135)))*24634.6345);
 c.rgb=GammaToLinearSpace(max(0,LinearToGammaSpace(max(0,c.rgb))+(a+b-1)/255));return c;}
 ENDCG
 Pass{CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment Volume
 #pragma target 3.0
 ENDCG}
 Pass{CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment Blur
 ENDCG}
 Pass{CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment Composite
 ENDCG}
 }
 Fallback Off
}
