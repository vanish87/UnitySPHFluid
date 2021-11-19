Shader "Fluid/SPHParticleQuadShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		// [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend mode Source", Int) = 1
    }


	CGINCLUDE
	#pragma multi_compile_local _ Quad Mesh

	#include "UnityCG.cginc"
	#include "SPHData.cginc"
	#include "Assets/Packages/UnityTools/UnityTools/Assets/Rendering/Shader/ShaderCommand.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
        uint vid : SV_VertexID;
    };

    struct v2f
    {
        float4 position : SV_POSITION;
		float2 uv : TEXCOORD0;
        float4 color : COLOR;
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

	StructuredBuffer<Particle> _ParticleBuffer;
	float _ParticleScale;
	bool _RenderBoundaryParticle;
	
	v2g vert(appdata i, uint iid : SV_VertexID) 
	{
        v2g o = (v2g)0;

		Particle p = _ParticleBuffer[iid];

		bool shoudRender = !(p.type == PT_INACTIVE || (p.type == PT_BOUNDARY && !_RenderBoundaryParticle));
        float radius = 0.5f * _H * _ParticleScale * shoudRender;
		float4 pos = float4(p.pos, 1);
        o.position = UnityObjectToViewPos(pos);
        o.size = radius;
        o.color = p.col;
        return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		float4 col = i.color;
        return col;
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
			o.position =  mul(UNITY_MATRIX_P, float4(position,1));
			o.texcoord = g_texcoords[i];
            o.color = col;
			outStream.Append(o);
		}

		outStream.RestartStrip();
    }

	[maxvertexcount(4)]
	void geom(point v2g p[1], inout TriangleStream<g2f> outStream)
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
		
		Pass
		{

			Blend [BlendSrc] [BlendDst]
			BlendOp [BlendOp]
			ColorMask [ColorMask]
			Cull [CullMode]
			ZClip [ZClip]
			ZTest [ZTest]
			ZWrite [ZWrite]

			Name "Quad"
			CGPROGRAM
			#if Quad
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#endif
			ENDCG
		}

		// Pass
		// {
		// 	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		// 	Cull Off
		// 	ZWrite Off
		// 	Blend Zero OneMinusSrcAlpha
		// 	CGPROGRAM
		// 	#if Mesh
		// 	#pragma vertex vert
		// 	#pragma geometry geom
		// 	#pragma fragment frag
		// 	#endif
		// 	ENDCG
		// }
		// Pass
		// {
		// 	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		// 	Cull Off
		// 	ZWrite Off
		// 	Blend SrcAlpha One
		// 	CGPROGRAM
		// 	#if Mesh
		// 	#pragma vertex vert
		// 	#pragma geometry geom
		// 	#pragma fragment frag
		// 	#endif
		// 	ENDCG
		// }
    }
}
