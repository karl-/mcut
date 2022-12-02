#pragma once

#include <cstdint>
#include "mcut/mcut.h"

struct MeshLayout
{
    uint32_t vertexCount;
    uint32_t indexCount;
    uint32_t faceSize;
};

struct vec3 { float x, y, z; };

struct Mesh
{
    vec3*       positions;
    uint32_t*   indices;
    MeshLayout  layout;
    ~Mesh();
};

struct MeshCutContext
{
    McContext   context;
    Mesh*       source;
    Mesh*       cut;
    ~MeshCutContext();
};

// mesh bindings
extern "C" Mesh* CreateMesh();
extern "C" void DestroyMesh(Mesh* mesh);
extern "C" void SetFaceSize(Mesh* mesh, int size);
extern "C" int GetFaceSize(const Mesh* mesh);
extern "C" void SetPositions(Mesh* mesh, const vec3* positions, int size);
extern "C" void GetPositions(const Mesh* mesh, vec3* array);
extern "C" void SetIndices(Mesh* mesh, const int* indices, int size);
extern "C" void GetIndices(const Mesh* mesh, int* array);
extern "C" int GetVertexCount(const Mesh* mesh);
extern "C" int GetIndexCount(const Mesh* mesh);

// McContext bindings
extern "C" uint32_t CreateContext(MeshCutContext** context);
extern "C" void DestroyContext(MeshCutContext* ptr);
extern "C" void SetSourceMesh(MeshCutContext* ctx, Mesh* mesh);
extern "C" void SetCutMesh(MeshCutContext* ctx, Mesh* mesh);
extern "C" Mesh* GetSourceMesh(const MeshCutContext* ctx);
extern "C" Mesh* GetCutMesh(const MeshCutContext* ctx);
extern "C" McResult Dispatch(MeshCutContext* ctx);
