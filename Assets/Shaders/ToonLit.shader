Shader "EmersynsBigDay/ToonLit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.7,0.6,0.8,1)
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSmooth ("Shadow Smooth", Range(0,0.5)) = 0.05
        _RimColor ("Rim Color", Color) = (1,0.8,0.9,1)
        _RimPower ("Rim Power", Range(0.5,8.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0,1)) = 0.5
        _OutlineWidth ("Outline Width", Range(0,0.05)) = 0.01
        _OutlineColor ("Outline Color", Color) = (0.2,0.15,0.25,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _AO ("Ambient Occlusion", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        // Main toon pass
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float fogCoord : TEXCOORD5;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_AO);
            SAMPLER(sampler_AO);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half _ShadowThreshold;
                half _ShadowSmooth;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
                half _Glossiness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseMap = TEXTURE2D_SAMPLE(_BaseMap, sampler_BaseMap, input.uv);
                half4 baseColor = baseMap * _BaseColor;
                half ao = TEXTURE2D_SAMPLE(_AO, sampler_AO, input.uv).r;

                // Main light
                Light mainLight = GetMainLight(input.shadowCoord);
                half NdotL = dot(normalize(input.normalWS), mainLight.direction);
                half shadow = mainLight.shadowAttenuation;

                // Toon shading - step function with smooth edge
                half toon = smoothstep(_ShadowThreshold - _ShadowSmooth, _ShadowThreshold + _ShadowSmooth, NdotL * shadow);
                half3 lighting = lerp(_ShadowColor.rgb, half3(1,1,1), toon) * mainLight.color;

                // Rim lighting for cute glow effect
                half rim = 1.0 - saturate(dot(normalize(input.viewDirWS), normalize(input.normalWS)));
                rim = pow(rim, _RimPower) * _RimIntensity;
                half3 rimColor = _RimColor.rgb * rim;

                half3 finalColor = baseColor.rgb * lighting * ao + rimColor;
                finalColor = MixFog(finalColor, input.fogCoord);
                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // Outline pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half _OutlineWidth;
                half4 _OutlineColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz + input.normalOS * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(pos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // Depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
