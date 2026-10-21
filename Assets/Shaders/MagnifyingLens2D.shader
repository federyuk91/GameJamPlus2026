Shader "Mouse Platformer/Magnifying Lens 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Magnification ("Magnification", Range(1,5)) = 2
        _GreenDominance ("Green Dominance", Range(0.01,1)) = 0.2
        _LensCenter ("Lens Center", Vector) = (0,0,0,0)
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex LensVertex
            #pragma fragment LensFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float4 screen : TEXCOORD1;
                float4 centerScreen : TEXCOORD2;
            };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D_X(_CameraSortingLayerTexture);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _LensCenter;
                float _Magnification;
                float _GreenDominance;
            CBUFFER_END
            Varyings LensVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                output.screen = ComputeScreenPos(output.positionCS);
                float3 center = UnityFlipSprite(_LensCenter.xyz, unity_SpriteProps.xy);
                output.centerScreen = ComputeScreenPos(TransformObjectToHClip(center));
                return output;
            }
            half4 LensFragment(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half mask = step(_GreenDominance, sprite.g - max(sprite.r, sprite.b));
                float2 uv = input.screen.xy / input.screen.w;
                float2 center = input.centerScreen.xy / input.centerScreen.w;
                float2 source = saturate(center + (uv - center) / max(1.0, _Magnification));
                half4 zoomed = SAMPLE_TEXTURE2D_X(_CameraSortingLayerTexture, sampler_PointClamp, source);
                half4 result = lerp(sprite * input.color, half4(zoomed.rgb, sprite.a * input.color.a), mask);
                return result;
            }
            ENDHLSL
        }
    }
}
