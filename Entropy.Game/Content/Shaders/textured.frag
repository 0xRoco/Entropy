#version 330 core
in vec4 vColor;
in vec2 vUv;

out vec4 FragColor;

uniform sampler2D uAtlas;

void main()
{
    vec4 tex = texture(uAtlas, vUv);
    FragColor = tex * vColor;
}