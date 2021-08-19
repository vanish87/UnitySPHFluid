

struct Particle 
{
	float3 pos;
	float3 vel;
	float4 col;
};


struct ParticleDensity
{
	float density;
};

struct ParticleForce
{
	float3 force;
};

struct ParticleVelocity
{
	float3 vel;
};

struct ParticleVorticity
{
	float3 vor;
};
