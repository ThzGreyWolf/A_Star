using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {

    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public bool displayGridGizmos;
    public TerrainType[] walkableRegions;
    public int obsticalProxPenality = 50;
    LayerMask walkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    Node [,] grid;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    void Awake() {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);

        foreach(TerrainType region in walkableRegions) {
            walkableMask.value |= region.terrainMask.value;
            walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }

        CreateGrid();
    }

    public int maxSize {
        get {
            return gridSizeX * gridSizeY;
        }
    }

    void CreateGrid() {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;

        for(int x = 0; x < gridSizeX; x++) {
            for(int y = 0; y < gridSizeY; y++) {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));

                int movementPenalty = 0;

                Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit, 100)) {
                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                }

                if(!walkable) {
                    movementPenalty += obsticalProxPenality;
                }

                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }

        BlurPenaltyMap(3);
    }

    void BlurPenaltyMap(int blurSize) {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHoriPass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVertPass = new int[gridSizeX, gridSizeY];

        for(int y = 0; y < gridSizeY; y++) {
            for(int x = -kernelExtents; x <= kernelExtents; x++) {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHoriPass[0, y] += grid[sampleX, y].movementPenalty;
            }

            for(int x = 1; x < gridSizeX; x++) {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX-1);

                penaltiesHoriPass[x, y] = penaltiesHoriPass[x-1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
            }
        }

        for(int x = 0; x < gridSizeX; x++) {
            for(int y = -kernelExtents; y <= kernelExtents; y++) {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVertPass[x, 0] += penaltiesHoriPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVertPass [x, 0] / (kernelSize * kernelSize));
			grid [x, 0].movementPenalty = blurredPenalty;

            for(int y = 1; y < gridSizeY; y++) {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY-1);
                
                penaltiesVertPass[x, y] = penaltiesVertPass[x, y-1] - penaltiesHoriPass[x, removeIndex] + penaltiesHoriPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVertPass[x, y] / (kernelSize * kernelSize));
                grid[x, y].movementPenalty = blurredPenalty;

                if(blurredPenalty > penaltyMax) { penaltyMax = blurredPenalty; }
                if(blurredPenalty < penaltyMin) { penaltyMin = blurredPenalty; }
            }
        }
    }

    public List<Node> GetNeighbours(Node node) {
        List<Node> neighbours = new List<Node>();

        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++) {
                if(x != 0 || y != 0) {
                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;

                    if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
                        neighbours.Add(grid[checkX, checkY]);
                    }
                }
            }
        }

        return neighbours;
    }

    public Node NodeFromWorldPoint(Vector3 worldPos) {
        float percentX = (worldPos.x + gridWorldSize.x/2) / gridWorldSize.x;
        float percentY = (worldPos.z + gridWorldSize.y/2) / gridWorldSize.y;
        // float percentX = (worldPos.x / gridWorldSize.x) + 0.5f;
        // float percentY = (worldPos.z / gridWorldSize.y) + 0.5f;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if(grid != null && displayGridGizmos) {
            foreach( Node n in grid) {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));

                Gizmos.color = n.walkable ? Gizmos.color : Color.red;
                Gizmos.DrawCube(n.worldPos, Vector3.one * (nodeDiameter));
            }
        }
    }

    [System.Serializable]
    public class TerrainType {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }
}
