using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH3D
{
	public class BoundaryConfigure : Configure<BoundaryConfigure.Data>
	{
		[System.Serializable]
		public class Data
		{
			public List<BData> boundaries = new List<BData>();
		}
        [System.Serializable]
		public class BData
		{
            public string name;
			public Space3D space = new Space3D();
		}


        protected void OnDrawGizmos()
        {
            foreach(var b in this.D.boundaries) b.space?.OnDrawGizmos();
        }
	}
}