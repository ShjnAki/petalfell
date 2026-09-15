shader_type spatial;
#include "rain_field.gdshaderinc"

// Sub-voxel ground detail: grass tufts, flower heads, pebbles, reeds, petals.
//
// One merged mesh per chunk. Colour is per vertex — each blade runs from a
// deeper base to a lighter tip — and CUSTOM0 carries (sway weight, clump
// phase): the weight is 0 at a root and 1 at a tip, the phase is per clump, so
// a meadow ripples instead of pulsing in lockstep.
//
// Depth is not written. Thousands of blades writing depth put a depth edge
// under every lawn, and anything that keys off depth discontinuities — the
// ink's own occlusion test included — then sees grass where it should see
// ground.
render_mode cull_disabled, depth_draw_never, diffuse_burley, specular_disabled;
#include "cloud_light.gdshaderinc"
#include "turf_surface.gdshaderinc"
varying vec3 detail_world;
varying vec3 detail_normal_world;
varying float detail_phase;
global uniform float pf_night;

uniform float wind_amp = 0.10;
uniform float wind_speed = 1.0;

void vertex() {
	float sway = CUSTOM0.x;
	float phase = CUSTOM0.y;
	detail_phase = phase;
	float t = TIME * wind_speed;
	// Two frequencies, so a gust travels rather than the whole field breathing.
	VERTEX.x += (sin(t + phase) * 0.7 + sin(t * 1.7 + phase * 1.3) * 0.3) * sway * wind_amp;
	VERTEX.z += (cos(t * 0.83 + phase * 1.1) * 0.7) * sway * wind_amp * 0.6;
	// Thin plant edges retain subpixel coverage at wide zoom. CUSTOM1 points
	// to the same edge midpoint at both corners; roots and wind joints stay put.
	// Limit widening to 3x and 0.9 pixels total, leaving broad faces unchanged.
	if (dot(CUSTOM1.xyz, CUSTOM1.xyz) > .000001) {
		vec4 edge = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX,1.0);
		vec4 middle = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX+CUSTOM1.xyz,1.0);
		float half_pixels = length((edge.xy/edge.w-middle.xy/middle.w)*VIEWPORT_SIZE*.5);
		float widen = clamp(.45/max(half_pixels,.001),1.0,3.0);
		VERTEX -= CUSTOM1.xyz*(widen-1.0);
	}
	detail_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	// Blades deliberately use the ground's upward normal on both sides.
	// Preserve it before the renderer flips back-face normals: otherwise a
	// quarter-turn changes the same meadow from pale grass into black slivers.
	detail_normal_world = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
	if (reflection_below_plane(detail_world, CAMERA_VISIBLE_LAYERS)) discard;
	NORMAL = normalize((VIEW_MATRIX * vec4(detail_normal_world, 0.0)).xyz);
	cloud_transmission = cloud_at(detail_world);
	ALBEDO = COLOR.rgb;
	// Existing green blade/leaf colours share the turf's local dry/lush tone.
	// Pink, cream, blue and yellow flowers, petals and mineral chips keep theirs.
	if (COLOR.g > COLOR.r * 1.04 && COLOR.g > COLOR.b * 1.25) {
		ALBEDO = turf_colour(ALBEDO, turf_surface(detail_world.xz).rgb);
	}
	float wet = rain_wetness(detail_world.xz);
	ALBEDO *= 1.0-wet*.12;
	ROUGHNESS = mix(.95,.58,wet);
	SPECULAR = wet*.35;
	// Only explicitly tagged terminal mushroom beads glow. Ground plants keep
	// alpha=1, ordinary scene lighting and no emission. This remains opaque.
	float spore = 1.0 - step(.05, abs(COLOR.a - .5));
	EMISSION = COLOR.rgb * spore * smoothstep(.12, .68, pf_night) *
		(.72 + .18 * sin(TIME * .7 + detail_phase));
}
