Shader "GameJamPlus/Platform Water"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SurfaceColor ("Surface", Color) = (0.55,0.95,1,0.7)
        _WaveAmplitude ("Wave amplitude (world units)", Range(0,0.15)) = 0.04
        _WaveSpeed ("Wave speed", Float) = 1.5
        _PixelsPerUnit ("Pixels per world unit", Float) = 32
        _DepthRange ("Depth shading range", Float) = 5
        [HideInInspector] _SurfaceHeight ("Surface height", Float) = 10000
        [HideInInspector] _BottomHeight ("Bottom height", Float) = -10000
        [HideInInspector] _Flow ("Signed flow", Float) = 0
        [HideInInspector] _FlowDirection ("Flow direction", Vector) = (1,0,0,0)
        [HideInInspector] _FlowOrigin ("Flow origin", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _SurfaceColor, _FlowOrigin, _FlowDirection;
                float _WaveAmplitude, _WaveSpeed, _PixelsPerUnit, _DepthRange;
                float _SurfaceHeight, _BottomHeight, _Flow;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; float2 uv : TEXCOORD1; float4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xy;
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float pixel = rcp(max(1, _PixelsPerUnit));
                float2 position = floor(input.world / pixel) * pixel;
                float time = _Time.y * _WaveSpeed;
                float wave = sin(position.x * 2.3 + time) * 0.65 + sin(position.x * 5.1 - time * 1.3) * 0.35;
                float distanceToFlow = abs(position.x - _FlowOrigin.x);
                float ripple = sin(distanceToFlow * 9 - time * 4) * exp(-distanceToFlow * 0.45) * saturate(abs(_Flow));
                float surface = _SurfaceHeight - _WaveAmplitude * saturate(0.5 + wave * 0.35 + ripple * 0.25);
                clip(surface - input.world.y);
                float depth = max(0, surface - position.y);
                float shading = saturate(depth / max(pixel, _DepthRange));
                half4 tint = _Color * input.color;
                half3 color = tint.rgb * lerp(1.1, 0.7, shading);
                float2 direction = _FlowDirection.xy;
                direction /= max(length(direction), 0.0001);
                float2 crossDirection = float2(-direction.y, direction.x);
                float stripe = step(0.94, sin(dot(position, direction) * 6 - _Time.y * abs(_Flow) * 5
                    + dot(position, crossDirection) * 15));
                color += stripe * saturate(abs(_Flow)) * exp(-distanceToFlow * 0.4) * 0.10;
                float edge = 1 - step(pixel * 1.4, depth);
                color = lerp(color, _SurfaceColor.rgb, edge);
                float alpha = lerp(tint.a, max(tint.a, _SurfaceColor.a), edge);
                alpha *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
