using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;

namespace FluidSPH
{
	[SerializeField]
	public struct BoundaryData
	{
		public BoundaryType type;
		public float4x4 localToWorld;
		public float3 velocity;
	}

	[SerializeField]
	public struct BoundaryParticleData
	{
		public int bid;
		public float3 localPos;
	}

	[System.Serializable]
	public class BoundaryGPUData : GPUContainer
	{
		[Shader(Name = "_BoundaryParticleData")] public GPUBufferVariable<BoundaryParticleData> boundaryParticleData = new GPUBufferVariable<BoundaryParticleData>();
		[Shader(Name = "_BoundaryParticleCount")] public int boundaryParticleCount;
		[Shader(Name = "_BoundaryData")] public GPUBufferVariable<BoundaryData> boundaryData = new GPUBufferVariable<BoundaryData>();
	}
	public class BoundaryController : MonoBehaviour, IInitialize
	{
		protected const int MAX_NUM_BOUNDARY = 128;
		protected const int MAX_NUM_BOUNDARY_PARTICLE = 1024 * 128;
		public BoundaryGPUData BoundaryGPUData => this.boundaryGPUData;
		protected BoundaryConfigure configure;
		protected BoundaryConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<BoundaryConfigure>();
		public bool Inited => this.inited;
		protected bool inited = false;
		protected List<IBoundary> boundaries = new List<IBoundary>();
		protected BoundaryGPUData boundaryGPUData = new BoundaryGPUData();

		public void Init(params object[] parameters)
		{
			if(this.Inited) return;

			this.boundaries.Clear();
			this.boundaries.AddRange(this.gameObject.GetComponentsInChildren<IBoundary>());

			this.Configure.Initialize();
			foreach (var bs in this.Configure.D.boundaries)
			{
				var go = new GameObject(bs.name);
				go.transform.parent = this.gameObject.transform;
				var b = default(IBoundary);
				switch(bs.type)
				{
					case BoundaryType.SDFSphere: b = go.AddComponent<SDFBoundary>(); break;
					default: b = go.AddComponent<PrimitiveBoundary>(); break;
				}
				b.Init(bs);
				this.boundaries.Add(b);
			}

			this.boundaryGPUData.boundaryData.InitBuffer(MAX_NUM_BOUNDARY, true, true);
			this.boundaryGPUData.boundaryParticleData.InitBuffer(MAX_NUM_BOUNDARY_PARTICLE, true, false);
			this.GenerateBoundaryParticleData();
			this.UpdateBoundary();

			this.inited = true;
		}
		public void Deinit(params object[] parameters)
		{
			this.boundaries.Clear();
			this.boundaryGPUData?.Release();
		}

		public void AddSamplesParticles(List<float3> samples, int bid)
		{
			if(samples.Count > 0)
			{
				var count = this.boundaryGPUData.boundaryParticleCount;
				foreach (var p in samples)
				{
					this.boundaryGPUData.boundaryParticleData.CPUData[count].bid = bid;
					this.boundaryGPUData.boundaryParticleData.CPUData[count].localPos = p;
					count++;
				}
				this.boundaryGPUData.boundaryParticleCount += samples.Count;
				this.boundaryGPUData.boundaryParticleData.SetToGPUBuffer(true);
			}
		}
		protected void GenerateBoundaryParticleData()
		{
			this.boundaryGPUData.boundaryParticleCount = 0;

			foreach(var bid in Enumerable.Range(0, this.boundaries.Count))
			{
				var b = this.boundaries[bid];
				if(b.Type == BoundaryType.SDFSphere || b.Type == BoundaryType.Disabled) continue;

				this.AddSamplesParticles(b.Sample(), bid);
			}
			this.boundaryGPUData.boundaryParticleData.SetToGPUBuffer(true);
		}
		protected void UpdateBoundary()
		{
			var count = 0;
			foreach(var bid in Enumerable.Range(0, this.boundaries.Count))
			{
				var b = this.boundaries[bid];
				this.boundaryGPUData.boundaryData.CPUData[count] = new BoundaryData()
				{
					type = b.IsActive ? b.Type : BoundaryType.Disabled,
					localToWorld = b.Space.TRS,
					velocity = b.Velocity,
				};
				count++;
			}
			while (count < this.boundaryGPUData.boundaryData.Size) this.boundaryGPUData.boundaryData.CPUData[count++].type = BoundaryType.Disabled;
		}

		protected void Update()
		{
			this.UpdateBoundary();
		}

		protected void OnEnable()
		{
			this.Init();
		}
		protected void OnDisable()
		{
			this.Deinit();
		}
	}
}