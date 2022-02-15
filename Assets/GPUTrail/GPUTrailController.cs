using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Debuging;
using UnityTools.Rendering;

namespace GPUTrail
{
	public interface ITrailSource<TrailSource, EmitSource>
	{
		//Trail will emit all data from EmitBuffer
		//usually it just index buffer
		GPUBufferAppendConsume<EmitSource> EmitBuffer { get; }
		//And will update trail node from SourceBuffer
		GPUBufferVariable<TrailSource> SourceBuffer { get; }

		void InitBuffer();
		void DeinitBuffer();
	}

	public class GPUTrailController<TrailSource, EmitSource> : MonoBehaviour, IInitialize, IDataBuffer<TrailNode>
	{
		public enum Kernel
		{
			InitHeader,
			InitNode,
			EmitTrail,
			EmitTrailFromSource,
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
		protected ITrailSource<TrailSource, EmitSource> source;

		public virtual void Init(params object[] parameters)
		{
			if(this.Inited) return;

			this.source = ObjectTool.FindAllObject<ITrailSource<TrailSource, EmitSource>>().FirstOrDefault();
			LogTool.AssertNotNull(this.source);

			this.Configure.Init();

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
				this.dispatcher.AddParameter(k, this.source.SourceBuffer);
				this.dispatcher.AddParameter(k, this.source.EmitBuffer);
			}

			this.dispatcher.Dispatch(Kernel.InitHeader, headNum);
			this.dispatcher.Dispatch(Kernel.InitNode, nodeNum);

			this.inited = true;
		}

		public virtual void Deinit(params object[] parameters)
		{
			this.trailData?.Release();
		}
		protected void EmitTrail(int num, int2 lenMinMax)
		{
            LogTool.AssertIsTrue(num > 0);
            LogTool.AssertIsTrue(lenMinMax.x > 0);
            LogTool.AssertIsTrue(lenMinMax.y > 0);

			this.Configure.D.emitTrailNum = num;
			this.Configure.D.trailLengthMinMax = lenMinMax;
            this.dispatcher.Dispatch(Kernel.EmitTrail, num);
		}
		protected virtual void Update()
		{
			this.dispatcher.Dispatch(Kernel.EmitTrailFromSource, this.Configure.D.emitTrailNum);

			var headerNum = this.Configure.D.trailHeaderNum;
			this.dispatcher.Dispatch(Kernel.UpdateFromSourceBuffer, headerNum);

			// count.z= this.trailData.trailNodeIndexDeadBufferAppend.GetCounter();

			const int appnedCount = 1024 * 512;
			this.dispatcher.Dispatch(Kernel.AppendDeadToNodePool, appnedCount);

			// count.x= this.trailData.trailHeaderIndexBufferConsume.GetCounter();
			// count.y= this.trailData.trailNodeIndexBufferConsume.GetCounter();
		}

		// public int3 count;
		protected void OnEnable()
		{
			this.Init();
		}
		protected void OnDisable()
		{
			this.Deinit();
		}
	}
}