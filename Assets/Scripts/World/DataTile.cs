using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class DataTile : ScriptableObject
{
    public TileBase tile;
    public bool walkable;
}
