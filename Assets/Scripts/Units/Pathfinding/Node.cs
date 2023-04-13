using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    //index of the node
    //public int x;
    //public int y;

    //cost of the node
    public int cost;

    /*
     * A Constructor for a Node object
     * 
     * int x - the x index of the node
     * int y - the y index of the node
     * int cost - the cost to traverse over the node
     */
    public Node(int x, int y, int cost)
    {
        //this.x = x;
        //this.y = y;
        this.cost = cost;
    }
}
