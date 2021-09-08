
struct TrailHeader
{
	int headNodeIndex;
	int currentlength;
	int maxLength;
};

struct TrailNode
{
	int prev; int next; int idx;
	float3 pos;
};
