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
in vec3 texCoords;
in vec3 Normal;
flat in Material material;
flat in BlockFace face;

uniform vec3 sun;
uniform vec3 ambient;
uniform sampler2DArray atlas;

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

    vec3 result = texColor.rgb * material.tint;

    FragColor = vec4(result, 1.0);
}
