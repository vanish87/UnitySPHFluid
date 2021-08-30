
using System.Collections.Generic;
using Unity.Mathematics;
using UnityTools.Common;

namespace FluidSPH3D
{
	public interface IBoundary
	{
		ISpace Space { get; }
		List<float3> Sample(float density = 1 / 32f);
	}

}