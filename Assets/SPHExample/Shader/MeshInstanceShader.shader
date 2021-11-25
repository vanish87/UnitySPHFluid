Shader "UnityTools/MeshInstanceShader"
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
    
    StructuredBuffer<Particle> _ParticleBuffer;
    float _ParticleScale;
    bool _RenderBoundaryParticle;

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
        o.uv = i.uv;
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        return i.color;
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
			#pragma fragment frag
			ENDCG
		}
    }
}
