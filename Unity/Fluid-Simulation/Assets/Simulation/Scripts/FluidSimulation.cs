using System;
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

        private Particle[] particles;
        
        public void Start()
        {
            particles = SpawnRandomParticles();
        }

        public void Update()
        {
            float density = CalculateDensity(point, particles);
            Debug.Log(density);
        }

        public float CalculateDensity(Vector3 samplePoint, Particle[] particles)
        {
            float density = 0;
            const float mass = 1;

            for (int i = 0; i < particles.Length; i++)
            {
                float distance = (particles[i].Position - samplePoint).magnitude;
                float influence = utils.SmoothingKernel(smoothingRadius, distance);
                density += mass * influence;
            }

            return density;
        }

        private void DrawSmoothingKernel()
        {
            float[] values = new float[nParticles];;
            for (int i = 0; i < nParticles; i++)
            {
                float distance = i / (float)nParticles * smoothingRadius;
                values[i] = utils.SmoothingKernel(smoothingRadius, distance) * pressureMultiplier;
            }
            utils.DrawGraph(nParticles, radius, values);
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

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(point, smoothingRadius);
        }
    }
}

