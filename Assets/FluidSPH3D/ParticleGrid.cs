using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityTools.Rendering;

namespace UnityTools.Common
{
	public class ParticleGrid : ObjectGrid<ParticleGrid.Particle>, IParticleBuffer<ParticleGrid.Particle>
	{
        [StructLayout(LayoutKind.Sequential)]
        public struct Particle
        {
            public float3 pos;
            public float4 col;
        }
        public GPUBufferVariable<Particle> Buffer => this.particleBufferSorted;
        [SerializeField] protected int numOfParticles = 1024 * 128;

        protected GPUBufferVariable<Particle> particleBuffer = new GPUBufferVariable<Particle>();
        protected GPUBufferVariable<Particle> particleBufferSorted;
        protected GPUBufferVariable<uint2> indexSorted;

        protected void OnEnable()
        {
            this.Init(this.gridData.gridSize);
            this.particleBuffer.InitBuffer(this.numOfParticles, true, false);
			foreach (var i in Enumerable.Range(0, this.particleBuffer.Size))
			{
				var rand = new float3(UnityEngine.Random.value,UnityEngine.Random.value,UnityEngine.Random.value);
				rand -= 0.5f;
				this.particleBuffer.CPUData[i].pos = this.space.TRS.MultiplyPoint(rand);
				this.particleBuffer.CPUData[i].col = 1;
			}
			this.particleBuffer.SetToGPUBuffer(true);
            // this.BuildSortedParticleGridIndex(this.particleBuffer, out this.particleBufferSorted, out this.indexSorted);
        }
        protected void OnDisable()
        {
            this.Deinit();
        }

        protected void Update()
        {
            // this.BuildSortedParticleGridIndex(this.particleBuffer, out this.particleBufferSorted, out this.indexSorted);


            // this.dispatcher.Dispatch(ObjectGrid<Particle>.Kernel.ResetColor, this.particleBuffer.Size);
            // this.dispatcher.Dispatch(ObjectGrid<Particle>.Kernel.UpdateColor, 1);
        }

	}
}
