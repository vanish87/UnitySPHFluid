
using UnityEngine;
using UnityTools.Debuging.EditorTool;

namespace FluidSPH
{
	public class SDFBoundary : PrimitiveBoundary
	{
		protected override void OnDrawGizmos()
		{
			using(new GizmosScope(this.Space.Color, Matrix4x4.identity))
			{
				Gizmos.DrawWireSphere(this.data.space.Center, this.data.space.Scale.x);
			}
		}
		

	}
}