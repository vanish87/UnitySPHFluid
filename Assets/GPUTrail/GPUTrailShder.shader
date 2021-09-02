Shader "Unlit/TrailShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}


	CGINCLUDE
	#include "UnityCG.cginc"
    #include "GPPUTrailData.cginc"

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

	sampler2D _MainTex;
	float4 _ST;

	StructuredBuffer<TrailHeader> _TrailHeaderBuffer;
    int _TrailHeaderBufferCount;

	StructuredBuffer<TrailNode> _TrailNodeBuffer;
    int _TrailNodeBufferConnt;

    int _MaxNodePerTrail;

	v2f vert(uint iid : SV_VertexID) 
	{
		v2f o = (v2f)0;

        TrailHeader header = _TrailHeaderBuffer[iid];

		// float4 wp = float4(i.vertex.xyz * 0.01f + _TrailHeaderBuffer[iid].pos,1);
		o.position = UnityObjectToClipPos(wp);
		o.color = 1;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
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
			#pragma fragment frag
			ENDCG
		}
	}
}
