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

BRDF GetBRDF (Surface surface) {
	BRDF brdf;

	brdf.diffuse = surface.color * OneMinusReflectivity(surface.metallic);
    // 나가는 빛의 양이 들어오는 빛의 양을 초과할 수 없으므로.
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	float perceptualRoughness =
		PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

#endif