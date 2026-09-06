shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

uniform vec3 violet;
uniform vec3 rim_colour;

void fragment() {
	vec2 p = (UV - 0.5) * 2.0;
	p.x *= 1.16;
	float r = length(p);
	if (r > 0.98) discard;
	float angle = atan(p.y, p.x);
	float spiral = sin(r * 31.0 - angle * 2.0 - TIME * 2.2);
	float thread = pow(max(spiral, 0.0), 18.0);
	float ring = exp(-abs(r - 0.85) * 64.0);
	float core = exp(-r * r * 340.0);
	float orbit = thread * smoothstep(0.05, 0.22, r) * (1.0 - smoothstep(0.66, 0.87, r));
	ALBEDO = violet * (orbit * 2.6 + ring * 2.8 + 0.18) + rim_colour * (core * 5.0 + ring * 0.30);
	ALPHA = (1.0 - smoothstep(0.86, 0.98, r)) * 0.96;
}
