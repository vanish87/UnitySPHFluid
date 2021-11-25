
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.Rendering;

namespace FluidSPH
{
	public class SPHQuadRender : DataRenderBase<Particle>
	{
		[SerializeField] protected Texture particleTexture;
		protected SPHConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHConfigure>();
		protected SPHConfigure configure;
		public override void Init(params object[] parameters)
		{
			base.Init(parameters);
			this.dataMaterial.Data.mainTexture = this.particleTexture;
		}
		public override void OnUpdateDraw()
		{
			this.Configure.D.UpdateGPU(this.dataMaterial);
			base.OnUpdateDraw();
		}

	}
}