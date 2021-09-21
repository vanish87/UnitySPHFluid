using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH3D
{
	public class SPHGrid : ObjectGrid<Particle, uint2>
	{
		public Data GridGPUData => this.gridData;
        public void Init(Space3D space, float gridSpacing)
        {
            this.space = space;
            this.Init(gridSpacing);
        }
	}
}
