shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;
#include "rain_field.gdshaderinc"
global uniform float pf_night;
varying float opacity;
void vertex() {
	vec3 base = MODEL_MATRIX[3].xyz;
	float amount = rain_at(base.xz);
	float cycle = fract(pf_rain_time * (.69 + INSTANCE_CUSTOM.a*.24) + INSTANCE_CUSTOM.r*43.0);
	float height = (1.0-cycle)*44.0;
	vec3 side = normalize(INV_VIEW_MATRIX[0].xyz);
	vec3 fall = vec3(.10,-1.0,.035);
	VERTEX = vec3(-.1,1.0,-.035)*height + side*VERTEX.x*.038 + fall*VERTEX.y*1.45;
	opacity = amount * INSTANCE_CUSTOM.g * smoothstep(0.0,.025,cycle) * (1.0-smoothstep(.96,1.0,cycle));
}
void fragment() {
	float line = pow(max(0.0,1.0-abs(UV.x*2.0-1.0)),1.8) * sin(UV.y*3.14159265);
	ALBEDO = mix(vec3(.60,.71,.86),vec3(.21,.28,.42),pf_night);
	ALPHA = line*opacity*.46;
}
