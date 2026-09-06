// Unlit scalar mineral detail. Palette and the live lights still own colour.
uniform sampler2D mineral_detail : filter_linear_mipmap_anisotropic, repeat_disable;

float mineral_sample(vec2 p) {
	// Mirror both ends so an imperfect authored tile cannot create a value seam.
	// Explicit gradients prevent the mirror turn selecting a false coarse mip.
	vec2 mirrored = 1.0 - abs(fract(p * 0.5) * 2.0 - 1.0);
	return textureGrad(mineral_detail, mirrored, dFdx(p), dFdy(p)).r;
}

float mineral_island_sample(vec2 p) {
	// Large ice islands need the texture's shapes, without magnifying its grit.
	// A six-texel minimum footprint removes the fine band before contrast
	// shaping. Keep the actual pixel footprint when it needs a coarser mip.
	vec2 texels = vec2(textureSize(mineral_detail, 0));
	float footprint = max(length(dFdx(p) * texels), length(dFdy(p) * texels));
	float lod = max(2.6, log2(max(footprint, 1.0)));
	vec2 mirrored = 1.0 - abs(fract(p * 0.5) * 2.0 - 1.0);
	return textureLod(mineral_detail, mirrored, lod).r;
}
