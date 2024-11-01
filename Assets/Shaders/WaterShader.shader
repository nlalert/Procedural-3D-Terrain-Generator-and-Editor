Shader "Custom/StylizedWater"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0.0, 0.6, 0.8, 1.0)
        _AbsorptionColor ("Absorption Color", Color) = (0.0, 0.3, 0.5, 1.0)
        _AbsorptionCoefficient ("Absorption Coefficient", Float) = 1.0
        _Depth ("Depth", Float) = 10.0
        _SurfaceTransparency ("Surface Transparency", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade

        struct Input
        {
            float3 worldPos;
            float4 screenPos;
        };

        half4 _WaterColor;
        half4 _AbsorptionColor;
        float _AbsorptionCoefficient;
        float _Depth;
        float _SurfaceTransparency;

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Calculate depth-based absorption effect
            float depthMultiplier = _AbsorptionCoefficient * _Depth;
            float absorptionFactor = exp(-depthMultiplier);

            // Color adjustment for absorption
            half4 adjustedColor = lerp(_AbsorptionColor, _WaterColor, absorptionFactor);

            // Screen-based transparency adjustmenta
            float screenTransparency = 1.0 - _SurfaceTransparency;
            half4 screenColor = half4(1.0, 1.0, 1.0, screenTransparency);

            // Final color with transparency applied
            o.Albedo = lerp(adjustedColor.rgb, screenColor.rgb, screenTransparency);
            o.Alpha = screenTransparency;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
