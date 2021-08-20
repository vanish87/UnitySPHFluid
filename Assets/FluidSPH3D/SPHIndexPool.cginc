
#include "DispatcherHelper.cginc"
 
AppendStructuredBuffer<uint> _ParticleBufferIndexAppend;
ConsumeStructuredBuffer<uint> _ParticleBufferIndexConsume;
int _ParticleBufferIndexConsumeCount;


[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void InitIndexPool(uint3 DTid : SV_DispatchThreadID)
{
	RETURN_IF_INVALID(DTid);

	const uint P_ID = DTid.x;
	_ParticleBuffer[P_ID].type = PT_INACTIVE;
	_ParticleBufferIndexAppend.Append(P_ID);
}

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void Emit(uint3 DTid : SV_DispatchThreadID)
{
	const uint P_ID = _ParticleBufferIndexConsume.Consume();

	Particle p = _ParticleBuffer[P_ID];
	p.type = PT_FLUID;
	_ParticleBuffer[P_ID] = p;
}