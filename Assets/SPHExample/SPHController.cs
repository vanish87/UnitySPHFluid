using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Attributes;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Debuging;
using UnityTools.Physics;
using UnityTools.Rendering;

namespace FluidSPH
{
	public class SPHController : MonoBehaviour,
                                 IDataBuffer<Particle>, //for rendering particles
                                 IInitialize
	{
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

			//particle neighbor count for parameter tuning
			[Shader(Name = "_ParticleCount")] public GPUBufferVariable<int> particleCount = new GPUBufferVariable<int>();
		}
		[System.Serializable]
		public class StaticsData
		{
			[DisableEdit] public int BoundaryParticleNum = 0;
			[DisableEdit] public int ActiveParticleNum = 0;

		}
		GPUBufferVariable<Particle> IDataBuffer<Particle>.Buffer => this.sphData.particleBuffer;
		public ISpace Space => this.Configure.D.simulationSpace;
		public bool Inited => this.inited;
		[SerializeField] protected SPHGPUData sphData = new SPHGPUData();
		[SerializeField] protected ComputeShader fluidSortedCS;
		[SerializeField] protected AccumulatorTimestep runner;
		protected bool inited = false;
		protected SPHConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHConfigure>();
		protected SPHConfigure configure;
		protected SPHGrid SPHGrid => this.sphGrid ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHGrid>();
		protected SPHGrid sphGrid;
		protected EmitterController EmitterController => this.emitterController ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<EmitterController>();
		protected EmitterController emitterController;
		protected BoundaryController BoundaryController => this.boundaryController ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<BoundaryController>();
		protected BoundaryController boundaryController;
		protected ComputeShaderDispatcher<SPHKernel> fluidDispatcher;
		protected StaticsData staticsData = new StaticsData();

