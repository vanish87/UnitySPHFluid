using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;

namespace GPUTrail
{
	public class TrailLineDebug : MonoBehaviour
	{
        [SerializeField] protected Shader shader;
        protected DisposableMaterial material;

        protected void OnEnable()
        {
            this.material = new DisposableMaterial(this.shader);
        }
        protected void OnDisable()
        {
            this.material?.Dispose();
        }

        protected void Update()
        {
			var b = new Bounds(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawProcedural(material, b, MeshTopology.Points, 16);
        }
	}
}
