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
		float3 prev : TEXCOORD0;
		float3 pos  : TEXCOORD1;
		float3 curr : TEXCOORD2;
		float3 next : TEXCOORD3;
	};

	struct g2f
	{
		float4 pos : POSITION;
		float2 uv  : TEXCOORD;
		float4 col : COLOR;
	};

	sampler2D _MainTex;
	float4 _ST;

	StructuredBuffer<TrailHeader> _TrailHeaderBuffer;
    int _TrailHeaderBufferCount;

	StructuredBuffer<TrailNode> _TrailNodeBuffer;
    int _TrailNodeBufferConnt;

    int _MaxNodePerTrail;

    float4 _Color;

    int GetIndex(int base, int offset, int len)
    {
        return clamp(base + offset, 0, base + len);
    }

	v2g vert(uint id : SV_VertexID) 
	{
		v2g o = (v2g)0;

        TrailHeader header = _TrailHeaderBuffer[id];
        const int hid = header.head;
        const int len = header.length;

        const int prev = GetIndex(hid, -1, len);
        const int this = GetIndex(hid,  0, len);
        const int curr = GetIndex(hid,  1, len);
        const int next = GetIndex(hid,  2, len);

        o.prev = _TrailNodeBuffer[prev].pos;
        o.pos  = _TrailNodeBuffer[this].pos;
        o.curr = _TrailNodeBuffer[curr].pos;
        o.next = _TrailNodeBuffer[next].pos;

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

    void AddQuad(float3 pos, float4 col, inout TriangleStream<g2f> outStream)
    {
		g2f o = (g2f)0;
		[unroll]
		for (int i = 0; i < 4; i++)
		{
			float3 position = g_positions[i] * 0.01f;
			position = mul(_InvViewMatrix, position) + pos;
			o.pos = UnityObjectToClipPos(float4(position, 1.0));
            o.col = col;

			outStream.Append(o);
		}

		outStream.RestartStrip();
    }
	[maxvertexcount(4)]
	void geom(point v2g p[1], inout TriangleStream<g2f> outStream)
	{
        const float _MITER_LIMIT = 0.75;

        float3 p0 = p[0].prev;
		float3 p1 = p[0].pos;
		float3 p2 = p[0].curr;
		float3 p3 = p[0].next;

		float2 uv = 0;//p[0].uv.xy;

		float  thickness = 0.05;
		
		// determine the direction of each of the 3 segments (previous, current, next)
		float2 v0 = normalize(p1.xy - p0.xy);
		float2 v1 = normalize(p2.xy - p1.xy);
		float2 v2 = normalize(p3.xy - p2.xy);

		// determine the normal of each of the 3 segments (previous, current, next)
		float2 n0 = float2(-v0.y, v0.x);
		float2 n1 = float2(-v1.y, v1.x);
		float2 n2 = float2(-v2.y, v2.x);

		// determine miter lines by averaging the normals of the 2 segments
		float2 miter_a = normalize(n0 + n1);	// miter at start of current segment
		float2 miter_b = normalize(n1 + n2);	// miter at end of current segment

		// determine the length of the miter by projecting it onto normal and then inverse it
		float length_a = thickness / (dot(miter_a, n1) + 1e-6);
		float length_b = thickness / (dot(miter_b, n1) + 1e-6);

		g2f pIn = (g2f)0;
		
		// prevent excessively long miters at sharp corners
		if( dot(v0, v1) < -_MITER_LIMIT)
		{
			miter_a  = n1;
			length_a = thickness;

			// close the gap
			if( dot(v0, n1) > 0 )
			{
				pIn.pos = UnityObjectToClipPos(float4((p1.xy + thickness * n0), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(1.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = UnityObjectToClipPos(float4((p1.xy + thickness * n1), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(1.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = UnityObjectToClipPos(float4( p1.xy, p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(0.5, uv.y);
				outStream.Append(pIn);
				
				outStream.RestartStrip();
			} else {
				pIn.pos = UnityObjectToClipPos(float4((p1.xy - thickness * n1), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(0.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = UnityObjectToClipPos(float4((p1.xy - thickness * n0), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(0.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = UnityObjectToClipPos(float4( p1.xy, p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(0.5, uv.y);
				outStream.Append(pIn);
				
				outStream.RestartStrip();
			}

		}

		if(dot(v1, v2) < -_MITER_LIMIT)
		{
			miter_b  = n1;
			length_b = thickness;
		}

		// generate the triangle strip
		pIn.pos = UnityObjectToClipPos(float4( (p1.xy + length_a * miter_a), p1.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(1.0, uv.y);
		outStream.Append(pIn);

		pIn.pos = UnityObjectToClipPos(float4( (p1.xy - length_a * miter_a), p1.z, 1.0 ));
		pIn.col =_Color;
		pIn.uv  = float2(0.0, uv.y);
		outStream.Append(pIn);

		pIn.pos = UnityObjectToClipPos(float4( (p2.xy + length_b * miter_b), p2.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(1.0, uv.y);
		outStream.Append(pIn);

		pIn.pos = UnityObjectToClipPos(float4( (p2.xy - length_b * miter_b), p2.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(0.0, uv.y);
		outStream.Append(pIn);
		
		outStream.RestartStrip();
        // AddQuad(p0, float4(1,0,0,1), outStream);
        AddQuad(p1, float4(1,1,1,1), outStream);
        // AddQuad(p2, float4(0,1,0,1), outStream);
        // AddQuad(p3, float4(0,0,1,1), outStream);
	}

	fixed4 frag(g2f i) : SV_Target
	{
        return 1;
		return i.col;
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
