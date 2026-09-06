// Shared by solid terrain, moving grass and the character. Only direct
// directional illumination receives cloud shadow; sky fill and local lamps
// remain available underneath it. Global XZ keeps moving windows continuous.
global uniform float pf_cloud_cover;
global uniform float pf_weather_time;
global uniform float pf_cloud_daylight;
#include "reflection_clip.gdshaderinc"
varying float cloud_transmission;
#ifdef PETALFELL_CANOPY_LIGHT
varying float canopy_wrap;
#endif

float cloud_hash(vec2 p) {
	uvec2 q = uvec2(ivec2(p));
	uint h = q.x * 374761393u + q.y * 668265263u;
	h = (h ^ (h >> 13u)) * 1274126177u;
	return float((h ^ (h >> 16u)) & 16777215u) / 16777216.0;
}

float cloud_noise(vec2 p) {
	vec2 i = floor(p), f = smoothstep(vec2(0), vec2(1), fract(p));
	return mix(mix(cloud_hash(i), cloud_hash(i + vec2(1,0)), f.x),
		mix(cloud_hash(i + vec2(0,1)), cloud_hash(i + vec2(1,1)), f.x), f.y);
}

float cloud_at(vec3 world) {
	vec2 p = world.xz * 0.011 + vec2(pf_weather_time * 0.0023, pf_weather_time * 0.0009);
	float cover = cloud_noise(p) * 0.72 + cloud_noise(p * 2.07 + 31.4) * 0.28;
	float mask = smoothstep(0.45, 0.72, cover + pf_cloud_cover * 0.24);
	return 1.0 - mask * (0.32 + pf_cloud_cover * 0.20) * pf_cloud_daylight;
}

void light() {
	float transmission = LIGHT_IS_DIRECTIONAL ? cloud_transmission : 1.0;
	float nl = max(dot(NORMAL, LIGHT), 0.0);
	float diffuse_nl = nl;
#ifdef PETALFELL_CANOPY_LIGHT
	// A small normalized wrap softens opaque blossom lobes only. Real shadow
	// and cloud attenuation still apply, including on the wrapped side. All
	// other voxel materials set zero; other shader consumers omit this path.
	diffuse_nl = max(dot(NORMAL, LIGHT) + canopy_wrap, 0.0)
		/ ((1.0 + canopy_wrap) * (1.0 + canopy_wrap));
#endif
	float nv = max(dot(NORMAL, VIEW), 0.0);
	vec3 half_dir = normalize(LIGHT + VIEW);
	float lh = max(dot(LIGHT, half_dir), 0.0);
	// Burley diffuse retains a soft matte grazing response. Godot supplies
	// shadow attenuation, light colour/energy, sky irradiance and ambient AO.
	float fd90 = 0.5 + 2.0 * ROUGHNESS * lh * lh;
	float fd = (1.0 + (fd90 - 1.0) * pow(1.0 - nl, 5.0))
		* (1.0 + (fd90 - 1.0) * pow(1.0 - nv, 5.0));
	DIFFUSE_LIGHT += diffuse_nl * fd * ATTENUATION * transmission * LIGHT_COLOR / PI;
	float highlight = pow(max(dot(NORMAL, half_dir), 0.0), mix(64.0, 12.0, ROUGHNESS));
	SPECULAR_LIGHT += highlight * nl * ATTENUATION * transmission * LIGHT_COLOR
		* (1.0 - ROUGHNESS) * 0.12;
}
