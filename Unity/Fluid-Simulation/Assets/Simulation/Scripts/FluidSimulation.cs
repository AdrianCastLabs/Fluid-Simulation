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
        [SerializeField] private float targetDensity = 1.0f;
        
        private Particle[] particles;
        
        public void Start()
        {
            particles = SpawnRandomParticles();

            foreach (Particle particle in particles)
            {
                float density = ComputeDensity(particle.Position);
                float pressure = ComputePressure(density, targetDensity);
                
                utils.SetPressureColor(particle, pressure);
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

