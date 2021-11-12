
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

//Configure parameters
float _H;
float2 _PressureK;// k1 and k2 in State Equation Pressure
float _RestDensity;
float _ParticleMass;
float _Viscosity;
float _Vorticity;
float3 _Gravity;
float _TimeStep;
int _StepIteration;
float2 _MaxSpeed;
float2 _ParticleLife;

bool3 _SimulationSpaceBounds;

float _NU_T;
float _NU_EXT;
float _Theta;
float4 _TransferForceParameter;
float4 _TransferTorqueParameter;
float4 _AngularVelocityParameter;
float4 _LinearVelocityParameter;

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

