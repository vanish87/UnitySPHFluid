using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH3D
{
	public class MeshBoundary : PrimitiveBoundary
	{
        [SerializeField] protected Mesh mesh;
        [SerializeField] protected Shader shader;
        [SerializeField] protected int sampleNum = 1024;
        protected DisposableMaterial material;
        public override List<float3> Sample(float density = 1/32f)
        {
            var wpos = Sampler.SampleMeshSurface(this.mesh, this.sampleNum);
            var b = this.mesh.bounds;
            var min = new float3(b.min);
            var size = new float3(b.size);
            var center = new float3(b.center);

            var s = this.transform.localScale.x / size.x;
            this.Space.Scale = size * s;
            this.transform.localScale = this.Space.Scale;
            foreach(var i in Enumerable.Range(0, wpos.Count))
            {
				var np = (wpos[i] - center) / size;
				// np -= 0.5f;
                wpos[i] = this.Space.TRS.MultiplyPoint(np);
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