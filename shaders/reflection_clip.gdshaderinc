// The mirror camera uses all layers except the reserved water layer (20).
// Main cameras retain that layer. Clip submerged geometry only in this pass.
global uniform float pf_reflection_plane;
bool reflection_below_plane(vec3 world, uint camera_layers) {
	return camera_layers == 0x7ffffu && world.y < pf_reflection_plane;
}
