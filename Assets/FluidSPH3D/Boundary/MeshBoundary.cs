using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH3D
{
	public class MeshBoundary : PrimitiveBoundary
	{
        [SerializeField] protected Mesh mesh;
        [SerializeField] protected float meshScale = 1;
        [SerializeField] protected int sampleNum = 1024;
        public override List<float3> Sample(float density = 1/32f)
        {
            var wpos = Sampler.SampleMeshSurface(this.mesh, this.sampleNum);
            var b = this.mesh.bounds;
            var min = new float3(b.min);
            var size = new float3(b.size);

            this.Space.Scale = size * this.meshScale;
            foreach(var i in Enumerable.Range(0, wpos.Count))
            {
				var np = (wpos[i] - min) / size;
                np -= 0.5f;
                wpos[i] = this.Space.TRS.MultiplyPoint(np);
            }

            return wpos;
        }
        protected override void Update()
        {
			this.data.space.Center = this.gameObject.transform.localPosition;
			this.data.space.Rotation = this.gameObject.transform.localRotation;
        }
	}

}