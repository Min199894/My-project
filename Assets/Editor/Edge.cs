using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Edge
{
  public readonly Vector3 Start;
  public readonly Vector3 End;
  public readonly Vector3 StartNormal;
  public readonly Vector3 EndNormal;

  public Edge(Vector3 start, Vector3 end, Vector3 startNormal, Vector3 endNormal)
  {
    this.Start = start;
    this.End = end;
    this.StartNormal = startNormal;
    this.EndNormal = endNormal;
  }

  public bool IsValid => this.Start != Vector3.zero || this.End != Vector3.zero;

  public Vector3 Direction => (this.End - this.Start).normalized;

  public Vector3 InvertedDirection => (this.Start - this.End).normalized;

  public Vector3 Center => this.GetPosition(0.5f);

  public Vector3 Normal => this.GetNormal(0.5f);

  public float Length => (this.End - this.Start).magnitude;

  public float LengthSquared => (this.End - this.Start).sqrMagnitude;

  public override string ToString() => $"({(object) this.Start} -> {(object) this.End})";

  public Vector3 GetPosition(float t) => Vector3.Lerp(this.Start, this.End, t);

  public Vector3 GetNormal(float t) => Vector3.Lerp(this.StartNormal, this.EndNormal, t);

  public int CountDuplicates(IEnumerable<Edge> edges)
  {
    int num = 0;
    foreach (Edge edge in edges)
    {
      if (this.IsOverlapping(edge))
        ++num;
    }
    return num;
  }

  public Edge Invert() => new Edge(this.End, this.Start, this.EndNormal, this.StartNormal);

  private void Swap(ref Edge a, ref Edge b)
  {
    Edge edge = a;
    a = b;
    b = edge;
  }

  public float Alignment(Edge next)
  {
    Edge b = this;
    if (next.End == b.Start)
      this.Swap(ref next, ref b);
    if (b.End == next.End)
      next = next.Invert();
    return Vector3.Dot(b.Direction, next.Direction);
  }

  public bool PointIsOnEdge(Vector3 point)
  {
    float num = Vector3.Dot((point - this.Start).normalized, this.Direction);
    if ((double) num >= 0.949999988079071)
    {
      if ((double) (point - this.Start).sqrMagnitude <= (double) this.LengthSquared)
        return true;
    }
    else if ((double) num <= -0.949999988079071 && (double) (point - this.End).sqrMagnitude <= (double) this.LengthSquared)
      return true;
    return false;
  }

  public bool IsConnected(Edge edge)
  {
    return !this.Equals((object) edge) && (edge.Start == this.Start || edge.Start == this.End || edge.End == this.Start || edge.End == this.End);
  }

  public bool IsOverlapping(Edge edge)
  {
    return this.Start == edge.Start && this.End == edge.End || this.End == edge.Start && this.Start == edge.End;
  }

  public bool IsOverlapping(Triangle triangle)
  {
    int num;
    if (!new Edge(triangle.Point1, triangle.Point2, Vector3.zero, Vector3.zero).IsOverlapping(this))
    {
      Edge edge = new Edge(triangle.Point2, triangle.Point3, Vector3.zero, Vector3.zero);
      if (!edge.IsOverlapping(this))
      {
        edge = new Edge(triangle.Point3, triangle.Point1, Vector3.zero, Vector3.zero);
        num = edge.IsOverlapping(this) ? 1 : 0;
        goto label_4;
      }
    }
    num = 1;
label_4:
    return num != 0;
  }

  public bool IsOverlapping(IEnumerable<Triangle> triangles)
  {
    foreach (Triangle triangle in triangles)
    {
      if (this.IsOverlapping(triangle))
        return true;
    }
    return false;
  }

  public override bool Equals(object obj) => obj is Edge edge && this.IsOverlapping(edge);

  public override int GetHashCode()
  {
    return unchecked(-2044201358 * -1521134295) + EqualityComparer<Vector3>.Default.GetHashCode(this.Center);
  }
}

public class EdgeCollection : IEnumerable<Edge>, IEnumerable
{
  public readonly int Count;
  public readonly Edge[] Edges;

  public EdgeCollection(Edge[] edges)
  {
    this.Edges = edges;
    this.Count = ((IEnumerable<Edge>) edges).Count<Edge>();
  }

  public EdgeCollection Boundaries()
  {
    if (this.Count <= 320)
      return new EdgeCollection(((IEnumerable<Edge>) this.Edges).Where<Edge>((Func<Edge, bool>) (edge => edge.CountDuplicates((IEnumerable<Edge>) this.Edges) == 1)).ToArray<Edge>());
    Dictionary<Edge, int> dictionary = new Dictionary<Edge, int>();
    foreach (Edge edge in this.Edges)
    {
      int num;
      dictionary.TryGetValue(edge, out num);
      dictionary[edge] = num + 1;
    }
    List<Edge> edgeList = new List<Edge>();
    foreach (KeyValuePair<Edge, int> keyValuePair in dictionary)
    {
      if (keyValuePair.Value == 1)
        edgeList.Add(keyValuePair.Key);
    }
    return new EdgeCollection(edgeList.ToArray());
  }

  public EdgeCollection[] Grouped()
  {
    List<EdgeCollection> edgeCollectionList = new List<EdgeCollection>();
    int length = this.Edges.Length;
    List<Edge> list = ((IEnumerable<Edge>) this.Edges).ToList<Edge>();
    while (list.Count > 0)
    {
      List<Edge> edgeList = new List<Edge>(list.Count);
      Edge current = list.First<Edge>();
      list.Remove(current);
      edgeList.Add(current);
      Edge edge = list.FirstOrDefault<Edge>((Func<Edge, bool>) (e => e.IsConnected(current)));
      int count = list.Count;
      while (edge.IsValid)
      {
        current = edge;
        list.Remove(current);
        edgeList.Add(current);
        edge = list.FirstOrDefault<Edge>((Func<Edge, bool>) (e => e.IsConnected(current)));
        if (edge.IsValid)
        {
          --count;
          if (count <= 0)
            throw new OverflowException();
        }
        else
          break;
      }
      edgeCollectionList.Add(new EdgeCollection(edgeList.ToArray()));
      edgeList.Clear();
      --length;
      if (length <= 0)
        throw new OverflowException();
    }
    return edgeCollectionList.ToArray();
  }

  public Bounds CalculateBounds()
  {
    Vector3 lhs1 = ((IEnumerable<Edge>) this.Edges).First<Edge>().Center;
    Vector3 lhs2 = ((IEnumerable<Edge>) this.Edges).First<Edge>().Center;
    foreach (Edge edge in this.Edges)
    {
      lhs1 = Vector3.Min(lhs1, Vector3.Min(edge.Start, edge.End));
      lhs2 = Vector3.Min(lhs2, Vector3.Max(edge.Start, edge.End));
    }
    return new Bounds((lhs1 + lhs2) * 0.5f, lhs2 - lhs1);
  }

  public Vector3 CalculateWeightedCenter()
  {
    Vector3 zero = Vector3.zero;
    foreach (Edge edge in this.Edges)
      zero += edge.Center;
    return zero / (float) this.Count;
  }

  public IEnumerator<Edge> GetEnumerator() => ((IEnumerable<Edge>) this.Edges).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator()
  {
    return (IEnumerator) ((IEnumerable<Edge>) this.Edges).GetEnumerator();
  }
}
