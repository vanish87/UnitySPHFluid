
using UnityEngine;

namespace FluidSPH3D
{
	public class SDFBoundary : PrimitiveBoundary
	{
		protected override void OnDrawGizmos()
		{
			Gizmos.DrawWireSphere(this.data.space.Center, this.data.space.Scale.x);
		}
		

	}
}