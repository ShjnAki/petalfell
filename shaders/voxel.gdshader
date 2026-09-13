shader_type spatial;

// Terrain and structures.
//
// Form comes from the silhouette, a constant face-direction ramp, baked vertex
// occlusion and the haze. On top of that sits one thin layer of surface
// pattern: irregular clods on earth, weathered blotches on rock, a fine tooth
// on turf, lengthwise grain on timber. Flat colour alone leaves a cliff reading
// as a painted card, but the variation has to stay ORGANIC — an earlier
// revision drew masonry coursing on the soil band and the terraces read as
// built brick walls instead of cut ground.
//
// Patterns are evaluated on WORLD-space coordinates projected onto the face
// plane, so they tile seamlessly across block seams and never betray where one
// voxel ends and the next begins.
render_mode cull_back, diffuse_burley, specular_schlick_ggx;
#define PETALFELL_CANOPY_LIGHT
#include "cloud_light.gdshaderinc"
#include "mineral_surface.gdshaderinc"
#include "turf_surface.gdshaderinc"

uniform vec3 warm_tint = vec3(1.02, 1.00, 0.985);
uniform vec3 cool_tint = vec3(0.98, 0.985, 1.025);
uniform float face_tint : hint_range(0.0, 1.0) = 0.24;
// Driven by DayCycle. Global rather than per-material: the character shader
// alone builds a material for every box of every figure in the world, and a
// clock that had to find them all would spend the frame chasing references.
global uniform vec3 pf_sun_dir;
global uniform float pf_night;

// Tonal variation within each material. Macro material placement stays authored.
uniform float pattern_strength : hint_range(0.0, 1.0) = 0.78;
// Patterns fade out with distance. Kept past the fog's own reach they turn the
// far field into a shimmering mesh.
uniform float pattern_fade_start = 85.0;
uniform float pattern_fade_end = 240.0;

varying vec3 v_world;
varying vec4 v_surface;
varying flat float v_material;
varying flat float v_exposed_edges;
uniform float relief_strength : hint_range(0.0, 1.0) = 0.32;
uniform vec3 moss_substrate_top_ratio = vec3(1.0);
uniform vec3 moss_substrate_side_ratio = vec3(1.0);

float hash21(vec2 p) {
	// Integer avalanche keeps the same distribution at the atlas's far edge.
	// A multiply/fract float hash lost its fractional bits around x=10,000,
	// turning fine weathering into long vertical bands.
	uvec2 q = uvec2(ivec2(p));
	uint h = q.x * 374761393u + q.y * 668265263u;
	h = (h ^ (h >> 13u)) * 1274126177u;
	return float((h ^ (h >> 16u)) & 16777215u) / 16777216.0;
}

float vnoise(vec2 p) {
	vec2 i = floor(p), f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash21(i), hash21(i + vec2(1, 0)), f.x),
	           mix(hash21(i + vec2(0, 1)), hash21(i + vec2(1, 1)), f.x), f.y);
}

// Fractal value noise with a ROTATION between octaves.
//
// Plain value noise is sampled on an integer lattice, so a single octave — or
// several stacked on the same axes — keeps that lattice visible as square,
// axis-aligned structure. At terrain scale it stops reading as weathering and
// starts reading as a printed pattern. Rotating each octave and offsetting it
// destroys the alignment; warping the domain first destroys what is left.
float fbm(vec2 p) {
	float sum = 0.0, amp = 0.5, norm = 0.0;
	mat2 rot = mat2(vec2(0.86, 0.51), vec2(-0.51, 0.86));
	for (int i = 0; i < 4; i++) {
		sum += vnoise(p) * amp;
		norm += amp;
		p = rot * p * 2.07 + 17.3;
		amp *= 0.5;
	}
	return sum / norm;
}

// Push the sample point around with a lower-frequency field. Cheap, and it is
// what turns a regular blotch field into an irregular one.
vec2 warp(vec2 p, float amount) {
	return p + vec2(vnoise(p * 0.41), vnoise(p * 0.41 + 53.7)) * (amount * 2.0) - amount;
}

#include "canopy_surface.gdshaderinc"

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_surface = CUSTOM0;
	v_material = UV2.y;
	v_exposed_edges = UV2.x;
}

