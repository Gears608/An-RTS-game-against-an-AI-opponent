using System.Collections.Generic;
using UnityEngine;

public class TileGrid<TTileType>
{
    private int width;
    private int height;
    private float cellSize;
    private Vector2 startPosition;
    public TTileType[,] tileArray;

    //constructor for a tile object
    public TileGrid(int width, int height, float cellSize, Vector2 startPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.startPosition = startPosition;

        tileArray = new TTileType[width, height];
    }

    //gets the world position of a given index
    public Vector2 GetWorldPositionFromIndex(int x, int y) 
    {
        return new Vector2(x, y) * cellSize + startPosition;
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
    public void SetObject(Vector2 worldPosition, TTileType value)
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
    public TTileType GetObject(Vector2 worldPositon)
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

    public Vector2 GetStartPosition()
    {
        return startPosition;
    }

    //returns the objects surrounding the given index
    public List<Vector2Int> GetNeighbours(int x, int y)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        if(x > 0)
        {
            neighbours.Add(new Vector2Int(x -1, y));
            if(y > 0)
            {
                neighbours.Add(new Vector2Int(x - 1, y - 1));
            }
            if(y < height - 1)
            {
                neighbours.Add(new Vector2Int(x - 1, y + 1));
            }
        }

        if(x < width - 1)
        {
            neighbours.Add(new Vector2Int(x + 1, y));
            if (y > 0)
            {
                neighbours.Add(new Vector2Int(x + 1, y - 1));
            }
            if (y < height - 1)
            {
                neighbours.Add(new Vector2Int(x + 1, y + 1));
            }
        }

        if(y > 0)
        {
            neighbours.Add(new Vector2Int(x, y - 1));
        }

        if(y < height - 1)
        {
            neighbours.Add(new Vector2Int(x, y + 1));
        }

        return neighbours;
    }

    public List<Vector2Int> GetCardinalNeighbours(int x, int y)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        if (x > 0)
        {
            neighbours.Add(new Vector2Int(x - 1, y));
        }

        if (x < width - 1)
        {
            neighbours.Add(new Vector2Int(x + 1, y));
        }

        if (y > 0)
        {
            neighbours.Add(new Vector2Int(x, y - 1));
        }

        if (y < height - 1)
        {
            neighbours.Add(new Vector2Int(x, y + 1));
        }

        return neighbours;
    }

    public List<Vector2Int> GetNeighbours(int x, int y, TTileType ignore)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();
        //Debug.Log("Checking: "+x +", "+y);
        bool up = IsValid(x, y + 1, ignore);
        bool down = IsValid(x, y - 1, ignore);
        bool left = IsValid(x - 1, y, ignore);
        bool right = IsValid(x + 1, y, ignore);

        if (up)
        {
            //Debug.Log("Up added");
            neighbours.Add(new Vector2Int(x, y + 1));

            if(right && IsValid(x + 1, y + 1, ignore))
            {
                //Debug.Log("Up-Right added");
                neighbours.Add(new Vector2Int(x + 1, y + 1));
            }
        }

        if (right)
        {
            //Debug.Log("Right added");
            neighbours.Add(new Vector2Int(x + 1, y));

            if(down && IsValid(x + 1, y - 1, ignore))
            {
                //Debug.Log("right-down added");
                neighbours.Add(new Vector2Int(x + 1, y - 1));
            }
        }

        if (down)
        {
            //Debug.Log("down added");
            neighbours.Add(new Vector2Int(x, y - 1));

            if(left && IsValid(x - 1, y - 1, ignore))
            {
                //Debug.Log("down-left added");
                neighbours.Add(new Vector2Int(x - 1, y - 1));
            }
        }

        if (left)
        {
            //Debug.Log("left added");
            neighbours.Add(new Vector2Int(x - 1, y));

            if(up && IsValid(x - 1, y + 1, ignore))
            {
                //Debug.Log("left-up added");
                neighbours.Add(new Vector2Int(x - 1, y + 1));
            }
        }

        return neighbours;
    }

    public bool IsValid(int x, int y, TTileType ignore)
    {
        return x >= 0 && y >= 0 && x < width && y < height && !EqualityComparer<TTileType>.Default.Equals(GetObject(x, y), ignore);
    }
}
