Shader "DefenseDuell/DioramaVertexLit"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Flash ("Hit flash", Range(0,1)) = 0
        _ToyMotion ("Troop sway: phase, amplitude, height, ground", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            // Painted-miniature lighting. No textures, extra passes, derivative noise,
            // screen-space effects, alpha blending or mesh-data migration.
            #define DD_FILL_DOWN 0.29
            #define DD_FILL_UP 0.49
            #define DD_DIRECT 0.64
            #define DD_SPECULAR 0.24
            #define DD_RIM 0.055
            #define DD_CRYSTAL 0.065
            struct appdata
            {
                float4 vertex:POSITION;
                float3 normal:NORMAL;
                fixed4 color:COLOR;
                float2 finish:TEXCOORD0;
                float2 occlusion:TEXCOORD1; // Optional baked darkening; absent UV1 is neutral zero.
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex:SV_POSITION;
                float3 normal:TEXCOORD0;
                float3 view:TEXCOORD1;
                half4 surface:TEXCOORD2; // finish, warm metal, cool crystal, baked darkening
                fixed4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Tint)
                UNITY_DEFINE_INSTANCED_PROP(float, _Flash)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ToyMotion)
            UNITY_INSTANCING_BUFFER_END(Props)
            float3 SafeUnit(float3 value)
            { return value * rsqrt(max(dot(value,value),1e-8)); }
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                // Explicit troop-only opt-in. The CPU owns phase/pause/reduced-motion;
                // no shader _Time means paused units do not keep swaying. Base vertices
                // remain fixed and normal correction follows the shear derivative.
                float4 motion=UNITY_ACCESS_INSTANCED_PROP(Props,_ToyMotion);
                if(motion.y>0)
                {
                    float height=max(motion.z,.1);
                    float rawHeight=(v.vertex.y-motion.w)/height;
                    float t=saturate(rawHeight);
                    float sway=sin(motion.x)*min(motion.y,.025);
                    v.vertex.x+=sway*t*t;
                    float slope=rawHeight>0 && rawHeight<1?2*sway*t/height:0;
                    v.normal.y-=slope*v.normal.x;
                }
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.view=_WorldSpaceCameraPos.xyz-mul(unity_ObjectToWorld,v.vertex).xyz;
                o.color=v.color;
                half finish=saturate(v.finish.x);
                // Existing authoring encodes surface finish, not physically measured
                // roughness/metalness. These are deliberately restrained palette hints.
                half warm=saturate((v.color.r-v.color.b)*4);
                half cool=saturate((v.color.b-v.color.r)*4);
                o.surface=half4(finish,warm*smoothstep(.45,.70,finish),cool*smoothstep(.68,.84,finish),clamp(v.occlusion.x,0,.35));
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 normal=SafeUnit(i.normal),sun=SafeUnit(_WorldSpaceLightPos0.xyz),view=SafeUnit(i.view);
                half3 key=max(_LightColor0.rgb,0); // Real Unity directional light colour AND intensity.
                half ndl=saturate(dot(normal,sun));
                half wrapped=saturate((dot(normal,sun)+.10)/1.10);
                half up=saturate(normal.y*.5+.5);
                half3 fillColour=lerp(half3(.80,.84,.82),half3(.91,.96,1),up);
                half fill=lerp(DD_FILL_DOWN,DD_FILL_UP,up);
                half3 albedo=saturate(i.color.rgb*UNITY_ACCESS_INSTANCED_PROP(Props,_Tint).rgb);
                half finish=i.surface.x,metal=i.surface.y,crystal=i.surface.z;
                half polish=smoothstep(.24,.78,finish);

                // Broad lacquer highlight plus a narrower metal highlight. Multiplying
                // by key and N.L avoids an independent white glow on unlit back faces.
                half nh=saturate(dot(normal,SafeUnit(sun+view)));
                half n2=nh*nh,n4=n2*n2,n8=n4*n4,n16=n8*n8,n32=n16*n16;
                half highlight=lerp(n8,n32,polish*.72);
                half3 reflectance=lerp(half3(1,1,1),lerp(albedo,half3(1,.94,.78),.25),metal*.75);
                // True precomputed geometric AO affects indirect fill only. It does not
                // dim the sun/specular, change palette RGB, or occupy the finish UVs.
                half3 diffuse=albedo*(fillColour*fill*(1-i.surface.w)+key*(wrapped*DD_DIRECT))*(1-metal*.055);
                half3 specular=reflectance*key*highlight*ndl*polish*DD_SPECULAR;
                half edge=1-saturate(dot(normal,view));edge=edge*edge*edge;
                half3 rim=half3(.67,.83,.90)*edge*polish*up*DD_RIM;
                // Small clear-crystal colour lift, not HDR bloom or an emissive light.
                half3 crystalLift=albedo*crystal*(.35+.65*edge)*DD_CRYSTAL;
                half3 result=diffuse+specular+rim+crystalLift;
                half flash=saturate(UNITY_ACCESS_INSTANCED_PROP(Props,_Flash));
                result=lerp(result,half3(1,.98,.80),flash);
                return fixed4(saturate(result),1);
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}
