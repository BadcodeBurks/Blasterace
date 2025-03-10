using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GroundChecker
{
    public Vector2Int dimensions = new Vector2Int(3, 3);
    public Vector2 spacing = new Vector2(.2f, .2f);
    public float maxDistance = 1f;

    private Vector3[] localCheckOffsets;
    private float[] distanceResults;
    private Vector3 lastCheckPos = Vector3.zero;
    private Vector3 lastCheckForward = Vector3.zero;

    private List<float[]> crunchArrays = new List<float[]>();
    private int[] crunchedArrayLengths;
    private Vector2Int[] crunchedArrayDimensions;
    private int _groundLayerMask;

    private Vector2 angles = Vector2.zero;
    public Vector2 Angles => angles;

    private float avgDist = 0;
    public float AvgDist => avgDist;

    public void Init()
    {
        _groundLayerMask = LayerMask.GetMask("Default");
        localCheckOffsets = new Vector3[dimensions.x * dimensions.y];
        distanceResults = new float[dimensions.x * dimensions.y];
        Vector3 startOffset = new Vector3(-(dimensions.x - 1) * spacing.x / 2, 0, -(dimensions.y - 1) * spacing.y / 2);
        for (int i = 0; i < dimensions.x; i++)
        {
            for (int j = 0; j < dimensions.y; j++)
            {
                localCheckOffsets[i * dimensions.y + j] = startOffset + new Vector3(i * spacing.x, 0, j * spacing.y);
            }
        }
        crunchArrays = new List<float[]>();
        int crunchAmount = Mathf.Min(dimensions.x, dimensions.y) - 1;
        crunchedArrayLengths = new int[crunchAmount];
        crunchedArrayDimensions = new Vector2Int[crunchAmount];
        for (int i = 0; i < crunchAmount; i++)
        {
            crunchedArrayDimensions[i] = new Vector2Int(dimensions.x - i - 1, dimensions.y - i - 1);
            crunchedArrayLengths[i] = crunchedArrayDimensions[i].x * crunchedArrayDimensions[i].y;
            crunchArrays.Add(new float[crunchedArrayLengths[i]]);
        }
    }

    public void Check(Vector3 checkPos, Vector3 checkForward)
    {
        if (checkPos == lastCheckPos && checkForward == lastCheckForward) return;
        lastCheckPos = checkPos;
        lastCheckForward = checkForward;
        for (int i = 0; i < localCheckOffsets.Length; i++)
        {
            Vector3 rotatedCheckOffset = Quaternion.Euler(0, Vector3.SignedAngle(Vector3.forward, checkForward, Vector3.up), 0) * localCheckOffsets[i];
            Vector3 p = checkPos + rotatedCheckOffset;
            CheckPosition(p, i);
        }
        CrunchValues();
        CalculateAngles();
    }

    private void CheckPosition(Vector3 pos, int writeIndex)
    {

        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, maxDistance, _groundLayerMask))
        {
            distanceResults[writeIndex] = hit.distance;
        }
        else
        {
            distanceResults[writeIndex] = maxDistance * .6f;
        }
    }

    private void CrunchValues()
    {
        for (int i = 0; i < crunchArrays.Count; i++)
        {
            float[] src;
            if (i == 0) src = distanceResults;
            else src = crunchArrays[i - 1];
            Vector2Int srcDim = i == 0 ? dimensions : crunchedArrayDimensions[i - 1];
            for (int j = 0; j < crunchedArrayDimensions[i].x; j++)
            {
                for (int k = 0; k < crunchedArrayDimensions[i].y; k++)
                {
                    int srcIndex = j * srcDim.y + k;
                    int destIndex = j * crunchedArrayDimensions[i].y + k;
                    float avg = src[srcIndex];
                    avg += src[srcIndex + srcDim.y];
                    avg += src[srcIndex + 1];
                    avg += src[srcIndex + srcDim.y + 1];
                    avg /= 4;
                    crunchArrays[i][destIndex] = avg;
                }
            }
        }
        avgDist = crunchArrays[crunchArrays.Count - 1][0];
    }

    public void CalculateAngles()
    {
        float[] src = crunchArrays[crunchArrays.Count - 2];
        float vert1 = 1 - (src[0] + src[1]) / 2;
        float vert2 = 1 - (src[2] + src[3]) / 2;
        float hor1 = 1 - (src[0] + src[2]) / 2;
        float hor2 = 1 - (src[1] + src[3]) / 2;
        angles.x = (vert2 - vert1) / (dimensions.y - 1) / spacing.y;
        angles.y = (hor2 - hor1) / (dimensions.x - 1) / spacing.x;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < localCheckOffsets.Length; i++)
        {
            Vector3 p = lastCheckPos + Quaternion.Euler(0, Vector3.SignedAngle(Vector3.forward, lastCheckForward, Vector3.up), 0) * localCheckOffsets[i];
            p += Vector3.down * distanceResults[i];
            Gizmos.DrawWireSphere(p, .03f);
        }

        Gizmos.color = Color.green;
        float[] src = crunchArrays[0];
        Vector3 offsetToOriginal = new Vector3(spacing.x / 2, 0, spacing.y / 2);
        for (int i = 0; i < src.Length; i++)
        {
            int iD = i + i / crunchedArrayDimensions[0].x;
            Vector3 p = lastCheckPos + Quaternion.Euler(0, Vector3.SignedAngle(Vector3.forward, lastCheckForward, Vector3.up), 0) * (localCheckOffsets[iD] + offsetToOriginal);
            p += Vector3.down * src[i];
            Gizmos.DrawWireSphere(p, .03f);
        }

        Gizmos.color = Color.black;
        Vector3 convertedAngles = new Vector3(angles.x, 0, angles.y);
        convertedAngles = Quaternion.Euler(0, Vector3.SignedAngle(Vector3.forward, lastCheckForward, Vector3.up), 0) * convertedAngles;
        Gizmos.DrawLine(lastCheckPos + Vector3.up, convertedAngles + lastCheckPos + Vector3.up);
    }

}