Shader "Custom/LensStencilWrite"
{
    // レンズ形状でステンシルバッファに書き込むシェーダー
    // 実際の描画はせず、ステンシル値のみを設定
    // SpineStencilMasked と組み合わせて使用

    Properties
    {
        _StencilRef ("Stencil Reference", Int) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Geometry+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StencilWrite"

            // ステンシル設定
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            // 色は書き込まない（ステンシルのみ）
            ColorMask 0
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 色は出力しない（ステンシルバッファへの書き込みのみ）
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
