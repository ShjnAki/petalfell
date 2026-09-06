// Outlines must disappear with distant surfaces, without becoming luminous
// fog-coloured strokes. The environment owner publishes the current view span.
global uniform vec4 pf_depth_haze; // begin, end, curve, density (zero when disabled)

float outline_air(float distance_to_camera) {
	float depth = smoothstep(pf_depth_haze.x, pf_depth_haze.y, distance_to_camera);
	float amount = pow(depth, pf_depth_haze.z) * pf_depth_haze.w;
	return pow(clamp(1.0 - amount, 0.0, 1.0), 3.0);
}
