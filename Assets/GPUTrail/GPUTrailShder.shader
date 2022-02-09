Shader "Unlit/TrailShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}


	CGINCLUDE
	#include "UnityCG.cginc"
    #include "GPUTrailData.cginc"
	#include "Gradient.cginc"

	struct v2g
	{
		float3 p0 : TEXCOORD0;
		float3 p1 : TEXCOORD1;
		float3 p2 : TEXCOORD2;
		float3 p3 : TEXCOORD3;
		float2 uv12  : TEXCOORD4;
		float3 vel : TEXCOORD5;
		float tlen : TEXCOORD6;
	};

	struct g2f
	{
		float4 pos : POSITION;
		float2 uv  : TEXCOORD;
		float4 col : COLOR;
	};

	struct gin
	{
		float4 col;
	};

	#define VertexOut g2f
	#define VertexIn gin
	VertexOut GenerateVertex(float3 pos, float2 uv , VertexIn vin)
	{
		VertexOut p = (VertexOut)0;
		p.pos = mul(UNITY_MATRIX_P, float4(pos, 1));
		// p.pos = float4(pos.xy,z,1);
		p.uv = uv;
		p.col = vin.col;
		return p;
	}
	#include "Trail.cginc"

	sampler2D _MainTex;
	float4 _ST;

	StructuredBuffer<TrailNode> _TrailNodeBuffer;
    int _TrailNodeBufferConnt;


	float _Thickness;
	int _CornerDivision;
	float _AngleThreshold;

	float _MaxSpeedScale;

	v2g vert(uint id : SV_VertexID) 
	{
		v2g o = (v2g)0;

        TrailNode node = _TrailNodeBuffer[id];

        if(node.idx == -1) return o;

		const bool prev = node.prev != -1;
        const bool next = node.next != -1;
        const bool nnext = next && (_TrailNodeBuffer[node.next].next != -1);

        const int i1 = node.idx;

        const int i0 = !prev ? i1:node.prev;
        const int i2 = !next ? i1:node.next;
        const int i3 = !nnext ? i1:_TrailNodeBuffer[node.next].next;

		const float uv1 = _TrailNodeBuffer[i1].uvx;
		const float uv2 = _TrailNodeBuffer[i2].uvx;

        float3 p0 = _TrailNodeBuffer[i0].pos;
        float3 p1 = _TrailNodeBuffer[i1].pos;
        float3 p2 = _TrailNodeBuffer[i2].pos;
        float3 p3 = _TrailNodeBuffer[i3].pos;

        float3 vel = _TrailNodeBuffer[i1].vel;
        float3 tlen = _TrailNodeBuffer[i1].totalLen;

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
		o.uv12 = float2(uv1, uv2);
		o.vel = vel;
		o.tlen = tlen;
		return o;
	}


	[maxvertexcount(32)]
	void geom(point v2g p[1], inout TriangleStream<g2f> outStream)
	{
		g2f pIn = (g2f)0;

        float3 p0 = p[0].p0;
		float3 p1 = p[0].p1;
		float3 p2 = p[0].p2;
		float3 p3 = p[0].p3;
		float speed = length(p[0].vel);
		float tlen = p[0].tlen;
		// speed = saturate(speed);
		// float scale = saturate(distance(p1, p2)/0.005);
		// speed = saturate(scale * 2);
		// float4 color = 1;
		// color *= scale;
		// color *= saturate(speed);
		// speed = saturate(speed);

		// if(distance(p1,p2)>1) return;
		// if(distance(p1,p2)<0.001) return;

		float2 uv12 = p[0].uv12;

		float4 t1 = SampleXLod(_GradientTexture, float2(uv12.x, 0), ThicknessGradient, _GradientTextureHeight);
        float4 t2 = SampleXLod(_GradientTexture, float2(uv12.y, 0), ThicknessGradient, _GradientTextureHeight);
        float2 thickness = _Thickness * float2(t1.a, t2.a);

		gin vertex = (gin)0;
		float t = speed;
		vertex.col = saturate(speed/_MaxSpeedScale);
		// vertex.col = saturate((tlen * speed)/_MaxSpeedScale);
		// vertex.col = smoothstep(0.2, 1, t/_MaxSpeedScale);
		// vertex.col = saturate(tlen/_MaxSpeedScale);

		GenerateMainLine(outStream, p0, p1, p2, p3, thickness, uv12, vertex, _AngleThreshold);
		GenerateCorner(outStream, p0, p1, p2, p3, thickness, uv12, vertex, _AngleThreshold, _CornerDivision);

    }

	fixed4 frag(g2f i) : SV_Target
	{
		float2 uv = i.uv;
		float4 col = float4(uv, 0, 1);
		float4 colGradient = SampleX(_GradientTexture, uv, ColorGradient, _GradientTextureHeight);
		col = colGradient;
		float4 alphaX = SampleX(_GradientTexture, uv, HorizontalAlphaGradient, _GradientTextureHeight);
		float4 alphaY = SampleY(_GradientTexture, uv, VerticalAlphaGradient, _GradientTextureHeight);
		col.a *= alphaX.a * alphaY.a * i.col;
		return col;
		return i.col;
	}

	ENDCG

	SubShader
	{
		// Pass
		// {
		// 	CGPROGRAM
		// 	#pragma vertex vert
		// 	#pragma geometry geom
		// 	#pragma fragment frag
		// 	ENDCG
		// }
		Pass
		{
			Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
			Cull Off
			ZWrite Off
			Blend Zero OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			ENDCG
		}
		Pass
		{
			Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
			Cull Off
			ZWrite Off
			Blend SrcAlpha One
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			ENDCG
		}
	}
}
