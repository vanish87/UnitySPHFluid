Shader "Fluid/SPHParticleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }


	CGINCLUDE
	#include "UnityCG.cginc"
	#include "SPHData.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
        uint vid : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 position : SV_POSITION;
		float2 uv : TEXCOORD0;
        float4 color : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2g
	{
		float3 position : TEXCOORD0;
		float4 color    : COLOR;
		float  size : TEXCOORD1;
	};
	struct g2f
	{
		float4 position : POSITION;
		float2 texcoord : TEXCOORD0;
		float4 color    : COLOR;
	};

	sampler2D _MainTex;
	float4 _ST;
	float _H;
	float _ParticleScale;
	bool _RenderBoundaryParticle;

    StructuredBuffer<Particle> _ParticleBuffer;

	v2f vert(appdata i, uint iid : SV_InstanceID) 
	{
        v2f o = (v2f)0;
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_TRANSFER_INSTANCE_ID(i, o);

		Particle p = _ParticleBuffer[iid];

		bool shoudRender = !(p.type == PT_INACTIVE || (p.type == PT_BOUNDARY && !_RenderBoundaryParticle));
		// bool shoudRender = IsFluid(p);
        float radius = 0.5f * _H * _ParticleScale * shoudRender;
        float4 wp = float4(i.vertex.xyz * radius + p.pos,1);
        o.position = UnityObjectToClipPos(wp);
        o.color = p.col;
		o.uv = i.uv;
        return o;
	}

	v2g vertQuad(appdata i, uint iid : SV_VertexID) 
	{
        v2g o = (v2g)0;
		Particle p = _ParticleBuffer[iid];

		bool shoudRender = !(p.type == PT_INACTIVE || (p.type == PT_BOUNDARY && !_RenderBoundaryParticle));
        float radius = 0.5f * _H * _ParticleScale * shoudRender;
        o.position = UnityObjectToViewPos(p.pos);
        o.size = radius;
        o.color = p.col;
        return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
        return i.color;// * (0.5-distance(i.uv,float2(0.5,0.5)));
	}

	static const float3 g_positions[4] =
	{
		float3(-1, 1, 0),
		float3(1, 1, 0),
		float3(-1,-1, 0),
		float3(1,-1, 0),
	};
	static const float2 g_texcoords[4] =
	{
		float2(0, 0),
		float2(1, 0),
		float2(0, 1),
		float2(1, 1),
	};

    void AddQuad(float3 pos, float4 col, float size, inout TriangleStream<g2f> outStream)
    {
		g2f o = (g2f)0;
		[unroll]
		for (int i = 0; i < 4; i++)
		{
			float3 position = g_positions[i] * size + pos;
			o.position =  mul(UNITY_MATRIX_P, pos);
            o.color = col;
			outStream.Append(o);
		}

		outStream.RestartStrip();
    }

	[maxvertexcount(4)]
	void geomQuad(point v2g p[1], inout TriangleStream<g2f> outStream)
	{
		if(p[0].size > 0)
		{
			float3 pos = p[0].position;
			float4 col = p[0].color;
			float size = p[0].size;
			AddQuad(pos, col, size, outStream);
		}
	}

	ENDCG

    SubShader
    {
			// Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
			// Cull Off
			// ZWrite Off
			// Blend SrcAlpha OneMinusSrcAlpha
		// Pass
		// {
		// 	Name "Quad"
		// 	CGPROGRAM
		// 	#pragma vertex vertQuad
		// 	#pragma geometry geomQuad
		// 	#pragma fragment frag
		// 	ENDCG
		// }
		Pass
		{
			Name "Mesh"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
    }
}
