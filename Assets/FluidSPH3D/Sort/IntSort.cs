using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;

namespace UnityTools.Algorithm
{
	public class IntSort : BitonicSort<int>
	{
		protected GPUBufferVariable<int> source = new GPUBufferVariable<int>();
		protected void Start()
		{
			this.source.InitBuffer(8192, true);
			foreach (var i in Enumerable.Range(0, this.source.Size))
			{
				this.source.CPUData[i] = Random.Range(-this.source.Size, this.source.Size);
				// this.source.CPUData[i] = (uint)(this.source.Size - i);
			}

			this.source.SetToGPUBuffer();
			this.Sort(ref this.source);
			this.source.GetToCPUData();

			foreach (var i in Enumerable.Range(0, this.source.Size - 1))
			{
				LogTool.AssertIsTrue(this.source.CPUData[i] <= this.source.CPUData[i + 1]);
			}
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			this.source?.Release();
		}
	}
}
