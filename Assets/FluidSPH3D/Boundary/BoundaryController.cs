using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityTools;

namespace FluidSPH3D
{
	public class BoundaryController : MonoBehaviour
	{
		public List<IBoundary> Boundaries => this.boundaries;
		protected BoundaryConfigure configure;
		protected BoundaryConfigure Configure => this.configure ??= this.gameObject.FindOrAddTypeInComponentsAndChildren<BoundaryConfigure>();
		protected List<IBoundary> boundaries = new List<IBoundary>();

		public void Init()
		{
			this.boundaries.Clear();
			this.boundaries.AddRange(this.gameObject.GetComponentsInChildren<IBoundary>());

			this.Configure.Initialize();
			foreach (var bc in this.Configure.D.boundaries)
			{
				var go = new GameObject(bc.name);
				go.transform.parent = this.gameObject.transform;
				var b = go.AddComponent<PrimeBoundary>();
				b.Init(bc);
				this.boundaries.Add(b);
			}
		}
		public void Deinit()
		{
			this.boundaries.Clear();
		}

		protected void OnEnable()
		{
			// this.Init();
		}
		protected void OnDisable()
		{
			// this.Deinit();
		}
	}
}