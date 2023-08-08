Shader "Custom/VertexColors" {
	Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor("BaseColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipline" = "UniversalRenderPipline" "RenderType"="Opaque" }
        LOD 200
        //替换为HLSLINCLUDE
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct a2v
        {
            float4 positionOS:POSITION;
            float4 normalOS:NORMAL;
            float2 texcoord:TEXCOORD;
        };

        struct v2f
        {
            float4 positionCS:SV_POSITION;
            float2 texcoord:TEXCOORD;
        };

        CBUFFER_START(UnityPerMaterial)

        float4 _MainTex_ST;
        float4 _BaseColor;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        ENDHLSL


        Pass
        {
           HLSLPROGRAM
           #pragma vertex Vert
           #pragma fragment Frag

           v2f Vert(a2v i)
           {
               v2f o;
               o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
               o.texcoord = TRANSFORM_TEX(i.texcoord,_MainTex);
               return o;
           }

           half4 Frag(v2f i):SV_TARGET
           {
               half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,i.texcoord)*_BaseColor;
               return _BaseColor;
           }
           ENDHLSL

        }
    }
}