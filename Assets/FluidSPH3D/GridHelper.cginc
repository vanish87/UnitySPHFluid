float3 _GridMin;
float3 _GridMax;
float3 _GridSize;
float3 _GridSpacing;

float3 PosToCellPos(float3 pos, float3 gridMin, float3 gridMax, float3 gridSpacing)
{
	pos = clamp(pos, gridMin + gridSpacing * 0.5f, gridMax - gridSpacing * 0.5f);
	pos = pos - gridMin; //Start from grid left bottom corner
	return pos/gridSpacing;
}

uint3 CellPosToCellIndex(float3 pos)
{
	return (uint3)pos;
}
uint CellIndexToCellID(int3 index, int3 gridSize)
{
	return index.x + index.y * gridSize.x + index.z * gridSize.x * gridSize.y;
}

uint2 PosToGridIndexPair(uint pid, float3 pos, float3 gridMin,float3 gridMax, int3 gridSize, float3 gridSpacing)
{
	float3 cellPos = PosToCellPos(pos, gridMin, gridMax, gridSpacing);
	uint3 cellIndex = CellPosToCellIndex(cellPos);
	uint cellID = CellIndexToCellID(cellIndex, gridSize);

	return uint2(cellID, pid);
}
