
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.Rendering;

namespace FluidSPH3D
{
	public class SPHParticleRender : MeshRender<Particle>
	{
		protected FluidSPH3DConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<FluidSPH3DConfigure>();
		protected FluidSPH3DConfigure configure;

		protected override void Draw(Material material)
		{
			this.Configure.D.UpdateGPU(material);
			base.Draw(material);
		}

	}
}