using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml.Serialization;

// 根节点
[XmlRoot("SpeedTreeRaw")]
public class SpeedTreeRawData
{
    
    [XmlElement("Objects")]
    public Objects Objects { get; set; }
    
    [XmlElement("CollisionObjects")]
    public CollisionObjects CollisionObjects { get; set; }
    
    [XmlElement("Bones")]
    public Bones Bones { get; set; }
  
}

// ==================== 对象/几何部分 ====================
public class Objects
{
    [XmlAttribute("Count")]
    public int Count { get; set; }
    
    [XmlAttribute("LodNear")]
    public float LodNear { get; set; }
    
    [XmlAttribute("LodFar")]
    public float LodFar { get; set; }
    
    [XmlAttribute("BoundsMinX")]
    public float BoundsMinX { get; set; }
    
    [XmlAttribute("BoundsMinY")]
    public float BoundsMinY { get; set; }
    
    [XmlAttribute("BoundsMinZ")]
    public float BoundsMinZ { get; set; }
    
    [XmlAttribute("BoundsMaxX")]
    public float BoundsMaxX { get; set; }
    
    [XmlAttribute("BoundsMaxY")]
    public float BoundsMaxY { get; set; }
    
    [XmlAttribute("BoundsMaxZ")]
    public float BoundsMaxZ { get; set; }
    
    [XmlElement("Object")]
    public List<SceneObject> ObjectList { get; set; }
}

public class SceneObject
{
    [XmlAttribute("ID")]
    public int ID { get; set; }
    
    [XmlAttribute("ParentID")]
    public int ParentID { get; set; }
    
    [XmlAttribute("Name")]
    public string Name { get; set; }
    
    [XmlAttribute("AbsX")]
    public float AbsX { get; set; }
    
    [XmlAttribute("AbsY")]
    public float AbsY { get; set; }
    
    [XmlAttribute("AbsZ")]
    public float AbsZ { get; set; }
    
    [XmlAttribute("RelX")]
    public float RelX { get; set; }
    
    [XmlAttribute("RelY")]
    public float RelY { get; set; }
    
    [XmlAttribute("RelZ")]
    public float RelZ { get; set; }
    
    [XmlAttribute("BoundsMinX")]
    public float BoundsMinX { get; set; }
    
    [XmlAttribute("BoundsMinY")]
    public float BoundsMinY { get; set; }
    
    [XmlAttribute("BoundsMinZ")]
    public float BoundsMinZ { get; set; }
    
    [XmlAttribute("BoundsMaxX")]
    public float BoundsMaxX { get; set; }
    
    [XmlAttribute("BoundsMaxY")]
    public float BoundsMaxY { get; set; }
    
    [XmlAttribute("BoundsMaxZ")]
    public float BoundsMaxZ { get; set; }
    
    [XmlElement("Points")]
    public Points Points { get; set; }
    
    [XmlElement("Vertices")]
    public Vertices Vertices { get; set; }
    
    [XmlElement("Triangles")]
    public Triangles Triangles { get; set; }
}

// ==================== 点云数据（简化版 LOD） ====================
public class Points
{
    [XmlAttribute("Count")]
    public int Count { get; set; }
    
    [XmlElement("X")]
    public string X { get; set; }
    
    [XmlElement("Y")]
    public string Y { get; set; }
    
    [XmlElement("Z")]
    public string Z { get; set; }
    
    [XmlElement("LodX")]
    public string LodX { get; set; }
    
    [XmlElement("LodY")]
    public string LodY { get; set; }
    
    [XmlElement("LodZ")]
    public string LodZ { get; set; }
    
    // 辅助方法：解析坐标数组
    public float[] GetXValues() => ParseFloatArray(X);
    public float[] GetYValues() => ParseFloatArray(Y);
    public float[] GetZValues() => ParseFloatArray(Z);
    public float[] GetLodXValues() => ParseFloatArray(LodX);
    public float[] GetLodYValues() => ParseFloatArray(LodY);
    public float[] GetLodZValues() => ParseFloatArray(LodZ);
    
