using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node> {
    public bool walkable;
    public Vector3 worldPos;
    public int gridX, gridY;
    public int movementPenalty;

    public int gCost, hCost;
    public Node parent;
    int HeapIndex;

    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty) {
        walkable = _walkable;
        worldPos = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
        movementPenalty = _penalty;
    }

    public int fCost { get { return gCost + hCost; } }

    public int heapIndex {
        get {
            return HeapIndex;
        }
        set {
            HeapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare) {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if(compare == 0) {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }

        return -compare;
    }
}
