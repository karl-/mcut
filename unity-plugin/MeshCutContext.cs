// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshCut
{
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
        public static extern UInt32 Dispatch(IntPtr ctx, UInt32 flags);
        [DllImport("libMCutBindingsd.so")]
        static extern UInt32 GetResultMeshCount(IntPtr ctx);
        [DllImport("libMCutBindingsd.so")]
        static extern McResult CreateMeshFromResult(IntPtr ctx, int index, out IntPtr mesh);

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

        public McResult Dispatch(McDispatchFlags flags = McDispatchFlags.MC_DISPATCH_VERTEX_ARRAY_FLOAT)
        {
            return (McResult)Dispatch(m_Ptr, (uint)flags);
        }
        
        public int GetResultMeshCount()
        {
            return (int) GetResultMeshCount(m_Ptr);
        }        

        public bool CreateMeshFromResult(int index, out Mesh mesh)
        {
            var res = CreateMeshFromResult(m_Ptr, index, out var ptr);

            if (res != McResult.MC_NO_ERROR || ptr == IntPtr.Zero)
            {
                Debug.Log($"Failed to create mesh from result {res}");
                mesh = null;
                return false;
            }

            using var mptr = new MeshPtr(ptr, true);
            mesh = (Mesh)mptr;
            mptr.Dispose();
            return true;
        }

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
}
