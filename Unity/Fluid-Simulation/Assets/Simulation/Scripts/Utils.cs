using UnityEngine;
using System;

namespace Simulation.Scripts
{
    public class Utils : MonoBehaviour
    {
        public GameObject particlePrefab;

        public void DrawGraph(int nParticles, float radius, float[] values)
        {
            // Create Particles
            Particle[] particles = CreateParticles(nParticles, 1);
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].Position = new Vector3(i, values[i], 0);
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
            if (distance >= radius) return 0;

            float volume = (float)Math.PI * (float)Math.Pow(radius, 4) / 6;
            return (radius - distance) * (radius - distance) / volume;
        }

        public float SmoothingKernelDerivative(float distance, float radius)
        {
            if (distance >= radius) return 0;
            float f = radius * radius - distance * distance;
            float scale = -24 / ((float)Math.PI * (float)Math.Pow(radius, 8));
            return scale * distance * f * f;
        }

        public void SetPressureColor(Particle particle, float pressure)
        {
            float t = Mathf.InverseLerp(0f, 50f, pressure); // min, max, value
            float hue = 1f - t;
            
            Renderer renderer = particle.GameObject.GetComponent<Renderer>();
            renderer.material.color = Color.HSVToRGB(hue / 1.2f, 1f, 1f);
        }
    }
}

