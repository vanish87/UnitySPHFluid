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
		protected override void Draw(Material material)
		{
			if (this.buffer == null || this.buffer.Buffer.Size == 0 || material == null)
			{
				LogTool.Log("Draw buffer is null, nothing to draw", LogLevel.Warning);
				return;
			}
            var inverseViewMatrix = Camera.main.worldToCameraMatrix.inverse;
            material.SetMatrix("_InvViewMatrix", inverseViewMatrix);

            material.SetBuffer("_TrailNodeBuffer", this.buffer.Buffer);
			var b = new Bounds(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawProcedural(material, b, MeshTopology.Points, this.buffer.Buffer.Size);
		}
	}
}
