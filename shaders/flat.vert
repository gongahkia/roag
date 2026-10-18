#version 330 core
layout(location=0) in vec3 position;
layout(location=1) in vec4 tint;
uniform mat4 vp;
out vec4 c;
void main(){gl_Position=vp*vec4(position,1);c=tint;}
