
struct SDFData
{
	bool enabled;
	float3 center;
	float3 size;
};

StructuredBuffer<SDFData> _SDFBoundaryBuffer;
int _SDFBoundaryBufferCount;


float3 GetSDFForce(float3 pos)
{
	for(int i = 0; i < _SDFBoundaryBufferCount; ++i)
	{
		SDFData sdf = _SDFBoundaryBuffer[i];
		if(sdf.enabled == false) continue;

		float r = sdf.size.x;
		float3 c = sdf.center;
		if(distance(pos,c) < r)
		{
			return normalize(pos-c) * 1000;
		}
	}

	return 0;
}
