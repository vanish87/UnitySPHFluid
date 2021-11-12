using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH
{
	public class MeshBoundary : PrimitiveBoundary
	{
        [SerializeField] protected Mesh mesh;
        [SerializeField] protected Shader shader;
        [SerializeField] protected int sampleNum = 1024;
        [SerializeField] protected float3 meshOriginalSize;
        protected DisposableMaterial material;
        public override List<float3> Sample(float density = 1/32f)
        {
            var wpos = Sampler.SampleMeshSurface(this.mesh, this.sampleNum);
            var b = this.mesh.bounds;
            this.meshOriginalSize = new float3(b.size);
            var center = new float3(b.center);

            this.Space.Scale = this.transform.localScale;
            foreach(var i in Enumerable.Range(0, wpos.Count))
            {
				var np = (wpos[i] - center) / this.meshOriginalSize;
				// np -= 0.5f;
                // wpos[i] = this.Space.TRS.MultiplyPoint(np);
                wpos[i] = np;
            }

            return wpos;
        }
        protected void OnEnable()
        {
            // this.material = new DisposableMaterial(this.shader);
            // var meshRender = this.gameObject.FindOrAddTypeInComponentsAndChildren<MeshRenderer>();
            // var meshFilter = this.gameObject.FindOrAddTypeInComponentsAndChildren<MeshFilter>();

            // meshFilter.sharedMesh = this.mesh;
            // meshRender.sharedMaterial = this.material;
        }
        protected void OnDisable()
        {
            // this.material?.Dispose();
        }
        // protected override void Update()
        // {
        //     this.data.space.Center = this.gameObject.transform.localPosition;
        //     this.data.space.Rotation = this.gameObject.transform.localRotation;
        // }
	}

}