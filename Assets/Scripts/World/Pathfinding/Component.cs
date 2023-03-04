using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Component
{
    public int x;
    public int y;

    public int indexX;
    public int indexY;

    public List<HierarchicalNode> portalNodes;

    public Component(int x, int y, int indexX, int indexY)
    {
        this.x = x;
        this.y = y;
        this.indexX = indexX;
        this.indexY = indexY;
        portalNodes = new List<HierarchicalNode>();
    }

    public void AddNode(HierarchicalNode node)
    {
        portalNodes.Add(node);
    }

    public void RemoveNode(HierarchicalNode node)
    {
        foreach(HierarchicalNode n in portalNodes)
        {
            n.RemoveNode(node);
        }

        portalNodes.Remove(node);
    }
}
