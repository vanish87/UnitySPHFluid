using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UnityTools.Common
{
	[ExecuteInEditMode]
	public class Sampler
	{
		static private List<float3> Sample(ISpace space, float3 bpos, int3 size)
		{
			var ret = new List<float3>();
			var offset = new float3(1.0f / math.max(size.x - 1, 1), 1.0f / math.max(size.y - 1, 1), 1.0f / math.max(size.z - 1, 1));

			foreach (var i in Enumerable.Range(0, size.x))
			{
				foreach (var j in Enumerable.Range(0, size.y))
				{
					foreach (var k in Enumerable.Range(0, size.z))
					{
						var p = space.TRS.MultiplyPoint(bpos + offset * new float3(i, j, k));
						ret.Add(p);
					}
				}
			}
			return ret;
		}

		static public List<float3> SampleSphereSurface(ISpace space, float density = 1f / 32)
		{
			var ret = new List<float3>();
			var radius = space.Scale.x / 2;
			var num = Mathf.CeilToInt(space.Scale.x / density);

			foreach(var theta in Enumerable.Range(0, num))
			{
				foreach(var phi in Enumerable.Range(0, num))
				{
					var t = theta * 1.0f / math.max(num - 1, 1) * math.PI;
					var p = phi * 1.0f / math.max(num - 1, 1) * math.PI * 2;
					var np = new float3(math.cos(p) * math.sin(t), math.sin(p) * math.sin(t), math.cos(t)) * radius;
					ret.Add(np + space.Center);
				}
			}
			return ret;
		}
		static public List<float3> SampleXY(ISpace space, int depth = 1, float density = 1f / 32f)
		{
			var bpos = new float3(-0.5f, -0.5f, depth == 1 ? 0 : -0.5f);
			var size = new float3(space.Scale / density);
			var ns = new int3(Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y), depth);

			return Sample(space, bpos, ns);
		}
		static public List<float3> SampleXZ(ISpace space, int depth = 1, float density = 1f / 32f)
		{
			var bpos = new float3(-0.5f, depth == 1 ? 0 : -0.5f, -0.5f);
			var size = new float3(space.Scale / density);
			var ns = new int3(Mathf.CeilToInt(size.x), depth, Mathf.CeilToInt(size.z));

			return Sample(space, bpos, ns);
		}
		static public List<float3> SampleYZ(ISpace space, int depth = 1, float density = 1f / 32f)
		{
			var bpos = new float3(depth == 1 ? 0 : -0.5f, -0.5f, -0.5f);
			var size = new float3(space.Scale / density);
			var ns = new int3(depth ,Mathf.CeilToInt(size.y), Mathf.CeilToInt(size.z));

			return Sample(space, bpos, ns);
		}

	}

}