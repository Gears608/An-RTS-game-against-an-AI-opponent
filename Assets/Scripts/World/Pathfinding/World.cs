using System.Collections.Generic;
using UnityEngine;

public class World<TTileType>
{
    private int width;
    private int height;
    private float cellSize;
    private Vector3 startPosition;
    public TTileType[,] tileArray;

    //constructor for a tile object
    public World(int width, int height, float cellSize, Vector3 startPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.startPosition = startPosition;

        tileArray = new TTileType[width, height];
    }

    //gets the world position of a given index
    public Vector3 GetWorldPositionFromIndex(int x, int y) 
    {
        return new Vector3(x, y) * cellSize + startPosition;
    }

    //returns a vector2int containing the index within the grid from a given world position
    public Vector2Int GetIndexFromWorldPosition(Vector3 worldPosition)
    {
        Vector2Int output = new Vector2Int();
        output.x = Mathf.FloorToInt((worldPosition.x - startPosition.x) / cellSize);
        output.y = Mathf.FloorToInt((worldPosition.y - startPosition.y) / cellSize);
        return output;
    }

    //sets the value based on an index
    public void SetObject(int x, int y, TTileType value)
    {
        if(x >= 0 && y >= 0 && x < width && y < height)
        {
            tileArray[x, y] = value;
        }
    }

    //sets the value given a world position
    public void SetObject(Vector3 worldPosition, TTileType value)
    {
        Vector2Int index = GetIndexFromWorldPosition(worldPosition);
        SetObject(index.x, index.y, value);
    }

    //gets a value in the tile from a given index
    public TTileType GetObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return tileArray[x, y];
        } 
        else
        {
            //Debug.Log("Object out of range: " + x + ", " + y);
            return default(TTileType);
        }
    }

    //gets the value from a given world position
    public TTileType GetObject(Vector3 worldPositon)
    {
        Vector2Int index = GetIndexFromWorldPosition(worldPositon);
        return GetObject(index.x, index.y);
    }

    //returns the height of the tile
    public int GetTileHeight()
    {
        return height;
    }

    //returns the width of the tile
    public int GetTileWidth()
    {
        return width;
    }

    //returns the objects surrounding the given index
    public List<TTileType> GetNeighbours(int x, int y)
    {
        List<TTileType> neighbours = new List<TTileType>();

        if(x > 0)
        {
            neighbours.Add(GetObject(x - 1, y));
            if(y > 0)
            {
                neighbours.Add(GetObject(x - 1, y - 1));
            }
            if(y < height - 1)
            {
                neighbours.Add(GetObject(x - 1, y + 1));
            }
        }

        if(x < width - 1)
        {
            neighbours.Add(GetObject(x + 1, y));
            if (y > 0)
            {
                neighbours.Add(GetObject(x + 1, y - 1));
            }
            if (y < height - 1)
            {
                neighbours.Add(GetObject(x + 1, y + 1));
            }
        }

        if(y > 0)
        {
            neighbours.Add(GetObject(x, y - 1));
        }

        if(y < height - 1)
        {
            neighbours.Add(GetObject(x, y + 1));
        }

        return neighbours;
    }
}
