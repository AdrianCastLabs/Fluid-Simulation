using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class GPUSimulationManager : MonoBehaviour
{
    [Header("ComputeShader")]
    [SerializeField] private ComputeShader computeShader;
    
    [Header("Rendering Settings")]
    [SerializeField] private Mesh mesh;

    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material material;
    
    [Header("Simulation Settings")]
    [SerializeField] private float gravity;
    [SerializeField] private int nParticles;
    [SerializeField] private float particleRadius;
    [SerializeField] private Vector3 simulationSize;
    [SerializeField] private float smoothingRadius;
    [SerializeField] private float mass;
    [SerializeField] private float targetDensity;
    [SerializeField] private float pressureMultiplier;
    [SerializeField] private float dt;

    private int kernelComputeDensity;
    private int kernelComputePressureForce;
    private int kernelIntegrate;
    private int kernelPredictPositions;

    private Vector3[] predictedPositions;
    private Vector3[] positions;
    private Vector3[] velocities;
    private float[] densities;

    private ComputeBuffer predictedPositionsBuffer;
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer velocitiesBuffer;
    private ComputeBuffer densitiesBuffer;
    private ComputeBuffer argsBuffer;

    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds bounds;

    private Vector3[] a;
    
    private void Start()
    {
        InitializeComputeShader();
        InitializeParticles();
        InitializeRendering();
    }

    private void InitializeComputeShader()
    {
        kernelComputeDensity = computeShader.FindKernel("ComputeDensity");
        kernelComputePressureForce = computeShader.FindKernel("ComputePressureForce");
        kernelIntegrate = computeShader.FindKernel("Integrate");
        kernelPredictPositions = computeShader.FindKernel("PredictPositions");
    }

    private void InitializeParticles()
    {
        predictedPositions = new Vector3[nParticles];
        positions = new Vector3[nParticles];
        velocities = new Vector3[nParticles];
        densities = new float[nParticles];
        a = new Vector3[nParticles];

        for (int i = 0; i < nParticles; i++)
        {
            positions[i] = new Vector3(
                Random.Range(-simulationSize.x, simulationSize.x),
                Random.Range(-simulationSize.y, simulationSize.y),
                Random.Range(-simulationSize.z, simulationSize.z)
            );
            
            velocities[i] = new Vector3(
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f)
            );
            predictedPositions[i] = positions[i];
            densities[i] = 0.00001f;
            a[i] = Vector3.zero;
        }

        predictedPositionsBuffer = new ComputeBuffer(nParticles, sizeof(float) * 3);
        predictedPositionsBuffer.SetData(predictedPositions);

        positionsBuffer = new ComputeBuffer(nParticles, sizeof(float) * 3);
        positionsBuffer.SetData(positions);
        
        velocitiesBuffer = new ComputeBuffer(nParticles, sizeof(float) * 3);
        velocitiesBuffer.SetData(velocities);

        densitiesBuffer = new ComputeBuffer(nParticles, sizeof(float));
        densitiesBuffer.SetData(densities);

        
        computeShader.SetBuffer(kernelComputeDensity, "positions",  positionsBuffer);
        computeShader.SetBuffer(kernelComputeDensity, "densities", densitiesBuffer);
        computeShader.SetBuffer(kernelComputeDensity, "predictedPositions", predictedPositionsBuffer);
        
        computeShader.SetBuffer(kernelComputePressureForce, "densities", densitiesBuffer);
        computeShader.SetBuffer(kernelComputePressureForce, "velocities", velocitiesBuffer);
        computeShader.SetBuffer(kernelComputePressureForce, "predictedPositions", predictedPositionsBuffer);
        
        computeShader.SetBuffer(kernelIntegrate, "positions", positionsBuffer);
        computeShader.SetBuffer(kernelIntegrate, "velocities", velocitiesBuffer);
        
        computeShader.SetBuffer(kernelPredictPositions, "predictedPositions", predictedPositionsBuffer);
        computeShader.SetBuffer(kernelPredictPositions, "positions", positionsBuffer);
        computeShader.SetBuffer(kernelPredictPositions, "velocities", velocitiesBuffer);
        
        SetComputeShaderParameters();
    }

    private void SetComputeShaderParameters()
    {
        computeShader.SetFloat("gravity", gravity);
        computeShader.SetInt("nParticles", nParticles);
        computeShader.SetFloat("smoothingRadius", smoothingRadius);
        computeShader.SetFloat("mass", mass);
        computeShader.SetFloat("targetDensity", targetDensity);
        computeShader.SetFloat("pressureMultiplier", pressureMultiplier);
        computeShader.SetFloat("dt", dt);
        computeShader.SetVector("simulationSize", simulationSize);
    }

    private void InitializeRendering()
    {
        // setup indirect rendering
        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)nParticles;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        
        // setup material
        material.SetBuffer("positions", positionsBuffer);
        material.SetFloat("_Radius", particleRadius);
        
        // set bounds for culling
        bounds = new Bounds(
            Vector3.zero,
            Vector3.one * 1000.0f
        );
    }

    private void Update()
    {
        SetComputeShaderParameters();
        RunComputeShader();
        RenderParticles();
        
        velocitiesBuffer.GetData(a);
        
        print(a[5]);
        
    }

    private void RunComputeShader()
    {
        int threadGroups = Mathf.CeilToInt(nParticles / 64f);
        
        computeShader.Dispatch(kernelPredictPositions, threadGroups, 1, 1);
        computeShader.Dispatch(kernelComputeDensity, threadGroups, 1, 1);
        computeShader.Dispatch(kernelComputePressureForce, threadGroups, 1, 1);
        computeShader.Dispatch(kernelIntegrate, threadGroups, 1 ,1);
    }

    private void RenderParticles()
    {
        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            material,
            bounds,
            argsBuffer
        );
    }

    private void OnDestroy()
    {
        densitiesBuffer?.Release();
        positionsBuffer?.Release();
        velocitiesBuffer?.Release();
        predictedPositionsBuffer?.Release();
        argsBuffer?.Release();
    }
}