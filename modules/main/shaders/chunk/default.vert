#version 460

layout(binding = 0) buffer FaceData {
    int faceData[];
};

layout(binding = 1) buffer Materials {
    int materials[];
};

layout(location = 0) in ivec3 vertexOffset;

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

flat out float time;
out vec3 texCoords;
out vec3 FragPos;
flat out Material material;
flat out BlockFace face;

uniform float uTime;
uniform int faceIndex;
uniform vec3 faceDirection;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform sampler2DArray atlas;

const vec3[] Normals = vec3[6](
    vec3(0.0, -1.0, 0.0), // DOWN
    vec3(0.0, 1.0, 0.0),  // UP
    vec3(0.0, 0.0, -1.0), // NORTH
    vec3(0.0, 0.0, 1.0),  // SOUTH
    vec3(-1.0, 0.0, 0.0), // WEST
    vec3(1.0, 0.0, 0.0)   // EAST
);

BlockFace Decode() {
    BlockFace face;
    int index = gl_InstanceID * 2 + faceIndex * 2;
    int position = faceData[index];
    face.position = vec3(0, 0, 0);
    face.position.x = ((position >> 0) & 0x1FF) / 16.0;
    face.position.y = ((position >> 9) & 0x1FF) / 16.0;
    face.position.z = ((position >> 18) & 0x1FF) / 16.0;

    int normalIndex = (position >> 27) & 0xF;
    face.normal = Normals[normalIndex];

    int visual = faceData[index + 1];
    face.light = vec4(
        ((visual >> 0) & 0x0F) / 15.0,
        ((visual >> 4) & 0x0F) / 15.0,
        ((visual >> 8) & 0x0F) / 15.0,
        ((visual >> 12) & 0x0F) / 15.0
    );
    face.materialId = visual >> 16;
    return face;
}

Material DecodeMaterial(BlockFace face) {
    int materialId = face.materialId * 2;
    int upperMaterial = materials[materialId];

    Material material;
    material.width = (((upperMaterial >> 0) & 0x0F) + 1.0) / 16.0;
    material.height = (((upperMaterial >> 4) & 0x0F) + 1.0) / 16.0;
    material.uv0 = vec2(
        ((upperMaterial >> 8) & 0x1F) / 16.0,
        ((upperMaterial >> 13) & 0x1F) / 16.0
    );
    material.uv1 = vec2(
        ((upperMaterial >> 18) & 0x1F) / 16.0,
        ((upperMaterial >> 23) & 0x1F) / 16.0
    );

    int lowerMaterial = materials[materialId + 1];
    material.textureId = lowerMaterial & 0xFFFF;
    material.tint = vec3(
        ((lowerMaterial >> 16) & 0x0F) / 15.0,
        ((lowerMaterial >> 20) & 0x0F) / 15.0,
        ((lowerMaterial >> 24) & 0x0F) / 15.0
    );

    return material;
}
vec3 getTangent(vec3 normal) {
    vec3 up = abs(normal.y) < 0.99 ? vec3(0, 1, 0) : vec3(0, 0, 1);
    return normalize(cross(up, normal));
}
vec3 getBitangent(vec3 normal, vec3 tangent) {
    return normalize(cross(normal, tangent));
}

void main()
{
    time = uTime;

    face = Decode();
    material = DecodeMaterial(face);

    const vec2 quadCorners[4] = vec2[4](
        vec2(0.0, 0.0), // bottom-left
        vec2(1.0, 0.0), // bottom-right
        vec2(0.0, 1.0), // top-left
        vec2(1.0, 1.0)  // top-right
    );
    vec2 local = quadCorners[gl_VertexID];

    vec3 tangent = getTangent(face.normal);
    vec3 bitangent = getBitangent(face.normal, tangent);

    vec3 offset = tangent * local.x * material.width +
                  bitangent * local.y * material.height;

    vec3 vertexPosition = face.position + offset;


    vec2 texCoord = mix(material.uv0, material.uv1, local * vec2(material.width, material.height));

    texCoords = vec3(texCoord, material.textureId);
    FragPos = vec3(model * vec4(vertexPosition.xyz, 1.0));
    gl_Position = projection * view * model * vec4(vertexPosition, 1.0);
}
