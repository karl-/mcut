// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshCut
{
    public class MeshCutContext : IDisposable
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
        static extern IntPtr GetSourceMesh(IntPtr ctx);
        [DllImport("libMCutBindingsd.so")]
        static extern IntPtr GetCutMesh(IntPtr ctx);
        [DllImport("libMCutBindingsd.so")]
        public static extern UInt32 Dispatch(IntPtr ctx, UInt32 flags);
        [DllImport("libMCutBindingsd.so")]
        public static extern IntPtr CreateMeshQuery(IntPtr ctx, McConnectedComponentType flags);

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

        public MeshPtr source
        {
            get => new MeshPtr(m_Source.ptr, false);

            set
            {
                m_Source?.Dispose();
                m_Source = value;
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

        public MeshPtr cut
        {
            get => new MeshPtr(m_Cut.ptr, false);

            set
            {
                m_Cut?.Dispose();
                m_Cut = value;
            }
        }

        public Mesh CopyCutMesh() => (Mesh)m_Cut;

        public McResult Dispatch(McDispatchFlags flags = McDispatchFlags.MC_DISPATCH_VERTEX_ARRAY_FLOAT)
        {
            return (McResult)Dispatch(m_Ptr, (uint)flags);
        }

        public bool TryCreateMeshQuery(McConnectedComponentType flags, out MeshCutQuery query)
        {
            query = null;
            var ptr = CreateMeshQuery(m_Ptr, flags);
            if (ptr == IntPtr.Zero)
                return false;
            query = new MeshCutQuery(ptr);
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
