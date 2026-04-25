using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation.Scripts
{
    public class FluidSimulation : MonoBehaviour
    {
        public Utils utils;
        
        // Particle object
        public GameObject particlePrefab;
        
        // Simulation settings
        [SerializeField] private Vector2 simulationSize = new Vector2(10.0f, 10.0f);
        [SerializeField] private int nParticles = 5;
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private float smoothingRadius = 1.0f;
        [SerializeField] private float pressureMultiplier = 1.0f;

        [SerializeField] private Vector3 point = new Vector3(0, 0, 0);
        [SerializeField] private float targetDensity = 0.0f;
        
        private Particle[] particles;
        
        public void Start()
        {
            particles = SpawnRandomParticles();

            
        }

        private void Update()
        {
            for (int i = 0; i < nParticles; i++)
            {
                Particle particle = particles[i];
                particle.Density = ComputeDensity(particle.Position);
                particle.Pressure = ComputePressure(particle.Density, targetDensity);
                Vector3 force = Vector3.zero;
                for (int j = 0; j < nParticles; j++)
                {
                    if (i == j) continue;
                    Particle otherParticle = particles[j];
                    Vector3 direction = (otherParticle.Position - particle.Position);
                    float distance = direction.magnitude;
                    direction /= distance + 0.001f;
                    float strength = (particle.Pressure + otherParticle.Pressure);
                    float fallof = utils.SmoothingKernelDerivative(smoothingRadius, distance);
                    force += direction * strength * fallof;
                }
                
                particle.Velocity += force * Time.deltaTime;
                particle.Position += particle.Velocity * Time.deltaTime;

                particle.Position.x = Mathf.Clamp(particle.Position.x, 0, simulationSize.x);
                particle.Position.y = Mathf.Clamp(particle.Position.y, 0, simulationSize.y);
                particle.Position.z = Mathf.Clamp(particle.Position.z, 0, simulationSize.y);

                particle.GameObject.transform.position = particle.Position;

                particles[i] = particle;
                utils.SetPressureColor(particle, particle.Pressure);
            }
        }

        private float ComputeDensity(Vector3 samplePoint)
        {
            float density = 0;
            const float mass = 1;

            for (int i = 0; i < nParticles; i++)
            {
                float distance = (particles[i].Position - samplePoint).magnitude;
                float influence = utils.SmoothingKernel(smoothingRadius, distance);
                density += mass * influence;
            }

            return density;
        }

        private float ComputePressure(float currentDensity, float targetDensity)
        {
            return currentDensity - targetDensity;
        }
        
        private Particle[] SpawnRandomParticles()
        {
            Particle[] particles =  utils.CreateParticles(nParticles, radius);
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].Position = new Vector3(Random.Range(0, simulationSize.x), Random.Range(0, simulationSize.y), 0);
                utils.UpdateParticles(particles, radius);
            }

            return particles;
        }
    }
}

