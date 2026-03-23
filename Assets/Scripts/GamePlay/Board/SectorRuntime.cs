using UnityEngine;
using System.Collections.Generic;
public class SectorRuntime : MonoBehaviour
{
    public Vector2Int coord;
    public bool isStartSector;
    public bool isOpened;

    public Transform cameraPoint;
    public Transform[] enemySpawnPoints;

    public SectorEdge XMin;
    public SectorEdge xMax;
    public SectorEdge ZMin;
    public SectorEdge ZMax;
}