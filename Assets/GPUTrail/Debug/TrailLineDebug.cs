using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace GPUTrail
{
	public class TrailLineDebug : MonoBehaviour
	{
        [SerializeField] protected Shader shader;
        protected DisposableMaterial material;

        public float4 p0;
        public float4 p1;
        public float4 p2;
        public float4 p3;

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
            Material mat = this.material;
            mat.SetVector("p0", this.p0);
            mat.SetVector("p1", this.p1);
            mat.SetVector("p2", this.p2);
            mat.SetVector("p3", this.p3);
			var b = new Bounds(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawProcedural(material, b, MeshTopology.Points, 1);
        }
        protected void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(new Vector3(this.p0.x, this.p0.y, this.p0.z), 0.1f);
            Gizmos.DrawWireSphere(new Vector3(this.p1.x, this.p1.y, this.p1.z), 0.1f);
            Gizmos.DrawWireSphere(new Vector3(this.p2.x, this.p2.y, this.p2.z), 0.1f);
            Gizmos.DrawWireSphere(new Vector3(this.p3.x, this.p3.y, this.p3.z), 0.1f);
        }
	}
}
