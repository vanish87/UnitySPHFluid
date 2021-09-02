using System;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;
using UnityTools.Rendering;

namespace GPUTrail
{
	public class GPUTrailController : MonoBehaviour, IInitialize, ITrailData, IDataBuffer<TrailHeader>
	{
		public enum Kernel
		{

		}
		public class GPUTrailData : GPUContainer
		{
			[Shader(Name = "_MaxNodeNumPerTrail")] public const int MAX_NODE_PER_TRAIL = 128;
			[Shader(Name = "_TrailNodeBuffer")] public GPUBufferVariable<TrailNode> trailNodeBuffer = new GPUBufferVariable<TrailNode>();
			[Shader(Name = "_TrailHeaderBuffer")] public GPUBufferVariable<TrailHeader> trailHeaderBuffer = new GPUBufferVariable<TrailHeader>();
		}
		public bool Inited => this.inited;
		public GPUBufferVariable<TrailHeader> Buffer => this.trailData.trailHeaderBuffer;
		public GPUBufferVariable<TrailNode> NodeBuffer => this.trailData.trailNodeBuffer;
		public int MaxNodePerTrail => GPUTrailData.MAX_NODE_PER_TRAIL;

		[SerializeField] protected ComputeShader trailCS;

		protected GPUTrailConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<GPUTrailConfigure>();


		protected GPUTrailConfigure configure;
		protected bool inited = false;
		protected GPUTrailData trailData = new GPUTrailData();
		protected ComputeShaderDispatcher<Kernel> dispatcher;

		public void Init()
		{
			var trailNum = this.Configure.D.trailNum;
			this.trailData.trailNodeBuffer.InitBuffer(GPUTrailData.MAX_NODE_PER_TRAIL * trailNum);
			this.trailData.trailHeaderBuffer.InitBuffer(trailNum);

			this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.trailCS);
			foreach (Kernel k in Enum.GetValues(typeof(Kernel)))
			{
				this.dispatcher.AddParameter(k, this.Configure.D);
				this.dispatcher.AddParameter(k, this.trailData);
			}

			this.inited = true;
		}

		public void Deinit()
		{
			this.trailData?.Release();
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