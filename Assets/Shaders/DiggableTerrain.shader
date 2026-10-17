Shader "Mouse Platformer/Diggable Terrain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _DigMask ("Solid mask", 2D) = "white" {}
        _DugAlpha ("Excavated opacity", Range(0,1)) = 0.9960784
        _DugTint ("Tunnel tint", Color) = (0.04,0.025,0.015,1)
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex DigVertex
            #pragma fragment DigFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 maskUV : TEXCOORD1;
            };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_DigMask); SAMPLER(sampler_DigMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _DugTint;
                float _DugAlpha;
                float4 _DigRect;
                float4x4 _DigWorldToLocal;
            CBUFFER_END
            Varyings DigVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                float2 local = mul(_DigWorldToLocal, float4(world, 1)).xy;
                output.maskUV = (local - _DigRect.xy) * _DigRect.zw;
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 DigFragment(Varyings input) : SV_Target
            {
                half4 earth = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half solid = SAMPLE_TEXTURE2D(_DigMask, sampler_DigMask, input.maskUV).r;
                half4 tunnel = half4(_DugTint.rgb, _DugAlpha * earth.a);
                return lerp(tunnel, earth, solid);
            }
            ENDHLSL
        }
    }
}
