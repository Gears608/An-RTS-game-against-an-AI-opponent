using System.Collections.Generic;
using UnityEngine;

public class TileGrid<TTileType>
{
    private int width;
    private int height;
    private float tileHeight;
    private float tileWidth;
    private Vector2 startPosition;
    public TTileType[,] tileArray;

    /*
     * Constructs a new TileGrid object
     */
    public TileGrid(int width, int height, float tileHeight, float tileWidth, Vector2 startPosition)
    {
        this.width = width;
        this.height = height;
        this.tileWidth = tileWidth;
        this.tileHeight = tileHeight;
        this.startPosition = startPosition;

        tileArray = new TTileType[width, height];
    }

    /*
     * A function to return the world position of a grid cell given the index of the cell
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * 
     * Returns a Vector2 which is the world position of the index
     */
    public Vector2 GetWorldPositionFromIndex(int x, int y) 
    {
        return new Vector2(((x * tileWidth - y * tileHeight) / 2f), (x * tileWidth  + y * tileHeight) / 4f) + startPosition;
    }

    /*
     * A function to return the index of a grid cell given the world position of the cell
     * 
     * Vector2 worldPosition - the world position to be converted
     * 
     * Returns a Vector2Int which is the index of the cell
     */
    public Vector2Int GetIndexFromWorldPosition(Vector2 worldPosition)
    {
        worldPosition -= startPosition;
        Vector2 x = new Vector2((2 * worldPosition.y + worldPosition.x), ((2 * worldPosition.y - worldPosition.x)));
        x = new Vector2(x.x / tileWidth, x.y / tileHeight);
        Vector2Int output = new Vector2Int(Mathf.FloorToInt(x.x), Mathf.FloorToInt(x.y));
        return output;
    }

    /*
     * A function which inserts a given value into a cell at a given index
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * TTileType value - the value to be inserted
     */
    public void SetObject(int x, int y, TTileType value)
    {
        if(x >= 0 && y >= 0 && x < width && y < height)
        {
            tileArray[x, y] = value;
        }
    }

    /*
     * A function which inserts a given value into a cell at a given world position
     * 
     * Vector2 worldPosition - the world position of the cell
     * TTileType value - the value to be inserted
     */
    public void SetObject(Vector2 worldPosition, TTileType value)
    {
        Vector2Int index = GetIndexFromWorldPosition(worldPosition);
        SetObject(index.x, index.y, value);
    }

    /*
     * A function which gets the value of a cell at a given index
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * 
     * Returns a TTileType which is the value of the cell
     */
    public TTileType GetObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return tileArray[x, y];
        } 
        else
        {
            return default(TTileType);
        }
    }

    /*
     * A function which gets the value of a cell at a given world position
     * 
     * Vector2 worldPosition - the world position of the cell
     * 
     * Returns a TTileType which is the value of the cell
     */
    public TTileType GetObject(Vector2 worldPositon)
    {
        Vector2Int index = GetIndexFromWorldPosition(worldPositon);
        return GetObject(index.x, index.y);
    }

    /*
     * A function which returns the height of the grid
     * 
     * Returns an int which is the height of the grid
     */
    public int GetTileHeight()
    {
        return height;
    }

    /*
     * A function which returns the width of the grid
     * 
     * Returns an int which is the width of the grid
     */
    public int GetTileWidth()
    {
        return width;
    }

    /*
     * A function which returns starting position of the grid
     * 
     * Returns a Vector2 which is the starting position/origin of the grid
     */
    public Vector2 GetStartPosition()
    {
        return startPosition;
    }

    /*
     * A function which returns the indexs of all the neighbours of a given index
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * 
     * Returns a List<Vector2Int> which is a list containing all the indexs of neighbouring cell
     */
    public List<Vector2Int> GetNeighbours(int x, int y)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        if(x > 0)
        {
            neighbours.Add(new Vector2Int(x - 1, y));
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

    /*
     * A function which returns the indexs of all the cardinally neighboured cells of a given index
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * 
     * Returns a List<Vector2Int> which is a list containing all the indexs of neighbouring cell
     */
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

    /*
     * A function which returns the indexs of all the intercardinal neighbours of a given index
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * 
     * Returns a List<Vector2Int> which is a list containing all the indexs of neighbouring cells
     */
    public List<Vector2Int> GetIntercardinalNeighbours(int x, int y)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        if (x > 0)
        {
            if (y > 0)
            {
                neighbours.Add(new Vector2Int(x - 1, y - 1));
            }
            if (y < height - 1)
            {
                neighbours.Add(new Vector2Int(x - 1, y + 1));
            }
        }

        if (x < width - 1)
        {
            if (y > 0)
            {
                neighbours.Add(new Vector2Int(x + 1, y - 1));
            }
            if (y < height - 1)
            {
                neighbours.Add(new Vector2Int(x + 1, y + 1));
            }
        }

        return neighbours;
    }

    /*
     * A function which returns the indexs of all the neighbours of a given index excluding a certain value
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * TTileType ignore - the value to ignore
     * 
     * Returns a List<Vector2Int> which is a list containing all the indexs of neighbouring cell
     */
    public List<Vector2Int> GetNeighbours(int x, int y, TTileType ignore)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        bool up = IsValid(x, y + 1, ignore);
        bool down = IsValid(x, y - 1, ignore);
        bool left = IsValid(x - 1, y, ignore);
        bool right = IsValid(x + 1, y, ignore);

        if (up)
        {
            neighbours.Add(new Vector2Int(x, y + 1));

            if(right && IsValid(x + 1, y + 1, ignore))
            {
                neighbours.Add(new Vector2Int(x + 1, y + 1));
            }
        }

        if (right)
        {
            neighbours.Add(new Vector2Int(x + 1, y));

            if(down && IsValid(x + 1, y - 1, ignore))
            {
                neighbours.Add(new Vector2Int(x + 1, y - 1));
            }
        }

        if (down)
        {
            neighbours.Add(new Vector2Int(x, y - 1));

            if(left && IsValid(x - 1, y - 1, ignore))
            {
                neighbours.Add(new Vector2Int(x - 1, y - 1));
            }
        }

        if (left)
        {
            neighbours.Add(new Vector2Int(x - 1, y));

            if(up && IsValid(x - 1, y + 1, ignore))
            {
                neighbours.Add(new Vector2Int(x - 1, y + 1));
            }
        }

        return neighbours;
    }

    /*
     * A function which checks if a given world position is a valid position in the tile grid
     * 
     * Vector2 position - the position to check
     * 
     * Returns true if valid or false if invalid
     */
    public bool IsValidPosition(Vector2 position)
    {
        Vector2Int index = GetIndexFromWorldPosition(position);
        return index.x >= 0 && index.y >= 0 && index.x < width && index.y < height;
    }

    /*
     * A function which checks if a given index is a valid index on the tile grid
     * 
     * int x - the x index of the cell
     * int y - the y index of the cell
     * TTileType ignore - a value to ignore
     * 
     * Returns true if valid or false if invalid
     */
    public bool IsValid(int x, int y, TTileType ignore)
    {
        return x >= 0 && y >= 0 && x < width && y < height && !EqualityComparer<TTileType>.Default.Equals(GetObject(x, y), ignore);
    }
}
