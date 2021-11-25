using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools;
using UnityTools.Rendering;

namespace FluidSPH
{
	public class SPHMeshRender : MeshRender<Particle>
	{
		protected SPHConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHConfigure>();
		protected SPHConfigure configure;
		public override void OnUpdateDraw()
		{
			this.Configure.D.UpdateGPU(this.dataMaterial);
			base.OnUpdateDraw();
		}
	}
}