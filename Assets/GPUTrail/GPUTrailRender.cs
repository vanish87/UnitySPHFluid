using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace GPUTrail
{
	public class GPUTrailRender : DataRenderBase<TrailNode>
	{
		protected GPUTrailRenderConfigure Configure => this.configure ??= this.GetComponentInChildren<GPUTrailRenderConfigure>();
		protected GPUTrailRenderConfigure configure;
		protected override void Draw(Material material)
		{
			if (this.buffer == null || this.buffer.Buffer.Size == 0 || material == null)
			{
				LogTool.Log("Draw buffer is null, nothing to draw", LogLevel.Warning);
				return;
			}

			this.Configure.D.UpdateGPU(material);
			material.SetBuffer("_TrailNodeBuffer", this.buffer.Buffer);
			var b = new Bounds(this.buffer.Space.Center, this.buffer.Space.Scale);
			Graphics.DrawProcedural(material, b, MeshTopology.Points, this.buffer.Buffer.Size);
		}
	}
}
