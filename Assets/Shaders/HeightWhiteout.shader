Shader "Mouse Platformer/Height Whiteout"
{
    Properties
    {
        _StartHeight ("Flash Start Height", Float) = 34.475
        _EndHeight ("Flash End Height", Float) = 37.475
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex WhiteoutVertex
            #pragma fragment WhiteoutFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _StartHeight;
                float _EndHeight;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings WhiteoutVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }
            half4 WhiteoutFragment(Varyings input) : SV_Target
            {
                float progress = saturate((GetCameraPositionWS().y - _StartHeight) / max(0.01, _EndHeight - _StartHeight));
                return half4(1, 1, 1, smoothstep(0, 1, progress));
            }
            ENDHLSL
        }
    }
}
