shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;
#include "rain_field.gdshaderinc"
global uniform float pf_night;
varying float age;
varying float opacity;
void vertex() {
	vec3 base=MODEL_MATRIX[3].xyz;
	float life=fract(pf_rain_time*(.69+INSTANCE_CUSTOM.a*.24)+INSTANCE_CUSTOM.r*43.0);
	age=clamp(life/.25,0.0,1.0);
	vec3 side=normalize(INV_VIEW_MATRIX[0].xyz);
	float grass=1.0-step(.15,abs(INSTANCE_CUSTOM.b-.5));
	VERTEX=side*VERTEX.x*.65+vec3(0.0,(VERTEX.y+.5)*.60,0.0);
	opacity=rain_at(base.xz)*INSTANCE_CUSTOM.g*(1.0-grass*.55)*(1.0-smoothstep(.12,.25,life));
}
void fragment() {
	vec2 p=vec2(UV.x*2.0-1.0,1.0-UV.y);
	float h=sin(age*3.14159)*.65;
	float left=length((p-vec2(-age*.62,h)) * vec2(1.0,.75));
	float right=length((p-vec2(age*.62,h*.7))*vec2(1.0,.75));
	float centre=length((p-vec2(age*.12,h*.92))*vec2(1.0,.75));
	float dot_alpha=1.0-smoothstep(.018,.060,min(left,min(right,centre)));
	ALBEDO=mix(vec3(.65,.75,.86),vec3(.15,.23,.34),pf_night);
	ALPHA=dot_alpha*opacity*.6;
}