		public void Init(params object[] parameters)
		{
			if (this.Inited) return;

			this.BoundaryController.Init();
			this.EmitterController.Init();

			this.Configure.Initialize();
			this.InitSPH();
			this.InitParticle();
			this.InitIndexPool();
			this.AddBoundary();

			this.inited = true;
		}
		public void Deinit(params object[] parameters)
		{
			this.sphData?.Release();

			this.BoundaryController.Deinit();
			this.EmitterController.Deinit();

			this.inited = false;
		}
		protected void InitParticle()
		{
			foreach (var i in Enumerable.Range(0, this.sphData.particleBuffer.Size))
			{
				var rand = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				rand -= 0.5f;
				rand = UnityEngine.Random.insideUnitSphere * 0.5f;
				this.sphData.particleBuffer.CPUData[i].uuid = i;
				this.sphData.particleBuffer.CPUData[i].bid = -1;
				this.sphData.particleBuffer.CPUData[i].col = 1;
				this.sphData.particleBuffer.CPUData[i].pos = this.Configure.D.simulationSpace.TRS.MultiplyPoint(rand);
				this.sphData.particleBuffer.CPUData[i].vel = 0;
				this.sphData.particleBuffer.CPUData[i].w = 0;
				this.sphData.particleBuffer.CPUData[i].type = ParticleType.Inactive;
			}
			this.sphData.particleBuffer.SetToGPUBuffer(true);
		}
		protected void ResetVortex()
		{
			this.sphData.particleBuffer.GetToCPUData();
			foreach (var i in Enumerable.Range(0, this.sphData.particleBuffer.Size))
			{
				this.sphData.particleBuffer.CPUData[i].vel = 0;
				this.sphData.particleBuffer.CPUData[i].w = 0;
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
			var size = this.BoundaryController.BoundaryGPUData.boundaryParticleCount;
			if (size > 0)
			{
				this.staticsData.BoundaryParticleNum = size;
				this.fluidDispatcher.Dispatch(SPHKernel.AddBoundary, size);
			}
		}

		protected void Emit()
		{
			var ec = this.EmitterController.EmitterGPUData;
			var num = this.EmitterController.CurrentParticleEmit;
			var poolNum = this.sphData.particleBufferIndexConsume.GetCounter();
			var maxActiveParticle = this.Configure.D.numOfParticle;
			if (this.staticsData.ActiveParticleNum - this.staticsData.BoundaryParticleNum > maxActiveParticle) return;
			var size = 0;
			var count = 0;
			foreach (var e in this.EmitterController.EmitterGPUData.emitterBuffer.CPUData)
			{
				if (count + e.particlePerSecond > poolNum) break;

				count += e.particlePerSecond;
				size++;
			}
			if (size > 0)
			{
				// Debug.Log(size);
				this.fluidDispatcher.DispatchNoneThread(SPHKernel.Emit, size);
			}
			else
			{
				LogTool.Log("pool particle " + poolNum + " not enough to emit " + num, LogLevel.Warning);
			}
		}

		protected void InitSPH()
		{
			this.SPHGrid.Init(this.Configure.D.simulationSpace, this.Configure.D.smoothlen);

			var pnum = this.Configure.D.numOfParticle;

			this.sphData.particleBufferIndexAppend.InitAppendBuffer(pnum);
			this.sphData.particleBufferIndexConsume.InitAppendBuffer(this.sphData.particleBufferIndexAppend);

			this.sphData.particleBuffer.InitBuffer(pnum, true, false);
			this.sphData.particleDensity.InitBuffer(pnum);
			this.sphData.particleForce.InitBuffer(pnum);
			this.sphData.particleVelocity.InitBuffer(pnum);
			this.sphData.particleVorticity.InitBuffer(pnum);

			this.sphData.particleCount.InitBuffer(pnum, true, false);

			var cs = this.fluidSortedCS;
			this.fluidDispatcher = new ComputeShaderDispatcher<SPHKernel>(cs);
			foreach (SPHKernel k in Enum.GetValues(typeof(SPHKernel)))
			{
				this.fluidDispatcher.AddParameter(k, this.Configure.D);
				this.fluidDispatcher.AddParameter(k, this.sphData);
				this.fluidDispatcher.AddParameter(k, this.SPHGrid.GridGPUData);
				this.fluidDispatcher.AddParameter(k, this.EmitterController.EmitterGPUData);
				this.fluidDispatcher.AddParameter(k, this.BoundaryController.BoundaryGPUData);
			}

			float h = this.Configure.D.smoothlen;
			float maxSpeed = this.Configure.D.maxSpeed.x;
			int iteration = this.Configure.D.stepIteration;
			var dt = this.GetCFL(h, maxSpeed);

			this.runner = new AccumulatorTimestep(dt, iteration);
			this.runner.OnUpdate(this.SimulationUpdate);
		}
		protected float GetCFL(float h, float maxSpeed)
		{
			const float lambda = 0.4f;
            const float minCFL = 0.00001f;
			return lambda * (h / math.max(maxSpeed, minCFL));
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
		protected void SimulationUpdate(float dt)
		{
			this.Configure.D.timeStep = dt;

			this.Emit();

			{
				GPUBufferVariable<Particle> sorted;
				this.SPHGrid.BuildSortedParticleGridIndex(this.sphData.particleBuffer, out sorted);
				this.sphData.particleBufferSorted.UpdateBuffer(sorted);
			}
			this.SPHStep();
		}

		protected void SaveBuffer()
		{
			this.sphData.particleBuffer.GetToCPUData();
			// FileTool.Write(this.DataPath, this.sphData.particleBuffer.CPUData);
		}
		protected void LoadBuffer()
		{
			// var data = FileTool.Read<Particle[]>(this.DataPath);
			// this.sphData.particleBuffer.OverWriteCPUData(data);
			// this.sphData.particleBuffer.SetToGPUBuffer(true);
		}
		protected void OnEnable()
		{
			this.Init();
		}
		protected void OnDisable()
		{
			this.Deinit();
		}

		protected void Update()
		{

			if (Input.GetKeyDown(KeyCode.R))
			{
				this.InitSPH();
				this.InitParticle();

				this.InitIndexPool();
				this.AddBoundary();
			}
			if (Input.GetKeyDown(KeyCode.V))
			{
				this.ResetVortex();
			}

			if (Input.GetKeyDown(KeyCode.F5)) this.SaveBuffer();
			if (Input.GetKeyDown(KeyCode.F6)) this.LoadBuffer();

			// if(Input.GetKeyDown(KeyCode.E)) this.Emit();

			float h = this.Configure.D.smoothlen;
			float maxSpeed = this.Configure.D.maxSpeed.x;
			int iteration = this.Configure.D.stepIteration;
			var dt = this.GetCFL(h, maxSpeed);
			this.runner.Update(dt, iteration);

			this.staticsData.ActiveParticleNum = this.Configure.D.numOfParticle - this.sphData.particleBufferIndexConsume.GetCounter();
		}

		protected void OnGUI()
		{
			var pcount = this.staticsData.ActiveParticleNum;
			var bcount = this.staticsData.BoundaryParticleNum;
			GUILayout.Label("Boundary Count " + bcount);
			GUILayout.Label("Active Count " + pcount);
			GUILayout.Label("Active Count without boundary " + (pcount - bcount));
		}
	}
}