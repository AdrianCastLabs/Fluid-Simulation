using UnityEngine;
using System;

namespace Simulation.Scripts
{
    public class Utils : MonoBehaviour
    {
        public GameObject particlePrefab;

        public void DrawGraph(int nParticles, float radius)
        {
            // Create Particles
            Particle[] particles = CreateParticles(5, 1);
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
                Particles[i].GameObject.transform.position = Particles[i].Position;
                Particles[i].GameObject.transform.localScale = Vector3.one * radius;
            }

            return Particles;
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

