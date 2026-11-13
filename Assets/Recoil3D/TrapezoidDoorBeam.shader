Shader "Recoil/3D/Trapezoid Door Beam"
{
    Properties
    {
        [HideInInspector] _Cookie("Source light aperture",2D)="white"{}
        [HideInInspector] _BeamColour("Light colour",Color)=(.62,.77,.84,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        // Render the exit face once, including when the camera enters the volume.
        // Depth clipping below also handles an exit face hidden behind the floor.
        Cull Front ZWrite Off ZTest Always Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            sampler2D _Cookie;
            float3 _BeamOrigin;
            float _ForegroundLength;
            float4 _NearFarHalf,_FarOffsetLength,_BeamSettings,_BeamColour,_DoorBeamLightPositionRange,_SpotShape;
            float4x4 _WorldToLight;
            struct v2f {float4 position:SV_POSITION;float4 screen:TEXCOORD0;float3 world:TEXCOORD1;};
            v2f vert(appdata_base v)
            {
                v2f o;o.position=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.position);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;
            }
            bool ClipPlane(float3 normal,float offset,float3 origin,float3 direction,inout float entry,inout float exit)
            {
                float d=dot(normal,origin)+offset,v=dot(normal,direction);
                if(abs(v)<.00001)return d<=0;
                float t=-d/v;if(v<0)entry=max(entry,t);else exit=min(exit,t);
                return exit>entry;
            }
            float DensityAt(float3 world)
            {
                float3 q=world-_BeamOrigin;
                float z=saturate(q.z/_FarOffsetLength.z);
                float2 halfSize=lerp(_NearFarHalf.xy,_NearFarHalf.zw,z);
                float2 unit=abs(q.xy-_FarOffsetLength.xy*z)/max(halfSize,.001);
                float edge=smoothstep(0,_BeamSettings.y,1-max(unit.x,unit.y));
                float ends=smoothstep(0,_FarOffsetLength.w,q.z+_ForegroundLength)*smoothstep(0,_FarOffsetLength.w,_FarOffsetLength.z-q.z);
                // Sample the doorway mask in aperture space, then carry it through the expanding volume.
                float2 apertureXY=(q.xy-_FarOffsetLength.xy*z)*_NearFarHalf.xy/max(halfSize,.001);
                float3 apertureWorld=_BeamOrigin+float3(apertureXY,0);
                float3 local=mul(_WorldToLight,float4(apertureWorld,1)).xyz;
                float distance=length(world-_DoorBeamLightPositionRange.xyz);
                float cosine=local.z/max(length(local),.001);
                if(local.z<=0||cosine<=_SpotShape.y||distance>=_DoorBeamLightPositionRange.w)return 0;
                float2 uv=.5+.5*local.xy/max(.001,local.z*_SpotShape.x);
                float cookie=tex2Dlod(_Cookie,float4(uv,0,0)).a;
                float cone=smoothstep(_SpotShape.y,max(_SpotShape.y+.001,_SpotShape.z),cosine);
                float attenuation=pow(saturate(1-distance/_DoorBeamLightPositionRange.w),2);
                return edge*ends*cookie*cone*attenuation;
            }
            float IntegrateBeam(float3 camera,float3 direction,float sceneDistance,bool foreground)
            {
                float3 origin=camera-_BeamOrigin;
                float length=max(.001,_FarOffsetLength.z),entry=0,exit=sceneDistance;
                float2 slope=foreground?float2(0,0):(_NearFarHalf.zw-_NearFarHalf.xy)/length;
                float2 shift=foreground?float2(0,0):_FarOffsetLength.xy/length;
                float nearZ=foreground?-_ForegroundLength:0,farZ=foreground?0:length;
                if(!ClipPlane(float3(0,0,-1),nearZ,origin,direction,entry,exit))return 0;
                if(!ClipPlane(float3(0,0,1),-farZ,origin,direction,entry,exit))return 0;
                if(!ClipPlane(float3(1,0,-slope.x-shift.x),-_NearFarHalf.x,origin,direction,entry,exit))return 0;
                if(!ClipPlane(float3(-1,0,-slope.x+shift.x),-_NearFarHalf.x,origin,direction,entry,exit))return 0;
                if(!ClipPlane(float3(0,1,-slope.y-shift.y),-_NearFarHalf.y,origin,direction,entry,exit))return 0;
                if(!ClipPlane(float3(0,-1,-slope.y+shift.y),-_NearFarHalf.y,origin,direction,entry,exit))return 0;
                float span=exit-entry;
                float density=(DensityAt(camera+direction*(entry+span*.2113))+DensityAt(camera+direction*(entry+span*.5))+DensityAt(camera+direction*(entry+span*.7887)))/3;
                return span*density;
            }
            float4 frag(v2f i):SV_Target
            {
                float3 forward=-UNITY_MATRIX_V[2].xyz;
                float eyeDepth=-mul(UNITY_MATRIX_V,float4(i.world,1)).z;
                float3 direction=lerp(normalize(i.world-_WorldSpaceCameraPos),forward,unity_OrthoParams.w);
                float3 camera=lerp(_WorldSpaceCameraPos,i.world-forward*eyeDepth,unity_OrthoParams.w);
                float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
                float sceneDistance=LinearEyeDepth(raw)/max(.001,dot(direction,forward));
                float integral=IntegrateBeam(camera,direction,sceneDistance,false);
                if(_ForegroundLength>.001)integral+=IntegrateBeam(camera,direction,sceneDistance,true);
                float opticalDepth=integral*_BeamSettings.x*_BeamSettings.z*_BeamSettings.w;
                float3 middle=_BeamOrigin+float3(0,0,_FarOffsetLength.z*.5);
                float phase=.75+.25*abs(dot(direction,normalize(middle-_DoorBeamLightPositionRange.xyz)));
                float intensity=(1-exp(-opticalDepth))*phase;
                return float4(_BeamColour.rgb*intensity,0);
            }
            ENDCG
        }
    }
    Fallback Off
}
