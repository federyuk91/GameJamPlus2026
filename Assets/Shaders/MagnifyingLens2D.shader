Shader "Mouse Platformer/Magnifying Lens 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _LensBackgroundTexture ("Background Behind Lens", 2D) = "black" {}
        _Magnification ("Magnification", Range(1,5)) = 2
        _LensDistortion ("Lens Distortion", Range(-1,1)) = 0
        _GreenDominance ("Green Dominance", Range(0.01,1)) = 0.2
        [Enum(GreenChroma,0,SeparateTexture,1,SpriteSecondaryTexture,2,SeparateSprite,3)] _MaskSource ("Mask Source", Float) = 0
        [NoScaleOffset] _SeparateMask ("Separate Mask", 2D) = "black" {}
        [PerRendererData] _LensMask ("Sprite Secondary Mask", 2D) = "black" {}
        _MaskChannel ("Mask Channel (RGBA weights)", Vector) = (1,0,0,0)
        _MaskStrength ("Mask Strength", Range(0,1)) = 1
        [HideInInspector] _MaskAvailable ("Mask Available", Float) = 0
        [HideInInspector] _SpriteRect ("Local Sprite Rectangle", Vector) = (-0.5,-0.5,1,1)
        [HideInInspector] _MaskUVOrigin ("Mask UV Origin", Vector) = (0,0,0,0)
        [HideInInspector] _MaskUVX ("Mask UV X Axis", Vector) = (1,0,0,0)
        [HideInInspector] _MaskUVY ("Mask UV Y Axis", Vector) = (0,1,0,0)
        _LensCenter ("Lens Center", Vector) = (0,0,0,0)
        [HideInInspector] _LensRadius ("Lens Radius", Vector) = (1,1,0,0)
        [HideInInspector] _LensCaptureReady ("Capture Ready", Float) = 0
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
                COMMON_2D_OUTPUTS_SHARED
                half4 color : COLOR;
                float4 screen : TEXCOORD1;
                float4 centerScreen : TEXCOORD2;
                float2 localPosition : TEXCOORD3;
                float4 upperScreen : TEXCOORD4;
            };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_LensBackgroundTexture); SAMPLER(sampler_LensBackgroundTexture);
            TEXTURE2D(_SeparateMask); SAMPLER(sampler_SeparateMask);
            TEXTURE2D(_LensMask); SAMPLER(sampler_LensMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _LensCenter;
                float4 _LensRadius;
                float _Magnification;
                float _LensDistortion;
                float _GreenDominance;
                float _LensCaptureReady;
                float _MaskSource, _MaskStrength, _MaskAvailable;
                float4 _MaskChannel, _SpriteRect, _MaskUVOrigin, _MaskUVX, _MaskUVY;
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
                output.localPosition = input.positionOS.xy;
                float3 upper = UnityFlipSprite(_LensCenter.xyz + float3(0, _LensRadius.y, 0), unity_SpriteProps.xy);
                output.upperScreen = ComputeScreenPos(TransformObjectToHClip(upper));
                return output;
            }
            half4 LensFragment(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half mask = step(_GreenDominance, sprite.g - max(sprite.r, sprite.b));
                if (_MaskSource > 0.5)
                {
                    float2 maskUV = saturate((input.localPosition - _SpriteRect.xy) * _SpriteRect.zw);
                    half4 maskSample;
                    if (_MaskSource > 2.5)
                    {
                        float2 spriteMaskUV = _MaskUVOrigin.xy + maskUV.x * _MaskUVX.xy + maskUV.y * _MaskUVY.xy;
                        maskSample = SAMPLE_TEXTURE2D(_SeparateMask, sampler_SeparateMask, spriteMaskUV);
                    }
                    else if (_MaskSource > 1.5) maskSample = SAMPLE_TEXTURE2D(_LensMask, sampler_LensMask, input.uv);
                    else maskSample = SAMPLE_TEXTURE2D(_SeparateMask, sampler_SeparateMask, maskUV);
                    mask = saturate(dot(maskSample, _MaskChannel));
                    // RGB mask data outside a transparent silhouette must not refract the background.
                    mask *= lerp(maskSample.a, 1.0h, saturate(_MaskChannel.w));
                }
                mask *= saturate(_LensCaptureReady) * saturate(_MaskStrength);
                if (_MaskSource > 0.5) mask *= saturate(_MaskAvailable);
                float2 uv = input.screen.xy / input.screen.w;
                float2 center = input.centerScreen.xy / input.centerScreen.w;
                float horizontal = (input.localPosition.x - _LensCenter.x) / max(0.0001, _LensRadius.x);
                float circularArc = sqrt(saturate(1.0 - horizontal * horizontal));
                float2 up = input.upperScreen.xy / input.upperScreen.w - center;
                // Inverse sampling: positive distortion bends the image upward, negative downward.
                float2 source = saturate(center + (uv - center - _LensDistortion * circularArc * up) / max(1.0, _Magnification));
                half4 zoomed = SAMPLE_TEXTURE2D(_LensBackgroundTexture, sampler_LensBackgroundTexture, source);
                if (_MaskSource > 0.5)
                {
                    half4 foreground = sprite * input.color;
                    half backgroundAlpha = mask * saturate(input.color.a);
                    half alpha = foreground.a + backgroundAlpha * (1.0h - foreground.a);
                    half3 premultiplied = foreground.rgb * foreground.a
                        + zoomed.rgb * backgroundAlpha * (1.0h - foreground.a);
                    // Return straight alpha for this pass's existing SrcAlpha blend mode.
                    return half4(premultiplied / max(alpha, 0.0001h), alpha);
                }
                half4 result = lerp(sprite * input.color, half4(zoomed.rgb, sprite.a * input.color.a), mask);
                return result;
            }
            ENDHLSL
        }
    }
}
