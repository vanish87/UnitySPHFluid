Shader "Unlit/TrailShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}


	CGINCLUDE
	#include "UnityCG.cginc"
    #include "GPUTrailData.cginc"

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

	StructuredBuffer<TrailHeader> _TrailHeaderBuffer;
    int _TrailHeaderBufferCount;

	StructuredBuffer<TrailNode> _TrailNodeBuffer;
    int _TrailNodeBufferConnt;

    int _MaxNodePerTrail;

	v2g vert(uint id : SV_VertexID) 
	{
		v2g o = (v2g)0;

        TrailHeader header = _TrailHeaderBuffer[id];
        TrailNode node = _TrailNodeBuffer[header.head];

		// float4 wp = float4(i.vertex.xyz * 0.01f + _TrailHeaderBuffer[iid].pos,1);
		o.position = node.pos;
		o.color = 1;
		return o;
	}

	float4x4  _InvViewMatrix;
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
	[maxvertexcount(4)]
	void geom(point v2g In[1], inout TriangleStream<g2f> SpriteStream)
	{
		g2f o = (g2f)0;
		[unroll]
		for (int i = 0; i < 4; i++)
		{
			float3 position = g_positions[i] * 0.01;
			position = mul(_InvViewMatrix, position) + In[0].position;
			o.position = UnityObjectToClipPos(float4(position, 1.0));

			o.color = In[0].color;
			o.texcoord = g_texcoords[i];

			SpriteStream.Append(o);
		}

		SpriteStream.RestartStrip();
	}
	fixed4 frag(g2f i) : SV_Target
	{
		return i.color;
	}

	ENDCG

	SubShader
	{
		// No culling or depth
		// Cull On ZWrite On ZTest Always
		// ZWrite On ZTest Always
		// Blend One OneMinusSrcAlpha
		// Blend SrcAlpha OneMinusSrcAlpha
	
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			ENDCG
		}
	}
}
