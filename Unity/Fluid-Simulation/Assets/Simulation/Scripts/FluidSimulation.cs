using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation.Scripts
{
    public class FluidSimulation : MonoBehaviour
    {
        // Utility functions
        public Utils utils;
        // Simulation settings
        [SerializeField] private Vector2 simulationSize = new Vector2(10.0f, 10.0f);
        [SerializeField] private int nParticles = 200;
        [SerializeField] private float radius = 0.5f;
        // fluid settings
        [SerializeField] private float targetDensity = 1.0f;
        [SerializeField] private float pressureMultiplier = 1.0f;
        [SerializeField] private float smoothingRadius = 1.0f;
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float timeStep = 0.02f;
        [SerializeField] private float gravity = 1.0f;
        
        private Particle[] particles;
        
        private void Start()
        {
            particles = SpawnRandomParticles();
        }

        private void Update()
        {
            // compute densities
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].Density = ComputeDensity(particles[i].Position);
            }
            
            // calculate pressure
            for (int i = 0; i < nParticles; i++)
            {
                Vector3 pressureForce = CalculatePressureForce(i);
                float density = Mathf.Max(particles[i].Density, 0.0001f);
                Vector3 pressureAcceleration = pressureForce / density;
                particles[i].Velocity += pressureAcceleration * timeStep;
            }

            HandleCollisions();
            
            // update positions
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].Velocity *= 0.999f;
                particles[i].Velocity.y -= 1 * gravity * timeStep; 
                particles[i].Position += particles[i].Velocity * timeStep;

                float damping = 0.5f;
                if (particles[i].Position.x <= 0.0f || particles[i].Position.x >= simulationSize.x)
                {
                    particles[i].Velocity.x *= -damping;
                    particles[i].Position.x = Mathf.Clamp(particles[i].Position.x, 0.0f, simulationSize.x);
                }
                if (particles[i].Position.y <= 0.0f || particles[i].Position.y >= simulationSize.y)
                {
                    particles[i].Velocity.y *= -damping;
                    particles[i].Position.y = Mathf.Clamp(particles[i].Position.y, 0.0f, simulationSize.y);
                }

                particles[i].GameObject.transform.position = particles[i].Position;
                utils.SetPressureColor(particles[i], ConvertDensityToPressure(particles[i].Density));

            }
        }
        
        private void HandleCollisions()
        {
            float minDistance = radius * 2.0f;
            float restitution = 0.2f;

            for (int i = 0; i < nParticles; i++)
            {
                for (int j = i + 1; j < nParticles; j++)
                {
                    Vector3 delta = particles[j].Position - particles[i].Position;
                    float distSquared = delta.sqrMagnitude;
                    float minDistSquared = minDistance * minDistance;

                    if (distSquared < minDistSquared && distSquared > 0.0001f)
                    {
                        float distance = Mathf.Sqrt(distSquared);
                        Vector3 normal = delta / distance;

                        // Separate overlapping particles
                        float overlap = minDistance - distance;
                        particles[i].Position -= normal * overlap * 0.5f;
                        particles[j].Position += normal * overlap * 0.5f;

                        // Impulse-based collision response
                        Vector3 relativeVelocity = particles[i].Velocity - particles[j].Velocity;
                        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

                        if (velAlongNormal > 0)
                        {
                            float impulseMagnitude = -(1.0f + restitution) * velAlongNormal * 0.5f;
                            Vector3 impulse = normal * impulseMagnitude;

                            particles[i].Velocity += impulse;
                            particles[j].Velocity -= impulse;
                        }
                    }
                }
            }
        }

        private float ComputeDensity(Vector3 samplePoint)
        {
            float density = 0;

            for (int i = 0; i < nParticles; i++)
            {
                float distance = (particles[i].Position - samplePoint).magnitude;
                float influence = utils.SmoothingKernel(smoothingRadius, distance);
                density += mass * influence;
            }

            return density;
        }

        private Vector3 CalculatePressureForce(int particleIndex)
        {
            Vector3 pressureForce = Vector3.zero;

            for (int otherParticleIndex = 0; otherParticleIndex < nParticles; otherParticleIndex++)
            {
                if (particleIndex == otherParticleIndex) continue;
                
                Vector3 offset = particles[otherParticleIndex].Position - particles[particleIndex].Position;
                float distance = offset.magnitude;
                if (distance == 0) continue;
                Vector3 direction = offset / distance;
                
                float slope = utils.SmoothingKernelDerivative(smoothingRadius, distance);
                float density = particles[otherParticleIndex].Density;
                float sharedPressure = CalculateSharedPressure(density, particles[particleIndex].Density);
                pressureForce += sharedPressure * direction * slope * mass / density;
            }

            return pressureForce;
        }

        private float CalculateSharedPressure(float densityA, float densityB)
        {
            float pressureA = ConvertDensityToPressure(densityA);
            float pressureB = ConvertDensityToPressure(densityB);
            return (pressureA + pressureB) / 2;
        }

        private float ConvertDensityToPressure(float density)
        {
            float pressure = density - targetDensity;
            pressure *= pressureMultiplier;
            return pressure;
        }
        
        private Particle[] SpawnRandomParticles()
        {
            Particle[] particles =  utils.CreateParticles(nParticles, radius);
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].Position = new Vector3(Random.Range(0, simulationSize.x), Random.Range(0, simulationSize.y), 0);
            }
            utils.UpdateParticles(particles, radius);
            return particles;
        }
    }
}

