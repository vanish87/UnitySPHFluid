using Unity.Mathematics;

namespace GPUTrail
{
	public struct TrailHeader
	{
		public int head;
		public int length;
	}
	public struct TrailNode
	{
		public int head;
		public float3 pos;
	}
}