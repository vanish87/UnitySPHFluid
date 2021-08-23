
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

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void InitIndexPool(uint3 DTid : SV_DispatchThreadID)
{
	RETURN_IF_INVALID(DTid);

	const uint P_ID = DTid.x;
	_ParticleBuffer[P_ID].type = PT_INACTIVE;
	_ParticleBufferIndexAppend.Append(P_ID);
}

[numthreads(1, THREAD_PER_EMITTER, 1)]
void Emit(uint3 DTid : SV_DispatchThreadID)
{
	int eid = DTid.x;
	if(eid >= _EmitterBufferCount) return;

	Emitter e = _EmitterBuffer[eid];
	if(e.enabled)
	{
		const uint P_ID = _ParticleBufferIndexConsume.Consume();

		Particle p = _ParticleBuffer[P_ID];
		float3 np = PosToNormalized01(p.pos, _GridMin, _GridMax);
		float4 pos = float4(wang_hash01((np.x + _Time.y)*10321), wang_hash01(DTid.y * np.y * _Time.z * 388), wang_hash01(DTid.y * _Time.x), 1);
		pos -= 0.5f;
		pos.w = 1;

		p.type = PT_FLUID;
		pos = mul(e.localToWorld, pos);
		pos /= pos.w;
		p.pos = pos.xyz;
		_ParticleBuffer[P_ID] = p;
	}
}