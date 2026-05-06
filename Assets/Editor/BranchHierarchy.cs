using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class BranchHierarchy
{
  private List<BranchHierarchy.Element> _elements = new List<BranchHierarchy.Element>();
  private readonly Vertex[] _vertices;

  public BranchHierarchy(Branch[] branches, Vertex[] vertices) => this._vertices = vertices;

  public void Build(Vector3 origin, Branch[] branches)
  {
    bool flag = false;
    for (int index = 0; index < branches.Length; ++index)
    {
      if (this.IsIntersectingFloorPlane(in branches[index], in origin))
      {
        this.SetTrunk(in branches[index], ref branches);
        flag = true;
      }
    }
    if (!flag)
      this.SetTrunk(((IEnumerable<Branch>) branches).OrderBy<Branch, float>((Func<Branch, float>) (b => this.DistanceToOrigin(in b, in origin))).FirstOrDefault<Branch>(), ref branches);
    float threshold = 0.2f;
    List<Branch> connectedBranches = new List<Branch>();
    List<List<Branch>> branchListList = new List<List<Branch>>();
    connectedBranches.AddRange(((IEnumerable<Branch>) branches).Where<Branch>((Func<Branch, bool>) (b => b.IsTrunk)));
    branchListList.Add(connectedBranches.ToList<Branch>());
    while (connectedBranches.Count > 0)
    {
      Branch[] array = connectedBranches.ToArray();
      connectedBranches.Clear();
      Parallel.ForEach<Branch>((IEnumerable<Branch>) array, (Action<Branch>) (branch =>
      {
        List<Branch> connectedBranches1 = this.FindConnectedBranches(in branch, branches, threshold);
        lock (connectedBranches)
          connectedBranches.AddRange((IEnumerable<Branch>) connectedBranches1);
      }));
      branchListList.Add(connectedBranches.ToList<Branch>());
    }
    while (((IEnumerable<Branch>) branches).Any<Branch>((Func<Branch, bool>) (b => b.Parent == -1)))
    {
      threshold *= 2f;
      for (int index = 0; index < branchListList.Count; ++index)
      {
        connectedBranches.Clear();
        connectedBranches.AddRange((IEnumerable<Branch>) branchListList[index]);
        int num = 0;
        while (connectedBranches.Count > 0)
        {
          Branch[] array = connectedBranches.ToArray();
          connectedBranches.Clear();
          Parallel.ForEach<Branch>((IEnumerable<Branch>) array, (Action<Branch>) (branch =>
          {
            List<Branch> connectedBranches2 = this.FindConnectedBranches(in branch, branches, threshold);
            lock (connectedBranches)
              connectedBranches.AddRange((IEnumerable<Branch>) connectedBranches2);
          }));
          if (index + num < branchListList.Count)
            branchListList[index + num].AddRange((IEnumerable<Branch>) connectedBranches);
          else
            branchListList.Add(connectedBranches.ToList<Branch>());
          ++num;
        }
      }
    }
  }

  private float DistanceToOrigin(in Branch branch, in Vector3 origin)
  {
    float distance;
    BranchHierarchy.DistanceToTrianglesSquared(origin, branch.Triangles, out distance, out Vector3 _);
    return Mathf.Sqrt(distance);
  }

  private void SetTrunk(in Branch branch, ref Branch[] branches)
  {
    if (!branch.IsValid)
      return;
    branches[branch.Id] = new Branch(branch.Id, branch.Type, branch.Triangles, branch.Boundaries, -2);
  }

  private void SetParent(in Branch branch, int parent, ref Branch[] allBranches)
  {
    allBranches[branch.Id] = new Branch(branch.Id, branch.Type, branch.Triangles, branch.Boundaries, parent);
  }

  private void SetParent(in Branch branch, in Branch parent, ref Branch[] allBranches)
  {
    allBranches[branch.Id] = new Branch(branch.Id, branch.Type, branch.Triangles, branch.Boundaries, parent.Id);
  }

  private bool IsIntersectingFloorPlane(in Branch branch, in Vector3 origin)
  {
    foreach (Triangle triangle in branch.Triangles)
    {
      if ((double) triangle.Point1.y <= (double) origin.y || (double) triangle.Point2.y <= (double) origin.y || (double) triangle.Point3.y <= (double) origin.y)
        return true;
    }
    return false;
  }

  private List<Branch> FindConnectedBranches(
    in Branch branch,
    Branch[] allBranches,
    float threshold)
  {
    List<Branch> connectedBranches = new List<Branch>();
    foreach (Branch allBranch in allBranches)
    {
      if (allBranch.Id != branch.Id && allBranch.Parent == -1 && this.DistanceToBranchIsBelowThreshold(in branch, in allBranch, threshold))
      {
        if (this.IsConnected(in branch, in allBranch))
        {
          if (branch.Parent > -1)
          {
            this.SetParent(in allBranch, branch.Parent, ref allBranches);
            connectedBranches.Add(allBranch);
          }
          else if (branch.IsTrunk)
          {
            this.SetTrunk(in allBranch, ref allBranches);
            connectedBranches.Add(allBranch);
          }
          else
          {
            this.SetParent(in allBranch, in branch, ref allBranches);
            connectedBranches.Add(allBranch);
          }
        }
        else
        {
          this.SetParent(in allBranch, in branch, ref allBranches);
          connectedBranches.Add(allBranch);
        }
      }
    }
    return connectedBranches;
  }

  private bool IsConnected(in Branch branch, in Branch other)
  {
    foreach (Edge boundary1 in branch.Boundaries)
    {
      foreach (Edge boundary2 in other.Boundaries)
      {
        if (boundary1.IsConnected(boundary2))
          return true;
      }
    }
    return false;
  }

  public bool GetClosestBranch(
    Vector3 point,
    Vector3 origin,
    Branch[] branches,
    out Branch closestBranch,
    out float distanceToBranch)
  {
    distanceToBranch = float.MaxValue;
    closestBranch = Branch.Invalid;
    foreach (Branch branch in branches)
    {
      float distance;
      this.DistanceToBranchSquared(point, branch, out distance, out Vector3 _);
      if ((double) distance < (double) distanceToBranch * (double) distanceToBranch)
      {
        distanceToBranch = Mathf.Sqrt(distance);
        closestBranch = branch;
      }
    }
    return (double) distanceToBranch != 3.4028234663852886E+38;
  }

  public void DistanceToTrunk(
    Vector3 point,
    out float distance,
    out Vector3 pointOnBranch,
    Branch[] branches)
  {
    distance = float.MaxValue;
    pointOnBranch = new Vector3();
    foreach (Branch branch in branches)
    {
      if (branch.IsTrunk)
      {
        float distance1;
        Vector3 pointOnBranch1;
        this.DistanceToBranch(point, branch, out distance1, out pointOnBranch1);
        if ((double) distance1 < (double) distance)
        {
          distance = distance1;
          pointOnBranch = pointOnBranch1;
        }
      }
    }
  }

  public void DistanceToBranch(
    Vector3 point,
    Branch branch,
    out float distance,
    out Vector3 pointOnBranch)
  {
    this.DistanceToBranchSquared(point, branch, out distance, out pointOnBranch);
    distance = Mathf.Sqrt(distance);
  }

  private bool DistanceToBranchIsBelowThreshold(in Branch branch, in Branch other, float threshold)
  {
    Bounds bounds1 = branch.Bounds;
    Bounds bounds2 = other.Bounds;
    bounds1.Expand(threshold);
    if (!bounds1.Intersects(bounds2))
      return false;
    foreach (Triangle triangle in branch.Triangles)
    {
      if (this.DistanceToTrianglesIsBelowThreshold(in triangle, in other.Triangles, threshold))
        return true;
    }
    return false;
  }

  public void DistanceToBranchSquared(
    in Branch branch,
    in Branch other,
    out float distance,
    out Vector3 pointOnBranch)
  {
    distance = float.MaxValue;
    pointOnBranch = new Vector3();
    foreach (Triangle triangle in branch.Triangles)
    {
      float distance1;
      Vector3 point;
      this.DistanceTriangleToTrianglesSquared(in triangle, in other.Triangles, out distance1, out point);
      if ((double) distance1 < (double) distance)
      {
        distance = distance1;
        pointOnBranch = point;
      }
    }
  }

  private void DistanceTriangleToTrianglesSquared(
    in Triangle triangle,
    in Triangle[] triangles,
    out float distance,
    out Vector3 point)
  {
    float distance1;
    Vector3 pointOut1;
    BranchHierarchy.DistanceToTrianglesSquared(triangle.Point1, triangles, out distance1, out pointOut1);
    float distance2;
    Vector3 pointOut2;
    BranchHierarchy.DistanceToTrianglesSquared(triangle.Point2, triangles, out distance2, out pointOut2);
    float distance3;
    Vector3 pointOut3;
    BranchHierarchy.DistanceToTrianglesSquared(triangle.Point3, triangles, out distance3, out pointOut3);
    if ((double) distance1 < (double) distance2)
    {
      distance = distance1;
      point = pointOut1;
    }
    else
    {
      distance = distance2;
      point = pointOut2;
    }
    if ((double) distance3 >= (double) distance)
      return;
    distance = distance3;
    point = pointOut3;
  }

  public void DistanceToBranchSquared(
    Vector3 point,
    Branch branch,
    out float distance,
    out Vector3 pointOnBranch)
  {
    float distance1 = float.MaxValue;
    Vector3 pointOut = new Vector3();
    if (branch.IsValid)
      BranchHierarchy.DistanceToTrianglesSquared(point, branch.Triangles, out distance1, out pointOut);
    distance = distance1;
    pointOnBranch = pointOut;
  }

  public static void DistanceToTrianglesSquared(
    Vector3 point,
    Triangle[] triangles,
    out float distance,
    out Vector3 pointOut)
  {
    double num = double.MaxValue;
    Vector3 vector3 = new Vector3();
    DistPoint3Triangle3 distPoint3Triangle3 = new DistPoint3Triangle3(new Vector3(), new Triangle());
    foreach (Triangle triangle in triangles)
    {
      distPoint3Triangle3.Point = point;
      distPoint3Triangle3.Triangle = triangle;
      distPoint3Triangle3.Compute();
      if (distPoint3Triangle3.DistanceSquared < num)
      {
        num = distPoint3Triangle3.DistanceSquared;
        vector3 = distPoint3Triangle3.TriangleClosest;
      }
    }
    distance = (float) num;
    pointOut = vector3;
  }

  private bool DistanceToTrianglesIsBelowThreshold(
    in Triangle triangle,
    in Triangle[] triangles,
    float threshold)
  {
    float num = threshold * threshold;
    Bounds bounds = triangle.Bounds;
    bounds.Expand(threshold);
    DistPoint3Triangle3 distPoint3Triangle3 = new DistPoint3Triangle3(new Vector3(), new Triangle());
    foreach (Triangle triangle1 in triangles)
    {
      if (triangle1.Bounds.Intersects(bounds))
        return true;
    }
    return false;
  }

  public struct Element
  {
    public int Id;
    public int[] Children;
  }
}

