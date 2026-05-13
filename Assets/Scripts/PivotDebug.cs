using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct PivotComp
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public float Distance;

    public PivotComp(Vector3 startPoint, Vector3 endPoint)
    {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.Distance =  Vector3.Distance(startPoint, endPoint);
    }
    public override bool Equals(object obj)
    {
        return obj is PivotComp other && startPoint == other.startPoint && endPoint == other.endPoint;
    }
}
public class PivotDebug : MonoBehaviour
{
    public bool ShowPivot = true;
    public bool DeleteTooShortBranch = true;
    public float MinBranchLength = 0.1f;
    [SerializeField]
    public List<PivotComp> PivotStartPoints;

    [ContextMenu("PivotDebug")]
    public void PivotDebugMenu()
    {
        Mesh mesh = GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
        HashSet<PivotComp> List = new HashSet<PivotComp>();
        
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 startPoint = new Vector3();
            Vector3 endPoint = new Vector3();
            if (mesh.uv3.Length != 0 && mesh.uv4.Length != 0)
            {
                 startPoint = new Vector3(mesh.uv3[i].x, mesh.uv3[i].y, mesh.uv4[i].x);
            }

            if (mesh.uv4.Length != 0 && mesh.uv5.Length != 0)
            {
                endPoint = new Vector3(mesh.uv4[i].y, mesh.uv5[i].x, mesh.uv5[i].y);
            }
            
            //List.Add(new PivotComp(startPoint, endPoint));
          
            List.Add(new PivotComp(startPoint, endPoint));
            
        }

        PivotStartPoints = List.ToList();
    }

    public void OnDrawGizmos()
    {
        if (ShowPivot&&PivotStartPoints.Count>0)
        {
            foreach (var VARIABLE in PivotStartPoints)
            {
                Vector3 start = transform.TransformPoint(VARIABLE.startPoint);
                Vector3 end = transform.TransformPoint(VARIABLE.endPoint);
                if (VARIABLE.Distance > MinBranchLength || !DeleteTooShortBranch)
                {
                    Gizmos.DrawLine(start, end);
                    Gizmos.DrawSphere(start, 0.01f);
                    Gizmos.DrawSphere(end, 0.01f);
                }
            }
        }
    }
}
