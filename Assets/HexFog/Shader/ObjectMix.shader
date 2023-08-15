Shader "ObjectMix"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _FogMask("Texture", 2D) = "white" {}
        _Offset("offset",vector)=(1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" "QUEUE"="transparent+500"
        }
        LOD 100
        ZTest always
        //Blend SrcAlpha OneMinusSrcAlpha
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
                float3 positionWS:TEXCOORD2;
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            float4 _Offset;
            CBUFFER_END


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_FogMask);
            SAMPLER(sampler_FogMask);


            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 UnlitPassFragment(Varyings input) : SV_Target
            {
                half2 uv = input.uv;
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half mindis = 1;


                float2 foguv = input.positionWS.xz - _Offset.xz;

                foguv /= 200;
                half4 fog = SAMPLE_TEXTURE2D(_FogMask, sampler_FogMask, foguv);
                fog.a = 1;

                return fog;
            }
            ENDHLSL
        }
    }
}