
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

	void Append(TrailNode other)
	{
		this.next = other.idx;
		other.prev = this.idx;
	}
	int RemovePrev()
	{
		int prev = this.prev;
		this.prev = -1;
		return prev;
	}
};
