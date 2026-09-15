// Mirrors RainField.Footprint. Plateau over 62% of radius; C1-continuous fringe.
global uniform vec4 pf_rain_0;
global uniform vec4 pf_rain_1;
global uniform vec4 pf_rain_2;
global uniform vec4 pf_wet_0;
global uniform vec4 pf_wet_1;
global uniform vec4 pf_wet_2;
global uniform float pf_rain_time;
float rain_cell(vec2 p, vec4 cell) {
	return cell.w * (1.0 - smoothstep(.62, 1.0, distance(p, cell.xy) / max(cell.z, 1.0)));
}
float rain_at(vec2 p) {
	return max(rain_cell(p,pf_rain_0),max(rain_cell(p,pf_rain_1),rain_cell(p,pf_rain_2)));
}
float rain_wetness(vec2 p) {
	return max(rain_cell(p,pf_wet_0),max(rain_cell(p,pf_wet_1),rain_cell(p,pf_wet_2)));
}
float rain_hash(vec2 p) {
	uvec2 q = uvec2(ivec2(p));
	uint n = q.x * 374761393u + q.y * 668265263u;
	n = (n ^ (n >> 13u)) * 1274126177u;
	return float((n ^ (n >> 16u)) & 16777215u) / 16777216.0;
}
// Analytic impact waves: randomized centers/time, expanding damped double rings.
// xy is the surface gradient, z the crest foam. No global screen-space overlay.
vec3 rain_ripples(vec2 p, float amount, float footprint) {
	if (amount < .001) return vec3(0.0);
	vec3 result = vec3(0.0);
	vec2 cell = floor(p / 3.8);
	for (int y=-1; y<=1; y++) for (int x=-1; x<=1; x++) {
		vec2 id = cell + vec2(float(x),float(y));
		float seed = rain_hash(id);
		float clock = pf_rain_time * .85 + seed * 71.0;
		float epoch = floor(clock);
		float age = fract(clock);
		vec2 jitter = vec2(rain_hash(id+epoch*13.0), rain_hash(id+epoch*29.0+41.0));
		vec2 d = p - (id + .12 + jitter*.76)*3.8;
		float radius = length(d);
		float wave = radius - age * mix(1.05,1.85,seed);
		float width = max(.055, footprint*.7);
		float crest = exp(-pow(wave/width,2.0)) + .45*exp(-pow((wave+.22)/width,2.0));
		float fade = smoothstep(0.0,.08,age)*(1.0-smoothstep(.4,1.0,age));
		float active = smoothstep(seed-.08,seed+.08,amount*.62);
		float grad = -2.0*wave/(width*width)*exp(-pow(wave/width,2.0));
		result += vec3(d/max(radius,.01)*grad*.018,crest*.16 * (.55 + .45*abs(dot(d/max(radius,.01),vec2(.6,.8))))) * fade * active;
	}
	return result * smoothstep(0.0,.05,amount);
}
