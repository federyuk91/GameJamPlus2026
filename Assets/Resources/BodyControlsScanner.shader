Shader "Hidden/MousePlatformer/BodyControlsScanner"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _SourceRect ("Source Rect", Vector) = (0, 0, 1, 1)
        _ContentRect ("Content Rect", Vector) = (0, 0, 1, 1)
        _ScanPosition ("Scan Position", Float) = 0
        _ScanDirection ("Scan Direction", Vector) = (0, 1, 0, 0)
        _BandWidth ("Band Width", Float) = 0.18
        _PixelOffset ("Pixel Offset", Float) = 0
        _LineWidth ("Line Width", Float) = 0.008
        _LineColor ("Line Color", Color) = (0.941, 0.898, 0.792, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SourceRect;
            float4 _ContentRect;
            float _ScanPosition;
            float4 _ScanDirection;
            float _BandWidth;
            float _PixelOffset;
            float _LineWidth;
            float4 _LineColor;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 spriteUv = (input.uv - _ContentRect.xy) / max(_ContentRect.zw, float2(0.00001, 0.00001));
                float inside = step(0.0, spriteUv.x) * step(spriteUv.x, 1.0)
                    * step(0.0, spriteUv.y) * step(spriteUv.y, 1.0);
                float2 uv = _SourceRect.xy + saturate(spriteUv) * _SourceRect.zw;
                fixed4 source = tex2D(_MainTex, uv);
                float scanCoordinate = 0.5 + dot(float2(input.uv.x - 0.5, 0.5 - input.uv.y), _ScanDirection.xy);
                float distance = abs(scanCoordinate - _ScanPosition);
                float scanStrength = _PixelOffset > 0.0
                    ? saturate(1.0 - (distance - _BandWidth * 0.5) / _PixelOffset)
                    : step(distance, _BandWidth * 0.5);
                float lineMask = step(distance, _LineWidth * 0.5);
                float3 color = source.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    color = LinearToGammaSpace(color);
                #endif
                color = lerp(color, 1.0 - color, scanStrength);
                #ifndef UNITY_COLORSPACE_GAMMA
                    color = GammaToLinearSpace(color);
                #endif
                color = lerp(color, _LineColor.rgb, lineMask * _LineColor.a);
                return fixed4(color, source.a * scanStrength * inside);
            }
            ENDCG
        }
    }
}
