#include <cstdio>
#include <cstring>
#include "MCutBindings.h"
#include "mcut/mcut.h"

Mesh::~Mesh()
{
	delete[] positions;
	delete[] indices;
}

MeshCutContext::~MeshCutContext()
{
	mcReleaseContext(context);
}

Mesh* CreateMesh()
{
	return new Mesh();
}

void DestroyMesh(Mesh* mesh)
{
	delete mesh;
}

void SetFaceSize(Mesh* mesh, int size)
{
	mesh->layout.faceSize = size;
}

int GetFaceSize(const Mesh* mesh)
{
	return (int)mesh->layout.faceSize;
}

void SetPositions(Mesh* mesh, const vec3* positions, int size)
{
	delete[] mesh->positions;
	mesh->layout.vertexCount = size;
	mesh->positions = new vec3[size];
	memcpy(mesh->positions, positions, size * sizeof(float) * 3);
}

void GetPositions(const Mesh* mesh, vec3* array)
{
	memcpy(array, mesh->positions, sizeof(float) * 3 * mesh->layout.vertexCount);
}

void SetIndices(Mesh* mesh, const int* indices, int size)
{
	delete[] mesh->indices;
	mesh->indices = new uint32_t[size];
	mesh->layout.indexCount = size;
	for (int i = 0; i < size; ++i)
		mesh->indices[i] = indices[i];
}

void GetIndices(const Mesh* mesh, int* array)
{
	for (int i = 0; i < (int) mesh->layout.indexCount; ++i)
		array[i] = (int) mesh->indices[i];
}

int GetVertexCount(const Mesh* mesh)
{
	return (int) mesh->layout.vertexCount;
}

int GetIndexCount(const Mesh* mesh)
{
	return (int) mesh->layout.indexCount;
}

uint32_t CreateContext(MeshCutContext** ctx)
{
	(*ctx) = new MeshCutContext();
	(*ctx)->context = MC_NULL_HANDLE;
	(*ctx)->source = nullptr;
	(*ctx)->cut = nullptr;
	return mcCreateContext(&(*ctx)->context, MC_NULL_HANDLE);
}

void DestroyContext(MeshCutContext* ctx)
{
	delete ctx;
}

void SetSourceMesh(MeshCutContext* ctx, Mesh* mesh)
{
	ctx->source = mesh;
}

void SetCutMesh(MeshCutContext* ctx, Mesh* mesh)
{
	ctx->cut = mesh;
}

Mesh* GetSourceMesh(const MeshCutContext* ctx)
{
	return ctx->source;
}

Mesh* GetCutMesh(const MeshCutContext* ctx)
{
	return ctx->cut;
}

