using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace GPUTrail
{
	public interface ITrailData
	{
		GPUBufferVariable<TrailNode> NodeBuffer { get; }
	}
	public class GPUTrailRender : DataRenderBase<TrailHeader>
	{
        protected ITrailData trailBuffer;
        public override void Init()
        {
            base.Init();
			this.trailBuffer ??= this.gameObject.GetComponent<ITrailData>();
        }
		protected override void Draw(Material material)
		{
			if (this.buffer == null || this.buffer.Buffer.Size == 0 || material == null || this.trailBuffer == null)
			{
				LogTool.Log("Draw buffer is null, nothing to draw", LogLevel.Warning);
				return;
			}
            var inverseViewMatrix = Camera.main.worldToCameraMatrix.inverse;
            material.SetMatrix("_InvViewMatrix", inverseViewMatrix);

            material.SetBuffer("_TrailHeaderBuffer", this.buffer.Buffer);
            material.SetBuffer("_TrailNodeBuffer", this.trailBuffer.NodeBuffer);
			var b = new Bounds(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawProcedural(material, b, MeshTopology.Points, this.trailBuffer.NodeBuffer.Size);
		}
	}
}
