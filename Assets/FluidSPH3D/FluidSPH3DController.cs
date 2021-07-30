using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Rendering;

namespace FluidSPH3D
{
	public struct Particle
	{
		public float3 pos;
		public float3 vel;
		public float4 col;
	}
	public struct ParticleDensity
	{
		public float density;
	}
	public struct ParticleForce
	{
		public float3 force;
	}
	public struct ParticleVelocity
	{
		public float3 velocity;
	}
	public class FluidSPH3DController : MonoBehaviour, IParticleBuffer<Particle>
	{
		public enum RunMode
		{
			SortedGrid,
			SharedMemory,
		}
		public enum SPHKernel
		{
			Density,
			Viscosity,
			Pressure,
			Integrate,
		}
		[System.Serializable]
		public class SPHGPUData : GPUContainer
		{
			// [Shader(Name = "_DensityCoef")] public float densityCoef = 0;
			// [Shader(Name = "_GradPressureCoef")] public float gradPressureCoef = 0;
			// [Shader(Name = "_LapViscosityCoef")] public float lapViscosityCoef = 0;
			
			[Shader(Name = "_ParticleBuffer")] public GPUBufferVariable<Particle> particleBuffer = new GPUBufferVariable<Particle>();
			[Shader(Name = "_ParticleDensityBuffer")] public GPUBufferVariable<ParticleDensity> particleDensity = new GPUBufferVariable<ParticleDensity>();
			[Shader(Name = "_ParticleForceBuffer")] public GPUBufferVariable<ParticleForce> particleForce = new GPUBufferVariable<ParticleForce>();
			[Shader(Name = "_ParticleVelocityBuffer")] public GPUBufferVariable<ParticleVelocity> particleVelocity = new GPUBufferVariable<ParticleVelocity>();

		}
		public GPUBufferVariable<Particle> Buffer => this.sphData.particleBuffer;
		[SerializeField] protected RunMode mode = RunMode.SharedMemory;
		[SerializeField] protected SPHGPUData sphData = new SPHGPUData();
		[SerializeField] protected ComputeShader fluidSortedCS;
		[SerializeField] protected ComputeShader fluidSharedCS;
		protected FluidSPH3DConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<FluidSPH3DConfigure>();
		protected FluidSPH3DConfigure configure;

		protected SPHGrid SPHGrid => this.sphGrid ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHGrid>();
		protected SPHGrid sphGrid;
		protected ComputeShaderDispatcher<SPHKernel> fluidDispatcher;
		protected void Init()
		{
			this.Configure.Initialize();
			this.InitSPH();
			this.InitParticle();
		}
		protected void InitParticle()
		{
			foreach (var i in Enumerable.Range(0, this.sphData.particleBuffer.Size))
			{
				var rand = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				rand -= 0.5f;
				rand = UnityEngine.Random.insideUnitSphere * 0.25f;
				this.sphData.particleBuffer.CPUData[i].col = 1;
				this.sphData.particleBuffer.CPUData[i].pos = this.Configure.D.simulationSpace.TRS.MultiplyPoint(rand);
				this.sphData.particleBuffer.CPUData[i].vel = 0;
			}
			this.sphData.particleBuffer.SetToGPUBuffer(true);
		}

		protected void InitSPH()
		{
			this.SPHGrid.Init(this.Configure.D.simulationSpace, 0.1f);

			this.sphData.particleBuffer.InitBuffer(this.Configure.D.numOfParticle, true, false);
			this.sphData.particleDensity.InitBuffer(this.Configure.D.numOfParticle, true, false);
			this.sphData.particleForce.InitBuffer(this.Configure.D.numOfParticle);
			this.sphData.particleVelocity.InitBuffer(this.Configure.D.numOfParticle);

			var cs = this.mode == RunMode.SharedMemory ? this.fluidSharedCS : this.fluidSortedCS;
			this.fluidDispatcher = new ComputeShaderDispatcher<SPHKernel>(cs);
			foreach (SPHKernel k in Enum.GetValues(typeof(SPHKernel)))
			{
				this.fluidDispatcher.AddParameter(k, this.Configure.D);
				this.fluidDispatcher.AddParameter(k, this.sphData);
				this.fluidDispatcher.AddParameter(k, this.SPHGrid.GridGPUData);
			}
		}
		protected void UpdateSPHParameter()
		{
			var p = this.sphData;
			var config = this.Configure.D;

			// p.densityCoef = config.particleMass * 315.0f / (64.0f * Mathf.PI * Mathf.Pow(config.smoothlen, 9));
			// p.gradPressureCoef = config.particleMass * -45.0f / (Mathf.PI * Mathf.Pow(config.smoothlen, 6));
			// p.lapViscosityCoef = config.particleMass * config.viscosity * 45.0f / (Mathf.PI * Mathf.Pow(config.smoothlen, 6));
		}

		protected void SPHStep()
		{
			var num = this.Configure.D.numOfParticle;
			this.fluidDispatcher.Dispatch(SPHKernel.Density, num);
			this.sphData.particleDensity.GetToCPUData();
			// foreach(var d in this.sphData.particleDensity.CPUData) Debug.Log(d.density);
			this.fluidDispatcher.Dispatch(SPHKernel.Viscosity, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Pressure, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Integrate, num);
		}
		protected void DeInit()
		{
			this.sphData?.Release();
		}
		protected void OnEnable()
		{
			this.Init();
		}
		protected void OnDisable()
		{
			this.DeInit();
		}

		protected void Update()
		{
			this.UpdateSPHParameter();
			this.SPHStep();

			if(Input.GetKeyDown(KeyCode.R)) this.InitParticle();
		}

	}
}