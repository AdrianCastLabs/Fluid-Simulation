using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour
{
    [SerializeField] private GameObject particlePrefab;

    [SerializeField] private Vector3[] positions;
    [SerializeField] private Vector3[] velocities;
    [SerializeField] private float[] densities;
    [SerializeField] private GameObject[] objects;
    
    [SerializeField] private Vector3 simulationSize;
    [SerializeField] private float smoothingRadius;

    [SerializeField] private uint nParticles;
    [SerializeField] private float particleRadius;
    [SerializeField] private float mass;

    private float PI = math.PI;

    private void Start()
    {
        InitializeParticles();
        UpdateParticles();
        
    }

    private void Update()
    {
        UpdateParticles();
        ResolveParticleCollisions();
        ResolveBoundaryCollisions();
        ComputeDensities();

        if (Input.GetMouseButtonDown(0))
        {
            print(CalculateDensity(positions[1]));
        }
    }

    private void InitializeParticles()
    {
        positions = new Vector3[nParticles];
        velocities = new Vector3[nParticles];
        
        densities = new float[nParticles];
        objects = new GameObject[nParticles];
        
        GameObject particles = new GameObject();
        particles.name = "Particles";

        for (int i = 0; i < nParticles; i++)
        {
            float posX = Random.Range(-simulationSize.x, simulationSize.x);
            float posY = Random.Range(-simulationSize.y, simulationSize.y);
            
            float velX = Random.Range(-simulationSize.x, simulationSize.x);
            float velY = Random.Range(-simulationSize.y, simulationSize.y);
            
            positions[i] = new Vector3(posX, posY, 0.0f);
            velocities[i] = new Vector3(velX, velY, 0.0f) * 0.1f;
            
            objects[i] = Instantiate(particlePrefab);
            objects[i].gameObject.transform.parent = particles.transform;
        }
    }

    private void UpdateParticles()
    {
        for (int i = 0; i < nParticles; i++)
        {
            positions[i] += velocities[i] * Time.deltaTime;
            positions[i] = math.clamp(positions[i], -simulationSize, simulationSize);
            
            objects[i].transform.position = positions[i];
            objects[i].transform.localScale = Vector3.one * particleRadius * 5.7f;
            objects[i].GetComponent<Renderer>().material.color = Color.HSVToRGB(math.min(1.0f, densities[i] / 300), 1.0f, 1.0f);
        }
    }

    private void ResolveParticleCollisions()
    {
        float damping = 0.9f;
        for (int i = 0; i < nParticles; i++)
        {
            for (int j = i + 1; j < nParticles; j++)
            {
                if (i == j) continue;
                
                Vector3 direction = positions[j] - positions[i];
                float distance = direction.magnitude;
                
                if (distance <= 0.001f || distance > particleRadius) continue;

                Vector3 normal = direction / distance;
                float overlap = distance - particleRadius;

                positions[i] += normal * overlap * 0.5f;
                positions[j] -= normal * overlap * 0.5f;

                Vector3 vi = velocities[i];
                Vector3 vj = velocities[j];

                float viN = Vector3.Dot(vi, normal);
                float vjN = Vector3.Dot(vj, normal);

                float impulse = vjN - viN * damping;

                velocities[i] += impulse * normal;
                velocities[j] -= impulse * normal;
            }
        }
    }

    private void ResolveBoundaryCollisions()
    {
        float damping = 0.9f;
        for (int i = 0; i < nParticles; i++)
        {
            if (positions[i].x >= simulationSize.x)
            {
                velocities[i].x *= -1 * damping;
            }
            
            if (positions[i].x <= -simulationSize.x)
            {
                velocities[i].x *= -1 * damping;
            }
            
            if (positions[i].y >= simulationSize.y)
            {
                velocities[i].y *= -1 * damping;
            }
            
            if (positions[i].y <= -simulationSize.y)
            {
                velocities[i].y *= -1 * damping;
            }
        }
    }

    private float SmoothingKernel(float distance, float radius)
    {
        float value = Math.Max(0, radius - distance);
        return value * value * value;
    }

    private float CalculateDensity(Vector3 position)
    {
        float density = 0;

        for (int i = 0; i < nParticles; i++)
        {
            float distance = (position - positions[i]).magnitude;
            density += mass * SmoothingKernel(distance, smoothingRadius);
        }

        return density;
    }

    private void ComputeDensities()
    {
        for (int i = 0; i < nParticles; i++)
        {
            densities[i] = CalculateDensity(positions[i]);
        }
    }
    
}