    private float[] ParseFloatArray(string value)
    {
        if (string.IsNullOrEmpty(value)) return new float[0];
        var parts = value.Trim().Split(' ');
        var result = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            float.TryParse(parts[i], out result[i]);
        }
        return result;
    }
}

// ==================== 完整顶点数据 ====================
public class Vertices
{
    [XmlAttribute("Count")]
    public int Count { get; set; }
    
    [XmlElement("NormalX")]
    public string NormalX { get; set; }
    
    [XmlElement("NormalY")]
    public string NormalY { get; set; }
    
    [XmlElement("NormalZ")]
    public string NormalZ { get; set; }
    
    [XmlElement("BinormalX")]
    public string BinormalX { get; set; }
    
    [XmlElement("BinormalY")]
    public string BinormalY { get; set; }
    
    [XmlElement("BinormalZ")]
    public string BinormalZ { get; set; }
    
    [XmlElement("TangentX")]
    public string TangentX { get; set; }
    
    [XmlElement("TangentY")]
    public string TangentY { get; set; }
    
    [XmlElement("TangentZ")]
    public string TangentZ { get; set; }
    
    [XmlElement("TexcoordU")]
    public string TexcoordU { get; set; }
    
    [XmlElement("TexcoordV")]
    public string TexcoordV { get; set; }
    
    [XmlElement("LightmapU")]
    public string LightmapU { get; set; }
    
    [XmlElement("LightmapV")]
    public string LightmapV { get; set; }
    
    [XmlElement("AO")]
    public string AO { get; set; }
    
    [XmlElement("Blend")]
    public string Blend { get; set; }
    
    [XmlElement("VertexColorR")]
    public string VertexColorR { get; set; }
    
    [XmlElement("VertexColorG")]
    public string VertexColorG { get; set; }
    
    [XmlElement("VertexColorB")]
    public string VertexColorB { get; set; }
    
    [XmlElement("VertexColorA")]
    public string VertexColorA { get; set; }
    
    [XmlElement("GeometryType")]
    public string GeometryType { get; set; }
    
    [XmlElement("WindAnchorX")]
    public string WindAnchorX { get; set; }
    
    [XmlElement("WindAnchorY")]
    public string WindAnchorY { get; set; }
    
    [XmlElement("WindAnchorZ")]
    public string WindAnchorZ { get; set; }
    
    [XmlElement("WindBranchX")]
    public string WindBranchX { get; set; }
    
    [XmlElement("WindBranchY")]
    public string WindBranchY { get; set; }
    
    [XmlElement("WindNonBranchX")]
    public string WindNonBranchX { get; set; }
    
    [XmlElement("WindNonBranchY")]
    public string WindNonBranchY { get; set; }
    
    [XmlElement("WindNonBranchZ")]
    public string WindNonBranchZ { get; set; }
    
    [XmlElement("WindLeaf2")]
    public string WindLeaf2 { get; set; }
    
    [XmlElement("BoneID")]
    public string BoneID { get; set; }
    
    // 辅助解析方法
    public float[] GetWindAnchorX() => ParseFloatArray(WindAnchorX);
    public float[] GetWindAnchorY() => ParseFloatArray(WindAnchorY);
    public float[] GetWindAnchorZ() => ParseFloatArray(WindAnchorZ);
    
    public float[] GetNormalXValues() => ParseFloatArray(NormalX);
    public float[] GetNormalYValues() => ParseFloatArray(NormalY);
    public float[] GetNormalZValues() => ParseFloatArray(NormalZ);
    public float[] GetTexcoordUValues() => ParseFloatArray(TexcoordU);
    public float[] GetTexcoordVValues() => ParseFloatArray(TexcoordV);
    public int[] GetBoneIDValues() => ParseIntArray(BoneID);
    
