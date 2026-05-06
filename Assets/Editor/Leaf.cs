using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Leaf
{
  public readonly int Id;
  public readonly Vector3 AnchorPoint;
  public readonly int Branch;
  public readonly Triangle[] Triangles;

  public Leaf(int id, Vector3 anchorPoint, Triangle[] triangles, int branch)
  {
    this.Id = id;
    this.AnchorPoint = anchorPoint;
    this.Branch = branch;
    this.Triangles = triangles;
  }

  public static Leaf Invalid => new Leaf(-1, new Vector3(), (Triangle[]) null, -1);

  public bool IsValid => this.Id > -1 && this.Triangles != null && this.Triangles.Length != 0;

  public static Leaf Create(
    int id,
    Segment island,
    Vertex[] vertices,
    Branch[] branches,
    BranchHierarchy hierarchy)
  {
    EdgeCollection edgeCollection = island.GetEdges(vertices).Boundaries();
    Branch branch = new Branch();
    float num = float.MaxValue;
    Vector3 anchorPoint = Vector3.zero;
    if (edgeCollection.Count > 0)
    {
      foreach (Edge edge in edgeCollection)
      {
        Branch closestBranch;
        float distanceToBranch;
        if (hierarchy.GetClosestBranch(edge.Start, Vector3.zero, branches, out closestBranch, out distanceToBranch) && (double) distanceToBranch < (double) num)
        {
          branch = closestBranch;
          num = distanceToBranch;
          anchorPoint = edge.Start;
        }
      }
    }
    else
    {
      foreach (Triangle triangle in island.Triangles)
      {
       Branch closestBranch;
        float distanceToBranch;
        if (hierarchy.GetClosestBranch(triangle.Point1, Vector3.zero, branches, out closestBranch, out distanceToBranch) && (double) distanceToBranch < (double) num)
        {
          num = distanceToBranch;
          branch = closestBranch;
          anchorPoint = triangle.Point1;
        }
        if (hierarchy.GetClosestBranch(triangle.Point2, Vector3.zero, branches, out closestBranch, out distanceToBranch) && (double) distanceToBranch < (double) num)
        {
          num = distanceToBranch;
          branch = closestBranch;
          anchorPoint = triangle.Point2;
        }
        if (hierarchy.GetClosestBranch(triangle.Point3, Vector3.zero, branches, out closestBranch, out distanceToBranch) && (double) distanceToBranch < (double) num)
        {
          num = distanceToBranch;
          branch = closestBranch;
          anchorPoint = triangle.Point3;
        }
      }
    }
    return new Leaf(id, anchorPoint, island.Triangles, branch.Id);
  }

  public Leaf Copy(int newId) => new Leaf(newId, this.AnchorPoint, this.Triangles, this.Branch);

  public float Size()
  {
    float num = 0.0f;
    foreach (Triangle triangle in this.Triangles)
      num = Mathf.Max(num, Vector3.Distance(triangle.Point1, this.AnchorPoint), Vector3.Distance(triangle.Point2, this.AnchorPoint), Vector3.Distance(triangle.Point3, this.AnchorPoint));
    return num;
  }
}
