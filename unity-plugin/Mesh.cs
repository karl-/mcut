using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshCut
{
    public class MeshPtr : IDisposable
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
        static extern unsafe void SetFaces(IntPtr mesh, int* sizes, int size);
        
        [DllImport("libMCutBindingsd.so")]
        static extern unsafe void GetFaces(IntPtr mesh, int* array);

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

        [DllImport("libMCutBindingsd.so")]
        static extern int GetFaceCount(IntPtr mesh);

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
    
        // setting faces is optional - if not set triangles are assumed
        public unsafe int[] faces
        {
            set
            {
                fixed (int* ptr = value) SetFaces(m_Ptr, ptr, value.Length);
            }

            get
            {
                var buffer = new int[GetFaceCount(m_Ptr)];
                fixed (int* ptr = buffer)
                    GetFaces(m_Ptr, ptr);
                return buffer;
            }
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
            var faceSize = GetFaceSize(mesh.GetTopology(0));
            var ind = mesh.GetIndices(0);
            if (faceSize != 3)
            {
                var fs = new int[ind.Length / faceSize];
                for (int i = 0; i < fs.Length; ++i)
                    fs[i] = faceSize;
                faces = fs;
            }
            indices = ind;
            
        }

        public MeshPtr(IntPtr ptr, bool ownsNativeMemory = false)
        {
            m_Ptr = ptr;
            m_OwnsNativeMemory = ownsNativeMemory;
        }

        public static explicit operator Mesh(MeshPtr ptr)
        {
            var m = new Mesh();
            m.vertices = ptr.positions;
            m.subMeshCount = 1;
            // we'll assume that anything coming from unity is uniform topology, and anything returning from mcut has
            // been triangulated
            m.SetIndices(ptr.indices, ptr.faces[0] == 3 ? MeshTopology.Triangles : MeshTopology.Quads, 0);
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
