using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityTools.Algorithm;
using UnityTools.ComputeShaderTool;
using UnityEngine;
using System;

namespace UnityTools.Common
{
	[RequireComponent(typeof(GridIndexSort))]
	public class ObjectGrid<T> : GPUGrid<uint2>
	{
		public enum Kernel
		{
			ObjectToGridIndex,
			ClearGridIndex,
			BuildGridIndex,
			BuildSortedObject,
		};

		public class GridBuffer : GPUContainer
		{
			[Shader(Name = "_ObjectBufferRead")] public GPUBufferVariable<T> objectBuffer= new GPUBufferVariable<T>();
			[Shader(Name = "_ObjectBufferSorted")] public GPUBufferVariable<T> objectBufferSorted = new GPUBufferVariable<T>();
			[Shader(Name = "_ObjectGridIndexBuffer")] public GPUBufferVariable<uint2> objectGridIndexBuffer = new GPUBufferVariable<uint2>();

		}
		[SerializeField] protected ComputeShader gridCS;
		protected GridBuffer gridbuffer = new GridBuffer();

		protected GridIndexSort Sort => this.sort ??= this.GetComponent<GridIndexSort>();
		protected GridIndexSort sort;
		protected ComputeShaderDispatcher<Kernel> dispatcher;

		public void BuildSortedParticleGridIndex(GPUBufferVariable<T> source, out GPUBufferVariable<T> sortedBuffer)
		{
			sortedBuffer = default;

			this.CheckBufferChanged(source);

			this.dispatcher.Dispatch(Kernel.ObjectToGridIndex, source.Size);
			this.Sort.Sort(ref this.gridbuffer.objectGridIndexBuffer);

			var s = this.gridData.gridSize;
			this.dispatcher.Dispatch(Kernel.ClearGridIndex, s.x, s.y, s.z);
			this.dispatcher.Dispatch(Kernel.BuildGridIndex, source.Size);
			this.dispatcher.Dispatch(Kernel.BuildSortedObject, source.Size);

			sortedBuffer = this.gridbuffer.objectBufferSorted;
		}

		protected void CheckBufferChanged(GPUBufferVariable<T> source)
		{
			if(this.gridbuffer.objectBufferSorted == null || this.gridbuffer.objectBufferSorted.Size != source.Size)
			{
				//use source as object buffer
				this.gridbuffer.objectBuffer.InitBuffer(source);
				//create new buffer for sorted data
				this.gridbuffer.objectBufferSorted.InitBuffer(source.Size);
				//create new buffer for object index
				this.gridbuffer.objectGridIndexBuffer.InitBuffer(source.Size);
				
				this.dispatcher = new ComputeShaderDispatcher<Kernel>(this.gridCS);
				foreach (Kernel k in Enum.GetValues(typeof(Kernel)))
				{
					this.dispatcher.AddParameter(k, this.gridData);
					this.dispatcher.AddParameter(k, this.gridbuffer);
				}
			}
			else
			{
				this.gridbuffer.objectBuffer.UpdateBuffer(source);
			}
		}
		protected void OnDestroy()
		{
			base.Deinit();
			this.gridbuffer?.Release();
		}
	}
}
