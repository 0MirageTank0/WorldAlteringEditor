#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4 OutlineColor = float4(0, 0, 0, 1); // 描边颜色 (黑色)
float OutlineThickness = 1.0;             // 描边粗细 (像素)

sampler2D SpriteTextureSampler : register(s0)
{
    Texture = (SpriteTexture); // this is set by spritebatch
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};
// 判断像素是否透明
float SampleAlpha(float2 uv)
{
    return tex2D(SpriteTextureSampler, uv).a;
}
float4 MainPS(VertexShaderOutput input) : COLOR
{
    // 利用 ddx/ddy 获取 texel 大小
    float dx = 0.01f;
    float dy = 0.01f;
    float2 texelSize = float2(abs(dx), abs(dy));

    float alpha = SampleAlpha(input.TextureCoordinates);

    // 原图像素直接输出
    if (alpha > 0.0)
    {
        return tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    }

    // 检查邻域像素（8邻域）
    float outline = 0.0;
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y) * OutlineThickness * texelSize;
            outline = max(outline, SampleAlpha(input.TextureCoordinates + offset));
        }
    }

    if (outline > 0.0)
        return OutlineColor;

    return float4(0, 0, 0, 0);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
