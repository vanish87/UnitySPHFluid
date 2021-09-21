using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Attributes;
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
			[Shader(Name = "_NU_T")] public float nu_t = 1f;
			[Shader(Name = "_NU_EXT")] public float nu_ext = 0f;
			[Shader(Name = "_Theta")] public float theta = 1f;
			[Shader(Name = "_Gravity")] public float3 gravity = new float3(0, -2, 0);
			[Shader(Name = "_TimeStep"), DisableEdit] public float timeStep = 0.001f;
			[Shader(Name = "_MaxSpeed")] public float maxSpeed = 1f;
			public int stepIteration = 4;

			[Shader(Name = "_ParticleScale")] public float particleScale = 1f;
			[Shader(Name = "_RenderBoundaryParticle")] public bool renderBoundaryParticle = true;
			[Shader(Name = "_ParticleLife")] public float2 particleLife = new float2(1, 20);
			[Shader(Name = "_SimulationSpaceBounds")] public bool3 addSimulationSpaceBounds = false;

			public bool3 addSimulationBoundaryParticle = false;
		}

		protected void OnDrawGizmos()
		{
			this.D.simulationSpace?.OnDrawGizmos();
		}
	}
}