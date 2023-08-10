Shader "Unlit/blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurRadius("_BlurRadius", float) =0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3: TEXCOORD3;
                float2 uv4: TEXCOORD4;
                float4 positionCS : SV_POSITION;
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _BlurRadius;
            CBUFFER_END


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            v2f vert(appdata input)
            {
                v2f output = (v2f)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.uv0 = TRANSFORM_TEX(input.uv, _MainTex);
                output.uv1 = output.uv0 + float2(_BlurRadius, _BlurRadius);
                output.uv2 = output.uv0 - float2(_BlurRadius, _BlurRadius);
                output.uv3 = output.uv0 + float2(-_BlurRadius, _BlurRadius);
                output.uv4 = output.uv0 + float2(_BlurRadius, -_BlurRadius);


                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                half4 col0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv0);
                col0 += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv1);
                col0 += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv2);
                col0 += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv3);
                col0 += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv4);
                col0 *= 0.2;
                return col0;
            }
            ENDHLSL
        }
    }
}