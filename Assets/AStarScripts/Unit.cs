using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour {
    
    const float minPathUpdateTime = 0.2f;
    const float pathUpdateModeThreshold = 0.5f;

    public Transform target;
    public float speed = 20, turnDst = 5, turnSpeed = 3;
    public float stoppingDst = 10;
    Path path;

    void Start() {
        StartCoroutine(UpdatePath());
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccess) {
        if(pathSuccess) {
            path = new Path(waypoints, transform.position, turnDst, stoppingDst);
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator UpdatePath() {
        if(Time.timeSinceLevelLoad < 0.3f) { yield return new WaitForSeconds(0.3f); }
        PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));

        float sqrMoveThreshold = pathUpdateModeThreshold * pathUpdateModeThreshold;
        Vector3 tragetOldPos = target.position;

        while(true) {
            yield return new WaitForSeconds(minPathUpdateTime);
            if((target.position - tragetOldPos).sqrMagnitude > sqrMoveThreshold) {
                PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
                tragetOldPos = target.position;
            }
        }
    }

    IEnumerator FollowPath() {
        bool followingPath = true;
        int pathIndex = 0;
        transform.LookAt(path.lookPoints[0]);

        float speedPercent = 1;

        while(followingPath) {
            Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
            while(path.turnBoundries[pathIndex].HasCrossedLine(pos2D)) {
                if(pathIndex == path.finishLineIndex) {
                    followingPath = false;
                    break;
                } else {
                    pathIndex++;
                }
            }

            if(followingPath) {
                if(pathIndex >= path.slowDownLineIndex && stoppingDst > 0) {
                    speedPercent = Mathf.Clamp01(path.turnBoundries[path.finishLineIndex].DistanceFromPoint(pos2D) / stoppingDst);
                    if(speedPercent < 0.01) {  followingPath = false; }
                }
                Quaternion targetRot = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
            }

            yield return null;
        }
    }

    public void OnDrawGizmos() {
        if(path != null) {
            path.DrawWithGizmos();
        }
    }
}
