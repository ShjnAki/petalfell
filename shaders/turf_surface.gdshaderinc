// Fine living-surface colour, shared by turf, its hanging lip and green plants.
// Authored biome/material placement owns the landscape. These continuous fields
// only vary the existing surface in global XZ, so streaming and face direction
// cannot move the patches. Ratios multiply already-linear material colours.
float turf_hash(vec2 p) {
	uvec2 q = uvec2(ivec2(p));
	uint h = q.x * 374761393u + q.y * 668265263u;
	h = (h ^ (h >> 13u)) * 1274126177u;
	return float((h ^ (h >> 16u)) & 16777215u) / 16777216.0;
}

float turf_noise(vec2 p) {
	vec2 i = floor(p), f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(turf_hash(i), turf_hash(i + vec2(1, 0)), f.x),
		mix(turf_hash(i + vec2(0, 1)), turf_hash(i + vec2(1, 1)), f.x), f.y);
}

// RGB: dry/lush colour ratio. A: a separate connected fringe-coverage field.
vec4 turf_surface(vec2 world_xz) {
	vec2 p = world_xz * 0.075;
	vec2 warped = p + (vec2(turf_noise(p * 0.43 + 7.3),
		turf_noise(p * 0.43 + 41.7)) - 0.5) * 1.3;
	mat2 turn = mat2(vec2(0.86, 0.51), vec2(-0.51, 0.86));
	float growth = turf_noise(warped + 31.2) * 0.66
		+ turf_noise(turn * warped * 2.07 + 17.3) * 0.34;
	float lush = smoothstep(0.32, 0.69, growth);
	vec3 dry_tone = vec3(1.05, 0.86, 0.77);
	vec3 lush_tone = vec3(0.76, 0.94, 1.09);
	vec3 colour = mix(dry_tone, lush_tone, lush);
	colour *= 1.0 + (growth - 0.5) * 0.20;
	float fringe = turf_noise(world_xz * 0.33 + vec2(9.1, 53.7));
	return vec4(colour, fringe);
}

vec3 turf_colour(vec3 linear_colour, vec3 field) {
	vec3 colour = linear_colour * vec3(0.83, 0.80, 0.88) * field;
	float luminance = dot(colour, vec3(0.2126, 0.7152, 0.0722));
	return mix(vec3(luminance), colour, 0.84);
}
