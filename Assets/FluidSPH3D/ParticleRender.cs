using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Debuging;

namespace UnityTools.Common
{
	public interface IParticleBuffer<T>
	{
		GPUBufferVariable<T> Buffer { get; }
	}

	public class ParticleRender<T> : MonoBehaviour
	{
		protected IParticleBuffer<T> buffer;

		protected DisposableMaterial particleMaterial;
		[SerializeField] protected Shader particleShader;
		[SerializeField] protected Mesh particleMesh;

		public class RenderData : GPUContainer
		{
			public GPUBufferVariable<uint> particleIndirectBuffer = new GPUBufferVariable<uint>();

		}

		protected RenderData data = new RenderData();

		public void Start()
		{
			if(this.buffer == null)
			{
				this.buffer = this.gameObject.GetComponent<IParticleBuffer<T>>();
			}
			this.particleMaterial = new DisposableMaterial(this.particleShader);

			this.data.particleIndirectBuffer.InitBuffer(5, true, true, ComputeBufferType.IndirectArguments);
			this.SetupIndirectBuffer(this.data.particleIndirectBuffer, this.particleMesh, this.buffer.Buffer.Size);
		}


		protected void SetupIndirectBuffer(GPUBufferVariable<uint> buffer, Mesh mesh, int count)
		{
			var args = buffer.CPUData;
			var subIndex = 0;
			args[0] = (uint)mesh.GetIndexCount(subIndex);
			args[1] = (uint)count;
			args[2] = (uint)mesh.GetIndexStart(subIndex);
			args[3] = (uint)mesh.GetBaseVertex(subIndex);
			buffer.SetToGPUBuffer();
		}

		protected void OnDestroy()
		{
			this.data?.Release();
			this.particleMaterial?.Dispose();
		}

		protected void Update()
		{
			this.Draw(this.particleMesh, this.particleMaterial, this.data.particleIndirectBuffer);
		}

		protected void Draw(Mesh mesh, Material material, GPUBufferVariable<uint> indirectBuffer)
		{
			if (this.buffer == null || mesh == null || material == null || indirectBuffer == null)
			{
				LogTool.Log("Draw buffer is null, nothing to draw", LogLevel.Warning);
				return;
			}
			this.data.UpdateGPU(material);
            material.SetBuffer("_ParticleBuffer", this.buffer.Buffer);
			var b = new Bounds(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawMeshInstancedIndirect(mesh, 0, material, b, indirectBuffer, 0);
		}
	}
}
