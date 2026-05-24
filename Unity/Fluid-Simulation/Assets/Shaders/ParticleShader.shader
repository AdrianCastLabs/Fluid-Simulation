Shader "Custom/BillboardParticles"
{
    Properties
    {
        _Size ("Size", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"

            StructuredBuffer<float3> positions;

            float _Size;

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;

                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 particlePos =
                    positions[v.instanceID];

                // Camera right vector
                float3 right =
                    UNITY_MATRIX_V[0].xyz;

                // Camera up vector
                float3 up =
                    UNITY_MATRIX_V[1].xyz;

                float3 worldPos =
                    particlePos
                    + right * v.vertex.x * _Size
                    + up    * v.vertex.y * _Size;

                o.pos = mul(
                    UNITY_MATRIX_VP,
                    float4(worldPos, 1)
                );

                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Circle mask
                float2 centeredUV =
                    i.uv * 2 - 1;

                float dist =
                    dot(centeredUV, centeredUV);

                if (dist > 1)
                    discard;

                return float4(1,1,1,1);
            }

            ENDCG
        }
    }
}