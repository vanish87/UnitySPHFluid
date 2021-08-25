
using Unity.Mathematics;

namespace FluidSPH3D
{
	public enum ParticleType
	{
		Inactive = 0,
		Fluid,
		Boundary
	}
	public struct Particle
	{
		public float3 pos;
		public float3 vel;
		public float3 w;
		public float4 col;
		public float life;
		public ParticleType type;
	}
	public struct ParticleDensity
	{
		public float density;
	}
	public struct ParticleForce
	{
		public float3 force;
		public float3 transferForce;
		public float3 transferTorque;
	}
	public struct ParticleVelocity
	{
		public float3 velocity;
	}
	public struct ParticleVorticity
	{
		public float3 vorticity;
	}
}