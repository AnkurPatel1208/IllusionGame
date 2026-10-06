Shader "Custom/CartoonWater"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Color (Turquoise)", Color) = (0.14, 0.80, 0.84, 0.65)
        _DeepColor ("Deep Water Color (Ocean Azure)", Color) = (0.06, 0.36, 0.60, 0.95)
        _DepthDistance ("Depth Fade Distance", Float) = 3.2

        [Header(Soft Water Caustics)]
        _CausticTex ("Caustics Texture", 2D) = "white" {}
        _CausticColor ("Caustics Tint Color", Color) = (0.70, 0.95, 1.0, 1.0)
        _CausticScale ("Caustics Scale", Float) = 0.08
        _CausticSpeed ("Caustics Speed", Float) = 0.35
        _CausticIntensity ("Caustics Brightness", Range(0.0, 1.0)) = 0.28

        [Header(Smooth Wave Normal Map)]
        _NormalTex ("Wave Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Float) = 0.08
        _NormalSpeed ("Normal Speed", Float) = 0.40
        _NormalStrength ("Wave Bump Strength", Range(0.0, 1.0)) = 0.25

        [Header(Crisp Shoreline Foam)]
        _FoamColor ("Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FoamDistance ("Shoreline Contact Foam Width", Range(0.05, 0.8)) = 0.22
        _RippleFrequency ("Ripple Ring Frequency", Float) = 12.0
        _RippleSpeed ("Ripple Expansion Speed", Float) = 2.5
        _RippleExtent ("Ripple Fade Distance", Range(0.2, 1.5)) = 0.65

        [Header(Sunlight Sheen)]
        _SunGlintColor ("Sun Sheen Color", Color) = (1.0, 0.98, 0.92, 1.0)
        _SpecularPower ("Specular Softness", Range(16.0, 128.0)) = 64.0
        _SpecularIntensity ("Sun Sheen Intensity", Range(0.0, 2.5)) = 1.2

        [Header(Surface Transparency)]
        _BaseOpacity ("Base Water Opacity", Range(0.0, 1.0)) = 0.70

        [Header(Vertex Rolling Swell)]
        _VertexWaveAmp ("Ocean Swell Height", Range(0.0, 0.10)) = 0.025
        _VertexWaveFreq ("Swell Frequency", Float) = 0.25
        _VertexWaveSpeed ("Swell Speed", Float) = 0.80
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_CausticTex);
            SAMPLER(sampler_CausticTex);

            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _DepthDistance;

                float4 _CausticColor;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticIntensity;

                float _NormalScale;
                float _NormalSpeed;
                float _NormalStrength;

                float4 _FoamColor;
                float _FoamDistance;
                float _RippleFrequency;
                float _RippleSpeed;
                float _RippleExtent;

                float4 _SunGlintColor;
                float _SpecularPower;
                float _SpecularIntensity;

                float _BaseOpacity;

                float _VertexWaveAmp;
                float _VertexWaveFreq;
                float _VertexWaveSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenUV   : TEXCOORD1;
            };

            float GetEyeDepth(float rawDepth)
            {
                if (unity_OrthoParams.w > 0.5)
                {
                    #if UNITY_REVERSED_Z
                        return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
                    #else
                        return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
                    #endif
                }
                else
                {
                    return LinearEyeDepth(rawDepth, _ZBufferParams);
                }
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                if (_VertexWaveAmp > 0.0001)
                {
                    float swell1 = sin(worldPos.x * _VertexWaveFreq * 0.8 + worldPos.z * _VertexWaveFreq * 1.1 - _Time.y * _VertexWaveSpeed);
                    float swell2 = cos(worldPos.x * _VertexWaveFreq * 1.2 - worldPos.z * _VertexWaveFreq * 0.7 + _Time.y * (_VertexWaveSpeed * 0.8));
                    worldPos.y += (swell1 + swell2 * 0.5) * _VertexWaveAmp;
                }

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.screenUV   = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calculate Scene Depth & Water Depth
                float2 screenUV = input.screenUV.xy / max(0.0001, input.screenUV.w);
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = GetEyeDepth(rawDepth);
                float thisEyeDepth  = GetEyeDepth(input.positionCS.z);
                float waterDepth    = max(0.0, sceneEyeDepth - thisEyeDepth);

                if (rawDepth <= 0.00001 || rawDepth >= 0.99999)
                {
                    waterDepth = _DepthDistance * 2.0;
                }

                // 2. Pure, Clean Cel-Depth Gradient (Clear Turquoise -> Serene Ocean Azure)
                float depthFactor = saturate(waterDepth / max(0.01, _DepthDistance));
                float smoothDepth = depthFactor * depthFactor * (3.0 - 2.0 * depthFactor);
                half4 baseWater = lerp(_ShallowColor, _DeepColor, smoothDepth);

                // 3. Smooth, Silky Normal Perturbation from Low-Frequency Normal Map
                float2 nUV1 = input.positionWS.xz * _NormalScale + float2(0.02, 0.015) * (_Time.y * _NormalSpeed);
                float2 nUV2 = input.positionWS.xz * (_NormalScale * 1.2) - float2(0.015, 0.025) * (_Time.y * _NormalSpeed * 0.85);
                
                half4 normSamp1 = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, nUV1);
                half4 normSamp2 = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, nUV2);
                half3 unpackedN1 = UnpackNormal(normSamp1);
                half3 unpackedN2 = UnpackNormal(normSamp2);
                half3 blendedNormalTS = normalize(half3(unpackedN1.xy + unpackedN2.xy, unpackedN1.z));

                float3 waveNormal = normalize(float3(blendedNormalTS.x * _NormalStrength, 1.0, blendedNormalTS.y * _NormalStrength));

                // 4. Soft, Large-Scale Undulating Caustics (Gentle, relaxing, serene)
                float2 cDistort = waveNormal.xz * 0.05;
                float2 cUV1 = input.positionWS.xz * _CausticScale + float2(0.02, 0.01) * (_Time.y * _CausticSpeed) + cDistort;
                float2 cUV2 = input.positionWS.xz * (_CausticScale * 1.15) - float2(0.015, 0.02) * (_Time.y * _CausticSpeed * 0.9) - cDistort;

                half caustic1 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, cUV1).r;
                half caustic2 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, cUV2).r;
                half caustics = saturate(caustic1 * 0.6 + caustic2 * 0.4);

                // Caustics are gently visible in shallow water, softly fading as depth increases
                float shallowFade = saturate(1.0 - (waterDepth / max(0.1, _DepthDistance * 1.2)));
                half3 causticGlow = _CausticColor.rgb * (caustics * _CausticIntensity * shallowFade);

                // 5. Crisp, Clean Shoreline Contact Foam (Hugging citadel base, pillars, steps, and rocks)
                float shoreWash = sin(_Time.y * 2.2 + input.positionWS.x * 1.5 + input.positionWS.z * 1.2) * 0.02;
                float shoreDist = saturate((waterDepth + shoreWash) / max(0.001, _FoamDistance));
                float contactFoam = pow(1.0 - shoreDist, 1.6); // Crisp, neat outline right at contact edge

                // 6. Delicate Concentric Ripple Rings (Strictly within _RippleExtent, fading outward)
                float ringDistMask = saturate(1.0 - (waterDepth / max(0.01, _RippleExtent)));
                float ringPhase = waterDepth * _RippleFrequency - _Time.y * _RippleSpeed;
                float delicateRing = smoothstep(0.75, 0.98, sin(ringPhase)) * ringDistMask * 0.40;

                // Total Foam (Clean, confined strictly around submerged obstacles)
                float totalFoam = saturate(contactFoam + delicateRing);

                // 7. Diffuse Lighting & Soft Sun Sheen
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                half3 halfVector = normalize(lightDir + viewDir);

                float NdotL = saturate(dot(waveNormal, lightDir));
                float toonDiff = smoothstep(0.15, 0.55, NdotL) * 0.20 + 0.80;
                half3 litWater = baseWater.rgb * toonDiff * mainLight.color;

                // Add soft caustics
                litWater += causticGlow;

                // Soft Blinn-Phong specular sheen across gentle wave crests
                float NdotH = saturate(dot(waveNormal, halfVector));
                float spec = pow(NdotH, _SpecularPower);
                half3 sunSheen = _SunGlintColor.rgb * spec * _SpecularIntensity * mainLight.color;

                // 8. Layer Composition
                half3 finalRGB = litWater;
                // Add crisp white shoreline foam and ripple rings
                finalRGB = lerp(finalRGB, _FoamColor.rgb, totalFoam * _FoamColor.a);
                // Add gentle sun sheen
                finalRGB += sunSheen;

                // 9. Translucency
                float alpha = lerp(_ShallowColor.a, _DeepColor.a, smoothDepth);
                alpha = saturate(alpha + totalFoam * 0.40);
                alpha = max(alpha, _BaseOpacity);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
