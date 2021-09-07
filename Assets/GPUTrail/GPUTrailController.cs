using System;
using System.Linq;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace GPUTrail
{
	public class GPUTrailController : MonoBehaviour, IInitialize, ITrailData, IDataBuffer<TrailHeader>
	{
		public enum Kernel
		{
			InitHeader,
			InitNode,
			EmitTrail,
			UpdateParticle,
			UpdateFromParticle,
		}
		[System.Serializable]
		public class GPUTrailData : GPUContainer
		{
			[Shader(Name = "_TrailNodeBuffer")] public GPUBufferVariable<TrailNode> trailNodeBuffer = new GPUBufferVariable<TrailNode>();
			[Shader(Name = "_TrailHeaderBuffer")] public GPUBufferVariable<TrailHeader> trailHeaderBuffer = new GPUBufferVariable<TrailHeader>();
			[Shader(Name = "_TrailHeaderIndexBufferAppend")] public GPUBufferAppendConsume<int> trailIndexHeaderBufferAppend = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailHeaderIndexBufferConsume")] public GPUBufferAppendConsume<int> trailIndexHeaderBufferConsume = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailNodeIndexBufferAppend")] public GPUBufferAppendConsume<int> trailNodeIndexBufferAppend = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailNodeIndexBufferConsume")] public GPUBufferAppendConsume<int> trailNodeIndexBufferConsume = new GPUBufferAppendConsume<int>();


			//particle buffer from fluid sph is rearranged every frame
			//we need fixed particle buffer to update trails
			[Shader(Name = "_FixedParticleBuffer")] public GPUBufferVariable<FluidSPH3D.Particle> fixedParticleBuffer = new GPUBufferVariable<FluidSPH3D.Particle>();

			[Shader(Name = "_EmitTrailNum")] public int emitTrailNum = 2048;
			[Shader(Name = "_EmitTrailLen")] public int emitTrailLen = 128;
		}
		public bool Inited => this.inited;
		public GPUBufferVariable<TrailHeader> Buffer => this.trailData.trailHeaderBuffer;
		public GPUBufferVariable<TrailNode> NodeBuffer => this.trailData.trailNodeBuffer;

		[SerializeField] protected ComputeShader trailCS;
		[SerializeField] protected GPUTrailData trailData = new GPUTrailData();

		protected GPUTrailConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<GPUTrailConfigure>();
		protected GPUTrailConfigure configure;
		protected bool inited = false;
		protected ComputeShaderDispatcher<Kernel> dispatcher;
		protected IDataBuffer<FluidSPH3D.Particle> particleBuffer;

		public void Init()
		{
			this.particleBuffer = ObjectTool.FindAllObject<IDataBuffer<FluidSPH3D.Particle>>().FirstOrDefault();

			this.Configure.Initialize();

			var headNum = this.Configure.D.trailHeaderNum;
			var nodeNum = this.Configure.D.trailNodeNum;
			this.trailData.trailHeaderBuffer.InitBuffer(headNum, true, false);
			this.trailData.trailNodeBuffer.InitBuffer(nodeNum, true, false);

			this.trailData.trailIndexHeaderBufferAppend.InitAppendBuffer(headNum);
			this.trailData.trailIndexHeaderBufferConsume.InitAppendBuffer(this.trailData.trailIndexHeaderBufferAppend);

			this.trailData.trailNodeIndexBufferAppend.InitAppendBuffer(nodeNum);
			this.trailData.trailNodeIndexBufferConsume.InitAppendBuffer(this.trailData.trailNodeIndexBufferAppend);

			this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.trailCS);
			foreach (Kernel k in Enum.GetValues(typeof(Kernel)))
			{
				this.dispatcher.AddParameter(k, this.Configure.D);
				this.dispatcher.AddParameter(k, this.trailData);
				this.dispatcher.AddParameter(k, this.particleBuffer.Buffer);
			}

			this.trailData.trailIndexHeaderBufferAppend.ResetCounter();
			this.trailData.trailNodeIndexBufferAppend.ResetCounter();

			this.dispatcher.Dispatch(Kernel.InitHeader, headNum);
			this.dispatcher.Dispatch(Kernel.InitNode, nodeNum);

			this.dispatcher.Dispatch(Kernel.EmitTrail, this.trailData.emitTrailNum);


			this.trailData.trailHeaderBuffer.GetToCPUData();
			this.trailData.trailNodeBuffer.GetToCPUData();

			var idnex = new int[32];
			this.trailData.trailNodeIndexBufferAppend.Data.GetData(idnex);


			foreach(var d in this.trailData.trailHeaderBuffer.CPUData)
			{
				if(d.maxLength > 0) Debug.Log(d.headNodeIndex + " " + d.maxLength + " " + d.currentLength);
			}

			this.inited = true;
		}

		public void Deinit()
		{
			this.trailData?.Release();
		}

		protected void Update()
		{
			if (this.trailData.fixedParticleBuffer.Size != this.particleBuffer.Buffer.Size)
			{
				//Update trail buffer after particle buffer created
				this.trailData.fixedParticleBuffer.InitBuffer(this.particleBuffer.Buffer.Size);
			}

			var headerNum = this.Configure.D.trailHeaderNum;
			var pNum = this.particleBuffer.Buffer.Size;

			this.dispatcher.Dispatch(Kernel.UpdateParticle, pNum);
			// if(Input.GetKeyDown(KeyCode.T))
			{
				this.dispatcher.Dispatch(Kernel.UpdateFromParticle, headerNum);
				this.trailData.trailHeaderBuffer.GetToCPUData();
				this.trailData.trailNodeBuffer.GetToCPUData();
				// foreach (var d in this.trailData.trailHeaderBuffer.CPUData)
				// {
				// 	if (d.currentLength > 0) Debug.Log(d.headNodeIndex + " " + d.maxLength + " " + d.currentLength);
				// }

				// var hdata = this.trailData.trailHeaderBuffer.CPUData;
				// var ndata = this.trailData.trailNodeBuffer.CPUData;
				// var header = hdata.Where(hd=>hd.maxLength > 0).FirstOrDefault();
				// var n = header.headNodeIndex;
				// Debug.Log("hid:" + header.headNodeIndex + " current len:" + header.currentLength);
				// while(n != -1)
				// {
				// 	Debug.Log("idx:" + ndata[n].idx + " prev:" + ndata[n].prev + " next:"+ ndata[n].next);
				// 	n = ndata[n].next;
				// }
			}
		}
		protected void OnEnable()
		{
			if (!this.Inited) this.Init();
		}
		protected void OnDisable()
		{
			this.Deinit();
		}
		protected void OnDrawGizmos()
		{

				var hdata = this.trailData.trailHeaderBuffer.CPUData;
				var ndata = this.trailData.trailNodeBuffer.CPUData;
				var header = hdata[0];
				var n = header.headNodeIndex;
				while(n != -1)
				{
					var current = ndata[n];
					// Debug.Log("idx:" + ndata[n].idx + " prev:" + ndata[n].prev + "next:"+ ndata[n].next);

					if (current.prev != -1) Gizmos.DrawLine(current.pos, ndata[current.prev].pos);
					n = ndata[n].next;
				}
		}
	}
}