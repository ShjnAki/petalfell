shader_type spatial;

// Arbitrary imported sculpture has no voxel edge-run metadata, so the world's
// screen-space ink shader cannot consume it. This inverted hull expands in
// framebuffer pixels: scaling an imported subject must not widen its ink.
render_mode unshaded, cull_front, depth_draw_never, shadows_disabled,
	fog_disabled, specular_disabled;
#include "outline_haze.gdshaderinc"

uniform vec3 ink_colour : source_color = vec3(0.47, 0.42, 0.52);
uniform float outline_pixels : hint_range(0.0, 2.0) = 0.50;
global uniform float pf_night;

void vertex() {
	vec4 clip = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
	vec3 view_normal = normalize(MODELVIEW_NORMAL_MATRIX * NORMAL);
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
