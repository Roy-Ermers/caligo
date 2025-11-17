#version 460
struct BlockFace {
    vec3 position;
    vec3 normal;
    vec4 light;
    int materialId;
};

struct Material {
    float width;
    float height;
    vec2 uv0;
    vec2 uv1;
    vec3 tint;
    int textureId;
};

out vec4 FragColor;

flat in float time;
flat in float interpolation;
in vec3 texCoords;
in vec3 fragPos;
flat in Material material;
flat in BlockFace face;

uniform vec3 sun;
uniform vec3 ambient;
uniform sampler2DArray atlas;

struct Camera
{
    mat4 view;
    mat4 projection;
    vec3 position;
};

uniform Camera camera;
const float fogDistance = 10*16.0;
const float fogDensity = 0.005;

void main()
{
    vec4 texColor = texture(atlas, texCoords);
    if (texColor.a < 0.1)
    {
        discard;
    }

    // use normal
    vec3 normal = normalize(face.normal);

    vec3 lightDir = normalize(sun);
    // diffuse shading
    float diffuse = max(dot(normal, lightDir), 0.0);

    vec3 ambient = ambient + diffuse;

    vec3 result = ambient * (texColor.rgb * material.tint);

    // fog
    float distance = length(fragPos - camera.position);
    float fogFactor = 1.0 - clamp(exp(-fogDensity * distance), 0.0, 1.0);
    vec3 fogColor = vec3(0.9, 0.9, 1);
    result = mix(result, fogColor, min(1, fogFactor + (1 - interpolation)));

    FragColor = vec4(result, 1.0);
}
