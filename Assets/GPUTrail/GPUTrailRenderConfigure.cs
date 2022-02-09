
using System.Collections.Generic;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace GPUTrail
{
	public class GPUTrailRenderConfigure : Configure<GPUTrailRenderConfigure.Data>
	{
		[System.Serializable]
		public class Data : GPUContainer
		{

			[Shader(Name = "_Thickness")] public float thickness = 0.1f;
			[Shader(Name = "_CornerDivision")] public int cornerDivision = 4;
			// cos(angle) value for corner division
			// -1 for always division
			// 1 for never division
			[Shader(Name = "_AngleThreshold")] public float angleThreshold = 0f;
			[Shader(Name = "_MaxSpeedScale")] public float maxSpeedScale = 0.1f;
			public List<Gradient> gradients = new List<Gradient>();
			[Shader(Name = "_GradientTexture")] public Texture2D gradientTexture;
			[Shader(Name = "_GradientTextureHeight")] public int gradientTextureHeight;

		}
		public override void Save()
		{
			this.D.gradientTexture?.DestoryObj();
			this.D.gradientTexture = null;
			base.Save();
		}
		public override void Load()
		{
			base.Load();
			this.UpdateGradient();
		}
		protected void UpdateGradient()
		{
			var tex = this.D.gradientTexture;
			if (tex == null || tex.height != this.D.gradients.Count)
			{
				this.D.gradientTexture?.DestoryObj();
				this.D.gradientTexture = PalletTexture.GenerateGradientTexture(this.D.gradients);
			}
			else
			{
				PalletTexture.UpdateGradientTexture(this.D.gradientTexture, this.D.gradients);
			}
			this.D.gradientTextureHeight = this.D.gradientTexture.height;
		}
		protected override void Update()
		{
			base.Update();
			this.UpdateGradient();
		}
	}

}
