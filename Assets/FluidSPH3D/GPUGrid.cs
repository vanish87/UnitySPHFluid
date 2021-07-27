using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UnityTools.Common
{
	public static class GridHelper
	{
		public static void OnDrawGridCenter(float3 center, float3 size, float3 spacing)
		{

		}
		public static void OnDrawGrid(float3 min, float3 max, float3 spacing)
		{
			var size = (max - min) / spacing;
			foreach (var x in Enumerable.Range(0, Mathf.CeilToInt(size.x)))
			{
				foreach (var y in Enumerable.Range(0, Mathf.CeilToInt(size.y)))
				{
					var s = new float3(spacing.x, spacing.y, 0);
					var center = min + s * 0.5f + s * new float3(x, y, 0);
					Gizmos.DrawWireCube(center, s);
				}
			}
			foreach (var y in Enumerable.Range(0, Mathf.CeilToInt(size.y)))
			{
				foreach (var z in Enumerable.Range(0, Mathf.CeilToInt(size.z)))
				{
					var s = new float3(0, spacing.y, spacing.z);
					var center = min + s * 0.5f + s * new float3(0, y, z);
					Gizmos.DrawWireCube(center, s);
				}
			}
			foreach (var x in Enumerable.Range(0, Mathf.CeilToInt(size.x)))
			{
				foreach (var z in Enumerable.Range(0, Mathf.CeilToInt(size.z)))
				{
					var s = new float3(spacing.x, 0, spacing.z);
					var center = min + s * 0.5f + s * new float3(x, 0, z);
					Gizmos.DrawWireCube(center, s);
				}
			}
		}
	}
	public class GPUGrid<Cell> : MonoBehaviour
	{
		public enum CenterMode
		{
			Center = 0,
			LeftDown,
			RightUp,
		}
		[System.Serializable]
		public class GPUData : GPUContainer
		{
			[Shader(Name = "_GridCenterMode")] public int centerMode = (int)CenterMode.Center;
			[Shader(Name = "_GridSize")] public int3 gridSize = new int3(128, 128, 128);
			[Shader(Name = "_GridSpacing")] public float3 gridSpacing = new float3(1, 1, 1);
			[Shader(Name = "_GridMin")] public float3 gridMin = new float3(0, 0, 0);
			[Shader(Name = "_GridMax")] public float3 gridMax = new float3(1, 1, 1);
			[Shader(Name = "_GridBuffer")] public GPUBufferVariable<Cell> gridBuffer = new GPUBufferVariable<Cell>();
		}

		[SerializeField] protected Space3D space;
		[SerializeField] protected GPUData gridData = new GPUData();

		private void Init()
		{
			var p = this.gridData;
			var gridBufferSize = p.gridSize.x * p.gridSize.y * p.gridSize.z;
			p.gridBuffer.InitBuffer(gridBufferSize);
			p.gridMin = this.space.Center - this.space.Scale * 0.5f;
			p.gridMax = this.space.Center + this.space.Scale * 0.5f;
		}
		public void Init(float3 gridSpacing)
		{
			var p = this.gridData;
			p.gridSpacing = gridSpacing;
			var newSize = this.space.Scale / gridSpacing;
			p.gridSize = new int3(Mathf.CeilToInt(newSize.x), Mathf.CeilToInt(newSize.y), Mathf.CeilToInt(newSize.z));

			this.Init();
		}

		public void Init(int3 gridSize)
		{
			var p = this.gridData;
			p.gridSize = gridSize;
			p.gridSpacing = this.space.Scale / p.gridSize;

			this.Init();
		}

		public virtual void Deinit()
		{
			this.gridData?.Release();
		}

		protected virtual void OnDrawGizmos()
		{
			this.space?.OnDrawGizmos();
			if (Application.isPlaying) GridHelper.OnDrawGrid(this.gridData.gridMin, this.gridData.gridMax, this.gridData.gridSpacing);
		}
	}
}
