Shader "Custom/ParticleShader"
{
    Properties
    {
        _Radius ("Radius", float) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque"}
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
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                
                GPUParticle particle = particles[v.instanceID];
                
                // Scale and position the mesh
                float4 worldPos = float4(particle.position, 1.0);
                worldPos.xyz += v.vertex.xyz * _Radius;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                
                return o;
                
            }
            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(1.0, 1.0, 1.0, 0.0);
            }
            ENDCG
        }
    }
}









