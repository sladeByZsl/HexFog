Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Step ("Texture", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite off
        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;
                float4 positionCS : SV_POSITION;
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _Surface;
            half _Step;
            CBUFFER_END


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            const float3 n[6] = {
                float3(0, 1, -1),
                float3(1, 0, -1),
                float3(-1, 1, 0),
                float3(1, -1, 0),
                float3(-1, 0, 1),
                float3(0, -1, 1),
            };

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 UnlitPassFragment(Varyings input) : SV_Target
            {
                half2 uv = input.uv;
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half mindis = 1;


                half disc = distance(input.uv.xy, half2(0.5, 0.5));
                for (int i = 0; i < 6; i++)
                {
                    half2 pos = half2(0.5 + 0.5 * 1.73205080756887 * (n[i].x + n[i].z * .5f), 0.5 + 0.5 * 1.5 * n[i].z);
                    half a = distance(input.uv.xy, pos);
                    mindis = min(mindis, a);
                }
                half e = step(disc , mindis);


               texColor.a = e;

                return texColor;
            }
            ENDHLSL
        }
    }
}