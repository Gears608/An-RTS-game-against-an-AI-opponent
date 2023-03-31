using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Component
{
    public float x;
    public float y;

    public int indexX;
    public int indexY;

    public List<HierarchicalNode> portalNodes;

    /*
     * A constructor for a Component object
     * 
     * float x - the x position of the component
     * float y - the y position of the component
     * int indexX - the x index of the component
     * int indexY - the y index of the component
     */
    public Component(float x, float y, int indexX, int indexY)
    {
        this.x = x;
        this.y = y;
        this.indexX = indexX;
        this.indexY = indexY;
        portalNodes = new List<HierarchicalNode>();
    }

    /*
     * A function which adds a node to the component
     * 
     * HierarchicalNode node - the node to add
     */
    public void AddNode(HierarchicalNode node)
    {
        portalNodes.Add(node);
    }

    /*
     * A function which removes a node from the component
     * 
     * HierarchicalNode node - the node to remove
     */
    public void RemoveNode(HierarchicalNode node)
    {
        foreach(HierarchicalNode n in portalNodes)
        {
            n.RemoveNode(node);
        }

        portalNodes.Remove(node);
    }
}
