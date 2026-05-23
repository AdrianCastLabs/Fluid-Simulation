using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class GPUSimulationManager : MonoBehaviour
{
    [Header("Compute Shader")]
    [SerializeField] private ComputeShader computeShader;

    [Header("Rendering")]
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    
    [Header("Simulation Settings")]
    [SerializeField] private Vector3 simulationSize;
    [SerializeField] private float smoothingRadius;
    [SerializeField] private float particleRadius;
    [SerializeField] private float pressureMultiplier;
    [SerializeField] private float targetDensity;
    [SerializeField] private int nParticles;
    [SerializeField] private float mass;
    [SerializeField] private float gravity;
    [SerializeField] private float dt = 0.02f;

    private int kernelPredictPositions;
    private int kernelComputeDensities;
    private int kernelComputePressureForces;
    private int kernelIntegrate;
    private int kernelHandleCollisions;
    
    private Vector3[] positions;
    private Vector3[] predictedPositions;
    private Vector3[] velocities;
    private float[] densities;

    private ComputeBuffer positionsBuffer;
    private ComputeBuffer velocitiesBuffer;
    private ComputeBuffer predictedPositionsBuffer;
    private ComputeBuffer densitiesBuffer;
    private ComputeBuffer argsBuffer;

    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds bounds;

    private void Start()
    {
       InitializeComputeShader();
       InitializeParticles();
       InitializeRendering();
    }

    private void InitializeComputeShader()
    {
        kernelPredictPositions = computeShader.FindKernel("PredictPositions");
        kernelComputeDensities = computeShader.FindKernel("ComputeDensities");
        kernelComputePressureForces = computeShader.FindKernel("ComputePressureForces");
        kernelIntegrate = computeShader.FindKernel("Integrate");
        kernelHandleCollisions = computeShader.FindKernel("HandleCollisions");
    }

    private void InitializeParticles()
    {
        positions = new Vector3[nParticles];
        predictedPositions = new Vector3[nParticles];
        velocities = new Vector3[nParticles];
        densities = new float[nParticles];
        
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
        }
        
        // create compute buffers
        positionsBuffer = new ComputeBuffer(nParticles, Marshal.SizeOf(typeof(Vector3)));
        velocitiesBuffer = new ComputeBuffer(nParticles, Marshal.SizeOf(typeof(Vector3)));
        predictedPositionsBuffer = new ComputeBuffer(nParticles, Marshal.SizeOf(typeof(Vector3)));
        densitiesBuffer = new ComputeBuffer(nParticles, Marshal.SizeOf(typeof(Vector3)));
        
        positionsBuffer.SetData(positions);
        velocitiesBuffer.SetData(velocities);
        predictedPositionsBuffer.SetData(predictedPositions);
        densitiesBuffer.SetData(densities);
        
        // set buffers to all kernels
        computeShader.SetBuffer(kernelPredictPositions, "predictedPositionsBuffer", predictedPositionsBuffer);
        computeShader.SetBuffer(kernelPredictPositions, "positionsBuffer", positionsBuffer);
        computeShader.SetBuffer(kernelPredictPositions, "velocitiesBuffer", velocitiesBuffer);
        
        computeShader.SetBuffer(kernelComputeDensities, "densitiesBuffer", densitiesBuffer);
        computeShader.SetBuffer(kernelComputeDensities, "positionsBuffer", positionsBuffer);
        computeShader.SetBuffer(kernelComputeDensities, "predictedPositionsBuffer", predictedPositionsBuffer);
        
        computeShader.SetBuffer(kernelComputePressureForces, "densitiesBuffer", densitiesBuffer);
        computeShader.SetBuffer(kernelComputePressureForces, "velocitiesBuffer", velocitiesBuffer);
        computeShader.SetBuffer(kernelComputePressureForces, "predictedPositionsBuffer", velocitiesBuffer);

        
        computeShader.SetBuffer(kernelIntegrate, "positionsBuffer", positionsBuffer);
        computeShader.SetBuffer(kernelIntegrate, "velocitiesBuffer", velocitiesBuffer);
        
        computeShader.SetBuffer(kernelHandleCollisions, "positionsBuffer", positionsBuffer);
        
        // set constant parameters
        SetComputeShaderParameters();
    }
    
    private void SetComputeShaderParameters()
    {
        computeShader.SetInt("nParticles", nParticles);
        computeShader.SetFloat("smoothingRadius", smoothingRadius);
        computeShader.SetFloat("mass", mass);
        computeShader.SetFloat("targetDensity", targetDensity);
        computeShader.SetFloat("pressureMultiplier", pressureMultiplier);
        computeShader.SetFloat("dt", dt);
        computeShader.SetFloat("gravity", gravity);
        computeShader.SetVector("simulationSize", simulationSize);
        computeShader.SetFloat("particleRadius", particleRadius);
    }

    private void InitializeRendering()
    {
        // setup indirect rendering
        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)nParticles;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        
        // set buffers to material
        material.SetBuffer("positions", positionsBuffer);
        material.SetBuffer("velocities", velocitiesBuffer);
        material.SetFloat("_Radius", particleRadius);
        
        // set bounds for culling
        bounds = new Bounds(
            new Vector3(simulationSize.x / 2, simulationSize.y / 2, simulationSize.z / 2),
            new Vector3(simulationSize.x, simulationSize.y, simulationSize.z)
        );
    }

    private void Update()
    {
        RunSimulation();
        RenderParticles();
        SetComputeShaderParameters();
    }

    private void RunSimulation()
    {
        int threadGroups = Mathf.CeilToInt(nParticles / 64f);
        
        computeShader.Dispatch(kernelPredictPositions, threadGroups, 1, 1);
        computeShader.Dispatch(kernelComputeDensities, threadGroups, 1, 1);
        computeShader.Dispatch(kernelComputePressureForces, threadGroups, 1, 1);
        computeShader.Dispatch(kernelIntegrate, threadGroups, 1, 1);
        computeShader.Dispatch(kernelHandleCollisions, threadGroups, 1 ,1);
    }
    
    private void RenderParticles()
    {
        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            material,
            bounds,
            argsBuffer,
            castShadows: UnityEngine.Rendering.ShadowCastingMode.Off
        );
    }
    
    private void OnDestroy()
    {
        positionsBuffer?.Release();
        velocitiesBuffer?.Release();
        predictedPositionsBuffer?.Release();
        densitiesBuffer?.Release();
        argsBuffer?.Release();
    }
}
