using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    //index of the node
    public int x;
    public int y;

    //cost of the node
    public int cost;

    public Node(int x, int y, int cost)
    {
        this.x = x;
        this.y = y;
        this.cost = cost;
    }
}
