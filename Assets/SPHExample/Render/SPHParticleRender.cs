
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.Rendering;

namespace FluidSPH
{
	public class SPHParticleRender : DataRenderBase<Particle>
	{
		[SerializeField] protected Texture particleTexture;
		protected SPHConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHConfigure>();
		protected SPHConfigure configure;
		public override void OnUpdateDraw()
		{
			this.Configure.D.UpdateGPU(this.dataMaterial);
			base.OnUpdateDraw();
		}

	}
}