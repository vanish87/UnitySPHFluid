using System;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.ComputeShaderTool;

namespace GPUTrail
{
	public class GPUTrailController : MonoBehaviour, IInitialize
	{
		public enum Kernel
		{

		}
		public class GPUTrainData : GPUContainer
		{
			[Shader(Name = "_MaxNodeNumPerTrail")] public const int MAX_NODE_PER_TRAIL = 128;
			[Shader(Name = "_TrailNodeBuffer")] public GPUBufferVariable<TrailNode> trailBuffer = new GPUBufferVariable<TrailNode>();
			[Shader(Name = "_TrailHeaderBuffer")] public GPUBufferVariable<TrailHeader> trailHeaderBuffer = new GPUBufferVariable<TrailHeader>();
		}
		public bool Inited => this.inited;

		[SerializeField] protected ComputeShader trailCS;

		protected GPUTrailConfigure Configure=> this.configure??= this.gameObject.FindOrAddTypeInComponentsAndChildren<GPUTrailConfigure>();

		protected GPUTrailConfigure configure;
		protected bool inited = false;
		protected GPUTrainData trailData = new GPUTrainData();
		protected ComputeShaderDispatcher<Kernel> dispatcher;

		public void Init()
		{
			var tnum = this.Configure.D.trailNum;
			this.trailData.trailBuffer.InitBuffer(GPUTrainData.MAX_NODE_PER_TRAIL * tnum);
			this.trailData.trailHeaderBuffer.InitBuffer(tnum);

			this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.trailCS);
			foreach(Kernel k in Enum.GetValues(typeof(Kernel)))
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
			if(!this.Inited) this.Init();
		}
		protected void OnDisable()
		{
			this.Deinit();
		}
	}
}