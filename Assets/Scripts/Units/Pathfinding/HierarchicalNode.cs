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

    /*
     * A constructor for a HierarchicalNode object
     * 
     * int x - the x index of the hierarchical node
     * int y - the y index of the hier archical node
     * Component component - the parent component of the node
     */
    public HierarchicalNode(int x, int y, Component component)
    {
        this.x = x;
        this.y = y;
        this.component = component;
        connectedNodes = new Dictionary<HierarchicalNode, int>();
    }


    /*
     * A function which updates the f value of the hierarchical node
     */
    public void CalculateF()
    {
        f = g + h;
    }

    /*
     * A function which adds a new node and weight to the connected hierarchical nodes dictionary
     * 
     * HierarchicalNode node - the connected node
     * int weight - the weight of travelling between the two nodes
     */
    public void AddNode(HierarchicalNode node, int weight)
    {
        connectedNodes.Add(node, weight);
    }

    /*
     * A function which removes a specified hierarchical node from the connected nodes dictionary
     * 
     * HierarchicalNode node - the node to remove
     */
    public void RemoveNode(HierarchicalNode node)
    {
        connectedNodes.Remove(node);
    }

    /*
     * A function which returns the cost of travelling between 2 connected nodes
     * 
     * HierarchicalNode node - the connected node
     * 
     * Returns an int value as the weight of travelling between the two nodes or -1 if the nodes are not connected
     */
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