    private float[] ParseFloatArray(string value)
    {
        if (string.IsNullOrEmpty(value)) return new float[0];
        var parts = value.Trim().Split(' ');
        var result = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            float.TryParse(parts[i], out result[i]);
        }
        return result;
    }
    
    private int[] ParseIntArray(string value)
    {
        if (string.IsNullOrEmpty(value)) return new int[0];
        var parts = value.Trim().Split(' ');
        var result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            int.TryParse(parts[i], out result[i]);
        }
        return result;
    }
}

// ==================== 三角形索引 ====================
public class Triangles
{
    [XmlAttribute("Material")]
    public int Material { get; set; }
    
    [XmlAttribute("Count")]
    public int Count { get; set; }
    
    [XmlElement("PointIndices")]
    public string PointIndices { get; set; }
    
    [XmlElement("VertexIndices")]
    public string VertexIndices { get; set; }
    
    public int[] GetPointIndices() => ParseIntArray(PointIndices);
    public int[] GetVertexIndices() => ParseIntArray(VertexIndices);
    
    private int[] ParseIntArray(string value)
    {
        if (string.IsNullOrEmpty(value)) return new int[0];
        var parts = value.Trim().Split(' ');
        var result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            int.TryParse(parts[i], out result[i]);
        }
        return result;
    }
}

// ==================== 碰撞对象 ====================
public class CollisionObjects
{
    [XmlAttribute("Count")]
    public int Count { get; set; }
    
    [XmlElement("CollisionObject")]
    public List<CollisionObject> CollisionObjectList { get; set; }
}

public class CollisionObject
{
    [XmlAttribute("Type")]
    public string Type { get; set; }
    
    [XmlAttribute("Pos1X")]
    public float Pos1X { get; set; }
    
    [XmlAttribute("Pos1Y")]
    public float Pos1Y { get; set; }
    
    [XmlAttribute("Pos1Z")]
    public float Pos1Z { get; set; }
    
    [XmlAttribute("Pos2X")]
    public float Pos2X { get; set; }
    
    [XmlAttribute("Pos2Y")]
    public float Pos2Y { get; set; }
    
    [XmlAttribute("Pos2Z")]
    public float Pos2Z { get; set; }
    
    [XmlAttribute("Radius")]
    public float Radius { get; set; }
    
    [XmlAttribute("UserData")]
    public string UserData { get; set; }
}

// ==================== 骨骼数据 ====================
public class Bones
{
    [XmlAttribute("Count")]
    public int Count { get; set; }
    
    [XmlElement("Bone")]
    public List<Bone> BoneList { get; set; }
}

public class Bone
{
    [XmlAttribute("ID")]
    public int ID { get; set; }
    
    [XmlAttribute("ParentID")]
    public int ParentID { get; set; }
    
    [XmlAttribute("Radius")]
    public float Radius { get; set; }
    
    [XmlAttribute("StartX")]
    public float StartX { get; set; }
    
    [XmlAttribute("StartY")]
    public float StartY { get; set; }
    
    [XmlAttribute("StartZ")]
    public float StartZ { get; set; }
    
    [XmlAttribute("EndX")]
    public float EndX { get; set; }
    
    [XmlAttribute("EndY")]
    public float EndY { get; set; }
    
    [XmlAttribute("EndZ")]
    public float EndZ { get; set; }
    
    [XmlAttribute("Mass")]
    public float Mass { get; set; }
    
    [XmlAttribute("Generator")]
    public string Generator { get; set; }
}

// ==================== Spine 数据 ====================
public class Spines
{
    [XmlElement("Spine")]
    public List<Spine> SpineList { get; set; }
}

public class Spine
{
    [XmlAttribute("Count")]
    public int Count { get; set; }
    
    [XmlElement("X")]
    public string X { get; set; }
    
    [XmlElement("Y")]
    public string Y { get; set; }
    
    [XmlElement("Z")]
    public string Z { get; set; }
    
    [XmlElement("Radius")]
    public string Radius { get; set; }
    
