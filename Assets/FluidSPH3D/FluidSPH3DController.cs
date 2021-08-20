using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace FluidSPH3D
{
	public enum ParticleType
	{
		InActive = 0,
		Fluid,
		Boundary
	}
	public struct Particle
	{
		public float3 pos;
		public float3 vel;
		public float4 col;
		// public ParticleType type;
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
	public struct ParticleVorticity
	{
		public float3 vorticity;
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
			Vorticity,
			Viscosity,
			Pressure,
			Integrate,

			InitIndexPool,
			Emit,
		}
		[System.Serializable]
		public class SPHGPUData : GPUContainer
		{
			[Shader(Name = "_ParticleBuffer")] public GPUBufferVariable<Particle> particleBuffer = new GPUBufferVariable<Particle>();
			[Shader(Name = "_ParticleBufferSorted")] public GPUBufferVariable<Particle> particleBufferSorted = new GPUBufferVariable<Particle>();
			[Shader(Name = "_ParticleBufferIndexAppend")] public GPUBufferAppendConsume<uint> particleBufferIndexAppend = new GPUBufferAppendConsume<uint>();
			[Shader(Name = "_ParticleBufferIndexConsume")] public GPUBufferAppendConsume<uint> particleBufferIndexConsume = new GPUBufferAppendConsume<uint>();
			[Shader(Name = "_ParticleDensityBuffer")] public GPUBufferVariable<ParticleDensity> particleDensity = new GPUBufferVariable<ParticleDensity>();
			[Shader(Name = "_ParticleForceBuffer")] public GPUBufferVariable<ParticleForce> particleForce = new GPUBufferVariable<ParticleForce>();
			[Shader(Name = "_ParticleVelocityBuffer")] public GPUBufferVariable<ParticleVelocity> particleVelocity = new GPUBufferVariable<ParticleVelocity>();
			[Shader(Name = "_ParticleVorticityBuffer")] public GPUBufferVariable<ParticleVorticity> particleVorticity = new GPUBufferVariable<ParticleVorticity>();
			[Shader(Name = "_ParticleCount")] public GPUBufferVariable<int> particleCount = new GPUBufferVariable<int>();

			public int emitNum = 256;

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

			// this.InitIndexPool();
		}
		protected void InitParticle()
		{
			foreach (var i in Enumerable.Range(0, this.sphData.particleBuffer.Size))
			{
				var rand = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				rand -= 0.5f;
				rand = UnityEngine.Random.insideUnitSphere * 0.5f;
				this.sphData.particleBuffer.CPUData[i].col = 1;
				this.sphData.particleBuffer.CPUData[i].pos = this.Configure.D.simulationSpace.TRS.MultiplyPoint(rand);
				this.sphData.particleBuffer.CPUData[i].vel = 0;
				// this.sphData.particleBuffer.CPUData[i].type = ParticleType.Fluid;
			}
			this.sphData.particleBuffer.SetToGPUBuffer(true);
		}
		protected void InitIndexPool()
		{
			this.sphData.particleBufferIndexAppend.ResetCounter();
			this.fluidDispatcher.Dispatch(SPHKernel.InitIndexPool, this.Configure.D.numOfParticle);
		}

		protected void Emit()
		{
			var num = this.sphData.emitNum;
			var poolNum = this.sphData.particleBufferIndexConsume.GetCounter();
			if(poolNum < num)
			{
				LogTool.Log("pool particle " + poolNum + " not enough to emit " + num, LogLevel.Warning);
				return;
			}
			this.fluidDispatcher.Dispatch(SPHKernel.Emit, num);
		}

		protected void InitSPH()
		{
			this.SPHGrid.Init(this.Configure.D.simulationSpace, this.Configure.D.smoothlen);

			this.sphData.particleBufferIndexAppend.InitAppendBuffer(this.Configure.D.numOfParticle);
			this.sphData.particleBufferIndexConsume.InitAppendBuffer(this.sphData.particleBufferIndexAppend);

			this.sphData.particleBuffer.InitBuffer(this.Configure.D.numOfParticle, true, false);
			this.sphData.particleDensity.InitBuffer(this.Configure.D.numOfParticle);
			this.sphData.particleForce.InitBuffer(this.Configure.D.numOfParticle);
			this.sphData.particleVelocity.InitBuffer(this.Configure.D.numOfParticle);
			this.sphData.particleVorticity.InitBuffer(this.Configure.D.numOfParticle);

			this.sphData.particleCount.InitBuffer(this.Configure.D.numOfParticle, true, false);

			var cs = this.mode == RunMode.SharedMemory ? this.fluidSharedCS : this.fluidSortedCS;
			this.fluidDispatcher = new ComputeShaderDispatcher<SPHKernel>(cs);
			foreach (SPHKernel k in Enum.GetValues(typeof(SPHKernel)))
			{
				this.fluidDispatcher.AddParameter(k, this.Configure.D);
				this.fluidDispatcher.AddParameter(k, this.sphData);
				this.fluidDispatcher.AddParameter(k, this.SPHGrid.GridGPUData);
			}
		}
		protected void SPHStep()
		{
			var num = this.Configure.D.numOfParticle;
			this.fluidDispatcher.Dispatch(SPHKernel.Density, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Vorticity, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Viscosity, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Pressure, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Integrate, num);

			// this.sphData.particleCount.GetToCPUData();
			// Debug.Log(this.sphData.particleCount.CPUData[10]);
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
			if (this.mode == RunMode.SortedGrid)
			{
				GPUBufferVariable<Particle> sorted;
				this.SPHGrid.BuildSortedParticleGridIndex(this.sphData.particleBuffer, out sorted);
				this.sphData.particleBufferSorted.UpdateBuffer(sorted);
			}
			// this.SPHStep();

			if (Input.GetKeyDown(KeyCode.R)) this.InitParticle();
			if (Input.GetKeyDown(KeyCode.E)) this.Emit();
		}

	}
}