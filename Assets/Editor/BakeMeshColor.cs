using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;  
using UnityEditor;
using System.Linq;
using Unity.VisualScripting;
using VisualDesignCafe.Nature.Editor.Importers;
using Object = UnityEngine.Object;


public class BakeMeshColor : EditorWindow
{
    public Object target;
    public bool BakePivot = false;
    public struct MeshImport
    {
        public Mesh mesh;
        public Material[] materials;
        public VertexType[] meshType;
    }
    
    public MeshImport[] importSettings;

    private MeshFilter[] meshFilters;
    private MeshRenderer[] meshRenderers;

    [MenuItem("Tool/BakeMeshColor")]
    public static void ShowWindow()
    {
        GetWindow<BakeMeshColor>("BakeMeshColor");
    }

    void OnGUI()
    {
        string addr = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (addr.EndsWith(".FBX") || addr.EndsWith(".fbx") || addr.EndsWith(".st"))
        {

            if (target != Selection.activeObject)
            {
                target = Selection.activeObject;
                Refresh();
            }
            Repaint();
        }
        else
        {
            target = null;
            Repaint();
        }

        EditorGUILayout.BeginVertical();
        target = EditorGUILayout.ObjectField("Target", target, typeof(Object), true) as GameObject;
        if (target != null)
        {
            EditorGUILayout.LabelField("Mesh", "Type");
            if (importSettings != null && importSettings.Length > 0)
            {
                for (int i = 0; i < importSettings.Length; i++)
                {
                    
                    EditorGUILayout.LabelField(importSettings[i].mesh.name);
                    using (new GUILayout.VerticalScope(Array.Empty<GUILayoutOption>()))
                    {
                        for (int j = 0; j < importSettings[i].materials.Length; j++)
                        {
                            using (new GUILayout.HorizontalScope(Array.Empty<GUILayoutOption>()))
                            {
                                EditorGUILayout.Space(7);
                                EditorGUILayout.ObjectField(importSettings[i].materials[j], typeof(Material), true);
                                if (importSettings[i].materials[j].name.Contains("Bark"))
                                {
                                    importSettings[i].meshType[j] = VertexType.Branch;
                                }
                                else if (importSettings[i].materials[j].name.Contains("Leaf") ||
                                         importSettings[i].materials[j].name.Contains("Branch"))
                                {
                                    importSettings[i].meshType[j] = VertexType.Leaf;
                                }
                                importSettings[i].meshType[j] =
                                    (VertexType)EditorGUILayout.EnumPopup(importSettings[i].meshType[j]);
                            }
                        }
                    }


                }
            }
        }
        
        if (GUILayout.Button("Bake"))
        {
            //ModelImporter importer = AssetImporter.GetAtPath(addr) as ModelImporter;
            //importer.isReadable = true;
            MeshAnalyzer[] meshAnalyzers = new MeshAnalyzer[importSettings.Length];
            for (int i = 0; i < importSettings.Length; i++)
            {
                
                meshAnalyzers[i] = new MeshAnalyzer(importSettings[i].mesh,importSettings[i].meshType);
                BakeVertexColor(meshAnalyzers[i]);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        EditorGUILayout.EndVertical();
    }
    float GetNormalizedHeightMaskFromBounds_Straight(Vector3 vertex, Bounds bounds)
    {
        return Mathf.Clamp01(vertex.y / bounds.size.y);
    }
    
    public struct VertexData
    {
        public Vector3 Pivot;
        public float Mask;
        public float HeightMask;
        public float PhaseOffset;
        public float BranchMask;
        public float TrunkMask;
        public float EdgeFlutter;
        public float Occlusion;
        public float SecondaryMask;
    }

    void BakeVertexColor(MeshAnalyzer meshAnalyzer)
    {
        Color[] color = new Color[meshAnalyzer.Vertices.Length];
        Vector2[] uv1 = new Vector2[meshAnalyzer.Vertices.Length];
        
        Bounds bounds = meshAnalyzer.mesh.bounds;
        for (int i = 0; i < meshAnalyzer.Vertices.Length; i++)
        {
            switch (meshAnalyzer.Vertices[i].Type)
            {
                case VertexType.Grass:
                    color[i].r = meshAnalyzer.mesh.colors[i].r;
                    color[i].g = (float) new System.Random(meshAnalyzer.Vertices[i].SegmentID).NextDouble() - 0.5f;
                    color[i].b = 1;
                    break;
                case VertexType.Branch:
                case VertexType.Leaf:
                    // float distance1 = 0.0f;
                    // float num1 = 0.0f;
                    // float num2 = 0.0f;
                    // float a;
                    // Branch branch = meshAnalyzer.TryGetBranchForVertex(i);
                    // Leaf leafForVertex = meshAnalyzer.TryGetLeafForVertex(i);
                    // if (leafForVertex.IsValid)
                    //     branch = meshAnalyzer.TryGetBranchByID(leafForVertex.Branch);
                    // if (branch.IsValid && branch.Parent>-1)
                    // {
                    //     meshAnalyzer.Hierarchy.DistanceToTrunk(meshAnalyzer.Vertices[i].Position, out distance1, out Vector3 _, meshAnalyzer.Branches);
                    //     num2 = (float) new System.Random(branch.GetRootParent(meshAnalyzer.Branches)).NextDouble() - 0.5f + (float) branch.GetDepth(meshAnalyzer.Branches) * 0.2f + (float) ((new System.Random(branch.Id).NextDouble() - 0.5) * 0.10000000149011612);
                    // }
                    //
                    // if (leafForVertex.IsValid)
                    // {
                    //     float distance2;
                    //     meshAnalyzer.Hierarchy.DistanceToBranch(meshAnalyzer.Vertices[i].Position, branch,
                    //         out distance2, out Vector3 _);
                    //     distance1 += distance2;
                    //     num1 = Mathf.Clamp01(distance2 + 2f);
                    //     num2 += (float) ((new System.Random(leafForVertex.Id).NextDouble() - 0.5) * 0.10000000149011612);
                    // }
                    // a = distance1 / Mathf.Max(bounds.extents.x, bounds.extents.z);
                    // color[i].r = num2 + num1 * 0.1f;
                    // color[i].g = num1;
                    // color[i].b = 1;
                    // color[i].a = 1;
                    // uv1[i].x = GetNormalizedHeightMaskFromBounds_Straight(meshAnalyzer.Vertices[i].Position, bounds);
                    // uv1[i].y = Mathf.Lerp(a, a * a, 0.5f);
                    break;
                default:
                    break;
            }
        }

        meshAnalyzer.mesh.colors = color;
        meshAnalyzer.mesh.uv2 = uv1;
        
    }

    void Refresh()
    {
        if (target != null)
        {
           
            meshFilters = target.GetComponentsInChildren<MeshFilter>();
            meshRenderers = target.GetComponentsInChildren<MeshRenderer>();
            importSettings = new MeshImport[meshFilters.Length];
            for (int i = 0; i < meshFilters.Length; i++)
            {
                importSettings[i].mesh = meshFilters[i].sharedMesh;
                importSettings[i].materials = meshRenderers[i].sharedMaterials;
                importSettings[i].meshType = new VertexType[meshFilters[i].sharedMesh.subMeshCount];
            }
        }
        else
        {
            meshFilters = null;
            meshRenderers = null;
            importSettings = null;
        }
    }
}
