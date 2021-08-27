
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH3D
{
	public interface IEmitter
	{
		ISpace Space { get; set;}
		string Name { get; }
		bool enabled { get; }

	}
	[System.Serializable]
	public struct EmitterGPUData
	{
		public BlittableBool enabled;
		public float4x4 localToWorld;
	}
	[ExecuteInEditMode]

	public class Emitter : MonoBehaviour, IEmitter
	{
		public ISpace Space { get => this.space; set => this.CopyFrom(value); }

		public string Name => this.ToString();

		[SerializeField] protected ISpace space = new Space3D();
		protected void Update()
		{
			this.Space.Center = this.gameObject.transform.localPosition;
			this.Space.Rotation = this.gameObject.transform.localRotation;
			this.Space.Scale = this.gameObject.transform.localScale;
		}
		protected void CopyFrom(ISpace space)
		{
			this.space = space;
			this.gameObject.transform.localPosition = space.Center;
			this.gameObject.transform.localRotation = space.Rotation;
			this.gameObject.transform.localScale = space.Scale;
		}
		protected void OnDrawGizmos()
		{
			(this.space as Space3D)?.OnDrawGizmos();
		}
	}

}