using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace GPUTrail
{
	public struct TrailHeader
	{
		public int state;
		public int sourceID;
		public int first;
		public int currentLength;
		public int maxLength;
	}
	public struct TrailNode
	{
		public int prev, next, idx;
		public float uvx;
		public float3 pos;
		public float3 vel;
		public float totalLen;// physical length of whole trail
	}
}
