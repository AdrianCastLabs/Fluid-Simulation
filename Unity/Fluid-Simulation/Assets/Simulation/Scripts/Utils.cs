using UnityEngine;
using System;

namespace Simulation.Scripts
{
    public class Utils : MonoBehaviour
    {
        public GameObject particlePrefab;

        public void DrawGraph(int nParticles, float radius, float y)
        {
            // Create Particles
            Particle[] particles = CreateParticles(5, 1);
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].Position = new Vector3(i, y * i, 0);
                UpdateParticles(particles, radius);
            }
        }

        public Particle[] CreateParticles(int nParticles, float radius)
        {
            // Create Particles
            Particle[] Particles;
            Particles = new Particle[nParticles];
            for (int i = 0; i < nParticles; i++)
            {
                Particles[i] = CreateParticle();
                Particles[i].Position = new Vector3(0, 0, 0);
            }

            return Particles;
        }

        public void UpdateParticles(Particle[] particles, float radius)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].GameObject.transform.position = particles[i].Position;
                particles[i].GameObject.transform.localScale = Vector3.one * radius;
            }
        }
        
        public Particle CreateParticle()
        {
            Particle particle = new Particle
            {
                GameObject = Instantiate(particlePrefab, Vector3.zero, Quaternion.identity),
                Position = Vector3.zero,
                Velocity = Vector3.zero
            };
            return particle;
        }
        
        public float SmoothingKernel(float radius, float distance)
        {
            float value = Math.Max(0, radius - distance);
            return value * value * value;
        }
    
    }
}

