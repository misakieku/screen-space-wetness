Shader "FullScreen/ScreenSpaceWetness"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.misaki.screen-space-wetness/Runtime/Shader/Include/HDRPNormalBufferFunctions.hlsl"
    #include "Packages/com.misaki.screen-space-wetness/Runtime/Shader/Include/Noise.hlsl"


    // The PositionInputs struct allow you to retrieve a lot of useful information for your fullScreenShader:
    // struct PositionInputs
    // {
    //     float3 positionWS;  // World space position (could be camera-relative)
    //     float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    //     uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    //     uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    //     float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    //     float  linearDepth; // View space Z coordinate                              : [Near, Far]
    // };

    // To sample custom buffers, you have access to these functions:
    // But be careful, on most platforms you can't sample to the bound color buffer. It means that you
    // can't use the SampleCustomColor when the pass color buffer is set to custom (and same for camera the buffer).
    // float4 CustomPassSampleCustomColor(float2 uv);
    // float4 CustomPassLoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    # define N_SAMPLE 16

    static const float2 poissonDisk4[] = {
        float2( -0.94201624, -0.39906216 ),
        float2( 0.94558609, -0.76890725 ),
        float2( -0.094184101, -0.92938870 ),
        float2( 0.34495938, 0.29387760 )
    };
    static const float2 poissonDisk16[] = {
        float2( -0.94201624, -0.39906216 ), 
        float2( 0.94558609, -0.76890725 ), 
        float2( -0.094184101, -0.92938870 ), 
        float2( 0.34495938, 0.29387760 ), 
        float2( -0.91588581, 0.45771432 ), 
        float2( -0.81544232, -0.87912464 ), 
        float2( -0.38277543, 0.27676845 ), 
        float2( 0.97484398, 0.75648379 ), 
        float2( 0.44323325, -0.97511554 ), 
        float2( 0.53742981, -0.47373420 ), 
        float2( -0.26496911, -0.41893023 ), 
        float2( 0.79197514, 0.19090188 ), 
        float2( -0.24188840, 0.99706507 ), 
        float2( -0.81409955, 0.91437590 ), 
        float2( 0.19984126, 0.78641367 ), 
        float2( 0.14383161, -0.14100790 ) 
    };

    float _intensity;
    float4 _noiseScaleOffset;
    float4 _noiseParams;

    #define NOISE_VALUE_MIN _noiseParams.x
    #define NOISE_VALUE_MAX _noiseParams.y
    #define NOISE_SLOPE_MIN _noiseParams.z
    #define NOISE_SLOPE_MAX _noiseParams.w

    TEXTURE2D_X(_maskBuffer);

    float4x4 _rainMatrix;
    float3 _rainDirection;

    float4 _waterColor;

    TEXTURE2D(_waterNormal1);
    SAMPLER(sampler_waterNormal1);
    TEXTURE2D(_waterNormal2);
    SAMPLER(sampler_waterNormal2);

    TEXTURE2D_ARRAY(_rippleNormalArray);
    SAMPLER(sampler_rippleNormalArray);

    float4 _rippleParams;

    #define RIPPLE_NORMAL_SCALE _rippleParams.x
    #define RIPPLE_NORMAL2_SCALE _rippleParams.y
    #define RIPPLE_NORMAL_SPEED _rippleParams.z
    #define RIPPLE_NORMAL_STRENGTH _rippleParams.w

    float4 _normalParams;
    float _normalStrength;
    float2 _flowDirection;

    #define WATER_NORMAL_1_SCALE _normalParams.x
    #define WATER_NORMAL_1_SPEED _normalParams.y
    #define WATER_NORMAL_2_SCALE _normalParams.z
    #define WATER_NORMAL_2_SPEED _normalParams.w

    float _waterSmoothness;
    
    TEXTURE2D(_shadowMap);

    float4 _shadowMap_TexelSize;
    float2 _bias;

    #define DEPTH_BIAS _bias.x
    #define NORMAL_BIAS _bias.y

    float2 Random2(float2 uv)
    {
        float n = dot(uv, float2(12.9898, 78.233));
        float randomX = frac(sin(n) * 43758.5453);
        float randomY = frac(sin(n + 1.0) * 43758.5453);
        return float2(randomX, randomY);
    }

    float3 NormalBlend(float3 A, float3 B)
    {
        return normalize(float3(A.rg + B.rg, A.b * B.b));
    }

    float3 NormalStrength(float3 In, float Strength)
    {
        return float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
    }

    float2 RotateRadians(float2 UV, float2 Center, float Rotation)
    {
        UV -= Center;
        float s = sin(Rotation);
        float c = cos(Rotation);
        float2x2 rMatrix = float2x2(c, -s, s, c);
        rMatrix *= 0.5;
        rMatrix += 0.5;
        rMatrix = rMatrix * 2 - 1;
        UV.xy = mul(UV.xy, rMatrix);
        UV += Center;

        return UV;
    }

    float ComputeWetnessMask(PositionInputs posInput, float3 positionAbsWS, float3 normalWS)
    {
        float mask = SAMPLE_TEXTURE2D_X_LOD(_maskBuffer, s_linear_clamp_sampler, posInput.positionNDC.xy, 0).r;

        int isNotSky = 1 - IsSky(posInput.positionNDC);
        mask *= isNotSky;
        
        float4 positionTC = mul(_rainMatrix, float4(positionAbsWS + NORMAL_BIAS * normalWS, 1.0f));
        float3 coord = positionTC.xyz / positionTC.w;

        float2 rotation = Random2(posInput.positionNDC.xy);
        rotation = rotation * 2 - 1;

        float NdotL = saturate(dot(normalWS, _rainDirection));

        //mask *= NdotL;

        float sum = 0.0f;
        float shadow = 0.0f;

        float2 uv = coord.xy / 2.0f + 0.5f;
        float kernalSize = _shadowMap_TexelSize.z * 5.0f;
        float bias = DEPTH_BIAS * tan(acos(NdotL));
        bias = clamp(bias, 0, 0.01f);

        if (NdotL > 0)
        {
            // take cheap "test" samples first
            UNITY_UNROLL
            for (int i = 0; i < 4; i++)
            {
                float2 offset = poissonDisk4[i];
                offset = float2(
                    rotation.x * offset.x - rotation.y * offset.y,
                    rotation.y * offset.x + rotation.x * offset.y
                    );
                float sampleDepth = SAMPLE_TEXTURE2D(_shadowMap, s_linear_clamp_sampler, uv + offset * kernalSize).x;
                sum += sampleDepth > coord.z + bias ? 0 : 1;
            }

            shadow = sum / 4;

            if ((shadow - 1) * shadow * NdotL != 0)
            {
                UNITY_UNROLL
                for (int i = 4; i < N_SAMPLE; i++)
                {
                    float2 offset = poissonDisk16[i];
                    offset = float2(
                        rotation.x * offset.x - rotation.y * offset.y,
                        rotation.y * offset.x + rotation.x * offset.y
                        );
                    float sampleDepth = SAMPLE_TEXTURE2D(_shadowMap, s_linear_clamp_sampler, uv + offset * kernalSize).x;
                    sum += sampleDepth > coord.z + bias ? 0 : 1;
                }
                shadow = sum / N_SAMPLE;
            }
        }

        mask *= shadow;

        float NdotU = saturate(dot(normalWS, float3(0.0f, 1.0f, 0.0f)));

        float noise = SimpleNoise(positionAbsWS.xz * _noiseScaleOffset.xy + _noiseScaleOffset.zw);
        noise = smoothstep(NOISE_VALUE_MIN, NOISE_VALUE_MAX, noise);
        noise = lerp(1.0f, noise, smoothstep(NOISE_SLOPE_MIN, NOISE_SLOPE_MAX, NdotU));

        mask *= smoothstep(0.0f, 0.5f, NdotU);
        mask *= noise;

        return saturate(mask * _intensity);
    }

    float3 ComputeWetnessNormal(PositionInputs posInput, float3 positionAbsWS, float3 normalWS)
    {
        float3 wetnessNormalWS = 0.0f;
        float2 uv = frac(positionAbsWS.xz);
        
        float3 viewDirection = normalize(GetWorldSpaceNormalizeViewDir(posInput.positionWS));
        float3 dx = ddx(viewDirection);
        float3 dy = ddy(viewDirection);
        float3 worldTangent = normalize(dx - dot(dx, viewDirection) * viewDirection);
        float3x3 tangentToWorldMatrix = CreateTangentToWorld(normalWS, worldTangent, 1.0f);

        //float2 flowDirection = normalize(_flowDirection);
        float3 right = cross(normalWS, float3(0.0f, 1.0f, 0.0f));
        float2 flowDirection = cross(right, normalWS).xz + _flowDirection;
        float4 normal1 = SAMPLE_TEXTURE2D(_waterNormal1, sampler_waterNormal1, uv * WATER_NORMAL_1_SCALE + _Time.y * WATER_NORMAL_1_SPEED * flowDirection);
        normal1.rgb = UnpackNormalmapRGorAG(normal1);
        float4 normal2 = SAMPLE_TEXTURE2D(_waterNormal2, sampler_waterNormal2, uv * WATER_NORMAL_2_SCALE + _Time.y * WATER_NORMAL_2_SPEED * flowDirection);
        normal2.rgb = UnpackNormalmapRGorAG(normal2);

        float3 waterNormal = NormalStrength(NormalBlend(normal1.rgb, normal2.rgb), _normalStrength);

        float chunkRandomValue1 = Random2(floor(uv * RIPPLE_NORMAL_SCALE + sin(_Time.y)));
        float chunkRandomRotation1 = floor(chunkRandomValue1 * 4.0f) * PI / 2.0f;
        float2 rotatedUV1 = RotateRadians(uv, float2(0.5f, 0.5f), chunkRandomRotation1);
        int slice1 = ((_Time.y + chunkRandomValue1 * 16.0f) * RIPPLE_NORMAL_SPEED) % 16;
        float4 rippleNormal1 = SAMPLE_TEXTURE2D_ARRAY(_rippleNormalArray, sampler_rippleNormalArray, rotatedUV1 * RIPPLE_NORMAL_SCALE, slice1);
        rippleNormal1.rgb = NormalStrength(UnpackNormalmapRGorAG(rippleNormal1), RIPPLE_NORMAL_STRENGTH * chunkRandomValue1);

        float chunkRandomValue2 = Random2(floor(uv * RIPPLE_NORMAL2_SCALE + sin(_Time.y)));
        float chunkRandomRotation2 = floor(chunkRandomValue2 * 4.0f) * PI / 2.0f;
        float2 rotatedUV2 = RotateRadians(uv, float2(0.5f, 0.5f), chunkRandomRotation2);
        int slice2 = ((_Time.y + chunkRandomValue2 * 16.0f) * RIPPLE_NORMAL_SPEED) % 16;
        float4 rippleNormal2 = SAMPLE_TEXTURE2D_ARRAY(_rippleNormalArray, sampler_rippleNormalArray, rotatedUV2 * RIPPLE_NORMAL2_SCALE, slice2);
        rippleNormal2.rgb = NormalStrength(UnpackNormalmapRGorAG(rippleNormal2), RIPPLE_NORMAL_STRENGTH * chunkRandomValue2);

        float3 rippleNormal = NormalBlend(rippleNormal1.rgb, rippleNormal2.rgb);

        waterNormal = NormalBlend(waterNormal, rippleNormal);

        wetnessNormalWS = normalize(TransformTangentToWorld(waterNormal, tangentToWorldMatrix));

        return wetnessNormalWS;
    }

    float4 WetnessPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        
        NormalData normalData;
        DecodeFromNormalBuffer(posInput.positionSS, normalData);

        float4 inNormalBuffer;
        float4 outNormalBuffer;

        if (_intensity == 0)
        {
            EncodeIntoNormalBuffer(normalData, outNormalBuffer);
            return outNormalBuffer;
        }

        float3 positionAbsWS = GetAbsolutePositionWS(posInput.positionWS);

        float3 wetnessNormalWS = ComputeWetnessNormal(posInput, positionAbsWS, normalData.normalWS);
        float mask = ComputeWetnessMask(posInput, positionAbsWS, normalData.normalWS);

        inNormalBuffer.rgb = lerp(normalData.normalWS, wetnessNormalWS, mask);
        inNormalBuffer.a = lerp(normalData.perceptualRoughness, 1.0f - _waterSmoothness, mask);
        
        EncodeIntoNormalBuffer(inNormalBuffer.rgb, inNormalBuffer.a, outNormalBuffer);

        return outNormalBuffer;
    }

    float4 ColorPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float4 output = 0.0f;
        output.a = 1.0f;
        output.rgb = SampleCameraColor(posInput.positionNDC);

        if (_intensity == 0)
        {
            return output;
        }

        float3 positionAbsWS = GetAbsolutePositionWS(posInput.positionWS);
        NormalData normalData;
        DecodeFromNormalBuffer(posInput.positionSS, normalData);

        float mask = ComputeWetnessMask(posInput, positionAbsWS, normalData.normalWS);

        output.rgb = lerp(output.rgb, output.rgb * _waterColor.rgb, mask * _waterColor.a);
        //output.rgb = mask * _waterColor.rgb;

        return output;
    }

    float4 DebugPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float4 output = 0.0f;

        if (_intensity == 0)
        {
            return output;
        }

        float3 positionAbsWS = GetAbsolutePositionWS(posInput.positionWS);
        NormalData normalData;
        DecodeFromNormalBuffer(posInput.positionSS, normalData);

        output = ComputeWetnessMask(posInput, positionAbsWS, normalData.normalWS);

        return output;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Wetness"

            ZWrite Off
            ZTest Off
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment WetnessPass
            ENDHLSL
        }

        Pass
        {
            Name "Color"

            ZWrite Off
            ZTest Off
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment ColorPass
            ENDHLSL
        }

        Pass
        {
            Name "Debug"

            ZWrite Off
            ZTest Off
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment DebugPass
            ENDHLSL
        }
    }
    Fallback Off
}
