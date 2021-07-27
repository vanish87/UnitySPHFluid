using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;

namespace UnityTools.Algorithm
{
	public class BitonicSort<T> : MonoBehaviour
	{
		protected const int BITONIC_BLOCK_SIZE = 512;
		protected const int TRANSPOSE_BLOCK_SIZE = 16;
		[SerializeField] protected ComputeShader bitonicCS;

		protected GPUBufferVariable<T> tempBuffer = new GPUBufferVariable<T>();

		public void Sort(ref GPUBufferVariable<T> source)
		{
			LogTool.LogAssertIsTrue(Mathf.IsPowerOfTwo(source.Size), "num of source should be power of 2");

			if (this.tempBuffer == null || this.tempBuffer.Size != source.Size)
			{
				this.tempBuffer.InitBuffer(source.Size);
			}
			ComputeShader sortCS = this.bitonicCS;

			int KERNEL_ID_BITONICSORT = sortCS.FindKernel("BitonicSort");
			int KERNEL_ID_TRANSPOSE = sortCS.FindKernel("MatrixTranspose");

			uint NUM_ELEMENTS = (uint)source.Size;
			uint MATRIX_WIDTH = BITONIC_BLOCK_SIZE;
			uint MATRIX_HEIGHT = (uint)NUM_ELEMENTS / BITONIC_BLOCK_SIZE;

			for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
			{
				SetGPUSortConstants(sortCS, level, level, MATRIX_HEIGHT, MATRIX_WIDTH);

				// Sort the row data
				sortCS.SetBuffer(KERNEL_ID_BITONICSORT, "Data", source);
				sortCS.Dispatch(KERNEL_ID_BITONICSORT, (int)(NUM_ELEMENTS / BITONIC_BLOCK_SIZE), 1, 1);
			}

			// Then sort the rows and columns for the levels > than the block size
			// Transpose. Sort the Columns. Transpose. Sort the Rows.
			for (uint level = (BITONIC_BLOCK_SIZE << 1); level <= NUM_ELEMENTS; level <<= 1)
			{
				// Transpose the data from buffer 1 into buffer 2
				SetGPUSortConstants(sortCS, level / BITONIC_BLOCK_SIZE, (level & ~NUM_ELEMENTS) / BITONIC_BLOCK_SIZE, MATRIX_WIDTH, MATRIX_HEIGHT);
				sortCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Input", source);
				sortCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Data", tempBuffer);
				sortCS.Dispatch(KERNEL_ID_TRANSPOSE, (int)(MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE), (int)(MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE), 1);

				// Sort the transposed column data
				sortCS.SetBuffer(KERNEL_ID_BITONICSORT, "Data", tempBuffer);
				sortCS.Dispatch(KERNEL_ID_BITONICSORT, (int)(NUM_ELEMENTS / BITONIC_BLOCK_SIZE), 1, 1);

				// Transpose the data from buffer 2 back into buffer 1
				SetGPUSortConstants(sortCS, BITONIC_BLOCK_SIZE, level, MATRIX_HEIGHT, MATRIX_WIDTH);
				sortCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Input", tempBuffer);
				sortCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Data", source);
				sortCS.Dispatch(KERNEL_ID_TRANSPOSE, (int)(MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE), (int)(MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE), 1);

				// Sort the row data
				sortCS.SetBuffer(KERNEL_ID_BITONICSORT, "Data", source);
				sortCS.Dispatch(KERNEL_ID_BITONICSORT, (int)(NUM_ELEMENTS / BITONIC_BLOCK_SIZE), 1, 1);
			}

		}
		protected void SetGPUSortConstants(ComputeShader cs, uint level, uint levelMask, uint width, uint height)
		{
			cs.SetInt("_Level", (int)level);
			cs.SetInt("_LevelMask", (int)levelMask);
			cs.SetInt("_Width", (int)width);
			cs.SetInt("_Height", (int)height);
		}

		protected virtual void OnDestroy()
		{
			this.tempBuffer?.Release();
		}
	}
}
