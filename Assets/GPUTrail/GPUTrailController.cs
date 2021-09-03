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
			Init,
			UpdateParticle,
			UpdateFromParticle,
		}
		[System.Serializable]
		public class GPUTrailData : GPUContainer
		{
			[Shader(Name = "_MaxNodeNumPerTrail")] public const int MAX_NODE_PER_TRAIL = 128;
			[Shader(Name = "_TrailNodeBuffer")] public GPUBufferVariable<TrailNode> trailNodeBuffer = new GPUBufferVariable<TrailNode>();
			[Shader(Name = "_TrailHeaderBuffer")] public GPUBufferVariable<TrailHeader> trailHeaderBuffer = new GPUBufferVariable<TrailHeader>();


			//particle buffer from fluid sph is rearranged every frame
			//we need fixed particle buffer to update trails
			[Shader(Name = "_FixedParticleBuffer")] public GPUBufferVariable<FluidSPH3D.Particle> fixedParticleBuffer = new GPUBufferVariable<FluidSPH3D.Particle>();
			[Shader(Name = "_ActiveParticleIndexBufferAppend")] public GPUBufferAppendConsume<int> activeParticleIndexBufferAppend = new GPUBufferAppendConsume<int>();
			[Shader(Name = "_ActiveParticleIndexBuffer")] public GPUBufferVariable<int> activeParticleIndexBuffer = new GPUBufferVariable<int>();
			[Shader(Name = "_ActiveParticleCount")] public int activeParticleCount = 0;
		}
		public bool Inited => this.inited;
		public GPUBufferVariable<TrailHeader> Buffer => this.trailData.trailHeaderBuffer;
		public GPUBufferVariable<TrailNode> NodeBuffer => this.trailData.trailNodeBuffer;
		public int MaxNodePerTrail => GPUTrailData.MAX_NODE_PER_TRAIL;

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

			var trailNum = this.Configure.D.trailNum;
			this.trailData.trailNodeBuffer.InitBuffer(GPUTrailData.MAX_NODE_PER_TRAIL * trailNum, true, false);
			this.trailData.trailHeaderBuffer.InitBuffer(trailNum, true, false);

			this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.trailCS);
			foreach (Kernel k in Enum.GetValues(typeof(Kernel)))
			{
				this.dispatcher.AddParameter(k, this.Configure.D);
				this.dispatcher.AddParameter(k, this.trailData);
				this.dispatcher.AddParameter(k, this.particleBuffer.Buffer);
			}

			this.dispatcher.Dispatch(Kernel.Init, trailNum);

			this.trailData.trailHeaderBuffer.GetToCPUData();
			this.trailData.trailNodeBuffer.GetToCPUData();

			this.inited = true;
		}

		public void Deinit()
		{
			this.trailData?.Release();
		}

		protected void InitTrail(int tsize)
		{
			this.Configure.D.trailNum = tsize;

			this.trailData.fixedParticleBuffer.InitBuffer(tsize);
			this.trailData.activeParticleIndexBufferAppend.InitAppendBuffer(tsize);
			this.trailData.activeParticleIndexBuffer.InitBuffer(this.trailData.activeParticleIndexBufferAppend);

			if (this.trailData.trailHeaderBuffer.Size != tsize)
			{
				//different size of trail and particle will leads inconstancy of append buffer
				//so we need trail size equal to or bigger than particle
				LogTool.Log("trail size is not same as particle size", LogLevel.Warning);
				this.trailData.trailNodeBuffer.InitBuffer(GPUTrailData.MAX_NODE_PER_TRAIL * tsize);
				this.trailData.trailHeaderBuffer.InitBuffer(tsize);
				this.dispatcher.Dispatch(Kernel.Init, tsize);
			}
		}

		protected void Update()
		{
			return;

			if (this.trailData.fixedParticleBuffer.Size != this.particleBuffer.Buffer.Size)
			{
				//Update trail buffer after particle buffer created
				var pSize = this.particleBuffer.Buffer.Size;
				this.InitTrail(pSize);
			}

			var trailNum = this.Configure.D.trailNum;
			var pNum = this.particleBuffer.Buffer.Size;

			this.trailData.activeParticleIndexBufferAppend.ResetCounter();
			this.dispatcher.Dispatch(Kernel.UpdateParticle, pNum);

			this.trailData.activeParticleCount = this.trailData.activeParticleIndexBufferAppend.GetCounter();
			this.dispatcher.Dispatch(Kernel.UpdateFromParticle, trailNum);
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