using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.ComputeShaderTool;

namespace UnityTools.Common
{
	public class ParticleIndexGrid : GPUGrid<uint2>
	{
		public enum Kernel
		{
			ParticleToGridIndex,
			ClearGridIndex,
			BuildGridIndex,
			BuildSortedParticle,

			ResetColor,
			UpdateColor,
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct Particle
		{
			public float3 position;
			public float4 color;
		}

		[System.Serializable]
		public class GridBuffer : GPUContainer
		{
			[Shader(Name = "_ParticleBufferRead")] public GPUBufferVariable<Particle> particleBuffer = new GPUBufferVariable<Particle>();
			[Shader(Name = "_ParticleBufferSorted")] public GPUBufferVariable<Particle> particleBufferSorted = new GPUBufferVariable<Particle>();
			[Shader(Name = "_ParticleGridIndexBufferRead")] public GPUBufferVariable<uint2> particleGridIndexBufferRead = new GPUBufferVariable<uint2>();
			[Shader(Name = "_ParticleGridIndexBufferWrite")] public GPUBufferVariable<uint2> particleGridIndexBufferWrite = new GPUBufferVariable<uint2>();

			[Shader(Name = "_TargetPos")] public float3 pos = new float3(0, 0, 0);

		}

		public GPUBufferVariable<Particle> Buffer => this.gridbuffer.particleBufferSorted;

		[SerializeField] protected ComputeShader gridCS;

		protected GridIndexSort Sort => this.sort ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<GridIndexSort>();
		protected GridIndexSort sort;
		protected int numOfParticles = 8192 * 16;
		[SerializeField] protected GridBuffer gridbuffer = new GridBuffer();
		protected ComputeShaderDispatcher<Kernel> dispatcher;
		public void Init()
		{
			base.Init(this.gridData.gridSize);

			var p = this.gridbuffer;
			p.particleBuffer.InitBuffer(this.numOfParticles, true, false);
			p.particleBufferSorted.InitBuffer(this.numOfParticles);
			p.particleGridIndexBufferRead.InitBuffer(this.numOfParticles);
			p.particleGridIndexBufferWrite.InitBuffer(p.particleGridIndexBufferRead);

			foreach (var i in Enumerable.Range(0, p.particleBuffer.Size))
			{
				var rand = new float3(UnityEngine.Random.value,UnityEngine.Random.value,UnityEngine.Random.value);
				rand -= 0.5f;
				p.particleBuffer.CPUData[i].position = this.space.TRS.MultiplyPoint(rand);
				p.particleBuffer.CPUData[i].color = 1;
			}
			p.particleBuffer.SetToGPUBuffer(true);

			this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.gridCS);
			foreach (Kernel k in Enum.GetValues(typeof(Kernel)))
			{
				this.dispatcher.AddParameter(k, this.gridData);
				this.dispatcher.AddParameter(k, this.gridbuffer);
			}

			this.BuildSortedParticleGridIndex();

		}

		protected void BuildSortedParticleGridIndex()
		{
			var p = this.gridbuffer;
			this.dispatcher.Dispatch(Kernel.ParticleToGridIndex, numOfParticles);
			this.Sort.Sort(ref p.particleGridIndexBufferWrite);

			var s = this.gridData.gridSize;
			this.dispatcher.Dispatch(Kernel.ClearGridIndex, s.x, s.y, s.z);
			this.dispatcher.Dispatch(Kernel.BuildGridIndex, this.numOfParticles);
			this.dispatcher.Dispatch(Kernel.BuildSortedParticle, this.numOfParticles);
		}

		public override void Deinit()
		{
			base.Deinit();
			this.gridbuffer?.Release();
		}

		protected void Update()
		{
			this.BuildSortedParticleGridIndex();
			this.dispatcher.Dispatch(Kernel.ResetColor, this.numOfParticles);
			this.dispatcher.Dispatch(Kernel.UpdateColor, 1);
		}
		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			Gizmos.DrawWireSphere(this.gridbuffer.pos + this.gridData.gridSpacing * 0.5f, 0.1f);
		}

		protected void Start()
		{
			this.Init();
		}
		protected void OnDestroy()
		{
			this.Deinit();
		}
	}
}