public class DistPoint3Triangle3
{
  private Vector3 point;
  private Triangle triangle;
  public double DistanceSquared = -1.0;
  public Vector3 TriangleClosest;
  public Vector3 TriangleBaryCoords;

  public Vector3 Point
  {
    get => this.point;
    set
    {
      this.point = value;
      this.DistanceSquared = -1.0;
    }
  }

  public Triangle Triangle
  {
    get => this.triangle;
    set
    {
      this.triangle = value;
      this.DistanceSquared = -1.0;
    }
  }

  public DistPoint3Triangle3(Vector3 PointIn, Triangle TriangleIn)
  {
    this.point = PointIn;
    this.triangle = TriangleIn;
  }

  public DistPoint3Triangle3 Compute()
  {
    this.GetSquared();
    return this;
  }

  public double Get() => Math.Sqrt(this.GetSquared());

  public double GetSquared()
  {
    if (this.DistanceSquared >= 0.0)
      return this.DistanceSquared;
    this.DistanceSquared = DistPoint3Triangle3.DistanceSqr(ref this.point, ref this.triangle, out this.TriangleClosest, out this.TriangleBaryCoords);
    return this.DistanceSquared;
  }

  public static double DistanceSqr(
    ref Vector3 point,
    ref Triangle triangle,
    out Vector3 closestPoint,
    out Vector3 baryCoords)
  {
    Vector3 v1 = triangle.Point1 - point;
    Vector3 v2_1 = triangle.Point2 - triangle.Point1;
    Vector3 v2_2 = triangle.Point3 - triangle.Point1;
    float sqrMagnitude1 = v2_1.sqrMagnitude;
    float num1 = DistPoint3Triangle3.Vector3_Dot(v2_1, ref v2_2);
    float sqrMagnitude2 = v2_2.sqrMagnitude;
    float num2 = DistPoint3Triangle3.Vector3_Dot(v1, ref v2_1);
    float num3 = DistPoint3Triangle3.Vector3_Dot(v1, ref v2_2);
    float sqrMagnitude3 = v1.sqrMagnitude;
    float num4 = Math.Abs((float) ((double) sqrMagnitude1 * (double) sqrMagnitude2 - (double) num1 * (double) num1));
    float num5 = (float) ((double) num1 * (double) num3 - (double) sqrMagnitude2 * (double) num2);
    float num6 = (float) ((double) num1 * (double) num2 - (double) sqrMagnitude1 * (double) num3);
    float z;
    float y;
    float val1;
    if ((double) num5 + (double) num6 <= (double) num4)
    {
      if ((double) num5 < 0.0)
      {
        if ((double) num6 < 0.0)
        {
          if ((double) num2 < 0.0)
          {
            z = 0.0f;
            if (-(double) num2 >= (double) sqrMagnitude1)
            {
              y = 1f;
              val1 = sqrMagnitude1 + 2f * num2 + sqrMagnitude3;
            }
            else
            {
              y = -num2 / sqrMagnitude1;
              val1 = num2 * y + sqrMagnitude3;
            }
          }
          else
          {
            y = 0.0f;
            if ((double) num3 >= 0.0)
            {
              z = 0.0f;
              val1 = sqrMagnitude3;
            }
            else if (-(double) num3 >= (double) sqrMagnitude2)
            {
              z = 1f;
              val1 = sqrMagnitude2 + 2f * num3 + sqrMagnitude3;
            }
            else
            {
              z = -num3 / sqrMagnitude2;
              val1 = num3 * z + sqrMagnitude3;
            }
          }
        }
        else
        {
          y = 0.0f;
          if ((double) num3 >= 0.0)
          {
            z = 0.0f;
            val1 = sqrMagnitude3;
          }
          else if (-(double) num3 >= (double) sqrMagnitude2)
          {
            z = 1f;
            val1 = sqrMagnitude2 + 2f * num3 + sqrMagnitude3;
          }
          else
          {
            z = -num3 / sqrMagnitude2;
            val1 = num3 * z + sqrMagnitude3;
          }
        }
      }
      else if ((double) num6 < 0.0)
      {
        z = 0.0f;
        if ((double) num2 >= 0.0)
        {
          y = 0.0f;
          val1 = sqrMagnitude3;
        }
        else if (-(double) num2 >= (double) sqrMagnitude1)
        {
          y = 1f;
          val1 = sqrMagnitude1 + 2f * num2 + sqrMagnitude3;
        }
        else
        {
          y = -num2 / sqrMagnitude1;
          val1 = num2 * y + sqrMagnitude3;
        }
      }
      else
      {
        float num7 = 1f / num4;
        y = num5 * num7;
        z = num6 * num7;
        val1 = (float) ((double) y * ((double) sqrMagnitude1 * (double) y + (double) num1 * (double) z + 2.0 * (double) num2) + (double) z * ((double) num1 * (double) y + (double) sqrMagnitude2 * (double) z + 2.0 * (double) num3)) + sqrMagnitude3;
      }
    }
    else if ((double) num5 < 0.0)
    {
      float num8 = num1 + num2;
      float num9 = sqrMagnitude2 + num3;
      if ((double) num9 > (double) num8)
      {
        float num10 = num9 - num8;
        float num11 = sqrMagnitude1 - 2f * num1 + sqrMagnitude2;
        if ((double) num10 >= (double) num11)
        {
          y = 1f;
          z = 0.0f;
          val1 = sqrMagnitude1 + 2f * num2 + sqrMagnitude3;
        }
        else
        {
          y = num10 / num11;
          z = 1f - y;
          val1 = (float) ((double) y * ((double) sqrMagnitude1 * (double) y + (double) num1 * (double) z + 2.0 * (double) num2) + (double) z * ((double) num1 * (double) y + (double) sqrMagnitude2 * (double) z + 2.0 * (double) num3)) + sqrMagnitude3;
        }
      }
      else
      {
        y = 0.0f;
        if ((double) num9 <= 0.0)
        {
          z = 1f;
          val1 = sqrMagnitude2 + 2f * num3 + sqrMagnitude3;
        }
        else if ((double) num3 >= 0.0)
        {
          z = 0.0f;
          val1 = sqrMagnitude3;
        }
        else
        {
          z = -num3 / sqrMagnitude2;
          val1 = num3 * z + sqrMagnitude3;
        }
      }
    }
    else if ((double) num6 < 0.0)
    {
      float num12 = num1 + num3;
      float num13 = sqrMagnitude1 + num2;
      if ((double) num13 > (double) num12)
      {
        float num14 = num13 - num12;
        float num15 = sqrMagnitude1 - 2f * num1 + sqrMagnitude2;
        if ((double) num14 >= (double) num15)
        {
          z = 1f;
          y = 0.0f;
          val1 = sqrMagnitude2 + 2f * num3 + sqrMagnitude3;
        }
        else
        {
          z = num14 / num15;
          y = 1f - z;
          val1 = (float) ((double) y * ((double) sqrMagnitude1 * (double) y + (double) num1 * (double) z + 2.0 * (double) num2) + (double) z * ((double) num1 * (double) y + (double) sqrMagnitude2 * (double) z + 2.0 * (double) num3)) + sqrMagnitude3;
        }
      }
      else
      {
        z = 0.0f;
        if ((double) num13 <= 0.0)
        {
          y = 1f;
          val1 = sqrMagnitude1 + 2f * num2 + sqrMagnitude3;
        }
        else if ((double) num2 >= 0.0)
        {
          y = 0.0f;
          val1 = sqrMagnitude3;
        }
        else
        {
          y = -num2 / sqrMagnitude1;
          val1 = num2 * y + sqrMagnitude3;
        }
      }
    }
    else
    {
      float num16 = sqrMagnitude2 + num3 - num1 - num2;
      if ((double) num16 <= 0.0)
      {
        y = 0.0f;
        z = 1f;
        val1 = sqrMagnitude2 + 2f * num3 + sqrMagnitude3;
      }
      else
      {
        float num17 = sqrMagnitude1 - 2f * num1 + sqrMagnitude2;
        if ((double) num16 >= (double) num17)
        {
          y = 1f;
          z = 0.0f;
          val1 = sqrMagnitude1 + 2f * num2 + sqrMagnitude3;
        }
        else
        {
          y = num16 / num17;
          z = 1f - y;
          val1 = (float) ((double) y * ((double) sqrMagnitude1 * (double) y + (double) num1 * (double) z + 2.0 * (double) num2) + (double) z * ((double) num1 * (double) y + (double) sqrMagnitude2 * (double) z + 2.0 * (double) num3)) + sqrMagnitude3;
        }
      }
    }
    closestPoint = triangle.Point1 + y * v2_1 + z * v2_2;
    baryCoords = new Vector3(1f - y - z, y, z);
    return (double) Math.Max(val1, 0.0f);
  }

  private static float Vector3_Dot(Vector3 v1, Vector3 v2)
  {
    return (float) ((double) v1.x * (double) v2.x + (double) v1.y * (double) v2.y + (double) v1.z * (double) v2.z);
  }

  private static float Vector3_Dot(Vector3 v1, ref Vector3 v2)
  {
    return (float) ((double) v1.x * (double) v2.x + (double) v1.y * (double) v2.y + (double) v1.z * (double) v2.z);
  }
}
