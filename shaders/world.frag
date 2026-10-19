#version 330 core
uniform sampler2DArray atlas;
uniform float fogDistance;
uniform float worldTime;
uniform int immersion;
in vec2 uv;
in float shade;
in vec2 voxelLight;
in float distanceToCamera;
flat in int layer;
flat in int material;
out vec4 color;
void main() {
    // Solid ice has an exterior surface; rendering its back faces through the
    // front layer caused chunk-shaped compositing seams above frozen water.
    if(material==31 && !gl_FrontFacing) discard;
    vec2 coords=uv;
    if(material==16) coords+=vec2(worldTime*0.045,worldTime*0.065);
    if(material==17) coords+=vec2(worldTime*0.018,-worldTime*0.026);
    vec3 tex=texture(atlas,vec3(coords,float(layer))).rgb;
    float range=immersion==1 ? 22.0 : (immersion==2 ? 5.0 : fogDistance);
    vec3 sky=immersion==1 ? vec3(0.07,0.22,0.39) : (immersion==2 ? vec3(0.60,0.13,0.015) : vec3(0.56,0.72,0.81));
    float fog=smoothstep(range*(immersion==0 ? 0.55 : 0.3),range,distanceToCamera);
    float alpha=material==16 ? 0.62 : (material==31 ? 0.78 : 1.0);
    // Curved response gives readable outdoor shade and truly dark enclosed caves.
    float sun=pow(voxelLight.x,1.65);
    float warm=pow(voxelLight.y,1.65);
    vec3 illumination=max(vec3(0.018)+sun*vec3(0.96,0.98,1.0)*shade,
                          warm*vec3(1.0,0.76,0.46)*(0.8+0.2*shade));
    if(material==17 || material==56) illumination=vec3(1.0);
    // Bright distance fog must not make an enclosed, unlit cave glow.
    vec3 fogColor=sky*(0.02+0.98*sun);
    color=vec4(mix(tex*illumination,fogColor,fog),alpha);
}
