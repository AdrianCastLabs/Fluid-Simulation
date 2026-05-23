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

    private int kernelComputeGravity;

    private Vector3[] positions;

    private ComputeBuffer positionsBuffer;
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
        kernelComputeGravity = computeShader.FindKernel("ComputeGravity");
    }

    private void InitializeParticles()
    {
        positions = new Vector3[nParticles];

        for (int i = 0; i < nParticles; i++)
        {
            positions[i] = new Vector3(
                Random.Range(-simulationSize.x, simulationSize.x),
                Random.Range(-simulationSize.y, simulationSize.y),
                Random.Range(-simulationSize.z, simulationSize.z)
                );
        }

        positionsBuffer = new ComputeBuffer(nParticles, sizeof(float) * 3);
        positionsBuffer.SetData(positions);
        
        computeShader.SetBuffer(kernelComputeGravity, "positions",  positionsBuffer);
        
        SetComputeShaderParameters();
    }

    private void SetComputeShaderParameters()
    {
        computeShader.SetFloat("gravity", gravity);
        computeShader.SetFloat("nParticles", nParticles);
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
        RunComputeShader();
        RenderParticles();
        SetComputeShaderParameters();
    }

    private void RunComputeShader()
    {
        int threadGroups = Mathf.CeilToInt(nParticles / 128f);
        
        computeShader.Dispatch(kernelComputeGravity, threadGroups, 1, 1);
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
}