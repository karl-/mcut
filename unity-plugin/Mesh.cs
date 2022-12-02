using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshCut
{
    class MeshPtr : IDisposable
    {
        IntPtr m_Ptr;

        // hacky little dance to get around the inability to store references to MonoObject* in our bindings library
        // when constructed with a ptr arg, assume that memory is owned by someone else
        bool m_OwnsNativeMemory;
        public IntPtr ptr => m_Ptr;

        [DllImport("libMCutBindingsd.so")]
        static extern IntPtr CreateMesh();

        [DllImport("libMCutBindingsd.so")]
        static extern void DestroyMesh(IntPtr mesh);

        [DllImport("libMCutBindingsd.so")]
        static extern void SetFaceSize(IntPtr mesh, int size);

        [DllImport("libMCutBindingsd.so")]
        static extern int GetFaceSize(IntPtr mesh);

        [DllImport("libMCutBindingsd.so")]
        static extern unsafe void SetPositions(IntPtr mesh, Vector3* positions, int size);

        [DllImport("libMCutBindingsd.so")]
        static extern unsafe void GetPositions(IntPtr mesh, Vector3* positions);

        [DllImport("libMCutBindingsd.so")]
        static extern unsafe void SetIndices(IntPtr mesh, int* indices, int size);

        [DllImport("libMCutBindingsd.so")]
        static extern unsafe void GetIndices(IntPtr mesh, int* indices);

        [DllImport("libMCutBindingsd.so")]
        static extern int GetVertexCount(IntPtr mesh);

        [DllImport("libMCutBindingsd.so")]
        static extern int GetIndexCount(IntPtr mesh);

        public unsafe Vector3[] positions
        {
            set
            {
                fixed (Vector3* ptr = value) SetPositions(m_Ptr, ptr, value.Length);
            }

            get
            {
                var buffer = new Vector3[GetVertexCount(m_Ptr)];
                fixed (Vector3* ptr = buffer)
                    GetPositions(m_Ptr, ptr);
                return buffer;
            }
        }

        public unsafe int[] indices
        {
            set
            {
                fixed (int* ptr = value) SetIndices(m_Ptr, ptr, value.Length);
            }

            get
            {
                var buffer = new int[GetIndexCount(m_Ptr)];
                fixed (int* ptr = buffer)
                    GetIndices(m_Ptr, ptr);
                return buffer;
            }
        }

        public int faceSize
        {
            get => GetFaceSize(m_Ptr);
            set => SetFaceSize(m_Ptr, value);
        }

        public MeshTopology topology
        {
            get => faceSize == 3 ? MeshTopology.Triangles : MeshTopology.Quads;
            set => faceSize = GetFaceSize(value);
        }

        public MeshPtr()
        {
            m_Ptr = CreateMesh();
            m_OwnsNativeMemory = true;
        }

        public MeshPtr(Mesh mesh)
            : this()
        {
            positions = mesh.vertices;
            indices = mesh.GetIndices(0);
            topology = mesh.GetTopology(0);
        }

        public MeshPtr(IntPtr ptr)
        {
            m_Ptr = ptr;
            m_OwnsNativeMemory = false;
        }

        public static explicit operator Mesh(MeshPtr ptr)
        {
            var m = new Mesh();
            m.vertices = ptr.positions;
            m.subMeshCount = 1;
            m.SetIndices(ptr.indices, ptr.topology, 0);
            return m;
        }

        public void Dispose()
        {
            if (m_Ptr == IntPtr.Zero || !m_OwnsNativeMemory)
                return;
            DestroyMesh(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }

        static int GetFaceSize(MeshTopology topo) => topo switch
        {
            MeshTopology.Triangles => 3,
            MeshTopology.Quads => 4,
            _ => throw new Exception("meshptr only understands triangle and quad topologies")
        };
    }
}
