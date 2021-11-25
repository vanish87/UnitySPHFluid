Shader "UnityTools/GeometryQuadShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }


	CGINCLUDE
	#include "UnityCG.cginc"
	#include "SPHData.cginc"
	#include "Assets/Packages/UnityTools/UnityTools/Assets/Rendering/Shader/ShaderCommand.cginc"

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

	struct gin
	{
		float3 pos;
		float4 col;
		float size;
	};

	#define VertexIn gin
	#define VertexOut g2f
	void UpdateVertex(in VertexIn vin, inout VertexOut o)
	{
		o.position =  mul(UNITY_MATRIX_P, o.position);
		// o.position =  UnityObjectToClipPos(o.position);
		o.color = vin.col;
	}
	#include "Assets/Packages/UnityTools/UnityTools/Assets/Rendering/Shader/GeometryQuad.cginc"

	sampler2D _MainTex;
	float4 _ST;
	
	StructuredBuffer<Particle> _ParticleBuffer;
	float _ParticleScale;
	bool _RenderBoundaryParticle;

	v2g vert(uint iid : SV_VertexID) 
	{
        v2g o = (v2g)0;
		Particle p = _ParticleBuffer[iid];

		bool shoudRender = !(p.type == PT_INACTIVE || (p.type == PT_BOUNDARY && !_RenderBoundaryParticle));
        float radius = 0.5f * _H * _ParticleScale * shoudRender;
        o.position = UnityObjectToViewPos(p.pos);
		// o.position = p.pos;
        o.size = radius;
        o.color = p.col;
        return o;
	}

	fixed4 frag(g2f i) : SV_Target
	{
		float4 col = i.color;
		col.a *= tex2D(_MainTex, i.texcoord).a;
        return col;
	}


	[maxvertexcount(4)]
	void geom(point v2g p[1], inout TriangleStream<g2f> outStream)
	{
		float size = p[0].size;
		if(size > 0)
		{
			float3 pos = p[0].position;
			float4 col = p[0].color;
			VertexIn vin;
			vin.pos = pos;
			vin.col = col;
			vin.size = size;
			AddQuad(vin, outStream);
		}
	}

	ENDCG

    SubShader
    {
		Pass
		{
			// Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

			// Blend [BlendSrc] [BlendDst]
			// BlendOp [BlendOp]
			// ColorMask [ColorMask]
			// Cull [CullMode]
			// ZClip [ZClip]
			// ZTest [ZTest]
			// ZWrite [ZWrite]

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			ENDCG
		}

		// Pass
		// {
		// 	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		// 	Cull Off
		// 	ZWrite Off
		// 	Blend Zero OneMinusSrcAlpha
		// 	CGPROGRAM
		// 	#pragma vertex vert
		// 	#pragma geometry geom
		// 	#pragma fragment frag
		// 	ENDCG
		// }
		// Pass
		// {
		// 	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		// 	Cull Off
		// 	ZWrite Off
		// 	Blend SrcAlpha One
		// 	CGPROGRAM
		// 	#pragma vertex vert
		// 	#pragma geometry geom
		// 	#pragma fragment frag
		// 	ENDCG
		// }
    }
}
