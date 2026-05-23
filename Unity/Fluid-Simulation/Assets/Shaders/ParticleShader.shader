Shader "Custom/ParticleShader"
{
    Properties
    {
        _Radius ("Radius", Float) = 0.5
        _MaxSpeed ("Max Speed", Float) = 10.0
        _MinSpeed ("Min Speed", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM

             #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5
             
            #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            StructuredBuffer<float3> positions;
            StructuredBuffer<float3> velocities; 
             
            float _Radius;
            float _MaxSpeed;
            float _MinSpeed;
             
             struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float speed : TEXCOORD0;
            };
             
             v2f vert(appdata v)
            {
                v2f o;
                
                float4 worldPos = float4(positions[v.instanceID], 1.0);
                worldPos.xyz += v.vertex.xyz * _Radius;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.speed = length(velocities[v.instanceID]);
                
                return o;
            }

            float3 Heatmap(float t)
            {
                t = saturate(t);

                float3 c1 = float3(0.0, 0.0, 1.0); 
                float3 c2 = float3(0.0, 1.0, 1.0); 
                float3 c3 = float3(1.0, 1.0, 0.0); 
                float3 c4 = float3(1.0, 0.0, 0.0); 

                if (t < 0.33)
                    return lerp(c1, c2, t / 0.33);
                else if (t < 0.66)
                    return lerp(c2, c3, (t - 0.33) / 0.33);
                else
                    return lerp(c3, c4, (t - 0.66) / 0.34);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = (i.speed - _MinSpeed) / (_MaxSpeed - _MinSpeed);
                t = smoothstep(0.0, 1.0, t);
                float3 col = Heatmap(t);
                col *= lerp(0.6, 1.5, t);
                return float4(col, 1.0);
            }

           
            ENDHLSL
        }
    }
}
