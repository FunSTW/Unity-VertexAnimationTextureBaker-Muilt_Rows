#ifndef VAT_INCLUDED
#define VAT_INCLUDED

sampler2D _AnimMap;
float4 _AnimMap_TexelSize;//x == 1/width

float _AnimLen;
#if VATMultipleRows_ON
int _AnimOffsetYPixel;
#endif

inline float remap01(float x, float s1, float s2)
{
	return x * (s2 - s1) + s1;
}

inline float4 GetSampledVATVertPos(int vid,float time,float diverse)
{
#if !VATMultipleRows_ON
	float sampleX = (vid + 0.5) * _AnimMap_TexelSize.x;
	float sampleY = time / _AnimLen + diverse;
#else
	//Avoiding "Texture Bilinear" sampling errors
	float paddingOffset = 1 / ((float)_AnimOffsetYPixel);

	int nowLayer = vid / _AnimMap_TexelSize.z;

	float offsetYUnit = _AnimOffsetYPixel * _AnimMap_TexelSize.y;
	float offsetY = nowLayer * offsetYUnit;

	float sampleX = ((vid - nowLayer * _AnimMap_TexelSize.z) + 0.5) * _AnimMap_TexelSize.x;
	float sampleY = frac(time / _AnimLen + diverse);
	sampleY = remap01(sampleY, paddingOffset, 1 - paddingOffset) * offsetYUnit;
	sampleY = sampleY + offsetY;
#endif
	float4 pos = tex2Dlod(_AnimMap, float4(sampleX, sampleY, 0, 0));

	return pos;
}

#endif