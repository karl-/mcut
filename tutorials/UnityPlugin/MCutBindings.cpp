#include <cstdio>
#include <cstring>
#include "MCutBindings.h"
#include "mcut/mcut.h"

uint32_t CreateContext(MeshCutContext** ctx) {
    (*ctx) = new MeshCutContext();
    (*ctx)->context = MC_NULL_HANDLE;
    (*ctx)->source = nullptr;
    (*ctx)->cut = nullptr;
    return mcCreateContext(&(*ctx)->context, MC_NULL_HANDLE);
}

MeshCutContext::~MeshCutContext() {
    mcReleaseContext(context);
    delete source;
    delete cut;
}

void DestroyContext(MeshCutContext* ctx) {
    delete ctx;
}

void SetSourceMesh(MeshCutContext* ctx, const float* positions, int positionsSize, const int* indices, int indicesSize, int faceSize)
{
    auto& mesh = *(ctx->source = new Mesh());
    mesh.indices = new uint32_t [indicesSize];
    for(int i = 0; i < indicesSize; ++i)
        mesh.indices[i] = (uint32_t) indices[i];
    mesh.positions = new float[positionsSize];
    memcpy(mesh.positions, positions, positionsSize * sizeof(float));
    mesh.layout = { (uint32_t) positionsSize, (uint32_t) indicesSize, (uint32_t) faceSize };
}

void SetCutMesh(MeshCutContext* ctx, float* positions, uint32_t positionsSize, int* indices, uint32_t indicesSize, int faceSize) {
    fprintf(stderr, "nope not done yet\n");
}

Mesh* GetSourceMesh(MeshCutContext* ctx)
{
    return ctx->source;
}

MeshLayout GetMeshLayout(Mesh* mesh)
{
    return mesh->layout;
}