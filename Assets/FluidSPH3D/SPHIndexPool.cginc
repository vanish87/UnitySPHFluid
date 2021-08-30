
#include "DispatcherHelper.cginc"
#include "Emitter.cginc"
#include "UnityCG.cginc"
 
AppendStructuredBuffer<uint> _ParticleBufferIndexAppend;
ConsumeStructuredBuffer<uint> _ParticleBufferIndexConsume;
int _ParticleBufferIndexConsumeCount;

const static float4x4 IDENTITY = float4x4 (1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);


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

Particle RandomParticle(int idx, float4x4 localToWorld = IDENTITY)
{
	Particle p = (Particle)0;
	float4 pos = float4(GenerateRandomPos01(idx) - 0.5f, 1);
	pos = mul(localToWorld, pos);
	pos /= pos.w;

	p.type = PT_INACTIVE;
	p.pos = pos.xyz;
	p.col = 1;

	return p;
}

Particle DeactiveParticle(Particle p)
{
	p = (Particle)0;
	return p;
}

Particle EmitParticle(int idx, Emitter e)
{
	if(!e.enabled) return (Particle)0;

	Particle p = RandomParticle(idx, e.localToWorld);
	p.type = PT_FLUID;
	p.life = lerp(_ParticleLife.x, _ParticleLife.y, wang_hash01(idx));
	return p;
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

[numthreads(MAX_PARTICLE_PER_EMITTER, 1, 1)]
void Emit(uint3 EmitterID : SV_GroupID, uint ParticleID : SV_GroupIndex)
{
	int eid = EmitterID.x;
	Emitter e = _EmitterBuffer[eid];

	int pid = ParticleID;
	int total = e.particlePerEmit;
	if(e.enabled)
	{
		int iter = ceil(total * 1.0f / MAX_PARTICLE_PER_EMITTER);
		for(int i = 0; i < iter; ++i)
		{
			if((pid + i * MAX_PARTICLE_PER_EMITTER)< total)
			{
				const int P_ID = _ParticleBufferIndexConsume.Consume();
				_ParticleBuffer[P_ID] = EmitParticle(P_ID, e);
			}

		}
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

	p.type = PT_BOUNDARY;
	p.pos = _BoundaryBuffer[pid];
	p.col = float4(1,0,0,0.1);
	_ParticleBuffer[P_ID] = p;

}