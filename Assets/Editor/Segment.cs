using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Segment
{
  public static Segment Invalid = new Segment(-1, VertexType.None, (Triangle[]) null);
  public readonly int Id;
  public readonly Triangle[] Triangles;
  public readonly VertexType Type;
  public readonly Bounds Bounds;

  public static Segment[] CreateAll(
    Vertex[] vertices,
    Triangle[] triangles,
    VertexType[] vertexTypes)
  {
    int length1 = vertices.Length;
    int length2 = triangles.Length;
    int[] ids = new int[length1];
    for (int index = 0; index < ids.Length; ++index)
      ids[index] = -1;
    for (int index = 0; index < length2; ++index)
    {
      Triangle triangle = triangles[index];
      if (ids[triangle.Vertex1] == -1)
        ids[triangle.Vertex1] = index;
      if (ids[triangle.Vertex2] == -1)
        ids[triangle.Vertex2] = index;
      if (ids[triangle.Vertex3] == -1)
        ids[triangle.Vertex3] = index;
    }
    int num1 = 0;
    bool flag;
    do
    {
      flag = false;
      for (int index = 0; index < length2; ++index)
      {
        Triangle triangle = triangles[index];
        int num2 = ids[triangle.Vertex1];
        int num3 = ids[triangle.Vertex2];
        int id = ids[triangle.Vertex3];
        if ((num2 != num3 || num3 != id) && (num3 == num2 || id == num2 || num3 == id))
        {
          if (num2 == num3)
          {
            Segment.OverrideID(ref ids, id, num2);
            flag = true;
          }
          else if (num2 == id)
          {
            Segment.OverrideID(ref ids, num3, num2);
            flag = true;
          }
          else if (num3 == id)
          {
            Segment.OverrideID(ref ids, num2, num3);
            flag = true;
          }
        }
      }
      ++num1;
      if (num1 > length1)
        throw new OverflowException();
    }
    while (flag);
    for (int index1 = 0; index1 < length1; ++index1)
    {
      for (int index2 = 0; index2 < length1; ++index2)
      {
        if (vertices[index1].Position == vertices[index2].Position && ids[index1] != ids[index2] && vertexTypes[index1] == vertexTypes[index2])
          Segment.OverrideID(ref ids, ids[index2], ids[index1]);
      }
    }
    List<Segment> segmentList = new List<Segment>();
    int[] array1 = ((IEnumerable<int>) ids).Distinct<int>().ToArray<int>();
    for (int id1 = 0; id1 < array1.Length; ++id1)
    {
      int id = array1[id1];
      Triangle[] array2 = ((IEnumerable<Triangle>) triangles).Where<Triangle>((Func<Triangle, bool>) (t => ids[t.Vertex1] == id)).ToArray<Triangle>();
      if (array2.Length != 0)
      {
        VertexType vertexType = Segment.GetVertexType(array2, vertexTypes);
        segmentList.Add(new Segment(id1, vertexType, array2));
      }
    }
    return segmentList.ToArray();
  }

  private static VertexType GetVertexType(Triangle[] triangles, VertexType[] vertexTypes)
  {
    Dictionary<VertexType, int> dictionary = new Dictionary<VertexType, int>();
    foreach (Triangle triangle in triangles)
    {
      Segment.IncreaseCounter<VertexType>(dictionary, vertexTypes[triangle.Vertex1]);
      Segment.IncreaseCounter<VertexType>(dictionary, vertexTypes[triangle.Vertex2]);
      Segment.IncreaseCounter<VertexType>(dictionary, vertexTypes[triangle.Vertex3]);
    }
    return dictionary.OrderBy<KeyValuePair<VertexType, int>, int>((Func<KeyValuePair<VertexType, int>, int>) (p => p.Value)).First<KeyValuePair<VertexType, int>>().Key;
  }

  private static void IncreaseCounter<T>(Dictionary<T, int> counter, T key)
  {
    int num;
    if (counter.TryGetValue(key, out num))
      counter[key] = num + 1;
    else
      counter[key] = 1;
  }

  private static void OverrideID(ref int[] ids, int id, int newId)
  {
    for (int index = 0; index < ids.Length; ++index)
    {
      if (ids[index] == id)
        ids[index] = newId;
    }
  }

  public static Segment Create(
    int id,
    VertexType type,
    Vertex[] vertices,
    Triangle[] triangles,
    Vertex startingPoint)
  {
    bool[] addedTriangles = new bool[triangles.Length];
    List<Triangle> connectedTriangles = new List<Triangle>(triangles.Length);
    Segment.FindConnectedTriangles(startingPoint.Position, triangles, vertices, addedTriangles, connectedTriangles, 0);
    return new Segment(id, type, connectedTriangles.ToArray());
  }

  private static void FindConnectedTriangles(
    Vector3 vertex,
    Triangle[] triangles,
    Vertex[] vertices,
    bool[] addedTriangles,
    List<Triangle> connectedTriangles,
    int depth)
  {
    int length1 = vertices.Length;
    int length2 = triangles.Length;
    int index1 = -1;
    for (int index2 = 0; index2 < length2; ++index2)
    {
      if (triangles[index2].IsTouching(vertex))
      {
        index1 = index2;
        break;
      }
    }
    if (index1 == -1)
      return;
    int[] numArray = new int[length1];
    for (int index3 = 0; index3 < length1; ++index3)
      numArray[index3] = -1;
    numArray[triangles[index1].Vertex1] = index1;
    numArray[triangles[index1].Vertex2] = index1;
    numArray[triangles[index1].Vertex3] = index1;
    for (int index4 = 0; index4 < length2; ++index4)
    {
      Triangle triangle = triangles[index4];
      if (numArray[triangle.Vertex1] == -1)
        numArray[triangle.Vertex1] = index4;
      if (numArray[triangle.Vertex2] == -1)
        numArray[triangle.Vertex2] = index4;
      if (numArray[triangle.Vertex3] == -1)
        numArray[triangle.Vertex3] = index4;
    }
    bool flag;
    do
    {
      flag = false;
      for (int index5 = 0; index5 < length2; ++index5)
      {
        Triangle triangle = triangles[index5];
        int num1 = numArray[triangle.Vertex1];
        int num2 = numArray[triangle.Vertex2];
        int num3 = numArray[triangle.Vertex3];
        if ((num1 != index1 || num2 != index1 || num3 != index1) && (num1 != num2 || num2 != num3))
        {
          if (num1 == index1 || num2 == index1 || num3 == index1)
          {
            numArray[triangle.Vertex1] = index1;
            numArray[triangle.Vertex2] = index1;
            numArray[triangle.Vertex3] = index1;
            for (int index6 = 0; index6 < length1; ++index6)
            {
              int num4 = numArray[index6];
              if (num4 != index1 && (num4 == num1 || num4 == num2 || num4 == num3))
                numArray[index6] = index1;
            }
            flag = true;
          }
          else
          {
            if (num2 != index1 && num2 != num1)
              numArray[triangle.Vertex2] = num1;
            if (num3 != index1 && num3 != num1)
              numArray[triangle.Vertex3] = num1;
          }
        }
      }
    }
    while (flag);
    for (int index7 = 0; index7 < length2; ++index7)
    {
      if (numArray[triangles[index7].Vertex1] == index1)
        connectedTriangles.Add(triangles[index7]);
    }
  }

  public bool IsValid => this.Id > -1 && this.Triangles != null && this.Triangles.Length != 0;

  public Segment(int id, VertexType type, Triangle[] triangles)
  {
    this.Id = id;
    this.Triangles = triangles;
    this.Type = type;
    if (triangles != null)
    {
      this.Bounds = ((IEnumerable<Triangle>) triangles).FirstOrDefault<Triangle>().Bounds;
      foreach (Triangle triangle in triangles)
        this.Bounds.Encapsulate(triangle.Bounds);
    }
    else
      this.Bounds = new Bounds();
  }

  public EdgeCollection GetEdges(Vertex[] vertices)
  {
    List<Edge> edgeList = new List<Edge>(this.Triangles.Length * 3);
    foreach (Triangle triangle in this.Triangles)
    {
      Vertex vertex1 = vertices[triangle.Vertex1];
      Vertex vertex2 = vertices[triangle.Vertex2];
      Vertex vertex3 = vertices[triangle.Vertex3];
      edgeList.Add(new Edge(vertex1.Position, vertex2.Position, vertex1.Normal, vertex2.Normal));
      edgeList.Add(new Edge(vertex2.Position, vertex3.Position, vertex2.Normal, vertex3.Normal));
      edgeList.Add(new Edge(vertex3.Position, vertex1.Position, vertex3.Normal, vertex1.Normal));
    }
    return new EdgeCollection(edgeList.ToArray());
  }

  public Vector3 GetWeightedCenter()
  {
    Vector3 zero = Vector3.zero;
    foreach (Triangle triangle in this.Triangles)
      zero += triangle.Center;
    return zero / (float) this.Triangles.Length;
  }
}
