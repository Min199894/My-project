using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;  
using UnityEditor;
using System.Linq;
using System.Xml.Serialization;
using Unity.VisualScripting;
using VisualDesignCafe.Nature.Editor.Importers;
using Object = UnityEngine.Object;


public class BakeMeshColor : EditorWindow
{
    public Object target;
    public bool BakePivot = false;
    public bool BakeFromXML = false;
    public TextAsset XMLAsset;
    public struct MeshImport
    {
        public Mesh mesh;
        public Material[] materials;
        public VertexType[] meshType;
        public int lod;
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
        BakePivot = EditorGUILayout.Toggle("Bake Pivot to branch?", BakePivot);
        target = EditorGUILayout.ObjectField("Target", target, typeof(Object), true) as GameObject;
        BakeFromXML = EditorGUILayout.Toggle("Bake to XML?", BakeFromXML);
        XMLAsset = EditorGUILayout.ObjectField("XMLAsset", XMLAsset, typeof(TextAsset), true) as TextAsset;
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
            MeshAnalyzer[] meshAnalyzers = new MeshAnalyzer[importSettings.Length];
            for (int i = 0; i < importSettings.Length; i++)
            {
                meshAnalyzers[i] = new MeshAnalyzer(importSettings[i].mesh, importSettings[i].meshType);
                BakeVertexColor(meshAnalyzers[i], i);
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
    
    protected float GetHeightMaskFromBounds_Sphere(Vector3 vertex, Bounds bounds)
    {
        float num = Mathf.Max(bounds.extents.x, bounds.extents.z);
        return Mathf.Max(Mathf.Abs(vertex.x) / num, Mathf.Abs(vertex.z) / num, Mathf.Max(vertex.y, 0.0f) / bounds.size.y);
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

    TreeData GetTreeData(TextAsset Asset)
    {
        SpeedTreeRawData rawData = new SpeedTreeRawData();
        XmlSerializer serializer = new XmlSerializer(typeof(SpeedTreeRawData));
        using (StringReader reader = new StringReader(Asset.text))
        {
            rawData = (SpeedTreeRawData)serializer.Deserialize(reader);
        }
        TreeData treeData = new TreeData();
        treeData.InitialTreeData(rawData);
        return treeData;
    }

    void BakeVertexColor(MeshAnalyzer meshAnalyzer, int lod)
    {
        Color[] color = new Color[meshAnalyzer.Vertices.Length];
        Vector2[] uv1 = new Vector2[meshAnalyzer.Vertices.Length];
        Vector2[] uv2 = new Vector2[meshAnalyzer.Vertices.Length];
        Vector2[] uv3 = new Vector2[meshAnalyzer.Vertices.Length];
        Vector2[] uv4 = new Vector2[meshAnalyzer.Vertices.Length];
        
        Bounds bounds = meshAnalyzer.mesh.bounds;

       
        TreeData treeData = new TreeData();//GetTreeData(XMLAsset);
        if (BakeFromXML)
        {
            treeData = GetTreeData(XMLAsset);
        }

        for (int i = 0; i < meshAnalyzer.Vertices.Length; i++)
        {
            switch (meshAnalyzer.Vertices[i].Type)
            {
                case VertexType.Grass:
                    //color[i].r = meshAnalyzer.mesh.colors[i].r;
                    color[i].r = GetNormalizedHeightMaskFromBounds_Straight(meshAnalyzer.Vertices[i].Position, bounds);
                    color[i].g = (float) new System.Random(meshAnalyzer.SegmentIDs[i]).NextDouble() - 0.5f;
                    color[i].b = 1;
                    meshAnalyzer.mesh.colors = color;
                    //meshAnalyzer.mesh.uv2 = uv1;
                    break;
                case VertexType.Branch:
                case VertexType.Leaf:
                    float distance1 = 0.0f;
                    float num1 = 0.0f;
                    float num2 = 0.0f;
                    float a;
                    Branch branch = meshAnalyzer.TryGetBranchForVertex(i);
                    Leaf leafForVertex = meshAnalyzer.TryGetLeafForVertex(i);
                    if (leafForVertex.IsValid)
                        branch = meshAnalyzer.TryGetBranchByID(leafForVertex.Branch);
                    if (branch.IsValid && !branch.IsTrunk)
                    {
                        meshAnalyzer.Hierarchy.DistanceToTrunk(meshAnalyzer.Vertices[i].Position, out distance1, out Vector3 _, meshAnalyzer.Branches);
                        num2 = (float) new System.Random(branch.GetRootParent(meshAnalyzer.Branches)).NextDouble() - 0.5f + (float) branch.GetDepth(meshAnalyzer.Branches) * 0.2f + (float) ((new System.Random(branch.Id).NextDouble() - 0.5) * 0.10000000149011612);
                    }

                    if (!BakeFromXML)
                    {
                        if (branch.IsValid && !branch.IsTrunk && branch.IsTrunk == false)
                        {
                            float maxDist = float.MinValue;
                            float minDist = float.MaxValue;
                            foreach (var VARIABLE in branch.Triangles)
                            {
                                Vector3 location = VARIABLE.Center;
                                meshAnalyzer.Hierarchy.DistanceToTrunk(location, out float dist, out Vector3 _,
                                    meshAnalyzer.Branches);
                                if (dist > maxDist)
                                {
                                    branch.branchPositionEnd = location;
                                    maxDist = dist;
                                }

                                if (dist < minDist)
                                {
                                    branch.branchPositionStart = location;
                                    minDist = dist;
                                }
                            }

                            if (BakePivot)
                            {
                                uv2[i].x = branch.branchPositionStart.x;
                                uv2[i].y = branch.branchPositionStart.y;
                                uv3[i].x = branch.branchPositionStart.z;
                                uv3[i].y = branch.branchPositionEnd.x;
                                uv4[i].x = branch.branchPositionEnd.y;
                                uv4[i].y = branch.branchPositionEnd.z;
                            }
                        }
                        else
                        {
                            branch.branchPositionEnd = new Vector3(0, 1, 0);
                            branch.branchPositionStart = new Vector3(0, 0, 0);
                            if (BakePivot)
                            {
                                uv2[i].x = branch.branchPositionStart.x;
                                uv2[i].y = branch.branchPositionStart.y;
                                uv3[i].x = branch.branchPositionStart.z;
                                uv3[i].y = branch.branchPositionEnd.x;
                                uv4[i].x = branch.branchPositionEnd.y;
                                uv4[i].y = branch.branchPositionEnd.z;
                            }
                        }

                        if (leafForVertex.IsValid)
                        {
                            float distance2;
                            meshAnalyzer.Hierarchy.DistanceToBranch(meshAnalyzer.Vertices[i].Position, branch,
                                out distance2, out Vector3 _);
                            distance1 += distance2;
                            num1 = Mathf.Clamp01(distance2 + 2f);
                            num2 += (float)((new System.Random(leafForVertex.Id).NextDouble() - 0.5) *
                                            0.10000000149011612);

                            uv2[i].x = leafForVertex.AnchorPoint.x;
                            uv2[i].y = leafForVertex.AnchorPoint.y;
                            uv3[i].x = leafForVertex.AnchorPoint.z;
                            uv3[i].y = leafForVertex.AnchorPoint.x;
                            uv4[i].x = leafForVertex.AnchorPoint.y;
                            uv4[i].y = leafForVertex.AnchorPoint.z;
                        }
                    }
                    else
                    {
                        int boneid = treeData.VertexDatas[lod][i].BoneID;
                        boneid = Mathf.Max(0, boneid);
          
                        if (meshAnalyzer.Vertices[i].Type == VertexType.Leaf)
                        {
                            uv2[i].x = treeData.VertexDatas[lod][i].WindAnchor.X;
                            uv2[i].y = treeData.VertexDatas[lod][i].WindAnchor.Y;
                            uv3[i].x = treeData.VertexDatas[lod][i].WindAnchor.Z;;
                            uv3[i].y = treeData.VertexDatas[lod][i].WindAnchor.X;
                            uv4[i].x = treeData.VertexDatas[lod][i].WindAnchor.Y;
                            uv4[i].y = treeData.VertexDatas[lod][i].WindAnchor.Z;;
                        }
                        else
                        {
                            uv2[i].x = treeData.RawData.Bones.BoneList[boneid].StartX;
                            uv2[i].y = treeData.RawData.Bones.BoneList[boneid].StartY;
                            uv3[i].x = treeData.RawData.Bones.BoneList[boneid].StartZ;
                            uv3[i].y = treeData.RawData.Bones.BoneList[boneid].EndX;
                            uv4[i].x = treeData.RawData.Bones.BoneList[boneid].EndY;
                            uv4[i].y = treeData.RawData.Bones.BoneList[boneid].EndZ;
                        }
                    }

                    a = distance1 / Mathf.Max(bounds.extents.x, bounds.extents.z);
                    color[i].r = num2 + num1 * 0.1f;
                    color[i].g = num1;
                    color[i].b = 1;
                    color[i].a = branch.IsTrunk ? 0f : 1f;
                    uv1[i].x = GetNormalizedHeightMaskFromBounds_Straight(meshAnalyzer.Vertices[i].Position, bounds);
                    uv1[i].y = Mathf.Lerp(a, a * a, 0.5f);
                    meshAnalyzer.mesh.colors = color;
                    meshAnalyzer.mesh.uv2 = uv1;
                    meshAnalyzer.mesh.uv3= uv2;
                    meshAnalyzer.mesh.uv4 = uv3;
                    meshAnalyzer.mesh.uv5 = uv4;
                    break;
                case VertexType.Plant:
                    float num4 = 0.0f;
                    Segment segmentForVertex2 = meshAnalyzer.TryGetSegmentForVertex(i);
                    if(segmentForVertex2.IsValid)
                        num4 = (float) new System.Random(segmentForVertex2.Id).NextDouble() - 0.5f;
                    float fromBoundsSphere = GetNormalizedHeightMaskFromBounds_Straight(meshAnalyzer.Vertices[i].Position, bounds);
                    color[i].r = Mathf.Lerp(fromBoundsSphere, fromBoundsSphere * fromBoundsSphere, 0.5f) * 0.8f;
                    color[i].g = num4;
                    meshAnalyzer.mesh.colors = color;
                    break;
                default:
                    break;
            }
        }
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
                importSettings[i].lod = i;
            }
        }
        else
        {
            meshFilters = null;
            meshRenderers = null;
            importSettings = null;
        }
    }
    
    //Gzimoz
}
