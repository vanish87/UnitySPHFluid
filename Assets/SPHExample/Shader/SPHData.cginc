
struct Particle 
{
	int uuid;
	int bpid; //boundary particle id for boundary only
	float3 pos;
	float3 vel;
	float3 w;//angular velocity
	float4 col;
	float life;
	int type;
};

struct ParticleDensity
{
	float density;
};

struct ParticleForce
{
	float3 force;
	float3 transferForce;
	float3 transferTorque;
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

bool IsFluid(Particle p) 
{
	return p.type == PT_FLUID;
}

bool IsBoundary(Particle p) 
{
	return p.type == PT_BOUNDARY;
}
bool IsActive(Particle p) 
{
	return p.type != PT_INACTIVE;
}