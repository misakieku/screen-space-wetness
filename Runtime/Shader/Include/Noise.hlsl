void Hash_Tchou_2_1_uint(uint2 v, out uint o)
{
    // ~6 alu (2 mul)
    v.y ^= 1103515245U;
    v.x += v.y;
    v.x *= v.y;
    v.x ^= v.x >> 5u;
    v.x *= 0x27d4eb2du;
    o = v.x;
}

void Hash_Tchou_2_1_float(float2 i, out float o)
{
    uint r;
    uint2 v = (uint2) (int2) round(i);
    Hash_Tchou_2_1_uint(v, r);
    o = (r >> 8) * (1.0 / float(0x00ffffff));
}

float ValueNoise (float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    uv = abs(frac(uv) - 0.5);
    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);
    float r0; Hash_Tchou_2_1_float(c0, r0);
    float r1; Hash_Tchou_2_1_float(c1, r1);
    float r2; Hash_Tchou_2_1_float(c2, r2);
    float r3; Hash_Tchou_2_1_float(c3, r3);
    float bottomOfGrid = lerp(r0, r1, f.x);
    float topOfGrid = lerp(r2, r3, f.x);
    float t = lerp(bottomOfGrid, topOfGrid, f.y);
    return t;
}
        
float SimpleNoise(float2 UV, float Scale)
{
    float freq, amp;
    float Out = 0.0f;
    freq = pow(2.0, float(0));
    amp = pow(0.5, float(3-0));
    Out += ValueNoise(float2(UV.xy*(Scale/freq)))*amp;
    freq = pow(2.0, float(1));
    amp = pow(0.5, float(3-1));
    Out += ValueNoise(float2(UV.xy*(Scale/freq)))*amp;
    freq = pow(2.0, float(2));
    amp = pow(0.5, float(3-2));
    Out += ValueNoise(float2(UV.xy*(Scale/freq)))*amp;

    return Out;
}