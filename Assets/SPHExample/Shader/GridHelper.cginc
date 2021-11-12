float3 _GridMin;
float3 _GridMax;
float3 _GridSize;
float3 _GridSpacing;

#define LOOP_RANGE(I, J, K, CURRENT, RANGE, SIZE) \
for(int I = max(CURRENT.x - RANGE.x, 0); I <= min(CURRENT.x + RANGE.x, SIZE.x-1); ++I)\
for(int J = max(CURRENT.y - RANGE.y, 0); J <= min(CURRENT.y + RANGE.y, SIZE.y-1); ++J)\
for(int K = max(CURRENT.z - RANGE.z, 0); K <= min(CURRENT.z + RANGE.z, SIZE.z-1); ++K)

#define FOR_EACH_NEIGHBOR_START(CURRENT, NID, GBUFFER, GMIN, GMAX, GSPACING, GSIZE) {\
int3 cid = PosToCellIndex(CURRENT.pos, GMIN, GMAX, GSPACING); \
int3 range = 1; \
for(int i = max(cid.x - range.x, 0); i <= min(cid.x + range.x, GSIZE.x-1); ++i)\
for(int j = max(cid.y - range.y, 0); j <= min(cid.y + range.y, GSIZE.y-1); ++j)\
for(int k = max(cid.z - range.z, 0); k <= min(cid.z + range.z, GSIZE.z-1); ++k){\
	uint2 startEnd = GBUFFER[CellIndexToCellID(int3(i,j,k), GSIZE)]; \
	for(uint NID = startEnd.x; NID < startEnd.y; ++NID) 

#define FOR_EACH_NEIGHBOR_END }}

float3 PosToCellPos(float3 pos, float3 gridMin, float3 gridMax, float3 gridSpacing)
{
	//TODO clamp gmin, gmax is correct
	pos = clamp(pos, gridMin+gridSpacing*0.5f, gridMax-gridSpacing*0.5f);
	pos = pos - gridMin; //Start from grid left bottom corner
	return pos/gridSpacing;
}
float3 PosToNormalized01(float3 pos, float3 gridMin, float3 gridMax)
{
	pos = clamp(pos, gridMin, gridMax);
	float3 size = gridMax-gridMin;
	return (pos-gridMin)/size;
}

uint3 CellPosToCellIndex(float3 pos)
{
	//TODO add center type calculation
	return (uint3)pos;
}

uint3 PosToCellIndex(float3 pos, float3 gridMin, float3 gridMax, float3 gridSpacing)
{
	return CellPosToCellIndex(PosToCellPos(pos, gridMin, gridMax, gridSpacing));
}

bool IsCellIndexValid(int3 index, int3 gridSize)
{
	return all(0<=index) && all(index<gridSize);
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
