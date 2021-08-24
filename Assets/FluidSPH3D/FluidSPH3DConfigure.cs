using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH3D
{
	public class FluidSPH3DConfigure : Configure<FluidSPH3DConfigure.Data>
	{
		[System.Serializable]
		public class Data : GPUContainer
		{
			public Space3D simulationSpace;
			[Shader(Name = "_NumOfParticle")] public int numOfParticle = 1024 * 16;
			[Shader(Name = "_H")] public float smoothlen = 0.012f;
			[Shader(Name = "_PressureK")] public float2 pressureK = new float2(200, 3);

			[Shader(Name = "_RestDensity")] public float restDensity = 800;

			[Shader(Name = "_ParticleMass")] public float particleMass = 0.0004f;
			[Shader(Name = "_Viscosity")] public float viscosity = 1.5f;
			[Shader(Name = "_Vorticity")] public float vorticity = 1f;
			[Shader(Name = "_Gravity")] public float3 gravity = new float3(0, -2, 0);
			[Shader(Name = "_TimeStep")] public float timeStep = 0.001f;

			[Shader(Name = "_ParticleScale")] public float particleScale = 1f;
		}

		protected void OnDrawGizmos()
		{
			this.D.simulationSpace?.OnDrawGizmos();
		}
	}
}