

struct Particle 
{
	float3 pos;
	float3 vel;
	float4 col;
	// int type;
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

static const int PT_INACTIVE = 0;
static const int PT_FLUID 	= 1;
static const int PT_BOUNDARY = 2;
