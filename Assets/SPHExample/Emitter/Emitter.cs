
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH
{
	public interface IEmitter
	{
		ISpace Space { get; }
		string Name { get; }
		bool IsActive { get; }
		int ParticlePerSecond { get; }

	}
	[System.Serializable]
	public struct EmitterGPUData
	{
		public BlittableBool enabled;
		public int particlePerSecond;
		public float4x4 localToWorld;
	}
	[ExecuteInEditMode]
	public class Emitter : MonoBehaviour, IEmitter
	{
		public string Name => this.ToString();
		public bool IsActive => this.data.isActive;
		public ISpace Space => this.data.space;

		public int ParticlePerSecond => this.data. particlePerSecond;

		[SerializeField] protected EmitterConfigure.EmitterSetting data;
		protected void Update()
		{
			this.Space.Center = this.gameObject.transform.localPosition;
			this.Space.Rotation = this.gameObject.transform.localRotation;
			this.Space.Scale = this.gameObject.transform.localScale;
		}
		public void Init(EmitterConfigure.EmitterSetting data)
		{
			this.data = data;

			var space = this.data.space;
			this.gameObject.transform.localPosition = space.Center;
			this.gameObject.transform.localRotation = space.Rotation;
			this.gameObject.transform.localScale = space.Scale;
		}
		protected void OnDrawGizmos()
		{
			if (this.data.isActive) (this.data.space as Space3D)?.OnDrawGizmos();
		}
	}

}