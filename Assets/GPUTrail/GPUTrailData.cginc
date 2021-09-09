
struct TrailHeader
{
	int state;
	int first;
	int currentlength;
	int maxLength;
};

struct TrailNode
{
	int prev; int next; int idx;
	float3 pos;
};

static const int TS_READY = 0;
static const int TS_ACTIVE = 1;
static const int TS_DEAD = 2;

bool IsReady(TrailHeader header)
{
	return header.state == TS_READY;
}

bool IsActive(TrailHeader header)
{
	return header.state == TS_ACTIVE;
}

bool IsDead(TrailHeader header)
{
	return header.state == TS_DEAD;
}
