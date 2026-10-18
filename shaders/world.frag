#version 330 core
uniform sampler2DArray atlas;
uniform float fogDistance;
uniform float worldTime;
uniform int immersion;
in vec2 uv;
in float shade;
in float distanceToCamera;
flat in int layer;
flat in int material;
out vec4 color;
void main() {
    vec2 coords=uv;
    if(material==16) coords+=vec2(worldTime*0.045,worldTime*0.065);
    if(material==17) coords+=vec2(worldTime*0.018,-worldTime*0.026);
    vec3 tex=texture(atlas,vec3(coords,float(layer))).rgb;
    float range=immersion==1 ? 22.0 : (immersion==2 ? 5.0 : fogDistance);
    vec3 sky=immersion==1 ? vec3(0.07,0.22,0.39) : (immersion==2 ? vec3(0.60,0.13,0.015) : vec3(0.56,0.72,0.81));
    float fog=smoothstep(range*0.3,range,distanceToCamera);
    float alpha=material==16 ? 0.62 : 1.0;
    color=vec4(mix(tex*shade,sky,fog),alpha);
}
