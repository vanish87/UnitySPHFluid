using Unity.Mathematics;

namespace GPUTrail
{
	public struct TrailHeader
	{
		public int state;
		public int headNodeIndex;
		public int currentLength;
		public int maxLength;
	}
	public struct TrailNode
	{
		public int prev, next, idx;
		public float3 pos;
	}
}
