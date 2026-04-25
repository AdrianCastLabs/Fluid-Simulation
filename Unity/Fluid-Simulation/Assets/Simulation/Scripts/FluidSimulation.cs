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
        [SerializeField] private int nParticles = 5;
        [SerializeField] private float radius = 0.5f;
        // fluid settings
        [SerializeField] private float targetDensity = 1.0f;
        [SerializeField] private float pressureMultiplier = 1.0f;
        [SerializeField] private float smoothingRadius = 1.0f;
        [SerializeField] private float mass = 1.0f;
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
                Vector3 pressureForce = CalculatePressureForce(particles[i].Position);
                Vector3 pressureAcceleration = pressureForce / particles[i].Density;
                particles[i].Velocity += pressureAcceleration * Time.deltaTime;
            }
            
            // update positions
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].Position += particles[i].Velocity * Time.deltaTime;

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

        private Vector3 CalculatePressureForce(Vector3 samplePoint)
        {
            Vector3 pressureForce = Vector3.zero;

            for (int i = 0; i < nParticles; i++)
            {
                float distance = (particles[i].Position - samplePoint).magnitude;
                Vector3 direction = (particles[i].Position - samplePoint) / (distance + 0.1f);
                float slope = utils.SmoothingKernelDerivative(smoothingRadius, distance);
                float density = particles[i].Density;
                float sharedPressure = CalculateSharedPressure(density, particles[i].Density);
                pressureForce += -sharedPressure * direction * slope * mass / density;
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
                utils.UpdateParticles(particles, radius);
            }

            return particles;
        }
    }
}

