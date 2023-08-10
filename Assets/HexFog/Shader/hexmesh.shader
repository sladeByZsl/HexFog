Shader "hexmesh"
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
                float4 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _DissolveMap_ST;
            
           
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


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DissolveMap);
            SAMPLER(sampler_DissolveMap);
            TEXTURE2D(_DirectionMap);
            SAMPLER(sampler_DirectionMap);

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
                output.uv.xy = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv.zw = TRANSFORM_TEX(input.uv, _DissolveMap);

                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 mask = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, input.uv.zw);
                //half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);
                float2 uv = input.uv.xy - 0.5f;
                uv = float2(uv.x * cos(_Direction) - uv.y * sin(_Direction), uv.x * sin(_Direction) + uv.y * cos(_Direction));
                uv += 0.5;
                float directionMask = SAMPLE_TEXTURE2D(_DirectionMap, sampler_DirectionMap, uv).r + .5;
                float dissolve= UNITY_ACCESS_INSTANCED_PROP(Props, _Dissolve);
                float alpha = 1 * directionMask - mask.r - (dissolve * 1.5);
                alpha = step(.2, alpha);

                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                baseColor.a = alpha;
                return baseColor;
                
                //_BaseColor.a = alpha; // saturate(alpha);
                //return _BaseColor;
            }
            ENDHLSL
        }
    }
}