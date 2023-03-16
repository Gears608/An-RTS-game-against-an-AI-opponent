using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public TileGrid<Node> tileMap;
    public int width;
    public int height;
    public float cellHeight;
    public float cellWidth;
    public Vector3 startPos;

    public TileGrid<Component> components;

    public int componentWidth;
    public int componentHeight;

    private int terrainMask;


    private Dictionary<(Component, Vector2Int), TileGrid<Vector2>> cachedFlowfields;

    public bool displayCost = false;
    public bool showportals = false;
    public bool showComponents = false;
    public bool showGrid = false;

    public int maxCachedComponents;

    private void Start()
    {
        tileMap = new TileGrid<Node>(width * componentWidth, height * componentHeight, cellHeight, cellWidth, startPos);
        terrainMask = LayerMask.GetMask("Impassable");

        components = new TileGrid<Component>(width, height, componentHeight*cellHeight, componentWidth*cellWidth, Vector2.zero);
        cachedFlowfields = new Dictionary<(Component, Vector2Int), TileGrid<Vector2>>();

        Debug.Log("Generating World...");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x1 = 0; x1 < componentWidth; x1++)
                {
                    for (int y1 = 0; y1 < componentHeight; y1++)
                    {
                        Node node = new Node((x1 + x * componentWidth), (y1 + y * componentHeight), 1);
                        tileMap.SetObject(x1 + x * componentWidth, y1 + y * componentHeight, node);
                    }
                }
                components.SetObject(x, y, new Component(x*componentWidth, y*componentHeight, x, y));
                //sets whether nodes are walkable or not
                UpdateComponent(components.GetObject(x, y));
            }
        }

        Debug.Log("World Generated.");
        Debug.Log("Initializing Paths...");

        foreach(Component component in components.tileArray)
        {
            Debug.Log("Checking Component: "+component.indexX +", "+component.indexY);
            if(component.indexX < width - 1)
            {
                Debug.Log("Checking vertical");
                CheckVertical(component, component.x+componentWidth-1, 1);
            }
            if(component.indexY < height - 1)
            {
                Debug.Log("Checking horizontal");
                CheckHorizontal(component, component.y+componentHeight-1, 1);
            }
            CalculatePaths(component);
        }

        Debug.Log("Paths Initialized.");
    }

    /*
     * 
     *  World Representation
     * 
     */

    //a method which sets nodes as walkable or not
    public void UpdateComponent(Component component)
    {
        int maxX = component.x + componentWidth;
        int maxY = component.y + componentHeight;

        for (int x = component.x; x < maxX; x++)
        {
            for (int y = component.y; y < maxY; y++)
            {
                if (Physics2D.OverlapBox(new Vector2(x,y) + new Vector2(cellWidth, cellHeight)/2, new Vector2(cellWidth - 0.1f, cellHeight - 0.1f), 0, terrainMask, -1f, 1f) != null)
                {
                    Node currNode = tileMap.GetObject(x, y);
                    currNode.cost = 255;
                    tileMap.SetObject(x, y, currNode);
                }
            }
        }
    }

    //checks a vertical line of a component
    private void CheckVertical(Component component, int x, int xModifier)
    {
        int length = 0;
        int startY = component.y;
        int endY = component.y + componentHeight;
        for (int y = startY; y < endY; y++)
        {
            Node currentNode = tileMap.GetObject(x, y);
            Node neighbourNode = tileMap.GetObject(x + xModifier, y);
            if (currentNode.cost < 255 && neighbourNode.cost < 255)
            {
                length++;
            }
            else if(length > 0)
            {
                int midPoint = y - length + Mathf.FloorToInt((y - (y-length))/2);
                HierarchicalNode newHNode = new HierarchicalNode(x,midPoint, component);
                HierarchicalNode neighbourHNode = new HierarchicalNode(x + xModifier, midPoint, components.GetObject(component.indexX + xModifier, component.indexY));

                newHNode.AddNode(neighbourHNode, 1);
                neighbourHNode.AddNode(newHNode, 1);

                component.AddNode(newHNode);
                components.GetObject(component.indexX + xModifier, component.indexY).AddNode(neighbourHNode);

                length = 0;
            }
        }
        if (length > 0)
        {
            int midPoint = endY - length + Mathf.FloorToInt((endY - (endY - length)) / 2);
            HierarchicalNode newHNode = new HierarchicalNode(x, midPoint, component);
            HierarchicalNode neighbourHNode = new HierarchicalNode(x + xModifier, midPoint, components.GetObject(component.indexX + xModifier, component.indexY));

            newHNode.AddNode(neighbourHNode, 1);
            neighbourHNode.AddNode(newHNode, 1);

            component.AddNode(newHNode);
            components.GetObject(component.indexX + xModifier, component.indexY).AddNode(neighbourHNode);
        }
    }

    //checks a horizontal line of a component
    private void CheckHorizontal(Component component, int y, int yModifier)
    {
        int length = 0;
        int startX = component.x;
        int endX = component.x + componentWidth;
        for (int x = startX; x < endX; x++)
        {
            Node currentNode = tileMap.GetObject(x, y);
            Node neighbourNode = tileMap.GetObject(x, y + yModifier);
            if (currentNode.cost < 255 && neighbourNode.cost < 255)
            {
                length++;
            }
            else if(length > 0)
            {
                int midPoint = x - length + Mathf.FloorToInt((x - (x - length)) / 2);
                HierarchicalNode newHNode = new HierarchicalNode(midPoint, y, component);
                HierarchicalNode neighbourHNode = new HierarchicalNode(midPoint, y + yModifier, components.GetObject(component.indexX, component.indexY + yModifier));

                newHNode.AddNode(neighbourHNode, 1);
                neighbourHNode.AddNode(newHNode, 1);

                component.AddNode(newHNode);
                components.GetObject(component.indexX, component.indexY + yModifier).AddNode(neighbourHNode);
                length = 0;
            }
        }
        if (length > 0)
        {
            int midPoint = endX - length + Mathf.FloorToInt((endX - (endX - length)) / 2);
            HierarchicalNode newHNode = new HierarchicalNode(midPoint, y, component);
            HierarchicalNode neighbourHNode = new HierarchicalNode(midPoint, y + yModifier, components.GetObject(component.indexX, component.indexY + yModifier));

            newHNode.AddNode(neighbourHNode, 1);
            neighbourHNode.AddNode(newHNode, 1);

            component.AddNode(newHNode);
            components.GetObject(component.indexX, component.indexY + yModifier).AddNode(neighbourHNode);
        }
    }

    // a function which will calculate paths for a component
    private void CalculatePaths(Component component)
    {
        foreach(HierarchicalNode node in component.portalNodes)
        {
            TileGrid<int> integrationField = CreateIntegrationField(component, new Vector2(node.x * cellWidth, node.y * cellHeight));

            foreach(HierarchicalNode node_ in component.portalNodes)
            {
                if(node == node_)
                {
                    continue;
                }

                int weight = integrationField.GetObject(node_.x - component.x, node_.y - component.y);

                Debug.Log(weight);

                if (weight == -1)
                {
                    continue;
                }

                node.AddNode(node_, weight);
            }
        }
    }

    // a function which will clear all the paths on a component
    private void ClearPaths(Component component)
    {
        
    }


    public HierarchicalNode AddNodeToGraph(Vector2 position)
    {
        Component component = components.GetObject(position);

        if (component == null)
        {
            return null;
        }

        Debug.Log(component.x + ", " + component.y);

        TileGrid<int> integrationField = CreateIntegrationField(component, position);
        HierarchicalNode newNode = new HierarchicalNode(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), component);
        component.AddNode(newNode);

        foreach (HierarchicalNode node in component.portalNodes)
        {
            int weight = integrationField.GetObject(node.x - component.x, node.y - component.y);
            if (weight != -1)
            {
                node.connectedNodes.Add(newNode, weight);
            }
        }

        return newNode;
    }

    public void RemoveNodeFromGraph(HierarchicalNode node)
    {
        Component component = components.GetObject(new Vector2(node.x, node.y));
        component.RemoveNode(node);
    }


    /*
     * 
     *  Pathfinding Methods
     * 
     */


    // a function to find a high level path between 2 points
    public List<HierarchicalNode> FindHierarchicalPath(Vector2 startPos, HierarchicalNode destinationNode)
    {
        //an open and closed list to hold the nodes to be searched
        List<HierarchicalNode> openList = new List<HierarchicalNode>();
        List<HierarchicalNode> closedList = new List<HierarchicalNode>();

        Vector2 destinationPosition = new Vector2(destinationNode.x, destinationNode.y);

        //finds the nodes accessible to the unit
        Component startComponent = components.GetObject(startPos);
        TileGrid<int> integrationField = CreateIntegrationField(startComponent, startPos);

        foreach (HierarchicalNode node in startComponent.portalNodes)
        {
            int weight = integrationField.GetObject(node.x - startComponent.x, node.y - startComponent.y);
            if (weight != -1)
            {
                node.g = weight;
                node.h = CalculateH(new Vector2(node.x, node.y), destinationPosition);
                node.CalculateF();
                node.previousNode = null;
                openList.Add(node);
            }
        }

        while (openList.Count > 0)
        {
            //select the node with the lowest f value
            HierarchicalNode currentNode = openList[0];
            foreach(HierarchicalNode node in openList)
            {
                if(node.f < currentNode.f)
                {
                    currentNode = node;
                }
            }

            if(currentNode == destinationNode)
            {
                List<HierarchicalNode> output = new List<HierarchicalNode>();
                output.Add(currentNode);
                while(currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                return output;
            }

            foreach(HierarchicalNode neighbour in currentNode.connectedNodes.Keys)
            {
                if (!closedList.Contains(neighbour))
                {
                    if (openList.Contains(neighbour))
                    {
                        if(neighbour.g > currentNode.g + currentNode.connectedNodes[neighbour])
                        {
                            neighbour.g = currentNode.g + currentNode.connectedNodes[neighbour];
                            neighbour.CalculateF();
                            neighbour.previousNode = currentNode;
                        }
                    }
                    else
                    {
                        neighbour.g = currentNode.g + currentNode.connectedNodes[neighbour];
                        neighbour.h = CalculateH(new Vector2(neighbour.x, neighbour.y), destinationPosition);
                        neighbour.CalculateF();
                        neighbour.previousNode = currentNode;
                        openList.Add(neighbour);
                    }
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);
        }

        return null;
    }

    public List<HierarchicalNode> FindHierarchicalPathMerging(Vector2 startPos, HierarchicalNode destinationNode, List<HierarchicalNode> path)
    {

        //finds the nodes accessible to the unit
        Component startComponent = components.GetObject(startPos);

        if (path[0].component == startComponent)
        {
            return path;
        }

        TileGrid<int> integrationField = CreateIntegrationField(startComponent, startPos);
        //an open and closed list to hold the nodes to be searched
        List<HierarchicalNode> openList = new List<HierarchicalNode>();
        List<HierarchicalNode> closedList = new List<HierarchicalNode>();

        Vector2 destinationPosition = new Vector2(destinationNode.x, destinationNode.y);

        foreach (HierarchicalNode node in startComponent.portalNodes)
        {
            if (path.Contains(node))
            {
                return path;
            }

            int weight = integrationField.GetObject(node.x - startComponent.x, node.y - startComponent.y);
            if (weight != -1)
            {
                node.g = weight;
                node.h = CalculateH(new Vector2(node.x, node.y), destinationPosition);
                node.CalculateF();
                node.previousNode = null;
                openList.Add(node);
            }
        }

        while (openList.Count > 0)
        {
            //select the node with the lowest f value
            HierarchicalNode currentNode = openList[0];
            foreach (HierarchicalNode node in openList)
            {
                if (node.f < currentNode.f)
                {
                    currentNode = node;
                }
            }

            //the merging bit
            if (path.Contains(currentNode))
            {
                List<HierarchicalNode> output = path.GetRange(0, path.IndexOf(currentNode));
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                return output;
            }

            foreach (HierarchicalNode neighbour in currentNode.connectedNodes.Keys)
            {
                if (!closedList.Contains(neighbour))
                {
                    if (openList.Contains(neighbour))
                    {
                        if (neighbour.g > currentNode.g + currentNode.connectedNodes[neighbour])
                        {
                            neighbour.g = currentNode.g + currentNode.connectedNodes[neighbour];
                            neighbour.CalculateF();
                            neighbour.previousNode = currentNode;
                        }
                    }
                    else
                    {
                        neighbour.g = currentNode.g + currentNode.connectedNodes[neighbour];
                        neighbour.h = CalculateH(new Vector2(neighbour.x, neighbour.y), destinationPosition);
                        neighbour.CalculateF();
                        neighbour.previousNode = currentNode;
                        openList.Add(neighbour);
                    }
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);
        }

        return null;
    }

    public List<HierarchicalNode> FindHierarchicalPathMerging(Vector2 startPos, Vector2 destinationPosition, List<HierarchicalNode> path)
    {

        //finds the nodes accessible to the unit
        Component startComponent = components.GetObject(startPos);

        if (path[0].component == startComponent)
        {
            return path;
        }

        TileGrid<int> integrationField = CreateIntegrationField(startComponent, startPos);
        //an open and closed list to hold the nodes to be searched
        List<HierarchicalNode> openList = new List<HierarchicalNode>();
        List<HierarchicalNode> closedList = new List<HierarchicalNode>();

        foreach (HierarchicalNode node in startComponent.portalNodes)
        {
            if (path.Contains(node))
            {
                return path.GetRange(0, path.IndexOf(node)); ;
            }

            int weight = integrationField.GetObject(node.x - startComponent.x, node.y - startComponent.y);
            if (weight != -1)
            {
                node.g = weight;
                node.h = CalculateH(new Vector2(node.x, node.y), destinationPosition);
                node.CalculateF();
                node.previousNode = null;
                openList.Add(node);
            }
        }

        HierarchicalNode destinationNode = AddNodeToGraph(destinationPosition);
        Debug.Log("Temp destination added");

        while (openList.Count > 0)
        {
            //select the node with the lowest f value
            HierarchicalNode currentNode = openList[0];
            foreach (HierarchicalNode node in openList)
            {
                if (node.f < currentNode.f)
                {
                    currentNode = node;
                }
            }

            //the merging bit
            if (path.Contains(currentNode))
            {
                List<HierarchicalNode> output = path.GetRange(0, path.IndexOf(currentNode));
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                RemoveNodeFromGraph(destinationNode);
                Debug.Log("Temp destination removed");
                return output;
            }

            //if destination node is reached without merging then return the path anyway
            if(currentNode == destinationNode)
            {
                List<HierarchicalNode> output = new List<HierarchicalNode>();
                output.Add(currentNode);
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                RemoveNodeFromGraph(destinationNode);
                Debug.Log("Temp destination removed");
                return output;
            }

            foreach (HierarchicalNode neighbour in currentNode.connectedNodes.Keys)
            {
                if (!closedList.Contains(neighbour))
                {
                    if (openList.Contains(neighbour))
                    {
                        if (neighbour.g > currentNode.g + currentNode.connectedNodes[neighbour])
                        {
                            neighbour.g = currentNode.g + currentNode.connectedNodes[neighbour];
                            neighbour.CalculateF();
                            neighbour.previousNode = currentNode;
                        }
                    }
                    else
                    {
                        neighbour.g = currentNode.g + currentNode.connectedNodes[neighbour];
                        neighbour.h = CalculateH(new Vector2(neighbour.x, neighbour.y), destinationPosition);
                        neighbour.CalculateF();
                        neighbour.previousNode = currentNode;
                        openList.Add(neighbour);
                    }
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);
        }

        RemoveNodeFromGraph(destinationNode);
        Debug.Log("Temp destination removed");
        return null;
    }

    // a functtion to create an integration field
    private TileGrid<int> CreateIntegrationField(Component component, Vector2 destination)
    {
        //gets the component of the destination
        Component destinationComponent = components.GetObject(new Vector2(destination.x, destination.y));
        //sets the bounds for the integration field
        int minX = Mathf.Min(component.x, destinationComponent.x);
        int maxX = Mathf.Max(component.x, destinationComponent.x) + componentWidth;
        int minY = Mathf.Min(component.y, destinationComponent.y);
        int maxY = Mathf.Max(component.y, destinationComponent.y) + componentHeight;
        TileGrid<int> integrationField = new TileGrid<int>(maxX - minX, maxY - minY, cellHeight, cellWidth, new Vector2(minX, minY));

        //initial value of -1 for all positions
        for (int x = 0; x < maxX-minX; x++)
        {
            for (int y = 0; y < maxY-minY; y++)
            {
                integrationField.SetObject(x, y,-1);
            }
        }

        List<Vector2Int> openList = new List<Vector2Int>();
        List<Vector2Int> closedList = new List<Vector2Int>();

        //adds the destination position
        Vector2Int destinationIndex = new Vector2Int(Mathf.FloorToInt(destination.x/cellWidth), Mathf.FloorToInt(destination.y/cellHeight));
        openList.Add(destinationIndex);

        //sets the destinations cost to 0
        integrationField.SetObject(destination, 0);

        //checks all the nodes in the open list
        while (openList.Count > 0)
        {
            //gets the first value in the open list
            Vector2Int currentNode = openList[0];

            //adds this node to the closed list and removes it from the open list
            closedList.Add(currentNode);
            openList.Remove(currentNode);

            //gets a list of the nodes neighbours
            List<Vector2Int> neighbours = tileMap.GetCardinalNeighbours(currentNode.x, currentNode.y);

            //loops over all the nodes neighbours
            foreach (Vector2Int neighbourNode in neighbours)
            {
                if (neighbourNode.x >= maxX || neighbourNode.x < minX || neighbourNode.y >= maxY || neighbourNode.y < minY)
                {
                    continue;
                }
                if (tileMap.GetObject(neighbourNode.x, neighbourNode.y).cost == 255)
                {
                    continue;
                }
                if (closedList.Contains(neighbourNode))
                {
                    continue;
                }

                int currentNode_ = integrationField.GetObject(currentNode.x - minX, currentNode.y - minY);
                int neighbourNode_ = integrationField.GetObject(neighbourNode.x - minX, neighbourNode.y - minY);

                //if the cost of the neighbour is not set, or is less than the current
                if (currentNode_ + 1 < neighbourNode_ || neighbourNode_ == -1)
                {
                    neighbourNode_ = currentNode_ + 1;
                    integrationField.SetObject(neighbourNode.x - minX, neighbourNode.y - minY, neighbourNode_);
                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }

        return integrationField;
    }

    private TileGrid<Vector2> CreateFlowField(TileGrid<int> integrationField)
    {
        int width = integrationField.GetTileWidth();
        int height = integrationField.GetTileHeight();

        TileGrid<Vector2> flowField = new TileGrid<Vector2>(width, height, cellHeight, cellWidth, integrationField.GetStartPosition());

        //loops over the source component
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //if the point is unreachable then skip
                if(integrationField.GetObject(x, y) == -1)
                {
                    continue;
                }
                if(integrationField.GetObject(x,y) == 0)
                {
                    flowField.SetObject(x, y, new Vector2(2,2));
                    continue;
                }



                //get a list of neighbouring nodes
                List<Vector2Int> neighbourCosts = integrationField.GetNeighbours(x, y, -1);
                Vector2Int bestDirection = Vector2Int.zero;

                foreach(Vector2Int currentNeighbour in neighbourCosts)
                {
                    //if the cost of the currentNeighbour is less than the cost of the best direction
                    if(integrationField.GetObject(currentNeighbour.x, currentNeighbour.y) < integrationField.GetObject(bestDirection.x + x, bestDirection.y + y))
                    {
                        bestDirection = new Vector2Int(currentNeighbour.x - x, currentNeighbour.y - y);
                    }
                }
                //Debug.Log(bestDirection);
                flowField.SetObject(x, y, bestDirection);
            }
        }

        return flowField;
    }

    public TileGrid<Vector2> GetFlowField(Vector3 source, Vector2Int destination)
    {
        Component component = components.GetObject(source);
        if(!cachedFlowfields.ContainsKey((component, destination)))
        {
            TileGrid<int> intField = CreateIntegrationField(component, new Vector2(destination.x * cellWidth, destination.y * cellHeight));
            cachedFlowfields.Add((component, destination), CreateFlowField(intField));
        }
        return cachedFlowfields[(component, destination)];
    }

    //a function which calculates the h value
    private int CalculateH(Vector2 source, Vector2 destination)
    {
        //logic for calculating the heuristic
        return Mathf.FloorToInt(Mathf.Abs(source.x - destination.x)) + Mathf.FloorToInt(Mathf.Abs(source.y - destination.y));
    }


    /*
     * 
     * Debug
     * 
     */

    private void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        if (tileMap != null)
        {
            if (displayCost)
            {
                Gizmos.color = Color.white;
                for (int x = 0; x < width * componentWidth; x++)
                {
                    for (int y = 0; y < height * componentHeight; y++)
                    {
                        //cost
                        Handles.Label(tileMap.GetWorldPositionFromIndex(x, y) + new Vector2(cellWidth, cellHeight) / 2, tileMap.GetObject(x, y).cost.ToString(), style);
                    }
                }
            }

            if (showportals)
            {
                foreach (Component c_ in components.tileArray)
                {
                    List<HierarchicalNode> n = c_.portalNodes;
                    foreach (HierarchicalNode n_ in n)
                    {
                        Gizmos.color = new Color(0.41961f, 0.41961f, 0.41961f);
                        Gizmos.DrawCube(new Vector2(n_.x * cellWidth, n_.y * cellHeight) + new Vector2(cellWidth, cellHeight) / 2, new Vector2(cellWidth, cellHeight));
                        //Gizmos.color = Color.white;
                        foreach (HierarchicalNode n__ in n_.connectedNodes.Keys)
                        {
                            Gizmos.DrawLine(new Vector2(n_.x * cellWidth, n_.y * cellHeight) + new Vector2(cellWidth, cellHeight) / 2, new Vector2(n__.x * cellWidth, n__.y * cellHeight) + new Vector2(cellWidth, cellHeight) / 2);
                        }
                    }
                }
            }

            if (showGrid)
            {
                Gizmos.color = Color.gray;

                foreach(Node n in tileMap.tileArray)
                {
                    Gizmos.DrawWireCube(new Vector3(n.x * cellWidth, n.y * cellHeight) + new Vector3(cellWidth, cellHeight) / 2, new Vector3(cellWidth, cellHeight));
                }
            }

            if (showComponents)
            {
                Gizmos.color = Color.black;

                foreach(Component c in components.tileArray)
                {
                    Gizmos.DrawWireCube(new Vector2(c.indexX * componentWidth * cellWidth, c.indexY * componentHeight * cellHeight) + new Vector2(componentWidth * cellWidth, componentHeight * cellHeight)/2, new Vector2(componentWidth * cellWidth, componentHeight * cellHeight));
                }
            }
        }
    }
}
