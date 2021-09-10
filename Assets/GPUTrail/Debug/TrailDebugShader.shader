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
		float2 uv12  : TEXCOORD4;
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
	StructuredBuffer<float4> _TrailData;
	int _TrailDataCount;

	float _Thiness;
	float _MiterLimit;
	int _zScale;


	v2g vert(uint vid : SV_VertexID) 
	{
		v2g o = (v2g)0;
		int id = vid;
		bool prev  = id > 0;
		bool next  = id+1<_TrailDataCount;
		bool nnext = id+2<_TrailDataCount;

		int i0 = prev?id-1:id;
		int i1 = id;
		int i2 = next?id+1:id;
		int i3 = nnext?id+2:next?id+1:id;

		float4 p0 = _TrailData[i0];
		float4 p1 = _TrailData[i1];
		float4 p2 = _TrailData[i2];
		float4 p3 = _TrailData[i3];

		p0 = prev?p0:p1+normalize(p1-p2);
		p3 = nnext?p3:p2+normalize(p2-p1);

		p0 = float4(UnityObjectToViewPos(p0), 1);
		p1 = float4(UnityObjectToViewPos(p1), 1);
		p2 = float4(UnityObjectToViewPos(p2), 1);
		p3 = float4(UnityObjectToViewPos(p3), 1);

        o.p0 = p0;
        o.p1 = p1;
        o.p2 = p2;
        o.p3 = p3;
		o.uv12 = float2(id, id+1)/(_TrailDataCount-1);
		return o;
	}

    inline float4 Generate(float4 pos)
    {
		// return pos;
        return mul(UNITY_MATRIX_P, pos);
    }

	//Geometry code modifed from https://github.com/paulhoux/Cinder-Samples/tree/master/GeometryShader
	[maxvertexcount(7)]
	void geom(point v2g p[1], inout TriangleStream<g2f> outStream)
	{
		g2f pIn = (g2f)0;

        float3 p0 = p[0].p0;
		float3 p1 = p[0].p1;
		float3 p2 = p[0].p2;
		float3 p3 = p[0].p3;

		float2 uv12 = p[0].uv12;

		float thickness = _Thiness;
		float proj = _ProjectionParams.x;
        float near = _ProjectionParams.y;
        float far = _ProjectionParams.z;

		float z1 = -p1.z;
		float z2 = -p2.z;
		z1 = (far - (z1-near))/(far-near);
		z2 = (far - (z2-near))/(far-near);
		z1 *= (proj != 0);
		z2 *= (proj != 0);

		z1 = _zScale?z1:1;
		z2 = _zScale?z2:1;

		
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

		
		// prevent excessively long miters at sharp corners
		if( dot(v0, v1) < -_MiterLimit)
		{
			miter_a  = n1;
			length_a = thickness;

			// close the gap
			if( dot(v0, n1) > 0 )
			{
				pIn.pos = Generate(float4((p1.xy + thickness * n0), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(uv12.x, 1.0);
				outStream.Append(pIn);

				pIn.pos = Generate(float4((p1.xy + thickness * n1), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(uv12.x, 1.0);
				outStream.Append(pIn);

				pIn.pos = Generate(float4( p1.xy, p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(uv12.x, 0.5);
				outStream.Append(pIn);
				
				outStream.RestartStrip();
			} else {
				pIn.pos = Generate(float4((p1.xy - thickness * n1), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(uv12.x, 0.0);
				outStream.Append(pIn);

				pIn.pos = Generate(float4((p1.xy - thickness * n0), p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(uv12.x, 0.0);
				outStream.Append(pIn);

				pIn.pos = Generate(float4( p1.xy, p1.z, 1.0 ));
				pIn.col = _Color;
				pIn.uv  = float2(uv12.x, 0.5);
				outStream.Append(pIn);
				
				outStream.RestartStrip();
			}

		}

		if(dot(v1, v2) < -_MiterLimit)
		{
			miter_b  = n1;
			length_b = thickness;
		}

		// generate the triangle strip
		pIn.pos = Generate(float4( (p2.xy + length_b * miter_b * z2), p2.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(uv12.y, 1.0);
		outStream.Append(pIn);

		pIn.pos = Generate(float4( (p2.xy - length_b * miter_b * z2), p2.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(uv12.y, 0.0);
		outStream.Append(pIn);
		
		pIn.pos = Generate(float4( (p1.xy + length_a * miter_a * z1), p1.z, 1.0 ));
		pIn.col = _Color;
		pIn.uv  = float2(uv12.x, 1.0);
		outStream.Append(pIn);

		pIn.pos = Generate(float4( (p1.xy - length_a * miter_a * z1), p1.z, 1.0 ));
		pIn.col =_Color;
		pIn.uv  = float2(uv12.x, 0.0);
		outStream.Append(pIn);
        
		outStream.RestartStrip();
    }

	fixed4 frag(g2f i) : SV_Target
	{
        return float4(i.uv.xy, 0, 1);
		return i.col;
	}

	ENDCG

	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        // ZWrite Off
        // Cull Off
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