// Only real exposed lips receive this patina. UV and the four edge bits come
// from the same face that supplies the physical chamfer. Coplanar voxel joins
// therefore cannot acquire a fake seam or a printed weathering grid.
float exposed_lip_wear(vec2 face_uv, float edge_bits, vec3 world) {
	int edges = int(edge_bits + 0.5);
	float distance_to_lip = 2.0;
	if ((edges & 1) != 0) distance_to_lip = min(distance_to_lip, face_uv.x);
	if ((edges & 2) != 0) distance_to_lip = min(distance_to_lip, 1.0 - face_uv.x);
	if ((edges & 4) != 0) distance_to_lip = min(distance_to_lip, face_uv.y);
	if ((edges & 8) != 0) distance_to_lip = min(distance_to_lip, 1.0 - face_uv.y);
	// The field is continuous in world coordinates, including along a lip
	// crossing a chunk boundary. It varies the width of a shallow pale scuff.
	vec3 grain_cell = floor(world * 6.0) / 6.0;
	float wear = vnoise(grain_cell.xz * 2.1 + grain_cell.y * vec2(0.73, 1.27));
	float patch = vnoise(world.xz * 0.17 + world.y * vec2(0.13, 0.19) + 73.2);
	float width = mix(0.025, 0.13, smoothstep(0.36, 0.72, wear));
	float aa = max(fwidth(distance_to_lip), 0.004);
	return (1.0 - smoothstep(width - aa, width + aa, distance_to_lip))
		* smoothstep(0.34, 0.66, patch) * smoothstep(0.43, 0.62, wear);
}

// Paving is the existing laid flagstone material (19). These shallow joints
// follow its one-metre cells; they do not place slabs, cut a silhouette or add
// masonry courses to natural stone. Continuous wear opens and closes the seam.
// Return the world-plane height gradient and recessed coverage. Evaluate the
// footprint before wrapping coordinates so a tile boundary cannot inflate it.
vec3 paving_joint(vec2 p, float footprint) {
	vec2 edge = fract(p + 0.5) - 0.5;
	vec2 distance_to_edge = abs(edge);
	float wear = vnoise(p * 0.57 + vec2(43.7, 9.2));
	float width = mix(0.009, 0.030, wear);
	float aa = max(footprint * 0.65, 0.003);
	vec2 recess = vec2(1.0) - smoothstep(vec2(width - aa), vec2(width + aa), distance_to_edge);
	vec2 shoulder = vec2(1.0) - smoothstep(vec2(width + aa), vec2(width + 0.085 + aa), distance_to_edge);
	vec2 direction = clamp(edge / max(aa, 0.012), vec2(-1.0), vec2(1.0));
	float visible = 1.0 - smoothstep(0.20, 0.65, footprint);
	// No minimum seam: an unbroken all-over grid reads as new ceramic tiles.
	// Most of the plane stays worn flush; only connected exposed patches keep
	// a joint. The fine grain is separate from this broad wear field.
	float weather = smoothstep(0.46, 0.72, wear);
	return vec3(direction * shoulder * 0.18,
		max(recess.x, recess.y) * 0.18) * visible * weather;
}

