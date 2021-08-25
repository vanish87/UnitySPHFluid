using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH3D
{
	public interface IBoundarySampler
	{
		List<float3> Sample(float density = 1 / 32f);
	}

	[ExecuteInEditMode]
	public class PlaneBoundary : MonoBehaviour, IBoundarySampler
	{
		//only sample xy plane
		[SerializeField] protected Space3D plane = new Space3D();

		public List<float3> Sample(float density = 1 / 32f)
		{
			return Sampler.SampleXY(this.plane, 1, density);
		}

		protected void Update()
		{
			this.plane.Center = this.gameObject.transform.localPosition;
			this.plane.Rotation = this.gameObject.transform.localRotation;
			this.plane.Scale = this.gameObject.transform.localScale;
		}

		protected void OnDrawGizmos()
		{
			this.plane?.OnDrawGizmos();
		}
	}
}