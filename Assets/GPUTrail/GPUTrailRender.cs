using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Attributes;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace GPUTrail
{
	public class GPUTrailRender : DataRenderBase<TrailNode>
	{
		[SerializeField] protected string layer;
		[SerializeField] protected bool allCamera;
		[SerializeField, DisableEdit] protected Camera referenceCamera;
		protected GPUTrailRenderConfigure Configure => this.configure ??= this.GetComponentInChildren<GPUTrailRenderConfigure>();
		protected GPUTrailRenderConfigure configure;
		public override void OnUpdateDraw()
		{
			Material mat = this.dataMaterial;
			if (this.Source == null || this.Source.Buffer.Size == 0 || mat == null)
			{
				LogTool.Log("Draw buffer is null, nothing to draw", LogLevel.Warning);
				return;
			}

			this.Configure.D.UpdateGPU(mat);
			mat.SetBuffer("_TrailNodeBuffer", this.Source.Buffer);

			Graphics.DrawProcedural(mat, this.Source.Space.Bound, MeshTopology.Points, this.Source.Buffer.Size);
			//, 1,
									// this.allCamera?null:this.referenceCamera, null, UnityEngine.Rendering.ShadowCastingMode.Off, false,
									// LayerMask.NameToLayer(this.layer));
		}
	}
}
