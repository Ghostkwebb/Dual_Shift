Shader "DualShift/ScrollableLit"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollOffset ("Scroll Offset", Vector) = (0,0,0,0)
        _DepthFade ("Depth Fade (0=None, 1=Full)", Range(0, 1)) = 0.3
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
                float depth     : TEXCOORD1; // For depth-based blending
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ScrollOffset;
            fixed4 _RendererColor;
            float _DepthFade;
            
            // CUSTOM VARIABLES
            float _CustomGlobalLight;
            fixed4 _CustomGlobalLightColor;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex) + _ScrollOffset.xy;
                OUT.color = _Color;
                
                // Calculate depth for parallax fade (normalized 0-1)
                // Objects further from camera get higher depth values
                float4 worldPos = mul(unity_ObjectToWorld, IN.vertex);
                OUT.depth = saturate((worldPos.z + 10.0) / 20.0); // Normalize Z position
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                c *= _Color;
                
                // 1. Global Light (Ambient)
                fixed4 totalLight = _CustomGlobalLightColor;
                if (length(totalLight) <= 0.001) totalLight = fixed4(1,1,1,1);
                
                // 2. Apply depth fade for distant layers (Hollow Knight style fog)
                // This makes far backgrounds blend into the ambient color
                float fadeFactor = lerp(1.0, 0.6, IN.depth * _DepthFade);
                c.rgb = lerp(totalLight.rgb * 0.3, c.rgb, fadeFactor);
                
                // 3. Final output (light already applied above)
                return c;
            }
            ENDCG
        }
    }
}
