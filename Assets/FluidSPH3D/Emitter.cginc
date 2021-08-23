
struct Emitter
{
	bool enabled;
	float4x4 localToWorld;
};

#define THREAD_PER_EMITTER 128

StructuredBuffer<Emitter> _EmitterBuffer;
int _EmitterBufferCount;