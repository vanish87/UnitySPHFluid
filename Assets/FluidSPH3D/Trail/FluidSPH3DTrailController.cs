
using GPUTrail;

namespace FluidSPH3D
{
	public class FluidSPH3DTrailController: GPUTrailController<TrailParticle>
	{
		public override void Init()
		{
			base.Init();
			// this.EmitTrail(this.trailData.emitTrailNum, this.trailData.maxTrailLen);
		}
		public override void Deinit()
		{
			base.Deinit();
		}
		protected override void Update()
		{
			base.Update();
		}
	}
}