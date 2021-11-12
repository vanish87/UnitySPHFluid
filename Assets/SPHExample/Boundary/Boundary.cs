
using System.Collections.Generic;
using Unity.Mathematics;
using UnityTools.Common;

namespace FluidSPH
{
	public interface IBoundary
	{
		bool IsActive { get; }
		BoundaryType Type { get; }
		ISpace Space { get; }
		float3 Velocity { get; }
		List<float3> Sample(float density = 1 / 32f);
		void Init(BoundarySetting setting);
	}

	public enum BoundaryType
	{
		Disabled = 0,
		SDFSphere,
		ParticlePlane,
		ParticleSphere,
		ParticleMesh,
	}
	[System.Serializable]
	public class BoundarySetting
	{
		public string name;
		public BoundaryType type = BoundaryType.Disabled;
		public float density = 1 / 32f;
		public Space3D space = new Space3D();
	}

}