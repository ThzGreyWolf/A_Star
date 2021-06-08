using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path {

    public readonly Vector3[] lookPoints;
    public readonly Line[] turnBoundries;
    public readonly int finishLineIndex;
    public readonly int slowDownLineIndex;

    public Path(Vector3[] waypoints, Vector3 startPos, float turnDst, float stoppingDst) {
        lookPoints = waypoints;
        turnBoundries = new Line[lookPoints.Length];
        finishLineIndex = turnBoundries.Length-1;

        Vector2 perviousPoint = V3ToV2(startPos);
        for(int i = 0; i < lookPoints.Length; i++) {
            Vector2 currentPoint = V3ToV2(lookPoints[i]);
            Vector2 dirToCurrentPoint = (currentPoint - perviousPoint).normalized;
            Vector2 turnBoundryPoint = i == finishLineIndex ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;

            turnBoundries[i] = new Line(turnBoundryPoint, perviousPoint - dirToCurrentPoint * turnDst);
            perviousPoint = turnBoundryPoint;
        }

        float distanceFromEndPoint = 0;
        for(int i = lookPoints.Length-1; i > 0; i--) {
            distanceFromEndPoint += Vector3.Distance(lookPoints[i], lookPoints[i-1]);
            if(distanceFromEndPoint > stoppingDst) {
                slowDownLineIndex = i;
                break;
            }
        }
    }

    Vector2 V3ToV2(Vector3 v3) {
        return new Vector2(v3.x, v3.z);
    }

    public void DrawWithGizmos() {
        Gizmos.color = Color.black;
        foreach(Vector3 p in lookPoints) {
            Gizmos.DrawCube(p + Vector3.up, Vector3.one);
        }

        Gizmos.color = Color.white;
        foreach(Line l in turnBoundries) {
            l.DrawWithGizmos(10);
        }
    }
}