    public float[] GetXValues() => ParseFloatArray(X);
    public float[] GetYValues() => ParseFloatArray(Y);
    public float[] GetZValues() => ParseFloatArray(Z);
    public float[] GetRadiusValues() => ParseFloatArray(Radius);
    
    private float[] ParseFloatArray(string value)
    {
        if (string.IsNullOrEmpty(value)) return new float[0];
        var parts = value.Trim().Split(' ');
        var result = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            float.TryParse(parts[i], out result[i]);
        }
        return result;
    }
}

// ==================== 简易结构 ======================
public struct VertexData
{
    public int BoneID;
    public Vector3 WindAnchor;  

    public VertexData(int boneID, Vector3 windAnchor)
    {
        
        this.BoneID = boneID;
        this.WindAnchor = windAnchor;
    }
}
public class TreeData
{
    public SpeedTreeRawData RawData;
    public VertexData[][] VertexDatas;//每个lod对应的mesh
    public int LodCount;
    public int[] LodStartIndex;
    public int[] LodEndIndex;

    public void InitialTreeData(SpeedTreeRawData rawData)
    {
        this.RawData = rawData;
        GetLodCount();
        InitialLodIndex();
        InitialVertexData();
    }
    private string[] LOD = new []{"LOD0", "LOD1", "LOD2", "LOD3", "LOD4"};

    public void GetLodCount()
    {
        LodCount = 0;
        foreach (var submesh in RawData.Objects.ObjectList)
        {
            if (submesh.Name.Contains("LOD"))
                LodCount++;
        }
    }

    public void InitialLodIndex()
    {
        LodStartIndex = new int[LodCount];
        LodEndIndex = new int[LodCount];

        for (int i = 0; i < LodCount; i++)
        {
            
            foreach (var submesh in RawData.Objects.ObjectList)
            {
                if (submesh.Name.Contains("LOD" + i))
                {
                    LodStartIndex[i] = submesh.ID;
                }

                if (submesh.Name.Contains("LOD" + (i + 1)))
                {
                    LodEndIndex[i] = submesh.ID - 2;
                }

                if (i == (LodCount - 1))
                {
                    LodEndIndex[i] = RawData.Objects.ObjectList.Count - 1;
                }
            }
        }
    }

    public void InitialVertexData()
    {
        VertexDatas = new VertexData[LodCount][];
        for (int i = 0; i < VertexDatas.Length; i++)
        {
            int count = 0;
            for (int j = LodStartIndex[i]; j <= LodEndIndex[i]; j++)
            {
                if (RawData.Objects.ObjectList[j].Vertices != null)
                    count += RawData.Objects.ObjectList[j].Vertices.Count;
                else
                    count += 0;
            }
            VertexDatas[i] = new VertexData[count];

            List<Vector3> position = new List<Vector3>();
            List<int> boneID = new List<int>();
            for (int j = LodStartIndex[i]; j <= LodEndIndex[i]; j++)
            {
                if (RawData.Objects.ObjectList[j].Vertices != null)
                {
                    int[] bone = RawData.Objects.ObjectList[j].Vertices.GetBoneIDValues();
                    float[] AnchorX = RawData.Objects.ObjectList[j].Vertices.GetWindAnchorX();
                    float[] AnchorY = RawData.Objects.ObjectList[j].Vertices.GetWindAnchorY();
                    float[] AnchorZ = RawData.Objects.ObjectList[j].Vertices.GetWindAnchorZ();
                    for (int k = 0; k < bone.Length; k++)
                    {
                        boneID.Add(bone[k]);
                        Vector3 Anchor = new Vector3(AnchorX[k], AnchorY[k], AnchorZ[k]);
                        position.Add(Anchor);
                    }
                }
            }

            for (int j = 0; j < VertexDatas[i].Length; j++)
            {
                VertexDatas[i][j] = new VertexData(boneID[j], position[j]);
            }
        }
    }
}