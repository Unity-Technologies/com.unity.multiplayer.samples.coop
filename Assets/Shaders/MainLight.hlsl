#ifndef MAIN_LIGHT_INCLUDED
#define MAIN_LIGHT_INCLUDED

void MainLight_float(float3 WorldPosition, out float3 Direction, out float3 Color, out float ShadowAttenuation)
{
    #if defined(SHADERGRAPH_PREVIEW)
        Direction = float3(0.5,0.5,0);
        Color = 1;
        ShadowAttenuation = 1;

    #else
        float4 shadowCoord = TransformWorldToShadowCoord(WorldPosition);
        Light mainLight = GetMainLight(shadowCoord);
        Direction = mainLight.direction;
        Color = mainLight.color;

        #if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECIEVE_SHADOWS_OFF)
            ShadowAttenuation = 1.0h;
        #else
            ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
            float shadowStrength = GetMainLightShadowStrength();
            ShadowAttentuation = SampleShadowmap(shadowCoord, TEXTURE2D_ARGS(_MainLightShadowmapTexture,
            sampler_MainLightShadowmapTexture), shadowSamplingData, shadowStrength, false);
        #endif
    #endif
}
#endif