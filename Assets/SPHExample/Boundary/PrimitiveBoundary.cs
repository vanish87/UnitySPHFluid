using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH
{
	[ExecuteInEditMode]
	public class PrimitiveBoundary : MonoBehaviour, IBoundary
	{
		public bool IsActive => this.enabled;
		public ISpace Space => this.data.space;

		public BoundaryType Type => this.data.type;

		public float3 Velocity => this.vel;

		[SerializeField] protected BoundarySetting data = new BoundarySetting();
		protected float3 prev;
		[SerializeField]protected float3 vel;

		public void Init(BoundarySetting data)
		{
			this.data = data;

			var space = this.data.space;
			this.gameObject.transform.localPosition = space.Center;
			this.gameObject.transform.localRotation = space.Rotation;
			this.gameObject.transform.localScale = space.Scale;
		}

		public virtual List<float3> Sample(float density = 1 / 32f)
		{
			if (this.data.type == BoundaryType.ParticlePlane)
			{
				//only sample xy plane
				return Sampler.SampleXY(this.data.space, 1, this.data.density);
			}
			else if (this.data.type == BoundaryType.ParticleSphere)
			{
				return Sampler.SampleSphereSurface(this.data.space, this.data.density);
			}
			return new List<float3>();
		}

		protected virtual void Update()
		{
			this.data.space.Center = this.gameObject.transform.localPosition;
			this.data.space.Rotation = this.gameObject.transform.localRotation;
			this.data.space.Scale = this.gameObject.transform.localScale;

			this.vel = (this.Space.Center - prev) / Time.deltaTime;
			this.prev = this.Space.Center;
		}

		protected virtual void OnDrawGizmos()
		{
			this.data.space?.OnDrawGizmos();
		}
	}
}