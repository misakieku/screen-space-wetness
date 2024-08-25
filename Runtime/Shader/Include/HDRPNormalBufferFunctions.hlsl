#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/TextureXR.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"


void EncodeIntoNormalBuffer(float3 normalWS, float perceptualRoughness, out float4 outNormalBuffer)
{
    NormalData normalData;
    normalData.normalWS = normalWS;
    normalData.perceptualRoughness = perceptualRoughness;

    EncodeIntoNormalBuffer(normalData, outNormalBuffer);
}