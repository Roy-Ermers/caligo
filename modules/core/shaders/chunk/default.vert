#version 460
struct ChunkInfo {
    vec3 position;
};

layout(binding = 0) buffer FaceData {
    int faceData[];
};

layout(binding = 1) buffer Materials {
    int materials[];
};
layout(binding = 2) buffer ChunkModelMatrices {
    vec4 chunkInfo[];
};

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
flat out float interpolation;
out vec3 texCoords;
out vec3 fragPos;
flat out Material material;
flat out BlockFace face;

uniform float uTime;
uniform int faceIndex;
uniform sampler2DArray atlas;

struct Camera
{
    mat4 view;
    mat4 projection;
    vec3 position;
};

uniform Camera camera;

const vec3[] Normals = vec3[6](
        vec3(0.0, -1.0, 0.0), // DOWN
        vec3(0.0, 1.0, 0.0), // UP
        vec3(0.0, 0.0, -1.0), // NORTH
        vec3(0.0, 0.0, 1.0), // SOUTH
        vec3(-1.0, 0.0, 0.0), // WEST
        vec3(1.0, 0.0, 0.0) // EAST
    );

BlockFace Decode() {
    BlockFace face;
    int index = (gl_BaseInstance + gl_InstanceID) * 2;
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

// Lookup table for tangent and bitangent per face normal
void getFaceBasis(vec3 normal, out vec3 tangent, out vec3 bitangent) {
    if (normal.y <= -1.0) {
        tangent = vec3(1, 0, 0);
        bitangent = vec3(0, 0, 1);
    }
    else if (normal.y >= 1.0) {
        tangent = vec3(-1, 0, 0);
        bitangent = vec3(0, 0, 1);
    }
    else if (normal.z <= -1.0) {
        tangent = vec3(-1, 0, 0);
        bitangent = vec3(0, 1, 0);
    }
    else if (normal.z >= 1.0) {
        tangent = vec3(1, 0, 0);
        bitangent = vec3(0, 1, 0);
    }
    else if (normal.x <= -1.0) {
        tangent = vec3(0, 0, 1);
        bitangent = vec3(0, 1, 0);
    }
    else {
        tangent = vec3(0, 0, -1);
        bitangent = vec3(0, 1, 0);
    }
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
            vec2(1.0, 1.0) // top-right
        );
    vec2 local = quadCorners[gl_VertexID];

    vec3 tangent, bitangent;
    getFaceBasis(face.normal, tangent, bitangent);

    vec3 vertexOffset = tangent * local.x * material.width +
            bitangent * local.y * material.height;

    vec3 chunkOffset = chunkInfo[gl_DrawID].xyz;
    float chunkStart = chunkInfo[gl_DrawID].w;
    interpolation = 1 - pow(1 - min(1, (uTime - chunkStart)), 3);
    chunkOffset.y += interpolation * 4.0;

    vec3 vertexPosition = face.position + vertexOffset + chunkOffset;

    texCoords = vec3(
            mix(material.uv0, material.uv1, local * vec2(material.width, material.height)),
            material.textureId
        );

    fragPos = vertexPosition.xyz;
    gl_Position = camera.projection * camera.view * vec4(vertexPosition, 1.0);
}
