Shader "Unlit/SPHParticleShader"
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
        float radius = 0.5f * _H * _ParticleScale * shoudRender;
        float4 wp = float4(i.vertex.xyz * radius + p.pos,1);
        o.position = UnityObjectToClipPos(wp);
        o.color = p.col;
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
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
		// Blend One One
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
    }
}
