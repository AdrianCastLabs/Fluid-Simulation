using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject particlePrefab;
    
    [Header("Simulation Data")]
    [SerializeField] private Vector3[] positions;
    [SerializeField] private Vector3[] velocities;
    [SerializeField] private float[] densities;
    [SerializeField] private GameObject[] objects;
    
    [Header("Simulation Settings")]
    [SerializeField] private Vector3 simulationSize;
    [SerializeField] private float smoothingRadius;
    [SerializeField] private uint nParticles;
    [SerializeField] private float particleRadius;
    [SerializeField] private float mass;
    [SerializeField] private float targetDensity;
    [SerializeField] private float pressureMultiplier;
    [SerializeField] private float dt = 0.02f;

    [Header("Gizmos")]
    [SerializeField] private bool viewSmoothingRadius;

    private float PI = math.PI;

    private void Start()
    {
        InitializeParticles();
        UpdateParticles();
        
    }

    private void Update()
    {
        SimulationStep(dt);
        ResolveParticleCollisions();
        ResolveBoundaryCollisions();
        UpdateParticles();
    }

    private void SimulationStep(float deltaTime)
    {
        // compute Densities
        for (int i = 0; i < nParticles; i++)
        {
            densities[i] = CalculateDensity(positions[i]);
        }
        
        // compute pressure
        for (int i = 0; i < nParticles; i++)
        {
            Vector3 pressureForce = CalculatePressureForce(i);
            Vector3 pressureAcceleration = pressureForce / densities[i];
            velocities[i] += pressureAcceleration * deltaTime;
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
            positions[i] += velocities[i] * dt;
            positions[i] = math.clamp(positions[i], -simulationSize, simulationSize);
            
            objects[i].transform.position = positions[i];
            objects[i].transform.localScale = Vector3.one * particleRadius * 5.7f;
            objects[i].GetComponent<Renderer>().material.color = Color.HSVToRGB(math.min(1.0f, densities[i] / 30), 1.0f, 1.0f);
        }
    }

    private void ResolveParticleCollisions()
    {
        float damping = 0.9f;
        for (int i = 0; i < nParticles; i++)
        {
            for (int j = i + 1; j < nParticles; j++)
            {
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
                
                velocities[i] += (vjN - viN) * damping * normal;
                velocities[j] += (viN - vjN) * damping * normal;
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

    // uses the poly6 smoothing kernel
    private float Poly6Kernel(float distance, float radius)
    {
        if (0.0f <= distance && distance <= radius)
        {
            float constant = 315 / ((64 * PI) * math.pow(smoothingRadius, 9));
            return math.pow((radius * radius) - (distance * distance), 3) * constant;
        }

        return 0.0f;
    }

    // returns the gradient of the spiky kernel
    private float SpikyKernelDerivative(float distance, float radius)
    {
        if (distance <= radius)
        {
            float constant = -15f / (PI * math.pow(radius, 6));
            float value = radius - distance;

            return constant * value * value * value;
        }

        return 0.0f;
    }
    

    // returns the calculated density on a given sample point
    private float CalculateDensity(Vector3 position)
    {
        float density = 0;

        for (int i = 0; i < nParticles; i++)
        {
            float distance = (position - positions[i]).magnitude;
            density += mass * Poly6Kernel(distance, smoothingRadius);
        }

        return density;
    }

    private float CalculatePressure(float density)
    {
        return (density - targetDensity) * pressureMultiplier;
    }

    private Vector3 CalculatePressureForce(int particleIndex)
    {
        Vector3 pressureForce = Vector3.zero;

        for (int otherParticleIndex = 0; otherParticleIndex < nParticles; otherParticleIndex++)
        {
            if (particleIndex == otherParticleIndex) continue;

            Vector3 offset = positions[otherParticleIndex] - positions[particleIndex];
            float distance = offset.magnitude;
            Vector3 direction = distance == 0 ? Random.insideUnitSphere : offset / distance;
            
            float slope = SpikyKernelDerivative(distance, smoothingRadius);
            float density = densities[otherParticleIndex];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            pressureForce += sharedPressure * direction * slope * mass / density;
        }

        return pressureForce;
    }

    float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = CalculatePressure(densityA);
        float pressureB = CalculatePressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    private void OnDrawGizmos()
    {
        if (viewSmoothingRadius)
            Gizmos.DrawWireSphere(positions[1], smoothingRadius);
    }
}
