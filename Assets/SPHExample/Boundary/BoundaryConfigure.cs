using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH
{
	public class BoundaryConfigure : Configure<BoundaryConfigure.Data>
	{
		[System.Serializable]
		public class Data
		{
			public List<BoundarySetting> boundaries = new List<BoundarySetting>();
		}

        protected void OnDrawGizmos()
        {
            foreach(var b in this.D.boundaries) b.space?.OnDrawGizmos();
        }
	}
}