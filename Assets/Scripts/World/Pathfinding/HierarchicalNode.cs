using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HierarchicalNode
{
    public int x;
    public int y;

    public int f;
    public int g;
    public int h;
    public HierarchicalNode previousNode;

    public Dictionary<HierarchicalNode, int> connectedNodes;
    public Component component;

    // Contstructor for a hierarchical node
    public HierarchicalNode(int x, int y, Component component)
    {
        this.x = x;
        this.y = y;
        this.component = component;
        connectedNodes = new Dictionary<HierarchicalNode, int>();
    }


    /*
     *    A* Pathfinding
     */

    public void CalculateF()
    {
        f = g + h;
    }


    /*
     *    Connected Node Manipulation 
     */

    // Function for adding a node to the connected nodes
    public void AddNode(HierarchicalNode node, int weight)
    {
        connectedNodes.Add(node, weight);
    }

    // Function for removing a node from the connected nodes
    public void RemoveNode(HierarchicalNode node)
    {
        connectedNodes.Remove(node);
    }

    // Function for getting the weight of a given node, returns -1 if the node is not within the connected nodes
    public int GetWeight(HierarchicalNode node)
    {
        if (connectedNodes.ContainsKey(node))
        {
            return connectedNodes[node];
        }
        else 
        {
            return -1;
        }
    }
}
