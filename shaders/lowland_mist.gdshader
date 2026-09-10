shader_type fog;

// A low water-level bank, lit by the ordinary sun/moon and sky. The froxel
// buffer is camera bounded; WORLD shape does not allocate world-sized data.
global uniform float pf_weather_time;
global uniform float pf_cloud_cover;
global uniform float pf_night;
uniform float water_level = 24.35;

float field_hash(vec2 p) {
	uvec2 q = uvec2(ivec2(p));
	uint h = q.x * 374761393u + q.y * 668265263u;
	h = (h ^ (h >> 13u)) * 1274126177u;
	return float((h ^ (h >> 16u)) & 16777215u) / 16777216.0;
}

float field(vec2 p) {
	vec2 i = floor(p), f = smoothstep(vec2(0), vec2(1), fract(p));
	return mix(mix(field_hash(i), field_hash(i + vec2(1,0)), f.x),
		mix(field_hash(i + vec2(0,1)), field_hash(i + vec2(1,1)), f.x), f.y);
}

void fog() {
	vec2 p = WORLD_POSITION.xz * 0.013 + vec2(pf_weather_time * 0.0011, pf_weather_time * 0.0004);
	float bank = smoothstep(0.32, 0.72, field(p) * 0.7 + field(p * 2.03 + 17.2) * 0.3);
	float altitude = (WORLD_POSITION.y - water_level - 2.0) / 12.0;
	float height_band = exp(-altitude * altitude);
	// Noon remains clear. Wet weather and evening reveal the same drifting banks.
	float southern = smoothstep(5000.0, 6800.0, WORLD_POSITION.z);
	DENSITY = height_band * bank * (0.0018 + 0.0015 * pf_cloud_cover + 0.0010 * pf_night) *
		mix(1.0, 2.3, southern);
	ALBEDO = vec3(0.78, 0.84, 0.95);
}
