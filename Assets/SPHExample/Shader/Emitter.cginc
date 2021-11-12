
struct Emitter
{
	bool enabled;
	int particlePerSecond;
	float4x4 localToWorld;
};

#define MAX_PARTICLE_PER_EMITTER 1024

StructuredBuffer<Emitter> _EmitterBuffer;
int _EmitterBufferCount;
