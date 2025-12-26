Shader "DualShift/ScrollableLit"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollOffset ("Scroll Offset", Vector) = (0,0,0,0)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline" = "UniversalPipeline"}

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Fog { Mode Off } // CRITICAL: Prevent Global Fog from graying out background

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            // Standard URP 2D keywords might vary, but we'll try standard lighting setup or minimal custom
            
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ScrollOffset;
            fixed4 _RendererColor;
            
            // CUSTOM VARIABLES
            float _CustomGlobalLight;
            fixed4 _CustomGlobalLightColor;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex) + _ScrollOffset.xy;
                OUT.color = _Color; 
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                c *= _Color;
                
                // 1. Global Light (Ambient)
                fixed4 totalLight = _CustomGlobalLightColor;
                if (length(totalLight) <= 0.001) totalLight = fixed4(1,1,1,1);
                
                return c * totalLight;
            }
            ENDCG
        }
    }
}
