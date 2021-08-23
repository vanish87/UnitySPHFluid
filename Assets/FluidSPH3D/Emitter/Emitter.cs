
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace FluidSPH3D
{
	public interface IEmitter
	{
		ISpace Space { get; }

	}
	[System.Serializable]
	public struct EmitterData
	{
		public BlittableBool enabled;
		public float4x4 localToWorld;
	}
	[ExecuteInEditMode]

	public class Emitter : MonoBehaviour, IEmitter
	{
		public ISpace Space => this.space;

		[SerializeField] protected Space3D space = new Space3D();
		protected void Update()
		{
			this.Space.Center = this.gameObject.transform.localPosition;
			this.Space.Rotation = this.gameObject.transform.localRotation;
			this.Space.Scale = this.gameObject.transform.localScale;
		}

		protected void OnDrawGizmos()
		{
			this.space?.OnDrawGizmos();
		}
	}

}