#version 330 core
layout(location=0) in uvec4 voxel;
uniform mat4 vp;
uniform vec3 offset;
out vec2 uv;
out float shade;
out float distanceToCamera;
flat out int layer;
void main() {
    int face=int(voxel.w & 7u);
    int block=int((voxel.w >> 3u) & 15u);
    float ao=float((voxel.w >> 7u) & 3u);
    vec3 p=vec3(voxel.xyz);
    if(face<2) uv=p.zy;
    else if(face<4) uv=p.xz;
    else uv=p.xy;
    layer=block;
    if(block==1) layer=(face==2 ? 8 : (face==3 ? 2 : 1));
    if(block==5 && face>=2 && face<=3) layer=9;
    float lighting[6]=float[6](0.82,0.66,1.0,0.46,0.88,0.72);
    shade=lighting[face]*(0.55+0.15*ao);
    vec3 relative=p+offset;
    distanceToCamera=length(relative);
    gl_Position=vp*vec4(relative,1.0);
}
