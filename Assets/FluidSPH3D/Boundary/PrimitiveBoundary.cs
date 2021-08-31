using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH3D
{
	[ExecuteInEditMode]
	public class PrimitiveBoundary : MonoBehaviour, IBoundary
	{
		public ISpace Space => this.data.space;

		public BoundaryType Type => this.data.type;

		[SerializeField] protected BoundaryConfigure.BData data = new BoundaryConfigure.BData();

		public void Init(BoundaryConfigure.BData data)
		{
			this.data = data;

			var space = this.data.space;
			this.gameObject.transform.localPosition = space.Center;
			this.gameObject.transform.localRotation = space.Rotation;
			this.gameObject.transform.localScale = space.Scale;
		}

		public List<float3> Sample(float density = 1 / 32f)
		{
			if (this.data.type == BoundaryType.Plane)
			{
				//only sample xy plane
				return Sampler.SampleXY(this.data.space, 1, this.data.density);
			}
			else if (this.data.type == BoundaryType.Sphere)
			{
				return Sampler.SampleSphereSurface(this.data.space, this.data.density);
			}
			return new List<float3>();
		}

		protected void Update()
		{
			this.data.space.Center = this.gameObject.transform.localPosition;
			this.data.space.Rotation = this.gameObject.transform.localRotation;
			this.data.space.Scale = this.gameObject.transform.localScale;
		}

		protected virtual void OnDrawGizmos()
		{
			this.data.space?.OnDrawGizmos();
		}
	}
}