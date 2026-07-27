Shader "PPE/Exterior View"
{
    Properties
    {
        _SkyColor ("Sky", Color) = (0.62, 0.75, 0.80, 1)
        _HorizonColor ("Horizon", Color) = (0.72, 0.68, 0.40, 1)
        _GroundColor ("Ground", Color) = (0.27, 0.34, 0.36, 1)
        _GridColor ("Grid", Color) = (0.58, 0.62, 0.61, 1)
        _HorizonHeight ("Horizon Height", Range(0.2, 0.8)) = 0.54
        _GridStrength ("Grid Strength", Range(0, 1)) = 0.32
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ExteriorView"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _GridColor;
                half _HorizonHeight;
                half _GridStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half GridLine(float coordinate, float width)
            {
                float wrapped = frac(coordinate);
                float distanceToLine = min(wrapped, 1.0 - wrapped);
                return 1.0h - smoothstep(width, width + fwidth(coordinate), distanceToLine);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float y = saturate(input.uv.y);
                float skyAmount = smoothstep(_HorizonHeight, 1.0, y);
                half3 sky = lerp(_HorizonColor.rgb, _SkyColor.rgb, skyAmount);

                float groundAmount = smoothstep(0.0, _HorizonHeight, y);
                half3 ground = lerp(_GroundColor.rgb, _HorizonColor.rgb * 0.72h, groundAmount);

                half3 color = y >= _HorizonHeight ? sky : ground;
                if (y < _HorizonHeight)
                {
                    float depth = max((_HorizonHeight - y) / _HorizonHeight, 0.018);
                    half horizontal = GridLine(0.72 / depth, 0.025);
                    half vertical = GridLine((input.uv.x - 0.5) * 3.6 / depth, 0.025);
                    half fade = smoothstep(0.015, 0.16, depth) * (1.0h - smoothstep(0.82, 1.0, depth));
                    half grid = max(horizontal, vertical) * fade * _GridStrength;
                    color = lerp(color, _GridColor.rgb, grid);
                }

                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
