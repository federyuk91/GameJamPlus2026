Shader "GameJamPlus/ExitTunnel"
{
    Properties
    {
        _MainTex ("Exit image", 2D) = "white" {}
        _Crop ("Centered crop", Vector) = (1,1,0,0)
        _Exposure ("Exposure multiplier", Float) = 1
        _Whiteout ("Whiteout", Range(0,1)) = 0
        _LightStrength ("Light strength", Float) = 2.5
        _Progress ("Travel progress", Range(0,1)) = 0
        _ScreenAspect ("Screen aspect", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Crop;
            float _Exposure;
            float _Whiteout;
            float _LightStrength;
            float _Progress;
            float _ScreenAspect;

            float4 frag(v2f_img input) : SV_Target
            {
                float2 uv = (input.uv - 0.5) * _Crop.xy + 0.5;
                float4 source = tex2D(_MainTex, uv);
                float3 color = saturate(source.rgb * _Exposure);
                // Transparent parts of the artwork reveal white light rather than the gameplay.
                color = lerp(float3(1,1,1), color, source.a);
                // Use screen coordinates so the opening stays circular independently of texture crop/zoom.
                float2 centered = (input.uv - 0.5) * float2(_ScreenAspect, 1);
                float radius = length(centered) / length(float2(_ScreenAspect, 1) * 0.5);
                float opening = lerp(0.012, 1.15, pow(_Progress, 3));
                float halo = 1 - smoothstep(opening * 0.7, opening * 2.4, radius);
                float core = 1 - smoothstep(opening * 0.35, opening, radius);
                float warmLight = saturate(halo * 0.8 * _LightStrength);
                color = lerp(color, float3(1,0.94,0.8), warmLight);
                color = lerp(color, float3(1,1,1), core);
                // Only the final part washes out the entire image; the expanding opening does the approach.
                return float4(lerp(color, float3(1,1,1), _Whiteout), 1);
            }
            ENDHLSL
        }
    }
}
