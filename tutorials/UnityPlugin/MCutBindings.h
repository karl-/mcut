#pragma once

#include <cstdint>
#include "mcut/mcut.h"

struct MeshLayout
{
    uint32_t vertexCount;
    uint32_t indexCount;
    uint32_t faceSize;
};

struct Mesh
{
    float*      positions;
    uint32_t*   indices;
    MeshLayout  layout;

    ~Mesh()
    {
        delete[] positions;
        delete[] indices;
    }
};

struct MeshCutContext
{
    McContext   context;
    Mesh*       source;
    Mesh*       cut;
    ~MeshCutContext();
};

extern "C" uint32_t CreateContext(MeshCutContext** context);
extern "C" void DestroyContext(MeshCutContext* ptr);
extern "C" void SetSourceMesh(MeshCutContext* ctx, const float* positions, int positionsSize, const int* indices, int indicesSize, int faceSize);
extern "C" void SetCutMesh(MeshCutContext* ctx, float* positions, int positionsSize, int* indices, int indicesSize, int faceSize);
extern "C" Mesh* GetSourceMesh(MeshCutContext* ctx);

extern "C" MeshLayout GetMeshLayout(Mesh* mesh);
extern "C" void GetPositions(Mesh* mesh, float* array);