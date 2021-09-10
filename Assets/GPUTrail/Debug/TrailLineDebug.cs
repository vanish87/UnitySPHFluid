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
        [SerializeField] protected float thiness = 0.1f;
        [SerializeField] protected float miterLimit = 0.75f;
        [SerializeField] protected bool zScale = true;
        protected DisposableMaterial material;
		protected GPUBufferVariable<float4> trailGPUData = new GPUBufferVariable<float4>();

        protected void UpdateBuffer()
        {
            if(this.trailPos.Count != this.trailGPUData.Size) this.trailGPUData.InitBuffer(this.trailPos.Count, true);
            foreach(var i in Enumerable.Range(0, this.trailGPUData.Size))
            {
                this.trailGPUData.CPUData[i] = this.trailPos[i];
            }
            this.trailGPUData.SetToGPUBuffer();
        }
        protected void OnEnable()
        {
            this.material = new DisposableMaterial(this.shader);
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
            mat.SetFloat("_Thiness", this.thiness);
            mat.SetFloat("_MiterLimit", this.miterLimit);
            mat.SetInt("_zScale", this.zScale?1:0);
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
