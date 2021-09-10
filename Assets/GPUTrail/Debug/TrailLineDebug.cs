using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace GPUTrail
{
    [ExecuteInEditMode]
	public class TrailLineDebug : MonoBehaviour
	{
        [SerializeField] protected List<float4> trailPos = new List<float4>();
        [SerializeField] protected Shader shader;
        protected DisposableMaterial material;
		protected GPUBufferVariable<float4> trailGPUData = new GPUBufferVariable<float4>();

        protected void UpdateBuffer()
        {
            foreach(var i in Enumerable.Range(0, this.trailGPUData.Size))
            {
                this.trailGPUData.CPUData[i] = this.trailPos[i];
            }
            this.trailGPUData.SetToGPUBuffer();
        }
        protected void OnEnable()
        {
            this.material = new DisposableMaterial(this.shader);
			this.trailGPUData.InitBuffer(this.trailPos.Count, true, true);
        }
        protected void OnDisable()
        {
            this.material?.Dispose();
            this.trailGPUData?.Release();
        }

        protected void Update()
        {
            this.UpdateBuffer();

            Material mat = this.material;
            mat.SetBuffer("_TrailData", this.trailGPUData);
            mat.SetInt("_TrailDataCount", this.trailGPUData.Size);
			var b = new Bounds(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawProcedural(material, b, MeshTopology.Points, this.trailGPUData.Size);
        }
        protected void OnDrawGizmos()
        {
            foreach(var p in this.trailPos)
            {
				Gizmos.DrawWireSphere(new Vector3(p.x, p.y, p.z), 0.1f);
            }
        }
	}
}
