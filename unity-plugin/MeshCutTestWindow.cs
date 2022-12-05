using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeshCut
{
    class TestMCut : EditorWindow
    {
        [SerializeField]
        Mesh m_Source, m_Cut;
        McResult m_Result;

        McDispatchFlags m_Flags = McDispatchFlags.MC_DISPATCH_VERTEX_ARRAY_FLOAT
            // | McDispatchFlags.MC_DISPATCH_INCLUDE_VERTEX_MAP
            // | McDispatchFlags.MC_DISPATCH_INCLUDE_FACE_MAP
            ;

        static Material s_DefaultMaterial;

        static Material defaultMaterial
        {
            get
            {
                if (s_DefaultMaterial != null)
                    return s_DefaultMaterial;
                var prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s_DefaultMaterial = prim.GetComponent<MeshRenderer>().sharedMaterial;
                DestroyImmediate(prim);
                return s_DefaultMaterial;
            }
        }
        
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
            // m_Source = (Mesh)EditorGUILayout.ObjectField("Source", m_Source, typeof(Mesh), true);
            // m_Cut = (Mesh)EditorGUILayout.ObjectField("Cut", m_Cut, typeof(Mesh), true);
            m_Flags = (McDispatchFlags)EditorGUILayout.EnumFlagsField("Dispatch Flags", m_Flags);

            if (GUILayout.Button("CUT"))
            {
                var context = new MeshCutContext();
        
                // todo
                // mcut requires source meshes to be manifold. by their definition, this means edges are shared between 
                // exactly two faces. since unity meshes generally have hard edges, we will need to do some processing to
                // put any generic mesh through the pipe. for how we'll just test that the "hello world" example is
                // producing something we can work with.
                using var src = new MeshPtr()
                {
                    positions = new Vector3[]
                    {
                        new Vector3(-5, -5, 5),
                        new Vector3(5, -5, 5),
                        new Vector3(5, 5, 5),
                        new Vector3(-5, 5, 5),
                        new Vector3(-5, -5, -5),
                        new Vector3(5, -5, -5),
                        new Vector3(5, 5, -5),
                        new Vector3(-5, 5, -5)
                    },
                    indices = new int[]
                    {
                        0, 1, 2, 3,
                        7, 6, 5, 4,
                        1, 5, 6, 2,
                        0, 3, 7, 4,
                        3, 2, 6, 7,
                        4, 5, 1, 0
                    },
                    faces = new int[]
                    {
                        4, 4, 4, 4, 4, 4
                    }
                };

                using var cut = new MeshPtr()
                {
                    positions = new Vector3[]
                    {
                        new Vector3(-20, -4, 0),
                        new Vector3(0, 20, 20),
                        new Vector3(20, -4, 0),
                        new Vector3(0, 20, -20)
                    },
                    indices = new int[]
                    {
                        0, 1, 2,
                        0, 2, 3
                    }
                };
                
                // important! does not assume ownership
                MeshCutContext.SetSourceMesh(context.ptr, src.ptr);
                MeshCutContext.SetCutMesh(context.ptr, cut.ptr);

                m_Result = context.Dispatch(m_Flags);

                if (m_Result == McResult.MC_NO_ERROR && context.GetResultMeshCount() > 0)
                {
                    for (int i = 0, c = context.GetResultMeshCount(); i < c; ++i)
                    {
                        if(!context.CreateMeshFromResult(i, out var mesh))
                            continue;
                        var go = new GameObject() { name = $"Cut Mesh {i}" };
                        go.AddComponent<MeshFilter>().sharedMesh = (Mesh) mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterial = defaultMaterial;
                    }
                }
                
                context.Dispose();
            }

            EditorGUILayout.LabelField("Last Result", $"{m_Result}");
        }
    }
}