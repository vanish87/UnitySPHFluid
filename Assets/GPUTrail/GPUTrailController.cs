using System;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace GPUTrail
{
	[StructLayout(LayoutKind.Sequential)]
	public class TrailSource
	{
		public float3 pos;
	}

	public interface ITrailSource<T> where T : TrailSource
	{
		GPUBufferVariable<T> Buffer { get; }
	}

	public class GPUTrailController<T> : MonoBehaviour, IInitialize, IDataBuffer<TrailNode> where T : TrailSource
	{
		public enum Kernel
		{
			InitHeader,
			InitNode,
			EmitTrail,
			UpdateSourceBuffer,
			UpdateFromSourceBuffer,
			AppendDeadToNodePool,
		}
		[System.Serializable]
		public class GPUTrailData : GPUContainer
		{
			[Shader(Name = "_TrailHeaderBuffer")] public GPUBufferVariable<TrailHeader> trailHeaderBuffer = new GPUBufferVariable<TrailHeader>();
			[Shader(Name = "_TrailHeaderIndexBufferAppend")] public GPUBufferAppendConsume<int> trailHeaderIndexBufferAppend = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailHeaderIndexBufferConsume")] public GPUBufferAppendConsume<int> trailHeaderIndexBufferConsume = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailNodeBuffer")] public GPUBufferVariable<TrailNode> trailNodeBuffer = new GPUBufferVariable<TrailNode>();
			[Shader(Name = "_TrailNodeIndexBufferAppend")] public GPUBufferAppendConsume<int> trailNodeIndexBufferAppend = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailNodeIndexBufferConsume")] public GPUBufferAppendConsume<int> trailNodeIndexBufferConsume = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailNodeIndexDeadBufferAppend")] public GPUBufferAppendConsume<int> trailNodeIndexDeadBufferAppend = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_TrailNodeIndexDeadBufferConsume")] public GPUBufferAppendConsume<int> trailNodeIndexDeadBufferConsume = new GPUBufferAppendConsume<int>();

			//optional local buffer: sometimes a local copy of source buffer is necessary
			[Shader(Name = "_SourceBuffer")] public GPUBufferVariable<T> sourceBuffer = new GPUBufferVariable<T>();
			[Shader(Name = "_EmitTrailNum")] public int emitTrailNum = 2048;
			[Shader(Name = "_EmitTrailLen")] public int emitTrailLen = 128;
		}
		public bool Inited => this.inited;
		public GPUBufferVariable<TrailNode> Buffer => this.trailData.trailNodeBuffer;
		public ISpace Space => this.Configure.D.space;

		[SerializeField] protected ComputeShader trailCS;
		[SerializeField] protected GPUTrailData trailData = new GPUTrailData();

		protected GPUTrailConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<GPUTrailConfigure>();


		protected GPUTrailConfigure configure;
		protected bool inited = false;
		protected ComputeShaderDispatcher<Kernel> dispatcher;
		protected ITrailSource<T> source;

		public void Init()
		{
			this.source = ObjectTool.FindAllObject<ITrailSource<T>>().FirstOrDefault();
			LogTool.AssertNotNull(this.source);

			this.Configure.Initialize();

			var headNum = this.Configure.D.trailHeaderNum;
			var nodeNum = this.Configure.D.trailNodeNum;
			this.trailData.trailHeaderBuffer.InitBuffer(headNum);
			this.trailData.trailHeaderIndexBufferAppend.InitAppendBuffer(headNum);
			this.trailData.trailHeaderIndexBufferConsume.InitAppendBuffer(this.trailData.trailHeaderIndexBufferAppend);

			this.trailData.trailNodeBuffer.InitBuffer(nodeNum);
			this.trailData.trailNodeIndexBufferAppend.InitAppendBuffer(nodeNum);
			this.trailData.trailNodeIndexBufferConsume.InitAppendBuffer(this.trailData.trailNodeIndexBufferAppend);
			this.trailData.trailNodeIndexDeadBufferAppend.InitAppendBuffer(nodeNum);
			this.trailData.trailNodeIndexDeadBufferConsume.InitAppendBuffer(this.trailData.trailNodeIndexDeadBufferAppend);

			this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.trailCS);
			foreach (Kernel k in Enum.GetValues(typeof(Kernel)))
			{
				this.dispatcher.AddParameter(k, this.Configure.D);
				this.dispatcher.AddParameter(k, this.trailData);
				this.dispatcher.AddParameter(k, this.source.Buffer);
			}

			this.dispatcher.Dispatch(Kernel.InitHeader, headNum);
			this.dispatcher.Dispatch(Kernel.InitNode, nodeNum);

			LogTool.AssertIsTrue(this.trailData.emitTrailLen > 0);
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
				//Update trail buffer after source buffer created
				this.trailData.sourceBuffer.InitBuffer(this.source.Buffer.Size);
			}

			var headerNum = this.Configure.D.trailHeaderNum;
			var pNum = this.source.Buffer.Size;

			this.dispatcher.Dispatch(Kernel.UpdateSourceBuffer, pNum);

			this.trailData.trailNodeIndexDeadBufferAppend.ResetCounter();
			this.dispatcher.Dispatch(Kernel.UpdateFromSourceBuffer, headerNum);

			var counter = this.trailData.trailNodeIndexDeadBufferAppend.GetCounter();
			if (counter > 0)
			{
				this.dispatcher.Dispatch(Kernel.AppendDeadToNodePool, counter);
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
	}
}