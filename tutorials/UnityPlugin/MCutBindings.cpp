#include <cstdio>
#include <cstring>
#include "MCutBindings.h"
#include "mcut/mcut.h"

Mesh::Mesh() : positions(nullptr), indices(nullptr), faceSizes(nullptr), layout()
{
    layout.vertexCount = 0;
    layout.indexCount = 0;
    layout.faceCount = 0;
}

Mesh::~Mesh()
{
    delete[] positions;
    delete[] indices;
    delete[] faceSizes;
}

MeshCutQuery::MeshCutQuery() : connectedComponentsSize(0), connectedComponents(nullptr)
{}

MeshCutQuery::~MeshCutQuery()
{
    delete[] connectedComponents;
}

MeshCutContext::MeshCutContext() : context(nullptr),
                                   source(nullptr),
                                   cut(nullptr)
{
}

MeshCutContext::~MeshCutContext()
{
    mcReleaseConnectedComponents(context, 0, nullptr);
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
void SetFaces(Mesh* mesh, const int* sizes, int size)
{
    delete[] mesh->faceSizes;
    mesh->layout.faceCount = size;
    mesh->faceSizes = new uint32_t [size];
    for(int i = 0; i < size; ++i)
        mesh->faceSizes[i] = (uint32_t) sizes[i];
}

void GetFaces(const Mesh* mesh, int* array)
{
    for (int i = 0; i < (int)mesh->layout.faceCount; ++i)
        array[i] = mesh->faceSizes == nullptr ? 3 : (int)mesh->faceSizes[i];
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

    if(mesh->layout.faceCount < 1)
    {
        delete[] mesh->faceSizes;
        mesh->layout.faceCount = size/3;
        mesh->faceSizes = new uint32_t [mesh->layout.faceCount];
        for(int i = 0, c = mesh->layout.faceCount; i < c; ++i)
            mesh->faceSizes[i] = 3;
    }
}

void GetIndices(const Mesh* mesh, int* array)
{
    for (int i = 0; i < (int)mesh->layout.indexCount; ++i)
        array[i] = (int)mesh->indices[i];
}

int GetVertexCount(const Mesh* mesh)
{
    return (int)mesh->layout.vertexCount;
}

int GetIndexCount(const Mesh* mesh)
{
    return (int)mesh->layout.indexCount;
}

int GetFaceCount(const Mesh* mesh)
{
    return (int) mesh->layout.faceCount;
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

void writeOFF(const char *fpath, Mesh* mesh);

McResult Dispatch(MeshCutContext* ctx, McFlags flags)
{
    if (!ctx->source || !ctx->cut)
    {
        fprintf(stderr, "[mcut] missing mesh, src %p cut %p", ctx->source, ctx->cut);
        return MC_INVALID_VALUE;
    }

    const Mesh& src = *ctx->source;
    const Mesh& cut = *ctx->cut;

//    writeOFF("input-src.off", ctx->source);
//    writeOFF("input-cut.off", ctx->cut);

    McResult err = mcDispatch(
        ctx->context,
        flags,
        src.positions,
        src.indices,
        src.faceSizes,
        src.layout.vertexCount,
        src.layout.faceCount,
        cut.positions,
        cut.indices,
        cut.faceSizes,
        cut.layout.vertexCount,
        cut.layout.faceCount);

    return err;
}

MeshCutQuery* CreateMeshQuery(MeshCutContext* ctx, McConnectedComponentType flags)
{
    auto query = new MeshCutQuery();
    query->context = ctx->context;

    auto err = mcGetConnectedComponents(ctx->context,
        flags,
        0,
        nullptr,
        &query->connectedComponentsSize);

    if (err != MC_NO_ERROR)
    {
        delete query;
        return nullptr;
    }

    query->connectedComponents = new McConnectedComponent [query->connectedComponentsSize];

    err = mcGetConnectedComponents(ctx->context,
        flags, // MC_CONNECTED_COMPONENT_TYPE_ALL,
        query->connectedComponentsSize,
        query->connectedComponents,
        nullptr);

    if(err != MC_NO_ERROR)
    {
        delete query;
        return nullptr;
    }

    return query;
}

void DestroyMeshQuery(MeshCutQuery* query)
{
    delete query;
}

uint32_t GetResultMeshCount(const MeshCutQuery* query)
{
    return query->connectedComponentsSize;
}

McResult CreateMeshFromResult(const MeshCutQuery* query, int index, Mesh** mesh)
{
    char fnameBuf[32];
    sprintf(fnameBuf, "mcut-lib-conncomp%d.off", index);

#define EARLY_EXIT_IF_ERROR(err) do {\
        if (err != MC_NO_ERROR)\
        {                            \
            fprintf(stderr, "[mcut] %s, %u error %li\n", __FILE__, __LINE__, err); \
            FILE *file = fopen(fnameBuf, "w");\
            if(file){\
                fprintf(file, "error at line %u (%li)", __LINE__, err);\
                fclose(file);\
            }\
            delete *mesh;\
            *mesh = nullptr;\
            return err;\
        }\
    } while(0);\

    McResult err = index < 0 || (uint32_t)index >= query->connectedComponentsSize ? MC_INVALID_VALUE : MC_NO_ERROR;
    EARLY_EXIT_IF_ERROR(err);

    auto& context = query->context;
    auto& component = query->connectedComponents[index];

    uint64_t vertexDataSize, faceDataSize;
    EARLY_EXIT_IF_ERROR(mcGetConnectedComponentData(context, component, MC_CONNECTED_COMPONENT_DATA_VERTEX_FLOAT, 0, nullptr, &vertexDataSize));
    EARLY_EXIT_IF_ERROR(mcGetConnectedComponentData(context, component, MC_CONNECTED_COMPONENT_DATA_FACE_TRIANGULATION, 0, nullptr, &faceDataSize));

    auto& m = *(*mesh = new Mesh());

    m.layout.vertexCount = vertexDataSize / (sizeof(float) * 3);
    m.layout.indexCount = faceDataSize / sizeof(uint32_t);
    m.layout.faceCount = m.layout.indexCount / 3;

    m.positions = new vec3[m.layout.vertexCount];
    m.indices = new uint32_t[m.layout.indexCount];
    m.faceSizes = nullptr;

    EARLY_EXIT_IF_ERROR(mcGetConnectedComponentData(context, component, MC_CONNECTED_COMPONENT_DATA_VERTEX_FLOAT, vertexDataSize, (void*) m.positions, nullptr));
    EARLY_EXIT_IF_ERROR(mcGetConnectedComponentData(context, component, MC_CONNECTED_COMPONENT_DATA_FACE_TRIANGULATION, faceDataSize, (void*) m.indices, nullptr));

//    writeOFF(fnameBuf, *mesh);

    return err;

#undef EARLY_EXIT_IF_ERROR
}

void writeOFF(const char *fpath, Mesh* mesh)
{
    float *pVertices = &mesh->positions[0].x;
    uint32_t *pFaceIndices = mesh->indices;
    uint32_t *pFaceSizes = mesh->faceSizes;
    uint32_t numVertices = mesh->layout.vertexCount;
    uint32_t numFaces = mesh->layout.faceCount;
    fprintf(stdout, "write: %s\n",fpath );

    FILE *file = fopen(fpath, "w");

    if (file == NULL)
    {
        fprintf(stderr, "[mcut] error: failed to open `%s`\n", fpath);
        return;
    }

    fprintf(file, "OFF\n");
    fprintf(file, "%d %d %d\n", numVertices, numFaces, 0 /*numEdges*/);
    int i;
    for (i = 0; i < (int)numVertices; ++i)
    {
        float *vptr = pVertices + (i * 3);
        fprintf(file, "%f %f %f\n", vptr[0], vptr[1], vptr[2]);
    }

    int faceBaseOffset = 0;
    for (i = 0; i < (int)numFaces; ++i)
    {
        uint32_t faceVertexCount = pFaceSizes == nullptr ? 3 : pFaceSizes[i];
        if(pFaceSizes == nullptr)
            fprintf(file, "%d*", (int)faceVertexCount);
        else
            fprintf(file, "%d", (int)faceVertexCount);

        int j;
        for (j = 0; j < (int)faceVertexCount; ++j)
        {
            uint32_t *fptr = pFaceIndices + faceBaseOffset + j;
            fprintf(file, " %d", *fptr);
        }
        fprintf(file, "\n");
        faceBaseOffset += (int) faceVertexCount;
    }

    fclose(file);
}


