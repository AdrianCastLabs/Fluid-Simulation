using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace  Simulation.Scripts
{
    public class FluidSimulationGPU : MonoBehaviour
    {
        // compute shader
        [SerializeField] private ComputeShader computeShader;
        
        // rendering
        [SerializeField] private Mesh particleMesh;
        [SerializeField] private Material particleMaterial;
        
        // simulation settings
        [SerializeField] private Vector3 simulationSize = new Vector2(10.0f, 10.0f);
        [SerializeField] private int nParticles = 2000;
        [SerializeField] private float particleRadius = 0.5f;
        
        // fluid settings
        [SerializeField] private float targetDensity = 1.0f;
        [SerializeField] private float pressureMultiplier = 1.0f;
        [SerializeField] private float smoothingRadius = 1.0f;
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float timeStep = 0.02f;
        [SerializeField] private float gravity = 1.0f;
        [SerializeField] private float damping = 0.5f;

        private ComputeBuffer particleBuffer;
        private ComputeBuffer argsBuffer;
        private GPUParticle[] particles;

        private int kernelComputeDensity;
        private int kernelComputePressureForce;
        private int kernelIntegrate;
        private int kernelHandleCollisions;

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
            kernelComputeDensity = computeShader.FindKernel("ComputeDensity");
            kernelComputePressureForce = computeShader.FindKernel("ComputePressureForce");
            kernelIntegrate = computeShader.FindKernel("Integrate");
            kernelHandleCollisions = computeShader.FindKernel("HandleCollisions");
        }

        private void InitializeParticles()
        {
            particles = new GPUParticle[nParticles];
            
            // initialize with random positions
            for (int i = 0; i < nParticles; i++)
            {
                particles[i] = new GPUParticle
                {
                    position = new Vector3(Random.Range(0, simulationSize.x), Random.Range(0, simulationSize.y), 0f),
                    velocity = Vector3.zero,
                    force = Vector3.zero,
                    density = 0f
                
                };
            }
            
            // create compute buffer
            int stride = Marshal.SizeOf(typeof(GPUParticle));
            particleBuffer = new ComputeBuffer(nParticles, stride);
            particleBuffer.SetData(particles);
            
            // set buffers to all kernels
            computeShader.SetBuffer(kernelComputeDensity, "particles", particleBuffer);
            computeShader.SetBuffer(kernelComputePressureForce, "particles", particleBuffer);
            computeShader.SetBuffer(kernelIntegrate, "particles", particleBuffer);
            computeShader.SetBuffer(kernelHandleCollisions, "particles", particleBuffer);
            
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
            computeShader.SetFloat("timeStep", timeStep);
            computeShader.SetFloat("gravity", gravity);
            computeShader.SetVector("simulationSize", simulationSize);
            computeShader.SetFloat("particleRadius", particleRadius);
            computeShader.SetFloat("damping", damping);
        }

        private void InitializeRendering()
        {
            // setup indirect rendering
            args[0] = particleMesh.GetIndexCount(0);
            args[1] = (uint)nParticles;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
            
            // set buffer to material
            particleMaterial.SetBuffer("particles", particleBuffer);
            particleMaterial.SetFloat("_Radius", particleRadius);
            
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
    
            computeShader.Dispatch(kernelComputeDensity, threadGroups, 1, 1);
            computeShader.Dispatch(kernelComputePressureForce, threadGroups, 1, 1);
            computeShader.Dispatch(kernelIntegrate, threadGroups, 1, 1);
    
            // Run collisions multiple times with smaller corrections
            for (int i = 0; i < 3; i++)
            {
                computeShader.Dispatch(kernelHandleCollisions, threadGroups, 1, 1);
            }
        }

        private void RenderParticles()
        {
            Graphics.DrawMeshInstancedIndirect(
                particleMesh,
                0,
                particleMaterial,
                bounds,
                argsBuffer,
                castShadows: UnityEngine.Rendering.ShadowCastingMode.Off
                );
        }

        private void OnDestroy()
        {
            particleBuffer?.Release();
            argsBuffer?.Release();
        }
    }
}