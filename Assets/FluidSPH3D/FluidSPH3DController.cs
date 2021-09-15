using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Attributes;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace FluidSPH3D
{
	public class FluidSPH3DController : MonoBehaviour, IDataBuffer<Particle>
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
			[Shader(Name = "_DeltaTime"), DisableEdit] public float deltaTime = 0.001f;
		}
		[System.Serializable]
		public class StaticsData
		{
			public int BoundaryParticleNum = 0;
			public int ActiveParticleNum = 0;

		}
		public GPUBufferVariable<Particle> Buffer => this.sphData.particleBuffer;
		public ISpace Space => this.Configure.D.simulationSpace;
		[SerializeField] protected RunMode mode = RunMode.SharedMemory;
		[SerializeField] protected SPHGPUData sphData = new SPHGPUData();
		[SerializeField] protected BoundaryGPUData boundaryGPUData = new BoundaryGPUData();
		[SerializeField] protected ComputeShader fluidSortedCS;
		[SerializeField] protected ComputeShader fluidSharedCS;
		protected FluidSPH3DConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<FluidSPH3DConfigure>();
		protected FluidSPH3DConfigure configure;

		protected SPHGrid SPHGrid => this.sphGrid ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHGrid>();
		protected SPHGrid sphGrid;
		protected EmitterController EmitterController => this.emitterController ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<EmitterController>();
		protected EmitterController emitterController;
		protected BoundaryController BoundaryController => this.boundaryController ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<BoundaryController>();


		protected BoundaryController boundaryController;
		protected ComputeShaderDispatcher<SPHKernel> fluidDispatcher;


		protected StaticsData staticsData = new StaticsData();

		protected void Init()
		{
			this.BoundaryController.Init();
			this.EmitterController.Init();

			this.Configure.Initialize();
			this.InitSPH();
			this.InitParticle();
			this.InitIndexPool();

			this.AddBoundary();
		}
		protected void InitParticle()
		{
			foreach (var i in Enumerable.Range(0, this.sphData.particleBuffer.Size))
			{
				var rand = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				rand -= 0.5f;
				rand = UnityEngine.Random.insideUnitSphere * 0.5f;
				this.sphData.particleBuffer.CPUData[i].uuid = i;
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

		protected void AddBoundary()
		{
			this.staticsData.BoundaryParticleNum = 0;

			this.boundaryGPUData.boundaryBuffer.InitBuffer(1024 * 32 * 2, true, true);
			var density = 1.0f/16;

			var config = this.Configure.D;
			if(config.addSimulationBoundaryParticle.x) this.AddSamples(Sampler.SampleYZ(config.simulationSpace, 2, density));
			if(config.addSimulationBoundaryParticle.y) this.AddSamples(Sampler.SampleXZ(config.simulationSpace, 2, density));
			if(config.addSimulationBoundaryParticle.z) this.AddSamples(Sampler.SampleXY(config.simulationSpace, 2, density));

		
			foreach(var b in this.BoundaryController.Boundaries)
			{
				this.AddSamples(b.Sample(density));
			}
		}

		protected void AddSamples(List<float3> samples)
		{
			if(samples.Count > 0)
			{
				var count = 0;
				foreach (var p in samples)
				{
					this.boundaryGPUData.boundaryBuffer.CPUData[count++] = p;
				}
				this.boundaryGPUData.boundarySize = samples.Count;

				this.fluidDispatcher.Dispatch(SPHKernel.AddBoundary, samples.Count);

				this.staticsData.BoundaryParticleNum += samples.Count;
			}
		}


		protected void Emit()
		{
			var ec  = this.EmitterController.EmitterGPUData;
			var num = this.EmitterController.CurrentParticleEmit;
			var poolNum = this.sphData.particleBufferIndexConsume.GetCounter();
			if(poolNum < num)
			{
				LogTool.Log("pool particle " + poolNum + " not enough to emit " + num, LogLevel.Warning);
				return;
			}
			this.fluidDispatcher.DispatchNoneThread(SPHKernel.Emit, ec.emitterBuffer.Size);
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
				this.fluidDispatcher.AddParameter(k, this.EmitterController.EmitterGPUData);
				this.fluidDispatcher.AddParameter(k, this.boundaryGPUData);
				this.fluidDispatcher.AddParameter(k, this.BoundaryController.SDFBoundaires);
			}

		}
		protected void UpdateRuntimeParameter()
		{
			this.sphData.deltaTime = Time.deltaTime / this.Configure.D.stepIteration;

			//CFL
			const float lambda = 0.4f;
			float h = this.Configure.D.smoothlen;
			float maxSpeed = this.Configure.D.maxSpeed;
			this.Configure.D.timeStep = lambda * (h / maxSpeed);
		}
		protected void SPHStep()
		{
			var num = this.Configure.D.numOfParticle;
			this.fluidDispatcher.Dispatch(SPHKernel.Density, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Vorticity, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Viscosity, num);
			this.fluidDispatcher.Dispatch(SPHKernel.Pressure, num);

			//intergrate will also update index buffer
			//so append index buffer should be reset
			this.sphData.particleBufferIndexAppend.ResetCounter();
			this.fluidDispatcher.Dispatch(SPHKernel.Integrate, num);

			// this.sphData.particleCount.GetToCPUData();
			// Debug.Log(this.sphData.particleCount.CPUData[10]);
		}
		protected void DeInit()
		{
			this.sphData?.Release();
			this.boundaryGPUData?.Release();

			this.BoundaryController.Deinit();
			this.emitterController.Deinit();
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
			foreach(var i in Enumerable.Range(0, this.Configure.D.stepIteration))
			{
				this.UpdateRuntimeParameter();

				if (this.mode == RunMode.SortedGrid)
				{
					GPUBufferVariable<Particle> sorted;
					this.SPHGrid.BuildSortedParticleGridIndex(this.sphData.particleBuffer, out sorted);
					this.sphData.particleBufferSorted.UpdateBuffer(sorted);
				}
				this.SPHStep();
			}

			if (Input.GetKeyDown(KeyCode.R)) 
			{
				this.InitSPH();
				this.InitParticle();

				this.InitIndexPool();
				this.AddBoundary();
			}
			this.Emit();
			// if (Input.GetKey(KeyCode.E)) this.Emit();

			if(Input.GetKeyDown(KeyCode.V))
			{
				this.sphData.particleBuffer.GetToCPUData();
				var count = 0;
				foreach(var p in this.sphData.particleBuffer.CPUData)
				{
					if(p.type != ParticleType.Inactive) count++;
				}

				Debug.Log(count);
			}

			this.staticsData.ActiveParticleNum = this.Configure.D.numOfParticle - this.sphData.particleBufferIndexConsume.GetCounter();
		}

		protected void OnGUI()
		{
			var pcount = this.staticsData.ActiveParticleNum;
			var bcount = this.staticsData.BoundaryParticleNum;
			GUILayout.Label("Boundary Count " + bcount);
			GUILayout.Label("Active Count " + pcount);
		}

	}
}