shader_type spatial;
render_mode cull_disabled, depth_draw_never, diffuse_burley, specular_disabled;

// Geometry supplies the outline and fold. Alpha only fades the existing pool;
// there is no billboard mask or unshaded white paper rectangle.
#define PETALFELL_CANOPY_LIGHT
#include "cloud_light.gdshaderinc"
varying vec3 petal_world;

void vertex() {
	petal_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	if (reflection_below_plane(petal_world, CAMERA_VISIBLE_LAYERS)) discard;
	cloud_transmission = cloud_at(petal_world);
	canopy_wrap = 0.24;
	// Soft pigment gathers at the root; the cupped tip stays slightly paler.
	float pigment = mix(0.84, 1.0, smoothstep(0.0, 0.85, UV.y));
	ALBEDO = COLOR.rgb * pigment;
	ROUGHNESS = 0.96;
	SPECULAR = 0.0;
	ALPHA = COLOR.a;
}
