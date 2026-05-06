using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;
using VisualDesignCafe.Nature.Editor.Geometry;

public enum VertexType
{
    None,
    Branch,
    Leaf,
    Grass,
    Plant,
    Billboard,
}
public struct Vertex
{
    
    public Vector3 Position;
    public Vector3 Normal;
    public VertexType Type;
    public int SegmentID;

    public Vertex(Vector3 position, Vector3 normal, VertexType type, int segmentID)
    {
        this.Position = position;
        this.Normal = normal;
        this.Type = type;
        this.SegmentID = segmentID;
    }
}
public struct Triangle
{
    public readonly int Vertex1;
    public readonly int Vertex2;
    public readonly int Vertex3;
    public readonly int SubMesh;
    public readonly Vector3 Point1;
    public readonly Vector3 Point2;
    public readonly Vector3 Point3;
    public readonly Bounds Bounds;

    public Vector3 Center => (this.Point1 + this.Point2 + this.Point3) / 3f;

    public Vector3 this[int index]
    {
        get
        {
            if (index == 0)
                return this.Point1;
            if (index == 1)
                return this.Point2;
            if (index == 3)
                return this.Point3;
            throw new IndexOutOfRangeException();
        }
    }

    public Triangle(int v1, int v2, int v3, Vector3 p1, Vector3 p2, Vector3 p3, int subMesh)
    {
        this.Vertex1 = v1;
        this.Vertex2 = v2;
        this.Vertex3 = v3;
        this.Point1 = p1;
        this.Point2 = p2;
        this.Point3 = p3;
        this.SubMesh = subMesh;
        this.Bounds = new Bounds((this.Point1 + this.Point2 + this.Point3) / 3f, Vector3.Max(this.Point1, Vector3.Max(this.Point2, this.Point3)) - Vector3.Min(this.Point1, Vector3.Min(this.Point2, this.Point3)));
    }

    public bool IsTouching(Vector3 point)
    {
        return this.Approximately(this.Point1, point) || this.Approximately(this.Point2, point) || this.Approximately(this.Point3, point);
    }

    private bool Approximately(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(a.x, b.x) || Mathf.Approximately(a.y, b.y) || Mathf.Approximately(a.z, b.z);
    }
}