void fragment() {
	if (reflection_below_plane(v_world, CAMERA_VISIBLE_LAYERS)) discard;
	cloud_transmission = cloud_at(v_world);
	canopy_wrap = 0.0;
	vec3 base = COLOR.rgb;

	vec3 n = normalize(NORMAL);
	vec3 wn = normalize((INV_VIEW_MATRIX * vec4(n, 0.0)).xyz);

	// A gentle stable face ramp supports the real directional light and AO.
	float face_shade = wn.y > 0.5 ? 1.0
		: wn.y < -0.5 ? 0.76
		: abs(wn.x) > 0.5 ? 0.97 : 0.94;
	base *= face_shade;

	// The face plane, in world units. A top face reads its XZ; a wall reads its
	// horizontal axis against world Y, so a bank's variation runs level.
	vec3 an = abs(wn);
	bool upright = an.y < 0.5;
	vec2 uv = an.y > 0.5 ? v_world.xz
		: (an.x > 0.5 ? vec2(v_world.z, v_world.y) : vec2(v_world.x, v_world.y));
	float footprint = max(length(dFdx(uv)), length(dFdy(uv)));
	vec3 paving = vec3(0.0);
	if (v_material > 18.5 && v_material < 19.5 && wn.y > 0.98) {
		paving = paving_joint(uv, footprint);
	}

	float pattern = floor(v_surface.x + 0.001);
	float drip = fract(v_surface.x + 0.001) / 0.9;
	bool turf = (v_material >= 1.0 && v_material <= 3.0)
		|| (v_material >= 6.0 && v_material <= 8.0) || v_material == 16.0;
	vec4 sward = vec4(1.0);
	if (turf || (upright && drip > 0.01)) sward = turf_surface(v_world.xz);

	float mark = 0.0;      // 0..1, how much this fragment is "in shadow" of its pattern
	float lift = 0.0;      // occasional brighter clods, so a face is not uniform
	float snow_drift = 0.0;
	float ice_crust = 0.0;
	float mineral_drift = 0.0;
	vec2 mineral_relief = vec2(0.0);
	float surface_relief = 0.65;
	vec2 wind_relief = vec2(0.0);
	float sand_grain = 0.0;
	vec2 petal_relief = vec2(0.0);

	if (pattern > 0.5 && pattern < 1.5) {
		// EARTH — soil and mud. Irregular clods and a broad tonal drift, with no
		// straight lines anywhere. An earlier revision drew masonry coursing
		// here; it read as a built brick wall rather than as a cut bank, which
		// is exactly wrong for terrain the player is meant to take as natural.
		vec2 wp = warp(uv * 0.9, 0.7);
		float broad = fbm(wp * 0.55);
		float clods = fbm(wp * 2.1);
		float grit = fbm(wp * 5.7);
		mark = (broad - 0.5) * 0.45 + (clods - 0.5) * 0.66 + (grit - 0.5) * 0.26;
		lift = smoothstep(0.70, 0.98, clods) * 0.10;
	} else if (pattern > 1.5 && pattern < 2.5) {
		// Correlated mineral patches retain a scale between macro authored wear
		// and fine grit. Quantised samples share a smooth field; they are not
		// independent random colours assigned to blocks or a masonry grid.
		vec2 wp = warp(uv * 0.72, 0.6);
		float broad = fbm(uv * vec2(0.085, 0.13) + 9.7);
		vec2 detail_uv = warp(uv * 0.075, 0.12);
		float flake = mineral_sample(detail_uv);
		mineral_relief = vec2(mineral_sample(detail_uv + vec2(0.0015,0)) - flake,
			mineral_sample(detail_uv + vec2(0,0.0015)) - flake);
		float blotch = fbm(wp * 1.35);
		// Intact mineral planes stay quieter than eroded patches. Uniform relief
		// over every stone face made close masonry look crumpled instead of worn.
		surface_relief = 0.32 + smoothstep(0.53, 0.70, blotch) * 0.48;
		float grain = vnoise(uv * 4.5);
		// A second scale supplies metre-sized mineral islands that still resolve
		// at the wide camera. Fine grit alone vanishes into a single average grey.
		// Filter out the texture's grit before magnifying this second sample.
		// Otherwise every wall inherits the same enlarged crumpled-paper grain.
		float islands = mineral_island_sample(warp(uv * 0.015, 0.10) + vec2(0.20,0.60));
		// The source's measured mean is .660 and its standard deviation only
		// .046. At the old .70 gain this survived tonemapping as almost flat
		// colour. Preserve the filtered island contours at a readable amplitude;
		// fine relief and grit still follow their separate near-distance fade.
		mineral_drift = clamp((broad - 0.50) * 0.42 + (islands - 0.660) * 1.80,
			-0.28, 0.22);
		float weathered = smoothstep(0.46, 0.68, blotch);
		mark = (blotch - 0.48) * 0.25 + (grain - 0.5) * 0.08
			+ (flake - 0.65) * mix(0.30, 0.80, weathered);
		lift = smoothstep(0.60, 0.82, flake) * 0.065;

	} else if (pattern > 2.5 && pattern < 3.5) {
		// PLANK — the one built material left. Boards are a made thing and read
		// as one, but the seams stay soft and lengthwise, never a grid.
		float board = uv.y * 2.6;
		float seam = abs(fract(board) - 0.5) * 2.0;
		mark = (1.0 - smoothstep(0.55, 0.95, seam)) * 0.40;
		mark += (fbm(vec2(uv.x * 2.6, floor(board) * 3.1)) - 0.5) * 0.32;
	} else if (pattern > 3.5 && pattern < 4.5) {
		// BARK — vertical striations, no cross-courses.
		float grain = fbm(vec2(uv.x * 6.2, uv.y * 0.6));
		mark = (grain - 0.5) * 0.75;
	} else if (pattern > 4.5 && pattern < 5.5) {
		// Turf's persistent colour field is shared with the fringe and plants.
		// Close thatch has soft interlocking contours instead of a square clod grid.
		vec2 wp = warp(uv * 0.70, 0.65);
		float clod = fbm(wp);
		float speck = vnoise(uv * 13.0);
		// Blossom-drift caps share this pattern but retain their pink identity.
		if (!turf) mineral_drift = (fbm(uv * 0.12 + 31.2) - 0.50) * 0.68;
		mark = (clod - 0.5) * 0.62 + (speck - 0.5) * 0.20;
		lift = smoothstep(0.62, 0.84, clod) * 0.05;

	} else if (pattern > 5.5 && pattern < 6.5) {
		// TRODDEN GROUND — paths worn through turf. Patchy wear, not pavers.
		vec2 wp = warp(uv, 0.8);
		float wear = fbm(wp * 1.0) * 0.6 + fbm(wp * 3.3) * 0.4;
		mark = (wear - 0.5) * 0.72;
		lift = smoothstep(0.76, 0.98, wear) * 0.10;
	} else if (pattern > 8.5 && pattern < 9.5) {
		// Broad deposits survive an overview; broken wind ripples and mineral
		// grains resolve only near the camera. Continuous fields group both, so
		// beaches do not turn into stripes or independently speckled voxels.
		surface_relief = 0.16;
		float drift = fbm(warp(uv * vec2(0.065, 0.12), 0.6));
		float deposit = vnoise(uv * 0.23 + vec2(51.7, 9.3));
		float grain = mineral_sample(uv * 0.24 + vec2(0.41, 0.13));
		vec2 wind = normalize(vec2(0.84, 0.54));
		float phase = dot(uv, wind) * 19.0 + vnoise(uv * 0.73) * 12.0;
		float ripple_resolved = 1.0 - smoothstep(0.06, 0.18, footprint);
		float ripple_patch = smoothstep(0.52, 0.72, deposit) * ripple_resolved;
		float ripple = sin(phase) * ripple_patch;
		mineral_drift = (drift - 0.50) * 0.60;
		mark = (grain - 0.660) * 1.40 + (vnoise(uv * 12.0) - 0.5) * 0.13
			+ ripple * 0.035;
		wind_relief = wind * cos(phase) * ripple_patch * 0.030;
		sand_grain = smoothstep(0.67, 0.79, grain);
	} else if (pattern > 7.5 && pattern < 8.5) {
		surface_relief = 0.24;
		// SNOW — close crust plus one very broad drift field. The fine part follows
		// the ordinary distance fade; the broad part is intentionally sampled in
		// global XZ and retained. Its 30-90 metre wavelengths remain smooth when a
		// shelf is only a few pixels wide, unlike persistent high-frequency speckle,
		// which shimmers and turns the far field into a printed mesh.
		vec2 wp = warp(uv * 0.72, 0.38);
		float crust = fbm(wp * 2.4);
		float wind_tooth = fbm(vec2(wp.x * 1.1 + wp.y * 0.28, wp.y * 5.1));
		float frozen = mineral_sample(warp(uv * 0.045, 0.16) + vec2(0.29,0.67));
		// Glazed islands belong to the material at overview scale too. Sampling
		// a larger, mip-filtered field keeps their contours after fine crust fades.
		float glaze = mineral_island_sample(warp(uv * 0.010, 0.12) + vec2(0.17,0.38));
		float glaze_aa = fwidth(glaze) * 0.5;
		ice_crust = smoothstep(0.640 - glaze_aa, 0.688 + glaze_aa, glaze);
		mark = (crust - 0.5) * 0.30 + (wind_tooth - 0.5) * 0.22 + (frozen - 0.63) * 1.10;
		lift = smoothstep(0.79, 0.98, crust) * 0.055;

		float broad = vnoise(v_world.xz * 0.031 + vec2(19.7, -8.3));
		float basin = vnoise(v_world.xz * 0.012 + vec2(-31.1, 42.6));
		// Snow sits near the high-key shoulder, so symmetric +/-3.5% modulation
		// mostly clipped its bright half and read as one white card. A slightly
		// darker centre and wider range preserves the pastel cap while making the
		// 30-90m drift legible beside categorical scree and stone regions.
		snow_drift = (broad * 0.64 + basin * 0.36 - 0.53) * 0.19;
		mineral_drift = (vnoise(floor(uv * 1.6) * 0.35) - 0.5) * 0.075;
	}

	// LEAF — one tone per BLOCK.
	//
	// Handled apart from the mark/recess path above, which desaturates toward
	// grey: a crown wants its cubes to differ in *value* while staying the same
	// blossom, not to go progressively colourless. Sampled in three dimensions
	// so a cube's three visible faces share one value and its neighbour gets
	// another; sampled in two, a whole vertical column would match and the crown
	// would band instead of reading as stacked cubes.
	//
	// The ink draws only the crown's outer contour — never the seams between
	// neighbouring leaf blocks — so without this the whole mass collapses into
	// one flat silhouette.
	if (pattern > 6.5 && pattern < 7.5) {
		surface_relief = 0.08;
		canopy_wrap = 0.12;
		vec3 cell = floor(v_world - wn * 0.5 + 0.001);
		float tone = hash21(cell.xz * 1.0 + cell.y * 17.0);
		base *= mix(0.88, 1.09, tone);
		vec3 petal = canopy_petals(uv, footprint);
		float cluster = fbm(warp(uv * 0.65, 0.45) + 23.1);
		base *= 1.0 + (cluster - 0.5) * 0.16;
		mark = petal.x * 0.085;
		petal_relief = petal.yz * 0.0018;
	}

	if (pattern > 9.5 && pattern < 10.5) {
		// The author's mushroom stems use square shade patches, without seams
		// or the horizontal plank grain of the former plaster material.
		vec3 cell = floor(v_world - wn * 0.5 + 0.001);
		float tone = hash21(cell.xz + cell.y * 17.0);
		base *= mix(0.84, 1.08, tone);
		surface_relief = 0.0;
	}

	float dist = length(v_world - CAMERA_POSITION_WORLD);
	float fade = 1.0 - smoothstep(pattern_fade_start, pattern_fade_end, dist);
	// Derivatives suppress detail once a world-space mark is subpixel.
	float resolved = 1.0 - smoothstep(0.12, 0.48, footprint);
	mark *= pattern_strength * fade * resolved;
	lift *= fade;
	base *= 1.0 + snow_drift + mineral_drift;

	// Darken toward a desaturated, slightly cooler version of the surface, never
	// toward black or a fixed grey. The mark has to read as the same material in
	// shadow, not as ink laid on top of it.
	// Both sides of the field matter. Clamping its negative half and then
	// blending only 20% toward a recess discarded nearly all the fine contrast.
	base *= 1.0 + mark * 0.95;
	base *= 1.0 + lift;
	base *= 1.0 - paving.z;
	if (pattern > 1.5 && pattern < 2.5) {
		float lip_wear = exposed_lip_wear(UV, v_exposed_edges, v_world);
		float lip_resolved = 1.0 - smoothstep(0.12, 0.55, footprint);
		base *= 1.0 + lip_wear * 0.19 * lip_resolved;
	}
	if (v_material > 18.5 && v_material < 19.5) surface_relief *= 0.30;
	if (turf) base = turf_colour(base, sward.rgb);
	if (pattern > 7.5 && pattern < 8.5) {
		base *= mix(vec3(1.0), vec3(0.55, 0.80, 1.03), ice_crust * 0.85);
		base *= upright ? vec3(0.94, 1.0, 1.025) : vec3(0.96, 1.00, 1.025);
	}
	if (v_material > 8.5 && v_material < 9.5) {
		// Only the authored MOSS_STONE mask can carry this layer. Expose mineral
		// beneath its growth instead of colouring the entire face flat green;
		// this never chooses another moss location or changes the plan's cells.
		float growth = fbm(warp(uv * vec2(1.25, 1.65), 0.45));
		float growth_aa = fwidth(growth) * 0.6;
		float exposed = 1.0 - smoothstep(0.37 - growth_aa, 0.57 + growth_aa, growth);
		vec3 substrate = upright ? moss_substrate_side_ratio : moss_substrate_top_ratio;
		base *= mix(vec3(1.0), substrate, exposed);
	}

	// The grass drip: turf spilling raggedly over the lip and hanging a little
	// way down the face below. The ragged edge is a per-column hash rather than
	// smooth noise — it has to break at block-ish intervals to look like turf
	// tearing, not like an airbrushed gradient.
	if (upright && drip > 0.01) {
		float column = an.x > 0.5 ? v_world.z : v_world.x;
		float jag = hash21(vec2(floor(column * 2.0), floor(v_world.y)));
		float jag2 = hash21(vec2(floor(column * 5.0) + 31.0, floor(v_world.y)));
		// Connected patches decide where the turf hangs deeply; small torn
		// contours sit inside that field rather than making an even green band.
		float coverage = smoothstep(0.26, 0.74,
			sward.a + (jag - 0.5) * 0.30 + (jag2 - 0.5) * 0.12);
		float depth = drip * mix(0.035, 0.56, coverage);
		float local = fract(v_world.y);
		// Differentiate before wrapping: the integer-height seam is not a
		// large pixel footprint and must not blur the fringe across that face.
		float w = fwidth(v_world.y) * 1.2 + 0.002;
		float lip = smoothstep(1.0 - depth - w, 1.0 - depth + w, local);
		vec3 fringe_colour = turf_colour(v_surface.yzw * face_shade, sward.rgb);
		base = mix(base, fringe_colour, lip);
	}

	// Narrow physical chamfers are supplied by ChunkMesher.
	vec3 rounded = wn;
	// Analytic world-plane relief avoids screen derivatives crossing unrelated
	// triangles at voxel edges (which produced dotted diagonal scratches).
	vec3 gu = an.y > 0.5 ? vec3(1,0,0) : an.x > 0.5 ? vec3(0,0,1) : vec3(1,0,0);
	vec3 gv = an.y > 0.5 ? vec3(0,0,1) : vec3(0,1,0);
	vec2 tooth_uv = uv * 2.4;
	float dh_u = vnoise(tooth_uv + vec2(0.035,0)) - vnoise(tooth_uv - vec2(0.035,0));
	float dh_v = vnoise(tooth_uv + vec2(0,0.035)) - vnoise(tooth_uv - vec2(0,0.035));
	vec3 textured_normal = normalize(rounded - gu * (paving.x + (wind_relief.x + petal_relief.x) * fade)
		- gv * (paving.y + (wind_relief.y + petal_relief.y) * fade)
		- (gu * (dh_u + mineral_relief.x * 4.0)
		+ gv * (dh_v + mineral_relief.y * 4.0)) * relief_strength * surface_relief * fade * resolved);
	NORMAL = normalize((VIEW_MATRIX * vec4(textured_normal, 0.0)).xyz);

	float facing = dot(rounded, normalize(pf_sun_dir));
	vec3 tint = mix(cool_tint, warm_tint, smoothstep(-0.6, 0.9, facing));
	base *= mix(vec3(1.0), tint, face_tint);

	ALBEDO = base;
	ROUGHNESS = pattern > 7.5 && pattern < 8.5 ? 0.78 : 0.90;
	if (pattern > 6.5 && pattern < 7.5) ROUGHNESS = 0.94;
	if (pattern > 8.5 && pattern < 9.5) ROUGHNESS = mix(0.93, 0.80, sand_grain * fade * resolved);
	SPECULAR = pattern > 7.5 && pattern < 8.5 ? 0.24 : 0.16;
	METALLIC = 0.0;
	// The mesher parks per-block emissive in the vertex alpha; lanterns, lit
	// windows and cascades are the only things in the world allowed to glow.
	//
	// After dark they are the only warm light on the continent, and they have to
	// grow to carry that — at daylight strength a lantern is invisible against a
	// night ambient, and the one signal that says somebody lives here would go
	// out exactly when it matters most.
	// Boosted after dark, but nowhere near enough to clip: at 7x the windows
	// came back as pure white rectangles, which reads as a hole in the wall
	// rather than as a warm room behind it. The COLOUR is the point.
	EMISSION = base * COLOR.a * (2.2 + pf_night * 1.9);
}
