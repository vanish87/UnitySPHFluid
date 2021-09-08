Shader "Unlit/TrailDebugShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}


	CGINCLUDE
	#include "UnityCG.cginc"

	struct v2g
	{
		float3 p0 : TEXCOORD0;
		float3 p1 : TEXCOORD1;
		float3 p2 : TEXCOORD2;
		float3 p3 : TEXCOORD3;
	};

	struct g2f
	{
		float4 pos : POSITION;
		float2 uv  : TEXCOORD;
		float4 col : COLOR;
	};

	sampler2D _MainTex;
	float4 _ST;

    float4 _Color;

    float4 p0;
    float4 p1;
    float4 p2;
    float4 p3;

	v2g vert(uint id : SV_VertexID) 
	{
		v2g o = (v2g)0;
        // float4 p0 = float4(0, 0, 0, 1);
        // float4 p1 = float4(1, 0, 0, 1);
        // float4 p2 = float4(2, 0, 0, 1);
        // float4 p3 = float4(3, 0, 0, 1);


        // p2 = float4(0.5, 1, 0, 1);
        // p3 = float4(0, 1, 0, 1);

        float4 idOffset = float4(0, id, 0, 0);
        p0 += idOffset;
        p1 += idOffset;
        p2 += idOffset;
        p3 += idOffset;

        bool useClip = false;
        if(useClip)
        {
            p0 = UnityObjectToClipPos(p0);
            p1 = UnityObjectToClipPos(p1);
            p2 = UnityObjectToClipPos(p2);
            p3 = UnityObjectToClipPos(p3);
            // p0 /= p0.w;
            // p1 /= p1.w;
            // p2 /= p2.w;
            // p3 /= p3.w;
        }
        else
        {
            p0 = float4(UnityObjectToViewPos(p0), 1);
            p1 = float4(UnityObjectToViewPos(p1), 1);
            p2 = float4(UnityObjectToViewPos(p2), 1);
            p3 = float4(UnityObjectToViewPos(p3), 1);

            // p0 = mul(UNITY_MATRIX_P, p0);
            // p1 = mul(UNITY_MATRIX_P, p1);
            // p2 = mul(UNITY_MATRIX_P, p2);
            // p3 = mul(UNITY_MATRIX_P, p3);
            // p0 /= p0.w;
            // p1 /= p1.w;
            // p2 /= p2.w;
            // p3 /= p3.w;
        }

        o.p0 = p0;
        o.p1 = p1;
        o.p2 = p2;
        o.p3 = p3;
        
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
			float3 position = g_positions[i] * 0.2f;
			position = mul(_InvViewMatrix, position) + pos;
			o.pos = UnityObjectToClipPos(float4(position, 1.0));
            o.col = col;

			outStream.Append(o);
		}

		outStream.RestartStrip();
    }

    float4 Generate(float4 pos)
    {
        return mul(UNITY_MATRIX_P, pos);
    }
	[maxvertexcount(7)]
	void geom(point v2g p[1], inout TriangleStream<g2f> outStream)
	{
        const float _MITER_LIMIT = 0.75;

        float3 p0 = p[0].p0;
		float3 p1 = p[0].p1;
		float3 p2 = p[0].p2;
		float3 p3 = p[0].p3;

		float2 uv = 0;//p[0].uv.xy;

		float thickness = 0.1;
        float far = _ProjectionParams.z;

		
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
				pIn.pos = Generate(float4((p1.xy + thickness * n0), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(1.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = Generate(float4((p1.xy + thickness * n1), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(1.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = Generate(float4( p1.xy, p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(0.5, uv.y);
				outStream.Append(pIn);
				
				outStream.RestartStrip();
			} else {
				pIn.pos = Generate(float4((p1.xy - thickness * n1), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(0.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = Generate(float4((p1.xy - thickness * n0), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(0.0, uv.y);
				outStream.Append(pIn);

				pIn.pos = Generate(float4( p1.xy, p1.z, 1.0 ));
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

        float base = abs(p2.z);
        float scale = abs(p1.z);
        float p2f = scale / base;

		// generate the triangle strip
		pIn.pos = Generate(float4( (p1.xy + length_a * miter_a), p1.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(1.0, uv.y);
		outStream.Append(pIn);

		pIn.pos = Generate(float4( (p1.xy - length_a * miter_a), p1.z, 1.0 ));
		pIn.col =_Color;
		pIn.uv  = float2(0.0, uv.y);
		outStream.Append(pIn);

		pIn.pos = Generate(float4( (p2.xy + length_b * miter_b * p2f), p2.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(1.0, uv.y);
		outStream.Append(pIn);

		pIn.pos = Generate(float4( (p2.xy - length_b * miter_b * p2f), p2.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(0.0, uv.y);
		outStream.Append(pIn);
		
		outStream.RestartStrip();
    }

	[maxvertexcount(4)]
	void geomParticle(point v2g p[1], inout TriangleStream<g2f> outStream)
	{
        float3 p0 = p[0].p0;
		float3 p1 = p[0].p1;
		float3 p2 = p[0].p2;
		float3 p3 = p[0].p3;

        // AddQuad(p0, float4(1,0,0,1), outStream);
        AddQuad(p1, float4(1,1,1,1), outStream);
        // AddQuad(p2, float4(0,1,0,1), outStream);
        // AddQuad(p3, float4(0,0,1,1), outStream);
	}


	fixed4 frag(g2f i) : SV_Target
	{
        return 1;
        return float4(1,1,1,0.3);
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
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			// #pragma geometry geomParticle
			#pragma fragment frag
			ENDCG
		}
	}
}

