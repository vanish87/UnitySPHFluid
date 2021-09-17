using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH3D
{
	[SerializeField]
	public struct SDFBoundaryData
	{
		public BlittableBool enabled;
		public float3 center;
		public float3 size;

	}
	public class BoundaryController : MonoBehaviour, IInitialize
	{
		protected const int MAX_NUM_SDF_BOUNDARY = 128;
		public List<IBoundary> Boundaries => this.boundaries;
		public GPUBufferVariable<SDFBoundaryData> SDFBoundaires => this.sdfBoundaries ??= new GPUBufferVariable<SDFBoundaryData>("_SDFBoundaryBuffer", MAX_NUM_SDF_BOUNDARY, true);
		protected BoundaryConfigure configure;
		protected BoundaryConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<BoundaryConfigure>();
		public bool Inited => this.inited;
		protected bool inited = false;

		protected List<IBoundary> boundaries = new List<IBoundary>();
		protected GPUBufferVariable<SDFBoundaryData> sdfBoundaries;

		public void Init()
		{
			if(this.Inited) return;

			this.boundaries.Clear();
			this.boundaries.AddRange(this.gameObject.GetComponentsInChildren<IBoundary>());

			this.Configure.Initialize();
			foreach (var bc in this.Configure.D.boundaries)
			{
				var go = new GameObject(bc.name);
				go.transform.parent = this.gameObject.transform;
				var b = default(IBoundary);
				if (this.IsSDF(bc))
				{
					b = go.AddComponent<SDFBoundary>();
					(b as SDFBoundary).Init(bc);
				}
				else
				{
					b = go.AddComponent<PrimitiveBoundary>();
					(b as PrimitiveBoundary).Init(bc);
				}
				this.boundaries.Add(b);
			}

			this.inited = true;
		}
		public void Deinit()
		{
			this.boundaries.Clear();
			this.sdfBoundaries?.Release();
		}
		protected bool IsSDF(BoundaryConfigure.BData boundary)
		{
			return boundary.type == BoundaryType.SDFSphere;
		}
		protected void UpdateSDF()
		{
			var sdfCount = 0;
			foreach (var b in this.boundaries)
			{
				if (b is SDFBoundary)
				{
					this.SDFBoundaires.CPUData[sdfCount] = new SDFBoundaryData()
					{
						enabled = true,
						center = b.Space.Center,
						size = b.Space.Scale
					};
					sdfCount++;
				}
			}
			while (sdfCount < this.SDFBoundaires.Size) this.SDFBoundaires.CPUData[sdfCount++].enabled = false;
		}

		protected void Update()
		{
			this.UpdateSDF();
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