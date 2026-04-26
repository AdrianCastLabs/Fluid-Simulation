Shader "Custom/ParticleInstancedShader"
{
    Properties
    {
        _Radius ("Radius", Float) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct GPUParticle
            {
                float3 position;
                float3 velocity;
                float3 force;
                float density;
            };

            StructuredBuffer<GPUParticle> particles;
            float _Radius;

            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float pressure : TEXCOORD0;
            };

            float ConvertDensityToPressure(float density)
            {
                return (density - 1.0) * 1.0; // Using default values
            }

            v2f vert(appdata v)
            {
                v2f o;
                
                GPUParticle particle = particles[v.instanceID];
                
                // Scale and position the mesh
                float4 worldPos = float4(particle.position, 1.0);
                worldPos.xyz += v.vertex.xyz * _Radius;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.pressure = ConvertDensityToPressure(particle.density);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Color based on pressure
                float t = saturate(i.pressure / 16.0); // Adjust range as needed
                float hue = 1.0 - t;
                
                // Simple HSV to RGB conversion
                float3 rgb = saturate(float3(
                    abs(hue * 6.0 - 3.0) - 1.0,
                    2.0 - abs(hue * 6.0 - 2.0),
                    2.0 - abs(hue * 6.0 - 4.0)
                ));
                
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}