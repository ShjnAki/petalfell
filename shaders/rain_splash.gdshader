shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;
#include "rain_field.gdshaderinc"
global uniform float pf_night;
varying float life;
varying float opacity;
varying float surface_kind;
void vertex() {
	vec3 base = MODEL_MATRIX[3].xyz;
	float amount = rain_at(base.xz);
	life = fract(pf_rain_time*(.69+INSTANCE_CUSTOM.a*.24)+INSTANCE_CUSTOM.r*43.0);
	surface_kind = INSTANCE_CUSTOM.b;
	float size = mix(.44,.86,step(.75,surface_kind));
	VERTEX = vec3(VERTEX.x*size,0.0,VERTEX.y*size);
	opacity = amount * INSTANCE_CUSTOM.g;
}
void fragment() {
	vec2 d=UV*2.0-1.0;
	float radius = length(d);
	float age=clamp(life/.34,0.0,1.0);
	float ring=1.0-smoothstep(.055,.15,abs(radius-age*.83));
	float crown=(1.0-smoothstep(.0,.12,abs(radius-.20))) * (1.0-age);
	float fade=(1.0-smoothstep(.08,.34,life))*smoothstep(0.0,.015,life);
	ALBEDO=mix(vec3(.60,.72,.85),vec3(.12,.20,.30),pf_night);
	ALPHA=(ring*.65+crown*.35)*fade*opacity*mix(.50,.90,step(.75,surface_kind));
}
