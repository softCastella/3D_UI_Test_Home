Shader "Project/Handwritten Signature Reveal"
{
    Properties
    {
        _MainTex ("Signature", 2D) = "white" {}
        _InkColor ("Ink Color", Color) = (0.03, 0.07, 0.09, 1)
        _Reveal ("Reveal", Range(0, 1)) = 0
        _RevealSoftness ("Reveal Softness", Range(0.001, 0.1)) = 0.025
        _InkThreshold ("Ink Threshold", Range(0, 1)) = 0.72
        _InkSoftness ("Ink Softness", Range(0.001, 0.3)) = 0.18
        _UseTextureAlpha ("Use Texture Alpha", Range(0, 1)) = 0
        _CropRect ("Crop Rect", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "SignatureReveal"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _InkColor;
                float4 _CropRect;
                float _Reveal;
                float _RevealSoftness;
                float _InkThreshold;
                float _InkSoftness;
                float _UseTextureAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 croppedUv = lerp(_CropRect.xy, _CropRect.zw, input.uv);
                half4 signature = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, croppedUv);
                half luminance = dot(signature.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half inkFromLuminance = 1.0h - smoothstep(
                    _InkThreshold,
                    _InkThreshold + _InkSoftness,
                    luminance);
                half sourceAlpha = lerp(
                    inkFromLuminance,
                    inkFromLuminance * signature.a,
                    saturate(_UseTextureAlpha));

                float revealPath = input.uv.x
                    + sin(input.uv.y * 17.0) * 0.018
                    + sin(input.uv.y * 41.0) * 0.008;
                half revealAlpha = smoothstep(
                    revealPath - _RevealSoftness,
                    revealPath + _RevealSoftness,
                    _Reveal);

                half4 output = _InkColor;
                output.a *= sourceAlpha * revealAlpha;
                return output;
            }
            ENDHLSL
        }
    }
}
