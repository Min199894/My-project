using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using VisualDesignCafe.Nature.Editor.Geometry;

public struct Branch
{
  public readonly int Id;
  public readonly int Parent;
  public readonly Triangle[] Triangles;
  public readonly Edge[] Boundaries;
  public readonly Bounds Bounds;
  public VertexType Type;
  public Vector3 branchPositionStart;
  public Vector3 branchPositionEnd;

  public static Branch Invalid
  {
    get => new Branch(-1, VertexType.None, (Triangle[]) null, (Edge[]) null, -1);
  }

  public bool IsValid => this.Id > -1 && this.Triangles != null && this.Triangles.Length != 0;

  public bool IsTrunk => this.Parent == -2;

  public static Branch Create(
    int id,
    VertexType type,
    Segment island,
    Vertex[] vertices,
    Triangle[] triangles,
    Segment[] allIslands)
  {
    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();
    EdgeCollection edges = island.GetEdges(vertices);
    stopwatch.Restart();
    EdgeCollection edgeCollection = edges.Boundaries();
    return new Branch(id, type, island.Triangles, edgeCollection.Edges, -1);
  }

  public Branch(int id, VertexType type, Triangle[] triangles, Edge[] boundaries, int parent)
  {
    this.Id = id;
    this.Type = type;
    this.Triangles = triangles;
    this.Parent = parent;
    this.Boundaries = boundaries;
    this.Bounds = new Bounds();
    this.branchPositionStart = Vector3.zero;
    this.branchPositionEnd = Vector3.zero;
    if (triangles == null || triangles.Length == 0)
      return;
    this.Bounds = triangles[0].Bounds;
    foreach (Triangle triangle in triangles)
      this.Bounds.Encapsulate(triangle.Bounds);
  }

  public Branch Copy(int newId)
  {
    return new Branch(newId, this.Type, this.Triangles, this.Boundaries, this.Parent);
  }

  public bool HasParent(Branch branch, Branch[] branches)
  {
    int parent = this.Parent;
    int num = 100;
    while (parent > -1)
    {
      if (parent == branch.Id)
        return true;
      parent = branches[parent].Parent;
      --num;
      if (num <= 0)
        throw new OverflowException();
    }
    return false;
  }

  public int GetRootParent(Branch[] branches)
  {
    if (this.Parent < 0)
      return this.Id;
    int num = 100;
    Branch branch = this;
    do
    {
      int id = branch.Id;
      int parent = branch.Parent;
      branch = branches[parent];
      if (branch.Parent < 0)
        return id;
      --num;
      if (num <= 0)
        throw new OverflowException();
    }
    while (branch.Parent > -1);
    return branch.Id;
  }

  public int GetDepth(Branch[] branches)
  {
    int num = 100;
    int depth = 0;
    int parent = this.Parent;
    while (parent > -1)
    {
      ++depth;
      parent = branches[parent].Parent;
      --num;
      if (num <= 0)
        throw new OverflowException();
    }
    return depth;
  }
  
}

