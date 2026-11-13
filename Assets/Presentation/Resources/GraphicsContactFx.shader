Shader "DefenseDuell/GraphicsContactFx"
{
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 shape:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 shape:TEXCOORD1; };
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;o.shape=v.shape;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float radius=length(i.uv);
                float coverage=1-smoothstep(i.shape.y,1,radius);
                if(i.shape.x>0)coverage*=smoothstep(max(0,i.shape.x-.04),i.shape.x+.04,radius);
                return fixed4(i.color.rgb,i.color.a*coverage);
            }
            ENDCG
        }
    }
}
