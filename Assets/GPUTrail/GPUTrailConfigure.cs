using UnityTools;
using UnityTools.Common;

namespace GPUTrail
{
	public class GPUTrailConfigure : Configure<GPUTrailConfigure.Data>
	{
		[System.Serializable]
		public class Data : GPUContainer
		{
			public Space3D space = new Space3D();
			public int trailNum = 1024 * 16;
		}

		protected void OnDrawGizmos()
		{
			this.D.space?.OnDrawGizmos();
		}

	}
}