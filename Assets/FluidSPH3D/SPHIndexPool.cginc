
#include "DispatcherHelper.cginc"
#include "Emitter.cginc"
#include "UnityCG.cginc"
 
AppendStructuredBuffer<uint> _ParticleBufferIndexAppend;
ConsumeStructuredBuffer<uint> _ParticleBufferIndexConsume;
int _ParticleBufferIndexConsumeCount;


float wang_hash01(uint seed)
{
	seed = (seed ^ 61) ^ (seed >> 16);
	seed *= 9;
	seed = seed ^ (seed >> 4);
	seed *= 0x27d4eb2d;
	seed = seed ^ (seed >> 15);
	return float(seed) / 4294967295.0; // 2^32-1
}

float3 GenerateRandomPos01(int idx)
{
	uint t = (uint)fmod(_Time * 8, 65535.0);
	int3 offset = int3(0, 173, 839) + t;
	return float3(wang_hash01(idx + offset.x), wang_hash01(idx + offset.y), wang_hash01(idx + offset.z));
}

Particle EmitParticle(Particle p, int idx, Emitter e)
{
	if(!e.enabled) return p;

	float4 pos = float4(GenerateRandomPos01(idx) - 0.5f, 1);
	pos = mul(e.localToWorld, pos);
	pos /= pos.w;

	// p = (Particle)0;
	p.type = PT_FLUID;
	p.pos = pos.xyz;
	p.life = lerp(_ParticleLife.x, _ParticleLife.y, wang_hash01(idx));

	return p;
}

Particle ReEmitParticle(Particle p, int idx)
{
	int ecount = 0;
	for(int eid = 0; eid < _EmitterBufferCount; ++eid)
	{
		Emitter emi = _EmitterBuffer[eid];
		if(emi.enabled)
		{
		}
		ecount++;
	}

	int rand = ecount>0?1:0;
	Emitter e = _EmitterBuffer[rand];

	return EmitParticle(p, idx, e);
}


[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void InitIndexPool(uint3 DTid : SV_DispatchThreadID)
{
	RETURN_IF_INVALID(DTid);

	const uint P_ID = DTid.x;
	Particle p = _ParticleBuffer[P_ID];
	p.type = PT_INACTIVE;
	_ParticleBuffer[P_ID] = p;
	_ParticleBufferIndexAppend.Append(P_ID);
}

[numthreads(1024, 1, 1)]
void Emit(uint3 EmitterID : SV_GroupID)
{
	int eid = EmitterID.x;
	if(eid >= _EmitterBufferCount) return;

	Emitter e = _EmitterBuffer[eid];
	if(e.enabled)
	{
		const int P_ID = _ParticleBufferIndexConsume.Consume();
		Particle p = _ParticleBuffer[P_ID];
		_ParticleBuffer[P_ID] = EmitParticle(p, P_ID, e);;
	}
}

StructuredBuffer<float3> _BoundaryBuffer;
int _BoundarySize;
[numthreads(128, 1, 1)]
void AddBoundary(uint3 DTid : SV_DispatchThreadID)
{
	int pid = DTid.x;
	if(pid >= _BoundarySize) return;

	const uint P_ID = _ParticleBufferIndexConsume.Consume();

	Particle p = _ParticleBuffer[P_ID];
	if(IsActive(p)) return;

	p.type = PT_BOUNDARY;
	p.pos = _BoundaryBuffer[pid];
	p.col = float4(1,0,0,0.1);
	_ParticleBuffer[P_ID] = p;

}