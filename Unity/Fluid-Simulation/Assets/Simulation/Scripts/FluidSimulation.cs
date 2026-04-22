using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation.Scripts
{
    public class FluidSimulation : MonoBehaviour
    {
        // 
        public GameObject particlePrefab;
        
        // Simulation settings
        [SerializeField] private int nParticles = 5;

        // Array to hold particle data
        public Particle[] Particles;
        
        public void Start()
        {
            // Create Particles
            Particles = new Particle[nParticles];
            for (int i = 0; i < nParticles; i++)
            {
                Particles[i] = CreateParticle();;
            }
        }

        public void Update()
        {
            for (int i = 0; i < nParticles; i++)
            {
                Particle particle = Particles[i];
                particle.Velocity += new Vector3(0.0f, -0.5f, 0.0f) * Time.deltaTime * 6;
                
                // Update position
                particle.Position += particle.Velocity * Time.deltaTime;
                particle.GameObject.transform.position = particle.Position;
                // Write back to the array of particles
                Particles[i] = particle;
            }
        }
        
        // Returns an initialized instance of a particle
        private Particle CreateParticle()
        {
            Particle particle = new Particle
            {
                GameObject = Instantiate(particlePrefab, Vector3.zero, Quaternion.identity),
                Position = new Vector3(Random.Range(0, 5), Random.Range(0, 5), Random.Range(0, 0)),
                Velocity = Vector3.zero
            };
            return particle;
        }
        
      
    }
}

