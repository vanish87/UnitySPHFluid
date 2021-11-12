using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH
{
	public class EmitterConfigure : Configure<EmitterConfigure.Data>
	{
		[System.Serializable]
		public class Data
		{
			public List<EmitterSetting> emitters = new List<EmitterSetting>();
		}
        [System.Serializable]
        public class EmitterSetting
        {
            public string name;
            public bool isActive;
            public int particlePerSecond = 8;
            public Space3D space = new Space3D();
        }

        public void OnDrawGizmos()
        {
			foreach (var e in this.D.emitters) if (e.isActive) e.space?.OnDrawGizmos();
        }
	}
}
