using Unity.Mathematics;

namespace GPUTrail
{
	public struct TrailHeader
	{
		public int state;
		public int first;
		public int currentLength;
		public int maxLength;
	}
	public struct TrailNode
	{
		public int prev, next, idx;
		public float uvy;
		public float3 pos;
	}
}
