
#include "DispatcherHelper.cginc"
#include "Emitter.cginc"
#include "UnityCG.cginc"
 
AppendStructuredBuffer<uint> _ParticleBufferIndexAppend;
ConsumeStructuredBuffer<uint> _ParticleBufferIndexConsume;
int _ParticleBufferIndexConsumeCount;

const static float4x4 IDENTITY = float4x4 (1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);

float _TrailEmitRate;


float wang_hash01(uint seed)
{
	seed = (seed ^ 61) ^ (seed >> 16);
	seed *= 9;
	seed = seed ^ (seed >> 4);
	seed *= 0x27d4eb2d;
	seed = seed ^ (seed >> 15);
	return float(seed) / 4294967295.0; // 2^32-1
}

float3 GenerateRandomPos01(int did, int uuid)
{
	uint t = (uint)fmod(_Time * 8, 65535.0);
	int3 offset = int3(0, 173, 839) + t;
	return float3(wang_hash01(did + offset.x), wang_hash01(did + offset.y), wang_hash01(did + offset.z));
}

Particle RandomParticle(int did, int uuid, float4x4 localToWorld = IDENTITY)
{
	Particle p = (Particle)0;
	float4 pos = float4(GenerateRandomPos01(did, uuid) - 0.5f, 1);
	pos = mul(localToWorld, pos);
	pos /= pos.w;

	p.type = PT_INACTIVE;
	p.uuid = uuid;
	p.pos = pos.xyz;
	p.col = float4(GenerateRandomPos01(did, uuid),1);

	return p;
}

Particle DeactiveParticle(Particle p, int did)
{
	p.pos = _GridMin + GenerateRandomPos01(did, p.uuid) * (_GridMax - _GridMin);
	p.type = PT_INACTIVE;
	return p;
}

Particle EmitParticle(int did, int uuid, Emitter e)
{
	Particle p = RandomParticle(did, uuid, e.localToWorld);
	p.type = PT_FLUID;
	p.life = lerp(_ParticleLife.x, _ParticleLife.y, wang_hash01(uuid));
	return p;
}

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void InitIndexPool(uint3 DTid : SV_DispatchThreadID)
{
	RETURN_IF_INVALID(DTid);

	const uint P_ID = DTid.x;
	_ParticleBuffer[P_ID].type = PT_INACTIVE;
	_ParticleBufferIndexAppend.Append(P_ID);
}

[numthreads(MAX_PARTICLE_PER_EMITTER, 1, 1)]
void Emit(uint3 EmitterID : SV_GroupID, uint ParticleID : SV_GroupIndex)
{
	int eid = EmitterID.x;
	Emitter e = _EmitterBuffer[eid];

	int pid = ParticleID;
	if(e.enabled)
	{
		int total = max(1 ,e.particlePerSecond * _TimeStep / _StepIteration);
		int iter = ceil(total * 1.0f / MAX_PARTICLE_PER_EMITTER);
		for(int i = 0; i < iter; ++i)
		{
			if((pid + i * MAX_PARTICLE_PER_EMITTER)< total)
			{
				const int P_ID = _ParticleBufferIndexConsume.Consume();
				const int uuid = _ParticleBuffer[P_ID].uuid;
				_ParticleBuffer[P_ID] = EmitParticle(P_ID, uuid, e);
				
				if(wang_hash01(P_ID) < _TrailEmitRate)
				{
					_TrailEmitBufferAppend.Append(uuid);
				}
			}

		}
	}
}

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void EmitFromBuffer(uint3 DTid : SV_DispatchThreadID)
{
	const uint P_ID = DTid.x;
	Particle p = _ParticleBuffer[P_ID];
	const int uuid = p.uuid;
	if(IsActive(p))
	{ 
		// if(wang_hash01(P_ID) < _TrailEmitRate)
		// {
		// 	_TrailEmitBufferAppend.Append(uuid);
		// }
	}
	else
	{
		_ParticleBufferIndexAppend.Append(P_ID);
	}

	// _TrailSourceBuffer[uuid].type = p.type;
	// _TrailSourceBuffer[uuid].pos = IsActive(p)? p.pos:0;
	// _TrailSourceBuffer[uuid].vel = IsActive(p)? p.vel:0;

}


[numthreads(128, 1, 1)]
void AddBoundary(uint3 DTid : SV_DispatchThreadID)
{
	int pid = DTid.x;
	if(pid >= _BoundaryParticleCount) return;

	const uint P_ID = _ParticleBufferIndexConsume.Consume();

	Particle p = _ParticleBuffer[P_ID];
	BoundaryParticleData bp = _BoundaryParticleData[pid];

	float4x4 mat = _BoundaryData[bp.bid].localToWorld;
	float4 worldPos = mul(mat, float4(bp.localPos,1));

	p.type = PT_BOUNDARY;
	p.pos = worldPos;
	p.bpid = pid; 
	p.col = float4(1,0,0,1);
	_ParticleBuffer[P_ID] = p;

}