using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour
{
    [SerializeField] private GameObject particlePrefab;

    [SerializeField] private Vector3[] positions;
    [SerializeField] private Vector3[] velocities;
    [SerializeField] private GameObject[] objects;
    
    [SerializeField] private Vector3 simulationSize;

    [SerializeField] private uint nParticles;

    private void Start()
    {
        InitializeParticles();
        UpdateParticles();
    }

    private void InitializeParticles()
    {
        positions = new Vector3[nParticles];
        velocities = new Vector3[nParticles];
        objects = new GameObject[nParticles];
        
        GameObject particles = new GameObject();
        particles.name = "Particles";

        for (int i = 0; i < nParticles; i++)
        {
            float randomX = Random.Range(0.0f, simulationSize.x);
            float randomY = Random.Range(0.0f, simulationSize.y);
            
            positions[i] = new Vector3(randomX, randomY, 0.0f);
            
            objects[i] = Instantiate(particlePrefab);
            objects[i].gameObject.transform.parent = particles.transform;
        }
    }

    private void UpdateParticles()
    {
        for (int i = 0; i < nParticles; i++)
        {
            objects[i].transform.position = positions[i];
        }
    }
}
