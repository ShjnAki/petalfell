shader_type spatial;

// Arbitrary imported sculpture has no voxel edge-run metadata, so the world's
// screen-space ink shader cannot consume it. This inverted hull expands in
// framebuffer pixels: scaling an imported subject must not widen its ink.
render_mode unshaded, cull_front, depth_draw_never, shadows_disabled,
	fog_disabled, specular_disabled;
#include "outline_haze.gdshaderinc"

uniform vec3 ink_colour : source_color = vec3(0.47, 0.42, 0.52);
uniform float outline_pixels : hint_range(0.0, 2.0) = 0.50;
uniform bool authored_outline_normals = false;
global uniform float pf_night;

vec3 outline_normal(vec2 encoded) {
	if (encoded.x < 0.0) return vec3(0.0);
	vec2 p = encoded * 2.0 - 1.0;
	vec3 n = vec3(p, 1.0 - abs(p.x) - abs(p.y));
	float fold = clamp(-n.z, 0.0, 1.0);
	n.xy += mix(vec2(fold), vec2(-fold), greaterThanEqual(n.xy, vec2(0.0)));
	return normalize(n);
}

void vertex() {
	vec4 clip = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
	// Lighting keeps the flat carved normals. The separately packed hull normal
	// agrees at shared corners, so hard face splits do not tear the outline apart.
	vec3 hull_normal = authored_outline_normals ? outline_normal(UV2) : NORMAL;
	vec3 view_normal = MODELVIEW_NORMAL_MATRIX * hull_normal;
	vec2 projected = (PROJECTION_MATRIX * vec4(view_normal, 0.0)).xy * VIEWPORT_SIZE;
	vec2 direction = projected / max(length(projected), 0.00001);
	float air = outline_air(length((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz - CAMERA_POSITION_WORLD));
	clip.xy += direction * (2.0 * outline_pixels * air / VIEWPORT_SIZE) * clip.w;
	POSITION = clip;
}

void fragment() {
	if (CAMERA_VISIBLE_LAYERS == 0x7ffffu) discard;
	ALBEDO = ink_colour * mix(1.0, 0.58, pf_night);
	ROUGHNESS = 1.0;
}
