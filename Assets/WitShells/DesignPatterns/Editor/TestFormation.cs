using System.Collections.Generic;
using UnityEngine;
using WitShells.DesignPatterns.Core;

public class TestFormation : MonoBehaviour
{
    public enum FormatationTest
    {
        Horizontal,
        Vertical,
        Circle,
        V,
        Wedge,
        Traingle,
        Echelon,
        Column,
        Diamond
    }

    public FormatationTest formation;
    public bool alignmentCenter = true;
    public int entities;
    public int spacingX;
    public float spacingZ;
    public float sphereSize = .2f;
    public float circleRadius = 2;

    void OnDrawGizmosSelected()
    {
        List<Pose> positions = new();
        switch (formation)
        {
            case FormatationTest.Horizontal:
                positions = FormationUtils.GenerateLineFormation(transform, entities, spacingX, alignmentCenter, false);
                break;
            case FormatationTest.Vertical:
                positions = FormationUtils.GenerateLineFormation(transform, entities, spacingX, alignmentCenter, true);
                break;
            case FormatationTest.Circle:
                positions = FormationUtils.GenerateCircleFormation(transform.position, circleRadius, entities);
                break;
            case FormatationTest.V:
                positions = FormationUtils.GenerateVFormation(transform, entities, spacingX, spacingZ);
                break;
            case FormatationTest.Wedge:
                positions = FormationUtils.GenerateWedgeFormation(transform, entities, spacingX, spacingZ);
                break;
            case FormatationTest.Traingle:
                positions = FormationUtils.GenerateTriangleFormation(transform, entities, spacingX);
                break;
            case FormatationTest.Echelon:
                positions = FormationUtils.GenerateEchelonFormation(transform, entities, spacingX, spacingZ, alignmentCenter);
                break;
            case FormatationTest.Column:
                positions = FormationUtils.GenerateColumnFormation(transform, entities, spacingZ, spacingX);
                break;
            case FormatationTest.Diamond:
                positions = FormationUtils.GenerateDiamondFormation(transform, entities, spacingX);
                break;
        }

        foreach (var pose in positions)
        {
            Gizmos.DrawSphere(pose.position, sphereSize);
        }
    }
}