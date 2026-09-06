// Shallow overlapping petal facets, sampled on the world-space face plane.
// The crown's existing block values still describe its large forms. Jittered
// centres and independently rotated ellipses break the old rectangular grain.
// Return height and its analytic plane gradient together: screen derivatives
// across unrelated faces must never become diagonal scratches in the normal.
vec3 canopy_petals(vec2 world_plane, float footprint) {
	if (footprint >= 0.095) return vec3(0.0);
	const float scale = 6.5;
	vec2 p = world_plane * scale;
	vec2 origin = floor(p);
	float height = 0.0;
	vec2 slope = vec2(0.0);
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			vec2 cell = origin + vec2(float(x), float(y));
			float seed = hash21(cell + vec2(71.0, 193.0));
			vec2 centre = cell + vec2(0.5) + (vec2(hash21(cell),
				hash21(cell + vec2(319.0, 47.0))) - 0.5) * 0.62;
			float angle = seed * TAU;
			vec2 axis = vec2(cos(angle), sin(angle));
			vec2 cross_axis = vec2(-axis.y, axis.x);
			vec2 delta = p - centre;
			vec2 q = vec2(dot(delta, axis), dot(delta, cross_axis));
			vec2 radius = vec2(mix(0.58, 0.78, seed), mix(0.36, 0.49, seed));
			vec2 inverse_square = 1.0 / (radius * radius);
			float r2 = dot(q * q, inverse_square);
			float t = clamp(1.0 - r2, 0.0, 1.0);
			float petal = t * t * (3.0 - 2.0 * t);
			if (petal > height) {
				height = petal;
				vec2 gradient = -12.0 * t * (1.0 - t) * q * inverse_square;
				slope = (axis * gradient.x + cross_axis * gradient.y) * scale;
			}
		}
	}
	// Suppress both albedo and relief before these small facets become subpixel.
	float visible = 1.0 - smoothstep(0.025, 0.095, footprint);
	return vec3((height - 0.33) * visible, slope * visible);
}
