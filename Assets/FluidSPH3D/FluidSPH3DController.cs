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
			VorticityConfinement,
			Viscosity,
			Pressure,
			Integrate,

			InitIndexPool,
			Emit,

			AddBoundary,
		}
		[System.Serializable]
		public class EmitterGPUData : GPUContainer
		{
			public int particlePerEmit = 256;
			public const int MAX_NUM_EMITTER = 128;

			[Shader(Name = "_EmitterBuffer")] public GPUBufferVariable<EmitterData> emitterBuffer = new GPUBufferVariable<EmitterData>();
		}
		[System.Serializable]
		public class BoundaryGPUData : GPUContainer
		{
			[Shader(Name = "_BoundaryBuffer")] public GPUBufferVariable<float3> boundaryBuffer = new GPUBufferVariable<float3>();
			[Shader(Name = "_BoundarySize")] public int boundarySize;
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
		}
		public GPUBufferVariable<Particle> Buffer => this.sphData.particleBuffer;
		[SerializeField] protected RunMode mode = RunMode.SharedMemory;
		[SerializeField] protected SPHGPUData sphData = new SPHGPUData();
		[SerializeField] protected EmitterGPUData emitterGPUData = new EmitterGPUData();
		[SerializeField] protected BoundaryGPUData boundaryGPUData = new BoundaryGPUData();
		[SerializeField] protected ComputeShader fluidSortedCS;
		[SerializeField] protected ComputeShader fluidSharedCS;
		protected FluidSPH3DConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<FluidSPH3DConfigure>();
		protected FluidSPH3DConfigure configure;

		protected SPHGrid SPHGrid => this.sphGrid ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHGrid>();
		protected SPHGrid sphGrid;
		protected ComputeShaderDispatcher<SPHKernel> fluidDispatcher;
		protected List<IEmitter> emitters = new List<IEmitter>();

		protected void Init()
		{
			this.Configure.Initialize();
			this.InitSPH();
			this.InitParticle();

			this.InitIndexPool();
			this.InitEmitters();
			this.InitBoundary();
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
				this.sphData.particleBuffer.CPUData[i].w = 0;
				this.sphData.particleBuffer.CPUData[i].type = ParticleType.Inactive;
			}
			this.sphData.particleBuffer.SetToGPUBuffer(true);
		}
		protected void InitIndexPool()
		{
			this.sphData.particleBufferIndexAppend.ResetCounter();
			this.fluidDispatcher.Dispatch(SPHKernel.InitIndexPool, this.Configure.D.numOfParticle);
		}
		protected void InitEmitters()
		{
			this.emitters.Clear();
			this.emitters = this.GetComponentsInChildren<IEmitter>().ToList();
			this.emitterGPUData.emitterBuffer.InitBuffer(EmitterGPUData.MAX_NUM_EMITTER, true, true);
		}

		protected void InitBoundary()
		{
			this.boundaryGPUData.boundaryBuffer.InitBuffer(1024 * 8 * 4, true, true);

			if(this.Configure.D.addSimulationBoundary)
			{
				var simSpace = this.Configure.D.simulationSpace;
				var samples = new List<float3>();
				samples.AddRange(Sampler.SampleXY(simSpace, 2));
				samples.AddRange(Sampler.SampleYZ(simSpace, 2));
				samples.AddRange(Sampler.SampleXZ(simSpace, 2));
				this.AddSamples(samples);
			}

			var boundary = this.gameObject.GetComponentsInChildren<IBoundarySampler>();
			foreach(var b in boundary)
			{
				this.AddSamples(b.Sample());
			}

		}

		protected void AddSamples(List<float3> samples)
		{
			var count = 0;
			foreach (var p in samples)
			{
				this.boundaryGPUData.boundaryBuffer.CPUData[count++] = p;
			}
			this.boundaryGPUData.boundarySize = samples.Count;

			this.fluidDispatcher.Dispatch(SPHKernel.AddBoundary, samples.Count);
		}

		protected void Emit()
		{
			var ecount = 0;
			var eCPU = this.emitterGPUData.emitterBuffer.CPUData;
			foreach(var e in this.emitters)
			{
				eCPU[ecount].enabled = true;
				eCPU[ecount].localToWorld = e.Space.TRS;
				ecount++;
			}
			while(ecount<EmitterGPUData.MAX_NUM_EMITTER) eCPU[ecount++].enabled = false;

			var num = this.emitterGPUData.particlePerEmit * this.emitters.Count;
			var poolNum = this.sphData.particleBufferIndexConsume.GetCounter();
			if(poolNum < num)
			{
				LogTool.Log("pool particle " + poolNum + " not enough to emit " + num, LogLevel.Warning);
				return;
			}
			this.fluidDispatcher.Dispatch(SPHKernel.Emit, this.emitterGPUData.emitterBuffer.Size, this.emitterGPUData.particlePerEmit);
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
				this.fluidDispatcher.AddParameter(k, this.emitterGPUData);
				this.fluidDispatcher.AddParameter(k, this.boundaryGPUData);
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
			this.emitterGPUData?.Release();
			this.boundaryGPUData?.Release();
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
			this.SPHStep();

			if (Input.GetKeyDown(KeyCode.R)) { this.InitParticle(); this.InitIndexPool(); this.InitBoundary();}
			if (Input.GetKeyDown(KeyCode.E)) this.Emit();
		}

	}
}