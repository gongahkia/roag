#version 330 core
uniform sampler2DArray atlas;
uniform float fogDistance;
in vec2 uv;
in float shade;
in float distanceToCamera;
flat in int layer;
out vec4 color;
void main() {
    vec3 tex=texture(atlas,vec3(uv,float(layer))).rgb;
    float fog=smoothstep(fogDistance*0.55,fogDistance,distanceToCamera);
    color=vec4(mix(tex*shade,vec3(0.56,0.72,0.81),fog),1.0);
}
