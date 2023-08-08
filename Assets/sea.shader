Shader "Unlit/sea"
{
    Properties
    {
        _BaseColor("Color",Color)=(1,1,1,1)
        _BaseMap ("Texture", 2D) = "white" {}
        _DissolveMap ("_DissolveMap", 2D) = "white" {}
        _DirectionMap("_DirectionMap", 2D) = "white" {}

        _Dissolve(" _Dissolve",range(0,1))=1
        _Direction("_Direction",float)=0
        _MaskUVScale("_MaskScale",float)=1

    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "QUEUE"="TRANSPARENT"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        zwrite off
        // blend srcalpha oneminussrcalpha
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
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _DissolveMap_ST;
            half4 _BaseColor;
            float _Dissolve;
            float _Direction;
            float _MaskUVScale;
            CBUFFER_END

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
            #endif


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DissolveMap);
            SAMPLER(sampler_DissolveMap);
            TEXTURE2D(_DirectionMap);
            SAMPLER(sampler_DirectionMap);


            v2f vert(appdata input)
            {
                v2f output = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.uv.xy = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv.zw = TRANSFORM_TEX(input.uv, _DissolveMap);

                return output;
            }

            float frag(v2f i) : SV_Target
            {
                half4 mask = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, i.uv.zw);
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv.xy);
                float2 uv = i.uv - 0.5;
                uv = float2(uv.x * cos(_Direction) - uv.y * sin(_Direction), uv.x * sin(_Direction) + uv.y * cos(_Direction));
                //uv *= _MaskUVScale;
                uv += 0.5;
                float directionMask = SAMPLE_TEXTURE2D(_DirectionMap, sampler_DirectionMap, uv).r + .5 - mask.r - (_Dissolve * 1.5);
                float alpha = 1 * directionMask;
                alpha = step(.2, alpha);
                col.a = saturate(alpha);
                // uv=saturate(i.uv-0.5);
                return  SAMPLE_TEXTURE2D(_DirectionMap, sampler_DirectionMap,  i.uv).b;
            }
            ENDHLSL
        }
    }
}