
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.Rendering;

namespace FluidSPH
{
	public class SPHParticleRender : MeshRender<Particle>
	{
		// [SerializeField] protected Shader quadShader;
		protected SPHConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<SPHConfigure>();
		protected SPHConfigure configure;
		// protected DisposableMaterial quadMaterial;
		public override void Init(params object[] parameters)
		{
			base.Init();
			// this.quadMaterial = new DisposableMaterial(this.quadShader);
		}
		public override void Deinit(params object[] parameters)
		{
			base.Deinit();
			// this.quadMaterial?.Dispose();
		}

		protected override void Draw(Material material)
		{
			// if(this.Configure.D.isRenderQuad)
			// {
			// 	Material mat = this.quadMaterial;
			// 	this.Configure.D.UpdateGPU(mat);
			// 	mat.SetBuffer("_ParticleBuffer", this.buffer.Buffer);
			// 	var b = new Bounds(Vector3.zero, Vector3.one * 10000);
			// 	Graphics.DrawProcedural(mat, b, MeshTopology.Points, this.buffer.Buffer.Size);
			// }
			// else
			{
				this.Configure.D.UpdateGPU(material);
				base.Draw(material);
			}

		}

	}
}