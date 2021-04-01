#ifndef VAT_INCLUDED
#define VAT_INCLUDED

sampler2D _AnimMap;
half4 _AnimMap_TexelSize;//x == 1/width

half _AnimLen;
#if VATMultipleRows_ON
int _AnimOffsetYPixel;
#endif

inline half remap01(half x, half s1, half s2)
{
	return x * (s2 - s1) + s1;
}

inline half4 GetSampledVATVertPos(int vid,float time,float diverse)
{
#if !VATMultipleRows_ON
	half sampleX = (vid + 0.5) * _AnimMap_TexelSize.x;
	half sampleY = time / _AnimLen + diverse;
#else
	//Avoiding "Texture Bilinear" sampling errors
	half paddingOffset = 1 / ((float)_AnimOffsetYPixel);

	int nowLayer = vid / _AnimMap_TexelSize.z;

	half offsetYUnit = _AnimOffsetYPixel * _AnimMap_TexelSize.y;
	half offsetY = nowLayer * offsetYUnit;

	half sampleX = ((vid - nowLayer * _AnimMap_TexelSize.z) + 0.5) * _AnimMap_TexelSize.x;
	half sampleY = frac(time / _AnimLen + diverse);
	sampleY = remap01(sampleY, paddingOffset, 1 - paddingOffset) * offsetYUnit;
	sampleY = sampleY + offsetY;
#endif
	half4 pos = tex2Dlod(_AnimMap, half4(sampleX, sampleY, 0, 0));

	return pos;
}

#endif