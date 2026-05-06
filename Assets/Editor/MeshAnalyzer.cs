using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public struct MeshAnalyzer
{
    public Mesh mesh;
    public Vertex[] Vertices;
    public Triangle[] Triangles;
    public int[] SegmentIDs;
    public Segment[] Segments;
    public VertexType[] VertexTypes;
    public Branch[] Branches;
    public int[] BranchIDs;
    public uint[] submeshStartIndices;
    public uint[] submeshEndIndices;
    public BranchHierarchy Hierarchy;
    public Leaf[] Leaves;
    public int[] LeafIDs;
    
    
    public MeshAnalyzer(Mesh mesh, VertexType[] meshType)
    {
        this.mesh = mesh;
        VertexTypes = new VertexType[mesh.vertices.Length];;
        Vertices = new Vertex[mesh.vertices.Length];
        Triangles = new Triangle[mesh.triangles.Length / 3];
        submeshStartIndices = new uint[mesh.subMeshCount];
        submeshEndIndices = new uint[mesh.subMeshCount];
        SegmentIDs = new int[mesh.subMeshCount];
        Branches = new Branch[mesh.subMeshCount];
        BranchIDs = new int[mesh.subMeshCount];
        Hierarchy = new BranchHierarchy(Branches, Vertices);
        Leaves = new Leaf[mesh.subMeshCount];
        LeafIDs = new int[mesh.subMeshCount];
        
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            submeshStartIndices[i] = mesh.GetIndexStart(i);
            submeshEndIndices[i] = submeshStartIndices[i] + mesh.GetIndexCount(i);
            VertexType type = meshType[i];
            for (uint j = submeshStartIndices[i]; j < submeshEndIndices[i]; j += 3U)
            { 
                int vertexId1 = mesh.triangles[j];
                int vertexId2 = mesh.triangles[j + 1];
                int vertexId3 = mesh.triangles[j + 2];
               
                Vertices[vertexId1] = new Vertex(mesh.vertices[vertexId1],mesh.normals[vertexId1],
                    type,-1);
                Vertices[vertexId2] = new Vertex(mesh.vertices[vertexId2],mesh.normals[vertexId2],
                    type,-1);
                Vertices[vertexId3] = new Vertex(mesh.vertices[vertexId3],mesh.normals[vertexId3],
                    type,-1);
                Triangles[j / 3U] = new Triangle((int)vertexId1, (int)vertexId2, (int)vertexId3,Vertices[vertexId1].Position,
                    Vertices[vertexId2].Position,Vertices[vertexId3].Position,i);
            }
        }

        for (int i = 0; i < Vertices.Length; i++)
        {
            VertexTypes[i] = Vertices[i].Type;
        }

        Segments = Segment.CreateAll(Vertices, Triangles, VertexTypes);
        foreach (Segment segment in Segments)
        {
            SegmentIDs =  AssignSegmentIDToVertices(Segments, Vertices);
        }

        if (VertexTypes.Contains(VertexType.Branch))
        {
            Branches = CreateBranches(Vertices, Triangles, Segments);
            BranchIDs = AssignBranchIDToVertices(Branches, Vertices);
            Hierarchy = new BranchHierarchy(Branches, Vertices);
            Hierarchy.Build(Vector3.zero, Branches);

            Leaves = CreateLeaves(Vertices, Branches, Hierarchy, Segments);
            LeafIDs = AssignLeafIDToVertices(Leaves, Vertices);
        }
    }
    
    private int[] AssignSegmentIDToVertices(Segment[] segments, Vertex[] vertices)
    {
        int[] vertices1 = new int[vertices.Length];
        for (int index = 0; index < vertices1.Length; ++index)
            vertices1[index] = -1;
        foreach (Segment segment in segments)
        {
            foreach (Triangle triangle in segment.Triangles)
            {
                vertices1[triangle.Vertex1] = segment.Id;
                vertices1[triangle.Vertex2] = segment.Id;
                vertices1[triangle.Vertex3] = segment.Id;
            }
        }
        return vertices1;
    }
    private Branch[] CreateBranches(Vertex[] vertices, Triangle[] triangles, Segment[] segments)
    {
        List<Branch> branches = new List<Branch>();
        Parallel.ForEach<Segment>(((IEnumerable<Segment>) segments).Where<Segment>((Func<Segment, bool>) (segment => segment.Type == VertexType.Branch)), (Action<Segment>) (segment =>
        {
            Branch branch = Branch.Create(0, segment.Type, segment, vertices, triangles, segments);
            if (!branch.IsValid)
                return;
            lock (branches)
                branches.Add(branch.Copy(branches.Count));
        }));
        return branches.ToArray();
    }
    
    private int[] AssignBranchIDToVertices(Branch[] branches, Vertex[] vertices)
    {
        int[] vertices1 = new int[vertices.Length];
        for (int index = 0; index < vertices1.Length; ++index)
            vertices1[index] = -1;
        foreach (Branch branch in branches)
        {
            foreach (Triangle triangle in branch.Triangles)
            {
                vertices1[triangle.Vertex1] = branch.Id;
                vertices1[triangle.Vertex2] = branch.Id;
                vertices1[triangle.Vertex3] = branch.Id;
            }
        }
        return vertices1;
    }
    
    private Leaf[] CreateLeaves(
        Vertex[] vertices,
        Branch[] branches,
        BranchHierarchy hierarchy,
        Segment[] segments)
    {
        List<Leaf> leaves = new List<Leaf>();
        Parallel.ForEach<Segment>(((IEnumerable<Segment>) segments).Where<Segment>((Func<Segment, bool>) (segment => segment.Type == VertexType.Leaf)), (Action<Segment>) (segment =>
        {
            Leaf leaf = Leaf.Create(0, segment, vertices, branches, hierarchy);
            if (!leaf.IsValid)
                return;
            lock (leaves)
                leaves.Add(leaf.Copy(leaves.Count));
        }));
        return leaves.ToArray();
    }
    
    private int[] AssignLeafIDToVertices(Leaf[] leaves, Vertex[] vertices)
    {
        int[] vertices1 = new int[vertices.Length];
        for (int index = 0; index < vertices1.Length; ++index)
            vertices1[index] = -1;
        foreach (Leaf leaf in leaves)
        {
            foreach (Triangle triangle in leaf.Triangles)
            {
                vertices1[triangle.Vertex1] = leaf.Id;
                vertices1[triangle.Vertex2] = leaf.Id;
                vertices1[triangle.Vertex3] = leaf.Id;
            }
        }
        return vertices1;
    }
    
    
    public Branch TryGetBranchByID(int branchID)
    {
        return this.Branches == null ? Branch.Invalid : (branchID <= -1 || branchID >= this.Branches.Length ? Branch.Invalid : this.Branches[branchID]);
    }
    
    public Branch TryGetBranchForVertex(int vertexID)
    {
        return this.BranchIDs == null || vertexID < 0 || vertexID >= this.BranchIDs.Length ? Branch.Invalid : this.TryGetBranchByID(this.BranchIDs[vertexID]);
    }

    public Leaf TryGetLeafForVertex(int vertexID)
    {
        if (this.LeafIDs == null || vertexID < 0 || vertexID >= this.LeafIDs.Length)
            return Leaf.Invalid;
        int leafId = this.LeafIDs[vertexID];
        return leafId <= -1 || leafId >= this.Leaves.Length ? Leaf.Invalid : this.Leaves[leafId];
    }
}
