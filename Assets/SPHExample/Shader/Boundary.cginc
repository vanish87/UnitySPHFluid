
struct BoundaryData
{
	int type;
	float4x4 localToWorld;
	float3 velocity;
};
struct BoundaryParticleData
{
	int bid;
	float3 localPos;
};

const static int BT_DISABLED = 0;
const static int BT_SDF_SPHERE = 1;
const static int BT_PARTICLE_PLANE = 2;
const static int BT_PARTICLE_SPHERE = 3;
const static int BT_PARTICLE_MESH = 4;

StructuredBuffer<BoundaryData> _BoundaryData;
int _BoundaryDataCount;

StructuredBuffer<BoundaryParticleData> _BoundaryParticleData;
int _BoundaryParticleDataCount;
int _BoundaryParticleCount;

float3 GetPos(float4x4 localToWorld)
{
	return float3(localToWorld[0][3], localToWorld[1][3], localToWorld[2][3]);
}

float3 GetScale(float4x4 localToWorld)
{
	return float3(localToWorld[0][0], localToWorld[1][1], localToWorld[2][2]);
}

float3 GetSDFForce(float3 pos)
{
	for(int i = 0; i < _BoundaryDataCount; ++i)
	{
		BoundaryData b = _BoundaryData[i];
		if(b.type == BT_DISABLED) continue;
		if(b.type != BT_SDF_SPHERE) continue;

		float r = GetScale(b.localToWorld).x;
		float3 c = GetPos(b.localToWorld);
		float dist = distance(pos,c);
		if(dist < r)
		{
			return normalize(pos-c) * pow(2, 20 * (1+(r-dist)));
		}
	}

	return 0;
}
