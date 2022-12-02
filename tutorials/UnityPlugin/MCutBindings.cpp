#include <cstdio>
#include <cstring>
#include <limits>
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

#define INVALID_SIZE std::numeric_limits<uint32_t>::max()

// mcut lib expects that topology is variable per-face, which is not the case in unity meshes
// todo looks like this can be skipped if topology is triangles
uint32_t CreateFaceSizesArray(size_t indexCount, size_t faceSize, uint32_t** array)
{
	if(indexCount % faceSize != 0)
		return INVALID_SIZE;
	size_t cnt = indexCount / faceSize;
	auto* buffer = (*array) = new uint32_t[cnt];
	for(size_t i = 0; i < cnt; ++i)
		buffer[i] = faceSize;
	return cnt;
}

McResult Dispatch(MeshCutContext* ctx, McFlags flags)
{
	if(!ctx->source || !ctx->cut)
	{
		fprintf(stderr, "missing mesh, src %p cut %p", ctx->source, ctx->cut);
		return MC_INVALID_VALUE;
	}

	const Mesh& src = *ctx->source;
	const Mesh& cut = *ctx->cut;

	uint32_t *srcFaceSizes, *cutFaceSizes;
	uint32_t srcFaceSizesCount = CreateFaceSizesArray(src.layout.indexCount, src.layout.faceSize, &srcFaceSizes);
	uint32_t cutFaceSizesCount = CreateFaceSizesArray(cut.layout.indexCount, cut.layout.faceSize, &cutFaceSizes);

	if(srcFaceSizesCount == INVALID_SIZE || cutFaceSizesCount == INVALID_SIZE)
	{
		fprintf(stderr, "srcFaceSizesCount %u curFaceSizesCount %u\n", srcFaceSizesCount, cutFaceSizesCount);
		return MC_INVALID_VALUE;
	}

	McResult err = mcDispatch(
        ctx->context,
        flags,
        src.positions,
        src.indices,
        srcFaceSizes,
        src.layout.vertexCount,
        srcFaceSizesCount,
        cut.positions,
        cut.indices,
        cutFaceSizes,
        cut.layout.vertexCount,
        cutFaceSizesCount);

	delete[] srcFaceSizes;
	delete[] cutFaceSizes;

	return err;
}