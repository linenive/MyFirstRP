#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface {
    float3 normal; // 조명 계산이 어디에서 일어나는 지 모르기 떄문에 normalWS를 사용하지 않음
    float3 viewDirection;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
};

#endif