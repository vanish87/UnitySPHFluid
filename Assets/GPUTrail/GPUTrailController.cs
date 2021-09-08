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
	public class GPUTrailController<T> : MonoBehaviour, IInitialize, IDataBuffer<TrailNode>
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
			[Shader(Name = "_TrailHeaderBuffer")] public GPUBufferVariable<TrailHeader> trailHeaderBuffer = new GPUBufferVariable<TrailHeader>();
			[Shader(Name = "_TrailNodeBuffer")] public GPUBufferVariable<TrailNode> trailNodeBuffer = new GPUBufferVariable<TrailNode>();
			[Shader(Name = "_TrailNodeIndexBufferAppend")] public GPUBufferAppendConsume<int> trailNodeIndexBufferAppend = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailNodeIndexBufferConsume")] public GPUBufferAppendConsume<int> trailNodeIndexBufferConsume = new GPUBufferAppendConsume<int>();

			//we need fixed particle buffer to update trails
			[Shader(Name = "_SourceBuffer")] public GPUBufferVariable<T> sourceBuffer = new GPUBufferVariable<T>();

			[Shader(Name = "_EmitTrailNum")] public int emitTrailNum = 2048;
			[Shader(Name = "_EmitTrailLen")] public int emitTrailLen = 128;
		}
		public bool Inited => this.inited;
		public GPUBufferVariable<TrailNode> Buffer => this.trailData.trailNodeBuffer;

		[SerializeField] protected ComputeShader trailCS;
		[SerializeField] protected GPUTrailData trailData = new GPUTrailData();

		protected GPUTrailConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<GPUTrailConfigure>();
		protected GPUTrailConfigure configure;
		protected bool inited = false;
		protected ComputeShaderDispatcher<Kernel> dispatcher;
		protected IDataBuffer<T> source;

		public void Init()
		{
			this.source = ObjectTool.FindAllObject<IDataBuffer<T>>().FirstOrDefault();
			LogTool.AssertNotNull(this.source);

			this.Configure.Initialize();

			var headNum = this.Configure.D.trailHeaderNum;
			var nodeNum = this.Configure.D.trailNodeNum;
			this.trailData.trailHeaderBuffer.InitBuffer(headNum);
			this.trailData.trailNodeBuffer.InitBuffer(nodeNum);

			this.trailData.trailNodeIndexBufferAppend.InitAppendBuffer(nodeNum);
			this.trailData.trailNodeIndexBufferConsume.InitAppendBuffer(this.trailData.trailNodeIndexBufferAppend);

			this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.trailCS);
			foreach (Kernel k in Enum.GetValues(typeof(Kernel)))
			{
				this.dispatcher.AddParameter(k, this.Configure.D);
				this.dispatcher.AddParameter(k, this.trailData);
				this.dispatcher.AddParameter(k, this.source.Buffer);
			}

			this.trailData.trailNodeIndexBufferAppend.ResetCounter();

			this.dispatcher.Dispatch(Kernel.InitHeader, headNum);
			this.dispatcher.Dispatch(Kernel.InitNode, nodeNum);

			this.dispatcher.Dispatch(Kernel.EmitTrail, this.trailData.emitTrailNum);

			this.inited = true;
		}

		public void Deinit()
		{
			this.trailData?.Release();
		}

		protected void Update()
		{
			if (this.trailData.sourceBuffer.Size != this.source.Buffer.Size)
			{
				//Update trail buffer after particle buffer created
				this.trailData.sourceBuffer.InitBuffer(this.source.Buffer.Size);
			}

			var headerNum = this.Configure.D.trailHeaderNum;
			var pNum = this.source.Buffer.Size;

			this.dispatcher.Dispatch(Kernel.UpdateParticle, pNum);
			this.dispatcher.Dispatch(Kernel.UpdateFromParticle, headerNum);
		}
		protected void OnEnable()
		{
			if (!this.Inited) this.Init();
		}
		protected void OnDisable()
		{
			this.Deinit();
		}
	}
}