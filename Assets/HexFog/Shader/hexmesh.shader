Shader "hexmesh"
{
    Properties
    {

        _SrcColor("_SrcColor",Color)=(1,1,1,1)
        _DestColor("_DestColor",Color)=(1,1,1,1)
        _Dissolve(" _Dissolve",range(0,1))=1

        _FogMaskMap ("_FogMaskMap", 2D) = "white" {}
        _Layer0 ("_Layer0", 2D) = "white" {}
        _Layer1 ("_Layer1", 2D) = "white" {}


        _DissolveMap ("_DissolveMap", 2D) = "white" {}

    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DrawHex"
            //colormask rgb
            blend srcalpha oneminussrcalpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //
            // CBUFFER_START(UnityPerMaterial)
            // //float _Dissolve;
            // CBUFFER_END


            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _SrcColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _Dissolve)
            UNITY_DEFINE_INSTANCED_PROP(float4, _DestColor)
            UNITY_INSTANCING_BUFFER_END(Props)


            v2f vert(appdata input)
            {
                v2f output = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.color = input.color;
                output.positionCS = vertexInput.positionCS;
                
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float dissolve = UNITY_ACCESS_INSTANCED_PROP(Props, _Dissolve);
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _SrcColor);
                float4 targetColor = UNITY_ACCESS_INSTANCED_PROP(Props, _DestColor);
                baseColor = lerp(baseColor, targetColor, dissolve);
                baseColor.a=input.color.a;
                return baseColor;
            }
            ENDHLSL
        }


        Pass
        {
            Name "Mix"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _Layer0_ST;
            float4 _Layer1_ST;
            float4 _DissolveMap_ST;
            float4 _FogMaskMap_ST;
            float _MaskUVScale;
            CBUFFER_END


            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
                    UNITY_DOTS_INSTANCED_PROP(float , _Dissolve)

            
            
            
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_BaseColor)
                #define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Cutoff)
                #define _Dissolve            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata_Dissolve)
            #endif


            TEXTURE2D(_Layer0);
            SAMPLER(sampler_Layer0);
            TEXTURE2D(_Layer1);
            SAMPLER(sampler_Layer1);


            TEXTURE2D(_DissolveMap);
            SAMPLER(sampler_DissolveMap);
            TEXTURE2D(_FogMaskMap);
            SAMPLER(sampler_FogMaskMap);


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