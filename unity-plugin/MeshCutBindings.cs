using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable IdentifierTypo

// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace MeshCutBindings
{
    enum McResult
    {
        // The function was successfully executed.
        MC_NO_ERROR = 0, 
        // An internal operation could not be executed successively.
        MC_INVALID_OPERATION = -(1 << 1), 
        // An invalid value has been passed to the API.
        MC_INVALID_VALUE = -(1 << 2), 
        // Memory allocation operation cannot allocate memory.
        MC_OUT_OF_MEMORY = -(1 << 3), 
        // Wildcard (match all) .
        MC_RESULT_MAX_ENUM = Int32.MaxValue 
    }

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
                fixed(Vector3* ptr = buffer)
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
                fixed(int* ptr = buffer)
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

        public MeshPtr(Mesh mesh) : this()
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

    class MeshCutContext : IDisposable
    {
        IntPtr m_Ptr;
        MeshPtr m_Source, m_Cut;
        
        [DllImport("libMCutBindingsd.so")]
        static extern UInt32 CreateContext(out IntPtr ctx);
        [DllImport("libMCutBindingsd.so")]
        static extern void DestroyContext(IntPtr ptr);
        [DllImport("libMCutBindingsd.so")]
        public static extern void SetSourceMesh(IntPtr ctx, IntPtr mesh);
        [DllImport("libMCutBindingsd.so")]
        public static extern void SetCutMesh(IntPtr ctx, IntPtr mesh);
        [DllImport("libMCutBindingsd.so")]
        public static extern IntPtr GetSourceMesh(IntPtr ctx);
        [DllImport("libMCutBindingsd.so")]
        public static extern IntPtr GetCutMesh(IntPtr ctx);
        [DllImport("libMCutBindingsd.so")]
        public static extern UInt32 Dispatch(IntPtr ctx);

        public IntPtr ptr => m_Ptr;

        public MeshCutContext()
        {
            var res = (McResult)CreateContext(out m_Ptr);
            if(res != McResult.MC_NO_ERROR)
                Debug.LogError(res);
        }

        public Mesh sourceMesh
        {
            set
            {
                m_Source?.Dispose();
                m_Source = new MeshPtr(value);
                SetSourceMesh(m_Ptr, m_Source.ptr);
            }
        }

        public Mesh CopySourceMesh() => (Mesh)m_Source;

        public Mesh cutMesh
        {
            set
            {
                m_Cut?.Dispose();
                m_Cut = new MeshPtr(value);
                SetCutMesh(m_Ptr, m_Cut.ptr);
            }
        }

        public Mesh CopyCutMesh() => (Mesh)m_Cut;

        public McResult Dispatch() => (McResult) Dispatch(m_Ptr);

        public void Dispose()
        {
            m_Source?.Dispose();
            m_Cut?.Dispose();
            
            if (m_Ptr == IntPtr.Zero)
                return;
            DestroyContext(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }
    }

    class TestMCut : EditorWindow
    {
        [SerializeField]
        Mesh m_Source, m_Cut;
        McResult m_Result;
        
        [MenuItem("Window/MCUT Window")]
        static void init() => GetWindow<TestMCut>();

        void OnEnable()
        {
            var meshes = Selection.GetFiltered<MeshFilter>(SelectionMode.TopLevel).Select(x => x.sharedMesh).Where(x => x != null).ToArray();
            int i = 0;
            if (m_Source == null && i < meshes.Length)
                m_Source = meshes[i++];
            if (m_Cut == null && i < meshes.Length)
                m_Cut = meshes[i++];
        }

        void OnGUI()
        {
            m_Source = (Mesh) EditorGUILayout.ObjectField("Source", m_Source, typeof(Mesh), true);
            m_Cut = (Mesh) EditorGUILayout.ObjectField("Cut", m_Cut, typeof(Mesh), true);

            if (GUILayout.Button("CUT"))
            {
                var context = new MeshCutContext();
                context.sourceMesh = m_Source;
                context.cutMesh = m_Cut;

                var src = context.CopySourceMesh();
                Debug.Log($"src vertex count: {src.vertexCount}");
                DestroyImmediate(src);
                var cut = context.CopyCutMesh();
                Debug.Log($"cut vertex count: {cut.vertexCount}");
                DestroyImmediate(cut);

                m_Result = context.Dispatch();
                context.Dispose();
            }
            
            EditorGUILayout.LabelField("Last Result", $"{m_Result}");
        }
    }
}
