using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UnityTools.Common
{
	[ExecuteInEditMode]
	public class BoundarySampler : MonoBehaviour
	{
		[SerializeField] public Space3D space = new Space3D();

		public List<float3> Sample(float density = 1f/32f)
		{
			var ret = new List<float3>();
			var bpos = new float3(-0.5f, -0.5f, -0.5f);
			var size = new float3(this.space.Scale / density);
			var ns = new int3(Mathf.CeilToInt(size.x),Mathf.CeilToInt(size.y),Mathf.CeilToInt(size.z));
			ns.z = 1;
			foreach (var i in Enumerable.Range(0, ns.x))
			{
				foreach (var j in Enumerable.Range(0, ns.y))
				{
					foreach (var k in Enumerable.Range(0, ns.z))
					{
						var offset = new float3(i * 1.0f / math.max(ns.x - 1, 1), j * 1.0f / math.max(ns.y - 1, 1), k * 1.0f / math.max(ns.z - 1, 1));
						var p = this.space.TRS.MultiplyPoint(bpos + offset);
						ret.Add(p);
					}
				}
			}

			return ret;
		}
		protected void Update()
		{
			this.space.Center = this.gameObject.transform.localPosition;
			this.space.Rotation = this.gameObject.transform.localRotation;
			this.space.Scale = this.gameObject.transform.localScale;
		}

		protected void OnDrawGizmos()
		{
			this.space?.OnDrawGizmos();
		}

	}

}