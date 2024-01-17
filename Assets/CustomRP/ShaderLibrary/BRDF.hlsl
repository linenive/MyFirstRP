#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED
#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity (float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

struct BRDF {
	float3 diffuse;
	float3 specular;
	float roughness;
};

BRDF GetBRDF (inout Surface surface, bool applyAlphaToDiffuse = false) {
	BRDF brdf;

	brdf.diffuse = surface.color * OneMinusReflectivity(surface.metallic);
	if (applyAlphaToDiffuse) {
		brdf.diffuse *= surface.alpha;
	
	}
	
    // 나가는 빛의 양이 들어오는 빛의 양을 초과할 수 없으므로.
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	float perceptualRoughness =
		PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

BRDF GetBRDFWithTexture (inout Surface surface, float4 baseMap, bool applyAlphaToDiffuse = false) {
	BRDF brdf;

	float metallic = surface.metallic;
	float smoothness = surface.smoothness;
	if (baseMap.b > baseMap.r) {
		metallic = baseMap.b;
		smoothness = 0.9;
	} 

	brdf.diffuse = surface.color * OneMinusReflectivity(metallic);
	
	if (applyAlphaToDiffuse) {
		brdf.diffuse *= surface.alpha;
	
	}
	
    // 나가는 빛의 양이 들어오는 빛의 양을 초과할 수 없으므로.
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, metallic);
	float perceptualRoughness =
		PerceptualSmoothnessToPerceptualRoughness(smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

// 너무 복잡해서 튜토리얼에서는 생략되었다. 
float SpecularStrength (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF (Surface surface, BRDF brdf, Light light) {
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif