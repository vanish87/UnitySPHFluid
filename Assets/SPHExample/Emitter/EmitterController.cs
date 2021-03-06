using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace FluidSPH
{
	public class EmitterController : MonoBehaviour, IInitialize
	{
        public class EmitterContainer : GPUContainer
        {
			[Shader(Name = "_EmitterBuffer")] public GPUBufferVariable<EmitterGPUData> emitterBuffer = new GPUBufferVariable<EmitterGPUData>();
        }
		public bool Inited => this.inited;
        public EmitterContainer EmitterGPUData => this.emitterContainer;
		public int CurrentParticleEmit => this.emitters.Sum(e => e.ParticlePerSecond);
		protected const int MAX_NUM_EMITTER = 128;
		protected bool inited = false;
		protected EmitterConfigure configure;
		protected EmitterConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<EmitterConfigure>();

		protected List<IEmitter> emitters = new List<IEmitter>();
		protected EmitterContainer emitterContainer = new EmitterContainer();

		public void Init(params object[] parameters)
		{
			if(this.Inited) return;

            this.emitters.Clear();
            this.emitters.AddRange(this.gameObject.GetComponentsInChildren<IEmitter>());

			this.Configure.Initialize();
			foreach (var ec in this.Configure.D.emitters)
			{
				var go = new GameObject(ec.name);
				go.transform.parent = this.gameObject.transform;
				var e = go.AddComponent<Emitter>();
                e.Init(ec);

                this.emitters.Add(e);
			}
			this.emitters = this.emitters.OrderBy(e => e.ParticlePerSecond).ToList();

            this.emitterContainer.emitterBuffer.InitBuffer(MAX_NUM_EMITTER, true, true);

			this.inited = true;
		}
		public void Deinit(params object[] parameters)
		{
			this.emitterContainer?.Release();
		}
		protected void UpdateEmitterData()
		{
			var ecount = 0;
			var eCPU = this.emitterContainer.emitterBuffer.CPUData;
			foreach (var e in this.emitters)
			{
				eCPU[ecount].enabled = e.IsActive;
				eCPU[ecount].particlePerSecond = e.ParticlePerSecond;
				eCPU[ecount].localToWorld = e.Space.TRS;
				ecount++;
			}
			while (ecount < MAX_NUM_EMITTER) eCPU[ecount++].enabled = false;
		}
		protected void Update()
		{
			this.UpdateEmitterData();
		}
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
