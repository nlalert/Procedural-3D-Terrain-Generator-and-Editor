Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture", 2D) = "white"{}      // Texture property for the terrain shader
        testScale("Scale", Float) = 1               // Scale property for texture mapping
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }              // The shader renders opaque objects
        LOD 200                                     // Level of detail (LOD) setting for shader performance

        CGPROGRAM
        // Use the physically-based Standard lighting model and enable shadows for all light types
        #pragma surface surf Standard fullforwardshadows

        // Set shader target to 3.0 for better lighting and advanced features
        #pragma target 3.0

        // Define constants for maximum number of texture layers and epsilon for height blending
        const static int maxLayerCount = 8;         // Maximum number of texture layers
        const static float epsilon = 1E-4;          // Small value to prevent blending issues

        // Shader variables to control terrain layering and blending
        int layerCount;                             // Number of active texture layers
        float3 baseColors[maxLayerCount];           // Array to hold base colors for each layer
        float baseStartHeights[maxLayerCount];      // Start height for each texture layer
        float baseBlends[maxLayerCount];            // Blending amount for each texture layer
        float baseColorStrength[maxLayerCount];     // Color strength for each texture layer
        float baseTextureScales[maxLayerCount];     // Texture scale for each texture layer

        // Minimum and maximum heights for terrain blending
        float minHeight;                            // Minimum terrain height
        float maxHeight;                            // Maximum terrain height

        // Properties for test texture and its scaling
        sampler2D testTexture;                      // Test texture sampler
        float testScale;                            // Scale of the test texture

        // Declare a texture array to hold terrain textures
        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        // Define input structure for surface shader with world position and normal
        struct Input
        {
            float3 worldPos;                        // World position of the pixel
            float3 worldNormal;                     // World normal at the pixel
        };

        // Add support for GPU instancing (disabled by default but can be enabled)
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // Define any per-instance properties for instancing here
        UNITY_INSTANCING_BUFFER_END(Props)

        // Function to calculate the percentage of a value between two points (inverse lerp)
        float inverseLerp(float a, float b, float value)
        {
            return saturate((value - a) / (b - a));  // Return clamped value between 0 and 1
        }

        // Triplanar texture mapping to reduce stretching on steep terrain
        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex)
        {
            // Scale the world position based on texture scale
            float3 scaledWorldPos = worldPos / scale;

            // Sample the texture from three axes (X, Y, and Z) and blend them
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;

            // Return the combined projection result for triplanar mapping
            return xProjection + yProjection + zProjection;
        }

        // Main function called for every pixel when the mesh is visible
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate height percentage based on the position of the pixel
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);

            // Calculate blend axes based on the world normal, used for triplanar mapping
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            // Loop through each layer and apply textures and color blending
            for(int i = 0; i < layerCount; i++)
            {
                // Calculate how much to blend the current texture layer based on height
                float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);

                // Get the base color for the current layer and scale its strength
                float3 baseColor = baseColors[i] * baseColorStrength[i];

                // Apply triplanar mapping to get the texture color
                float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1-baseColorStrength[i]);

                // Blend the current layer's color and texture into the final Albedo (surface color)
                o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
            }
        }

        ENDCG
    }

    FallBack "Diffuse"  // Fallback shader in case the target hardware can't handle this shader
}
