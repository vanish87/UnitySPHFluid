using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Attributes;
using UnityTools.Common;

namespace FluidSPH
{
	public class SPHConfigure : Configure<SPHConfigure.Data>
	{
		[System.Serializable]
		public class Data : GPUContainer
		{
			public Space3D simulationSpace;
			[Shader(Name = "_NumOfParticle")] public int numOfParticle = 1024 * 16;
			[Shader(Name = "_H")] public float smoothlen = 0.3f;
			[Shader(Name = "_PressureK")] public float2 pressureK = new float2(400, 3);
			[Shader(Name = "_RestDensity")] public float restDensity = 1000;
			[Shader(Name = "_ParticleMass")] public float particleMass = 1f;
			[Shader(Name = "_Viscosity")] public float viscosity = 100f;
			//for Vorticity Confinement
			[Shader(Name = "_Vorticity")] public float vorticity = 1f;
			//for Micropolar Model
			[Shader(Name = "_NU_T")] public float nu_t = 0.2f;
			[Shader(Name = "_NU_EXT")] public float nu_ext = 0f;
			[Shader(Name = "_Theta")] public float theta = 1f;
			//x is scale;
			//y/z is min, max
			//w is dissipation
			[Shader(Name = "_TransferForceParameter")] public float4 transferForceParameter = new float4(0, -8 * math.PI, 8 * math.PI, 1);			
			[Shader(Name = "_TransferTorqueParameter")] public float4 transferTorqueParameter = new float4(0, -8 * math.PI, 8 * math.PI, 1);			
			[Shader(Name = "_AngularVelocityParameter")] public float4 angularVelocityParameter = new float4(0, -8 * math.PI, 8 * math.PI, 1);			
			[Shader(Name = "_LinearVelocityParameter")] public float4 linearVelocityParameter = new float4(0, -1, 1, 1);			

			[Shader(Name = "_Gravity")] public float3 gravity = new float3(0, -9.8f, 0);
			[Shader(Name = "_TimeStep"), DisableEdit] public float timeStep = 0.001f;
			//x is max speed to assure cfl condition
			//y is max speed to achieve visual effect
			[Shader(Name = "_MaxSpeed")] public float2 maxSpeed = new float2(1, 1);
			[Shader(Name = "_StepIteration")] public int stepIteration = 4;
			[Shader(Name = "_ParticleScale")] public float particleScale = 1f;
			[Shader(Name = "_RenderBoundaryParticle")] public bool renderBoundaryParticle = true;
			[Shader(Name = "_ParticleLife")] public float2 particleLife = new float2(1, 20);
			[Shader(Name = "_SimulationSpaceBounds")] public bool3 addSimulationSpaceBounds = false;
			[Shader(Name = "_TrailEmitRate")] public float trailEmitRate = 0.01f;

			public bool3 addSimulationBoundaryParticle = false;
		}

		protected void OnDrawGizmos()
		{
			this.D.simulationSpace?.OnDrawGizmos();
		}
	}
}