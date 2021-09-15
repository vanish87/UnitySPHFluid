
static const int ThicknessGradient = 0;
static const int HorizontalAlphaGradient = 1;
static const int VerticalAlphaGradient = 2;
static const int ColorGradient = 3;

sampler2D _GradientTexture;
int _GradientTextureHeight;

float2 GradientToUV(float uvx, int gradient, int texHeight)
{
	float texSize = 1.0f/texHeight;
	return float2(uvx, gradient * texSize + texSize * 0.5f);
}

float4 SampleXLod(sampler2D tex, float2 uv, int gradient, int texHeight)
{
	return tex2Dlod(tex, float4(GradientToUV(uv.x, gradient, texHeight),0,0));
}
float4 SampleYLod(sampler2D tex, float2 uv, int gradient, int texHeight)
{
	return tex2Dlod(tex, float4(GradientToUV(uv.y, gradient, texHeight),0,0));
}
float4 SampleX(sampler2D tex, float2 uv, int gradient, int texHeight)
{
	return tex2D(tex, GradientToUV(uv.x, gradient, texHeight));
}
float4 SampleY(sampler2D tex, float2 uv, int gradient, int texHeight)
{
	return tex2D(tex, GradientToUV(uv.y, gradient, texHeight));
}