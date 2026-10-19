#version 330 core
layout(location=0) in uvec4 voxel;
uniform mat4 vp;
uniform vec3 offset;
out vec2 uv;
out float shade;
out vec2 voxelLight;
out float distanceToCamera;
flat out int layer;
flat out int material;
void main() {
    int face=int(voxel.w & 7u);
    int block=int((voxel.w >> 3u) & 63u);
    float ao=float((voxel.w >> 9u) & 3u);
    vec3 p=vec3(voxel.x & 63u, voxel.y, voxel.z);
    uint light=(voxel.x >> 6u) & 255u;
    voxelLight=vec2(float(light & 15u),float(light >> 4u))/15.0;
    if(block==16 || block==17) {
        float height=float((voxel.w >> 11u) & 15u)/8.0;
        p.y+=((voxel.w & (1u << 15u))!=0u ? height-1.0 : height);
    }
    if(face<2) uv=p.zy;
    else if(face<4) uv=p.xz;
    else uv=p.xy;
    layer=block;
    material=block;
    if(block==1) layer=(face==2 ? 18 : (face==3 ? 2 : 1));
    if(block==5 && face>=2 && face<=3) layer=19;
    // Appended material IDs match blocks. Extra layers hold grass and log caps.
    if(block==37 || block==39 || block==41 || block==43 || block==45) {
        if(face==2 || face==3) layer=65+(block-37)/2;
    }
    if(block>=53 && block<=55) layer=(face==2 ? 70+block-53 : (face==3 ? 2 : block));
    if(block==50) layer=(face==2 ? 73 : (face==3 ? 2 : 50));
    if(block==52) layer=(face==2 ? 74 : (face==3 ? 2 : 52));
    float lighting[6]=float[6](0.82,0.66,1.0,0.46,0.88,0.72);
    shade=lighting[face]*(0.55+0.15*ao);
    if(block==17 || block==56) shade=1.0;
    vec3 relative=p+offset;
    distanceToCamera=length(relative);
    gl_Position=vp*vec4(relative,1.0);
}
