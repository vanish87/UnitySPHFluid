
struct TrailParticle
{
	int type;
	float3 pos;
};
RWStructuredBuffer<TrailParticle> _TrailSourceBuffer;

AppendStructuredBuffer<int> _TrailEmitBufferAppend;
ConsumeStructuredBuffer<int> _TrailEmitBufferConsume;
StructuredBuffer<int> _TrailEmitBufferConsumeActiveCount;
