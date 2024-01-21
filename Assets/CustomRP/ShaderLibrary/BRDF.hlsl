#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED
#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity (float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

float customFunction(float x) {
    if (x > 0.9 && x < 1.1) {
        return 1 - abs(x - 1) * 10;
    } else {
        return 0;
    }
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

// 7주차 응용. 테스트 맵을 사용하여 별도 반사 효과를 구현한다.
BRDF GetBRDFWithTestMap (inout Surface surface, float4 testMap, bool applyAlphaToDiffuse = false) {
	BRDF brdf;

	if(testMap.a > 0.5) {
		brdf.diffuse = surface.color * OneMinusReflectivity(0);
		if (applyAlphaToDiffuse) {
			brdf.diffuse *= surface.alpha;
		
		}
		brdf.specular = lerp(0.2, surface.color, 0);
		float perceptualRoughness =
			PerceptualSmoothnessToPerceptualRoughness(0.9);
		brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
		return brdf;
	} else {
		brdf.diffuse = surface.color * OneMinusReflectivity(surface.metallic);
		if (applyAlphaToDiffuse) {
			brdf.diffuse *= surface.alpha;
		
		}
		
		// 나가는 빛의 양이 들어오는 빛의 양을 초과할 수 없으므로.
		brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
		float perceptualRoughness =
			PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
		brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	}

	return brdf;
}

// 7주차 응용. 텍스쳐의 파란 부분을 특정한 반사율로 고정한다.
BRDF GetBRDFWithTexture (inout Surface surface, float4 baseMap, bool applyAlphaToDiffuse = false) {
	BRDF brdf;

	float metallic = surface.metallic;
	float smoothness = surface.smoothness;
	if (baseMap.b > baseMap.r) {
		metallic = 0.0;
		smoothness = 0.95;
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

// 7주차 응용. 높이에 따른 반사율
BRDF GetBRDFWithHeight (inout Surface surface, float3 positionWS, bool applyAlphaToDiffuse = false) {
	BRDF brdf;

	float metallic = surface.metallic;
	float smoothness = customFunction(positionWS.y);

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