using UnityEngine;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct GPUParticle
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 force;
    public float density;
}