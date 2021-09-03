using Unity.Mathematics;

namespace GPUTrail
{
	public struct TrailHeader
	{
		public int headNodeIndex;
		public int length;
	}
	public struct TrailNode
	{
		public float3 pos;
	}
}
