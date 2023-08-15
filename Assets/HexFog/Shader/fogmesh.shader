Shader "hexmesh"
{
    Properties
    {
        _BaseColor("Color",Color)=(1,1,1,1)
        _Layer0 ("_Layer0", 2D) = "white" {}
        _Layer1 ("_Layer1", 2D) = "white" {}

        _FogMaskMap ("_FogMaskMap", 2D) = "white" {}
        _DissolveMap ("_DissolveMap", 2D) = "white" {}


        _Dissolve(" _Dissolve",range(0,1))=1

        _MaskUVScale("_MaskScale",float)=1

    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "QUEUE"="TRANSPARENT+100"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        zwrite off
        blend srcalpha oneminussrcalpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _Layer0_ST;
            float4 _Layer1_ST;
            float4 _DissolveMap_ST;
            float4 _FogMaskMap_ST;
            float _Direction;
            float _MaskUVScale;

            CBUFFER_END


            VECTOR _HexColor;

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
                    UNITY_DOTS_INSTANCED_PROP(float , _Dissolve)
                    UNITY_DOTS_INSTANCED_PROP(float , _Direction)
            
            
            
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_BaseColor)
                #define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Cutoff)
                #define _Dissolve            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Dissolve)
                #define _Direction            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Direction)
                #define _HexColor            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(VECTOR  , Metadata_HexColor)
            #endif


            TEXTURE2D(_Layer0);
            SAMPLER(sampler_Layer0);
            TEXTURE2D(_Layer1);
            SAMPLER(sampler_Layer1);


            TEXTURE2D(_DissolveMap);
            SAMPLER(sampler_DissolveMap);
            TEXTURE2D(_FogMaskMap);
            SAMPLER(sampler_FogMaskMap);

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _Dissolve)
            UNITY_INSTANCING_BUFFER_END(Props)


            v2f vert(appdata input)
            {
                v2f output = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.uv0.xy = TRANSFORM_TEX(input.uv, _FogMaskMap);
                output.uv0.zw = TRANSFORM_TEX(input.uv, _DissolveMap);


                output.uv1.xy = TRANSFORM_TEX(input.uv, _Layer0);
                output.uv1.zw = TRANSFORM_TEX(input.uv, _Layer1);
                output.uv2.xy = TRANSFORM_TEX(input.uv, _FogMaskMap);

                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                float3 layer0 = SAMPLE_TEXTURE2D(_Layer0, sampler_Layer0, input.uv1.xy).rgb;
                float3 layer1 = SAMPLE_TEXTURE2D(_Layer1, sampler_Layer1, input.uv1.zw).rgb;


                half4 fogmask = SAMPLE_TEXTURE2D(_FogMaskMap, sampler_FogMaskMap, input.uv0.xy);
                half4 dissolve = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, input.uv0.zw/2);



                fogmask.g = fogmask.g * 2 - dissolve.r;
                fogmask.g = step(.01, fogmask.g);
                fogmask.b = saturate(fogmask.b - fogmask.g);

                float v = max(fogmask.b, fogmask.g);
                float3 layer = layer0 * fogmask.b + layer1 * fogmask.g;
                layer *= 1 - fogmask.r;


                fogmask.a = 1 - (fogmask.r * 2 - dissolve.r + .1) * 5;
                fogmask.a = step(0.1, fogmask.a);

                fogmask.rgb = layer;
                return fogmask;
            }
            ENDHLSL
        }
    }
}