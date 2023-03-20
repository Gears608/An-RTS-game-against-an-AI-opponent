using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldController : MonoBehaviour
{
    public TileGrid<Node> tileMap;
    public int width;
    public int height;

    public float tileSize;

    public Vector3 startPos;

    public TileGrid<Component> components;

    public int componentWidth;
    public int componentHeight;

    public int terrainMask;

    private float ymod = 4f;

    private Dictionary<(Component, Vector2Int), TileGrid<Vector2>> cachedFlowfields;

    public bool displayCost = false;
    public bool showportals = false;
    public bool showComponents = false;
    public bool showGrid = false;

    public int maxCachedComponents;

    [SerializeField]
    private Tilemap walls;

    private void Start()
    {
        tileMap = new TileGrid<Node>(width * componentWidth, height * componentHeight, tileSize, tileSize, new Vector2((componentHeight * tileSize * height) / 2f, 0));
        terrainMask = LayerMask.GetMask("Impassable");

        components = new TileGrid<Component>(width, height, componentHeight*tileSize, componentWidth*tileSize, new Vector2((componentHeight * tileSize * height) / 2f, /*(componentHeight * tileSize / ymod)*/0));
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
                Vector2 pos = new Vector2(componentHeight * tileSize * height / 2f + ((x * componentWidth * tileSize - y * componentHeight * tileSize) / 2f), ((x * componentWidth * tileSize + y * componentHeight * tileSize) / ymod));
                components.SetObject(x, y, new Component(pos.x, pos.y, x, y));
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
                CheckVertical(component, new Vector2(component.x, component.y + (componentHeight * tileSize/2f) - tileSize/2f), new Vector2(tileSize/2f, tileSize/4f));
            }
            if(component.indexY < height - 1)
            {
                Debug.Log("Checking horizontal");
                CheckHorizontal(component, new Vector2(component.x - (componentWidth * tileSize / 2f) + tileSize/2f, component.y + componentHeight * tileSize / 4f), new Vector2(-tileSize / 2f, tileSize / 4f));
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
        int maxX = component.indexX*componentWidth + componentWidth;
        int maxY = component.indexY*componentWidth + componentHeight;

        for (int x = component.indexX*componentWidth; x < maxX; x++)
        {
            for (int y = component.indexY*componentHeight; y < maxY; y++)
            {
                Vector3Int pos = walls.LocalToCell(tileMap.GetWorldPositionFromIndex(x, y));
                if(walls.GetTile(pos) != null)
                {
                    tileMap.GetObject(x, y).cost = 255;
                }
            }
        }
    }

    //checks a vertical line of a component
    private void CheckVertical(Component component, Vector2 startPos, Vector2 modifier)
    {
        int length = 0;
        Vector2 currentPos = startPos;
        //Debug.Log("starting at: " + startPos);
        for (int x = 0; x < componentHeight-1; x++)
        {
            //Debug.Log("Checking: "+ currentPos);
            Node currentNode = tileMap.GetObject(currentPos);
            Node neighbourNode = tileMap.GetObject(currentPos + modifier);
            //Debug.Log(currentPos);
            if (currentNode.cost < 255 && neighbourNode.cost < 255)
            {
                length++;
            }
            else if (length > 0)
            {
                //Debug.Log("midpoint = " + currentPos + " - " + (new Vector2(-tileSize / 2f, tileSize / 4f) * length / 2f));
                Vector2 midPoint = currentPos + (new Vector2(-tileSize / 2f, tileSize / 4f) * length/2f);
                Vector2Int midPointInt = tileMap.GetIndexFromWorldPosition(midPoint);
                HierarchicalNode newHNode = new HierarchicalNode(midPointInt.x, midPointInt.y, component);
                Vector2Int temp = tileMap.GetIndexFromWorldPosition(midPoint + modifier);
                HierarchicalNode neighbourHNode = new HierarchicalNode(temp.x, temp.y, components.GetObject(currentPos + modifier));

                newHNode.AddNode(neighbourHNode, 1);
                neighbourHNode.AddNode(newHNode, 1);
                //Debug.Log("new node at: " + midPoint.x + ", " + midPoint.y);
                component.AddNode(newHNode);
                components.GetObject(currentPos + modifier).AddNode(neighbourHNode);
                length = 0;
            }

            currentPos += new Vector2(tileSize / 2f, -tileSize / 4f);
        }
        if (length > 0)
        {
            //Debug.Log("midpoint = " + currentPos + " - " + (new Vector2(-tileSize / 2f, tileSize / 4f) * length / 2f));
            Vector2 midPoint = currentPos + (new Vector2(-tileSize / 2f, tileSize / 4f) * length / 2f);
            Vector2Int midPointInt = tileMap.GetIndexFromWorldPosition(midPoint);
            HierarchicalNode newHNode = new HierarchicalNode(midPointInt.x, midPointInt.y, component);
            Vector2Int temp = tileMap.GetIndexFromWorldPosition(midPoint + modifier);
            HierarchicalNode neighbourHNode = new HierarchicalNode(temp.x, temp.y, components.GetObject(currentPos + modifier));

            newHNode.AddNode(neighbourHNode, 1);
            neighbourHNode.AddNode(newHNode, 1);
            //Debug.Log("new node at: " + midPoint.x + ", " + midPoint.y);
            component.AddNode(newHNode);
            //Debug.Log(currentPos + modifier);
            //Debug.Log(components.GetIndexFromWorldPosition(currentPos + modifier));
            components.GetObject(currentPos + modifier).AddNode(neighbourHNode);
        }
    }

    //checks a horizontal line of a component
    private void CheckHorizontal(Component component, Vector2 startPos, Vector2 modifier)
    {
        int length = 0;
        Vector2 currentPos = startPos;
        for (int x = 0; x < componentWidth-1; x++)
        {
            //Debug.Log("Checking: "+ currentPos);
            Node currentNode = tileMap.GetObject(currentPos);
            Node neighbourNode = tileMap.GetObject(currentPos + modifier);
            if (currentNode.cost < 255 && neighbourNode.cost < 255)
            {
                length++;
            }
            else if (length > 0)
            {
                //Debug.Log("midpoint = " +currentPos+" - " + (new Vector2(-tileSize / 2f, -tileSize / 4f) * length / 2f));
                Vector2 midPoint = currentPos + (new Vector2(-tileSize / 2f, -tileSize / 4f) * length/2f);
                Vector2Int midPointInt = tileMap.GetIndexFromWorldPosition(midPoint);
                HierarchicalNode newHNode = new HierarchicalNode(midPointInt.x, midPointInt.y, component);
                Vector2Int temp = tileMap.GetIndexFromWorldPosition(midPoint + modifier);
                HierarchicalNode neighbourHNode = new HierarchicalNode(temp.x, temp.y, components.GetObject(currentPos + modifier));

                newHNode.AddNode(neighbourHNode, 1);
                neighbourHNode.AddNode(newHNode, 1);

                //Debug.Log("new node at: " + midPoint.x + ", " + midPoint.y);
                component.AddNode(newHNode);
                components.GetObject(currentPos + modifier).AddNode(neighbourHNode);
                length = 0;
            }

            currentPos += new Vector2(tileSize / 2f, tileSize / 4f);
        }
        if (length > 0)
        {
            //Debug.Log("midpoint = " + currentPos + " - " + (new Vector2(-tileSize / 2f, -tileSize / 4f) * length / 2f));
            Vector2 midPoint = currentPos + (new Vector2(-tileSize / 2f, -tileSize / 4f) * length / 2f);
            Vector2Int midPointInt = tileMap.GetIndexFromWorldPosition(midPoint);
            HierarchicalNode newHNode = new HierarchicalNode(midPointInt.x, midPointInt.y, component);
            Vector2Int temp = tileMap.GetIndexFromWorldPosition(midPoint + modifier);
            HierarchicalNode neighbourHNode = new HierarchicalNode(temp.x, temp.y, components.GetObject(currentPos + modifier));

            newHNode.AddNode(neighbourHNode, 1);
            neighbourHNode.AddNode(newHNode, 1);
            //Debug.Log("new node at: " + midPoint.x + ", " + midPoint.y);
            component.AddNode(newHNode);
            components.GetObject(currentPos + modifier).AddNode(neighbourHNode);
        }
    }

    // a function which will calculate paths for a component
    private void CalculatePaths(Component component)
    {
        //Debug.Log(component.portalNodes.Count);
        foreach(HierarchicalNode node in component.portalNodes)
        {
            //Debug.Log(node.x +", "+node.y);
            TileGrid<int> integrationField = CreateIntegrationField(component, tileMap.GetWorldPositionFromIndex(node.x, node.y));

            foreach(HierarchicalNode node_ in component.portalNodes)
            {
                if(node == node_)
                {
                    continue;
                }

                int weight = integrationField.GetObject(tileMap.GetWorldPositionFromIndex(node_.x, node_.y));

                if (weight == -1)
                {
                    continue;
                }

                node.AddNode(node_, weight);
            }
        }
    }

    public Node GetNode(Vector2 position)
    {
        return tileMap.GetObject(position);
    }

    public Component GetComponent(Vector2 position) 
    {
        return components.GetObject(position);
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

        //Debug.Log(component.x + ", " + component.y);

        TileGrid<int> integrationField = CreateIntegrationField(component, position);
        Vector2Int temp = tileMap.GetIndexFromWorldPosition(position);
        HierarchicalNode newNode = new HierarchicalNode(temp.x, temp.y, component);
        component.AddNode(newNode);

        foreach (HierarchicalNode node in component.portalNodes)
        {
            int weight = integrationField.GetObject(tileMap.GetWorldPositionFromIndex(node.x, node.y));
            if (weight != -1)
            {
                //Debug.Log(node.x + ", " + node.y + " Connected to " + newNode.x + ", " + newNode.y);
                node.connectedNodes.Add(newNode, weight);
            }
        }

        return newNode;
    }

    public void RemoveNodeFromGraph(HierarchicalNode node)
    {
        Component component = components.GetObject(tileMap.GetWorldPositionFromIndex(node.x, node.y));
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

        Vector2 destinationPosition = tileMap.GetWorldPositionFromIndex(destinationNode.x, destinationNode.y);

        //finds the nodes accessible to the unit
        Component startComponent = components.GetObject(startPos);
        TileGrid<int> integrationField = CreateIntegrationField(startComponent, startPos);

        foreach (HierarchicalNode node in startComponent.portalNodes)
        {
            int weight = integrationField.GetObject(new Vector2(node.x, node.y) * tileSize);
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

            int weight = integrationField.GetObject(new Vector2(node.x, node.y) * tileSize);
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
                Debug.Log("Merging Path Found");
                List<HierarchicalNode> output = path.GetRange(0, path.IndexOf(currentNode));
                output.Add(currentNode);
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

            int weight = integrationField.GetObject(new Vector2(node.x, node.y) * tileSize);
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
        Debug.Log("Temp destination added: "+destinationNode.x +", "+destinationNode.y);

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
                Debug.Log("Merging Path Found");
                List<HierarchicalNode> output = path.GetRange(0, path.IndexOf(currentNode));
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                RemoveNodeFromGraph(destinationNode);
                Debug.Log("Temp destination removed: " + destinationNode.x + ", " + destinationNode.y);
                return output;
            }

            //if destination node is reached without merging then return the path anyway
            if(currentNode == destinationNode)
            {
                Debug.Log("Alternate Path Found");
                List<HierarchicalNode> output = new List<HierarchicalNode>();
                output.Add(currentNode);
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                RemoveNodeFromGraph(destinationNode);
                Debug.Log("Temp destination removed: " + destinationNode.x + ", " + destinationNode.y);
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
        //Debug.Log(destination);
        //gets the component of the destination
        Component destinationComponent = components.GetObject(new Vector2(destination.x, destination.y));
        //sets the bounds for the integration field
        int minX = Mathf.Min(component.indexX, destinationComponent.indexX)* componentWidth;
        int maxX = Mathf.Max(component.indexX, destinationComponent.indexX)* componentWidth + componentWidth;
        int minY = Mathf.Min(component.indexY, destinationComponent.indexY)*componentHeight;
        int maxY = Mathf.Max(component.indexY, destinationComponent.indexY)*componentHeight + componentHeight;
        //Debug.Log("Integration field starting pos: "+ tileMap.GetWorldPositionFromIndex(minX, minY) +", Width: "+(maxX - minX)+", Height: "+(maxY-minY));
        TileGrid<int> integrationField = new TileGrid<int>(maxX - minX, maxY - minY, tileSize, tileSize, tileMap.GetWorldPositionFromIndex(minX, minY));

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
        Vector2Int destinationIndex = tileMap.GetIndexFromWorldPosition(destination);
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
            //Debug.Log("Current Node Position: " + currentNode.x + ", " + currentNode.y);
            //loops over all the nodes neighbours
            foreach (Vector2Int neighbourNode in neighbours)
            {
                //Debug.Log("Neighbour Node Position: "+neighbourNode.x+", "+neighbourNode.y);
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

                int currentNode_ = integrationField.GetObject(tileMap.GetWorldPositionFromIndex(currentNode.x, currentNode.y));
                int neighbourNode_ = integrationField.GetObject(tileMap.GetWorldPositionFromIndex(neighbourNode.x, neighbourNode.y));

                //Debug.Log(currentNode_ +", "+neighbourNode_);

                //if the cost of the neighbour is not set, or is less than the current
                if (currentNode_ + 1 < neighbourNode_ || neighbourNode_ == -1)
                {
                    neighbourNode_ = currentNode_ + 1;
                    integrationField.SetObject(tileMap.GetWorldPositionFromIndex(neighbourNode.x, neighbourNode.y), neighbourNode_);
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

        TileGrid<Vector2> flowField = new TileGrid<Vector2>(width, height, tileSize, tileSize, integrationField.GetStartPosition());

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
                flowField.SetObject(x, y, flowField.GetWorldPositionFromIndex(x + bestDirection.x, y + bestDirection.y) - flowField.GetWorldPositionFromIndex(x,y));
            }
        }

        return flowField;
    }

    public (TileGrid<Vector2>, TileGrid<int>) GetFlowField(Vector3 source, Vector2Int destination)
    {
        Component component = components.GetObject(source);
        //Debug.Log("Source: "+component.indexX+", "+component.indexY);
        TileGrid<int> intField = CreateIntegrationField(component, tileMap.GetWorldPositionFromIndex(destination.x, destination.y));
        if(!cachedFlowfields.ContainsKey((component, destination)))
        {
            cachedFlowfields.Add((component, destination), CreateFlowField(intField));
        }
        return (cachedFlowfields[(component, destination)], intField);
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
                        Handles.Label(tileMap.GetWorldPositionFromIndex(x, y) + new Vector2(0, tileSize/4f), tileMap.GetObject(x, y).cost.ToString(), style);
                    }
                }
            }

            if (showportals)
            {
                Gizmos.matrix = Matrix4x4.Translate(new Vector2((componentHeight * tileSize * height) / 2f, (tileSize / ymod)));
                foreach (Component c_ in components.tileArray)
                {
                    List<HierarchicalNode> n = c_.portalNodes;
                    foreach (HierarchicalNode n_ in n)
                    {
                        Gizmos.color = Color.red;

                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) - (tileSize / 2f), (n_.x + n_.y) * tileSize / ymod), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / ymod) + (tileSize / ymod)));
                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) + (tileSize / 2f), (n_.x + n_.y) * tileSize / ymod), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / ymod) + (tileSize / ymod)));
                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) - (tileSize / 2f), (n_.x + n_.y) * tileSize / ymod), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / ymod) - (tileSize / ymod)));
                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) + (tileSize / 2f), (n_.x + n_.y) * tileSize / ymod), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / ymod) - (tileSize / ymod)));

                        //Gizmos.color = Color.white;
                        foreach (HierarchicalNode n__ in n_.connectedNodes.Keys)
                        {
                            Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f), (n_.x + n_.y) * tileSize / ymod), new Vector2((n__.x - n__.y) * tileSize / 2f, (n__.x + n__.y) * tileSize / ymod));
                            Handles.Label(Vector2.Lerp(new Vector2(((n_.x - n_.y) * tileSize / 2f), (n_.x + n_.y) * tileSize / ymod) + tileMap.GetStartPosition(), new Vector2((n__.x - n__.y) * tileSize / 2f, (n__.x + n__.y) * tileSize / ymod) + tileMap.GetStartPosition(), 0.5f), n_.connectedNodes[n__].ToString());
                        }
                    }
                }
                Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, 0));
            }

            if (showGrid)
            {
                Gizmos.color = Color.gray;
                Gizmos.matrix = Matrix4x4.Translate(new Vector2((componentHeight * tileSize * height )/2f, (tileSize/ymod)));
                foreach (Node n in tileMap.tileArray)
                {
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) - (tileSize / 2f), (n.x + n.y) * tileSize / ymod), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / ymod) + (tileSize / ymod)));
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) + (tileSize / 2f), (n.x + n.y) * tileSize / ymod), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / ymod) + (tileSize / ymod)));
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) - (tileSize / 2f), (n.x + n.y) * tileSize / ymod), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / ymod) - (tileSize / ymod)));
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) + (tileSize / 2f), (n.x + n.y) * tileSize / ymod), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / ymod) - (tileSize / ymod)));

                    //Handles.Label(new Vector2((componentHeight * tileHeight * height * (tileHeight / 2f)) + ((n.x * tileWidth - n.y * tileHeight) / 2f), ((n.x * tileWidth + n.y * tileHeight) / ymod) + (tileHeight/ymod)), n.x + ", " + n.y);
                }
                Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, 0));
            }

            if (showComponents)
            {
                Gizmos.color = Color.black;
                Gizmos.matrix = Matrix4x4.Translate(new Vector2((componentHeight * tileSize * height)/2f, (componentHeight * tileSize / ymod)));
                foreach (Component c in components.tileArray)
                {
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) - (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod) + (componentHeight * tileSize / ymod)));
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) + (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod) + (componentHeight * tileSize / ymod)));
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) - (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod) - (componentHeight * tileSize / ymod)));
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) + (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod) - (componentHeight * tileSize / ymod)));

                    //Handles.Label(new Vector2(componentHeight * tileSize * height/2f + ((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f), ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / ymod)), c.indexX + ", " + c.indexY);

                    //Gizmos.DrawWireCube(new Vector2(c.indexX * componentWidth * tileWidth, c.indexY * componentHeight * tileHeight) + new Vector2(componentWidth * tileWidth, componentHeight * tileHeight)/2, new Vector2(componentWidth * tileWidth, componentHeight * tileHeight));
                }
                Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, 0));
            }
        }
    }
}
