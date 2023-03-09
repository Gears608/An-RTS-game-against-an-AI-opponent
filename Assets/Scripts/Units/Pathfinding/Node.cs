using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public int x;
    public int y;

    public int cost;

    public Node(int x, int y, int cost)
    {
        this.x = x;
        this.y = y;
        this.cost = cost;
    }
}
