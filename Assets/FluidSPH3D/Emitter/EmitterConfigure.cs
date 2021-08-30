using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH3D
{
    [ExecuteInEditMode]
	public class EmitterConfigure : Configure<EmitterConfigure.Data>
	{
		[System.Serializable]
		public class Data
		{
			public List<EData> emitters = new List<EData>();
		}
        [System.Serializable]
        public class EData
        {
            public string name;
            public bool isActive;
            public int particlePerEmit = 8;
            public Space3D space = new Space3D();
        }

        public void OnDrawGizmos()
        {
			foreach (var e in this.D.emitters) if (e.isActive) e.space?.OnDrawGizmos();
        }
	}
}
