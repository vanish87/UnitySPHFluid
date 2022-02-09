
#define SourceType TrailParticle

struct TrailParticle
{
	int type;
	float3 pos;
	float3 vel;
};

bool IsActive(TrailParticle p) 
{
	return p.type != 0;
}

RWStructuredBuffer<TrailParticle> _TrailSourceBuffer;

AppendStructuredBuffer<int> _TrailEmitBufferAppend;
ConsumeStructuredBuffer<int> _TrailEmitBufferConsume;
StructuredBuffer<int> _TrailEmitBufferConsumeActiveCount;
