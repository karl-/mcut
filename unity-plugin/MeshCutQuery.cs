// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshCut
{
    public class MeshCutQuery : IDisposable
    {
        IntPtr m_Ptr;
        
        [DllImport("libMCutBindingsd.so")]
        static extern void DestroyMeshQuery(IntPtr query);
        [DllImport("libMCutBindingsd.so")]
        static extern UInt32 GetResultMeshCount(IntPtr query);
        [DllImport("libMCutBindingsd.so")]
        static extern McResult CreateMeshFromResult(IntPtr query, int index, out IntPtr mesh);

        internal MeshCutQuery(IntPtr ptr)
        {
            m_Ptr = ptr;
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
            if (m_Ptr == IntPtr.Zero)
                return;
            DestroyMeshQuery(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }
    }
}

