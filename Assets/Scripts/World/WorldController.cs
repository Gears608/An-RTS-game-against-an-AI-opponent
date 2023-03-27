using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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

    private Dictionary<(Component, Vector2Int), TileGrid<Vector2>> cachedFlowfields;

    [SerializeField] private bool displayCost = false;
    [SerializeField] private bool showportals = false;
    [SerializeField] private bool showComponents = false;
    [SerializeField] private bool showGrid = false;

    [SerializeField]
    private int maxCachedComponents;

    [SerializeField]
    private List<UnitClass> allUnits;

    private void Start()
    {
        allUnits = new List<UnitClass>();
        GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("PlayerUnit");
        foreach(GameObject unit in foundObjects)
        {
            allUnits.Add(unit.GetComponent<UnitClass>());
        }

        tileMap = new TileGrid<Node>(width * componentWidth, height * componentHeight, tileSize, tileSize, new Vector2((componentHeight * tileSize * height) / 2f, 0));
        terrainMask = LayerMask.GetMask("Impassable");

        components = new TileGrid<Component>(width, height, componentHeight*tileSize, componentWidth*tileSize, new Vector2((componentHeight * tileSize * height) / 2f, /*(componentHeight * tileSize / 4f)*/0));
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
                Vector2 pos = new Vector2(componentHeight * tileSize * height / 2f + ((x * componentWidth * tileSize - y * componentHeight * tileSize) / 2f), ((x * componentWidth * tileSize + y * componentHeight * tileSize) / 4f));
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
                // tileMap.GetIndexFromWorldPosition(new Vector2(component.x, component.y + (componentHeight * tileSize/2f) - tileSize/2f))
                CheckVertical(component, new Vector2Int((component.indexX * componentWidth) + componentWidth - 1, component.indexY * componentHeight), 1);
            }
            if(component.indexY < height - 1)
            {
                Debug.Log("Checking horizontal");
                CheckHorizontal(component, new Vector2Int(component.indexX * componentWidth, (component.indexY * componentHeight) + componentHeight - 1), 1);
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

    public void UpdateNodes(Vector2 position)
    {
        Component component = components.GetObject(position);
        Vector2Int positionAsIndex = tileMap.GetIndexFromWorldPosition(position);

        UpdateComponent(component);

        Debug.Log("Checking edges");
        if (positionAsIndex.x == ((component.indexX + 1) * componentWidth) - 1 && component.indexX < width - 1)
        {
            Component neighbourComponent = components.GetObject(component.indexX + 1, component.indexY);
            RemoveConnectingNodes(component, neighbourComponent);
            ClearInternalPaths(neighbourComponent);
            CheckVertical(component, new Vector2Int((component.indexX * componentWidth) + componentWidth - 1, component.indexY * componentHeight), 1);
            CalculatePaths(neighbourComponent);
        }
        if (positionAsIndex.y == ((component.indexY + 1) * componentHeight) - 1 && component.indexY < height - 1)
        {
            Component neighbourComponent = components.GetObject(component.indexX, component.indexY + 1);
            RemoveConnectingNodes(component, neighbourComponent);
            ClearInternalPaths(neighbourComponent);
            CheckHorizontal(component, new Vector2Int(component.indexX * componentWidth, (component.indexY * componentHeight) + componentHeight - 1), 1);
            CalculatePaths(neighbourComponent);
        }
        if (positionAsIndex.y == component.indexY * componentHeight && component.indexY > 0)
        {
            Component neighbourComponent = components.GetObject(component.indexX, component.indexY - 1);
            RemoveConnectingNodes(component, neighbourComponent);
            ClearInternalPaths(neighbourComponent);
            CheckHorizontal(component, new Vector2Int(component.indexX * componentWidth, component.indexY * componentHeight), -1);
            CalculatePaths(neighbourComponent);
        }
        if (positionAsIndex.x == component.indexX * componentWidth && component.indexX > 0)
        {
            Component neighbourComponent = components.GetObject(component.indexX - 1, component.indexY);
            RemoveConnectingNodes(component, neighbourComponent);
            ClearInternalPaths(neighbourComponent);
            CheckVertical(component, new Vector2Int(component.indexX * componentWidth, component.indexY * componentHeight), -1);
            CalculatePaths(neighbourComponent);
        }

        Debug.Log("calculating new paths");
        ClearInternalPaths(component);
        CalculatePaths(component);
    }

    public void UpdateNodes(List<Vector2Int> positionsAsIndex)
    {
        int maxY = positionsAsIndex[0].y;
        int maxX = positionsAsIndex[0].x;
        int minY = positionsAsIndex[0].y;
        int minX = positionsAsIndex[0].x;

        List<Component> componentsToUpdate = new List<Component>();
        foreach(Vector2Int position in positionsAsIndex)
        {
            Component current = components.GetObject(Mathf.FloorToInt(position.x / componentWidth), Mathf.FloorToInt(position.y / componentHeight));
            if (!componentsToUpdate.Contains(current))
            {
                componentsToUpdate.Add(current);
            }

            if(position.x < minX)
            {
                minX = position.x;
            }
            if(position.x > maxX)
            {
                maxX = position.x;
            }
            if (position.y > maxY)
            {
                maxY = position.y;
            }
            if (position.x < minY)
            {
                minY = position.y;
            }
        }

        foreach (Component component in componentsToUpdate)
        {
            //UpdateComponent(component);


            Debug.Log("Checking edges");
            if (maxX >= ((component.indexX + 1) * componentWidth) - 1 && component.indexX < width - 1)
            {
                Component neighbourComponent = components.GetObject(component.indexX + 1, component.indexY);
                RemoveConnectingNodes(component, neighbourComponent);
                ClearInternalPaths(neighbourComponent);
                CheckVertical(component, new Vector2Int((component.indexX * componentWidth) + componentWidth - 1, component.indexY * componentHeight), 1);
                CalculatePaths(neighbourComponent);
            }
            if (maxY >= ((component.indexY + 1) * componentHeight) - 1 && component.indexY < height - 1)
            {
                Component neighbourComponent = components.GetObject(component.indexX, component.indexY + 1);
                RemoveConnectingNodes(component, neighbourComponent);
                ClearInternalPaths(neighbourComponent);
                CheckHorizontal(component, new Vector2Int(component.indexX * componentWidth, (component.indexY * componentHeight) + componentHeight - 1), 1);
                CalculatePaths(neighbourComponent);
            }
            if (minY <= component.indexY * componentHeight && component.indexY > 0)
            {
                Component neighbourComponent = components.GetObject(component.indexX, component.indexY - 1);
                RemoveConnectingNodes(component, neighbourComponent);
                ClearInternalPaths(neighbourComponent);
                CheckHorizontal(component, new Vector2Int(component.indexX * componentWidth, component.indexY * componentHeight), -1);
                CalculatePaths(neighbourComponent);
            }
            if (minX <= component.indexX * componentWidth && component.indexX > 0)
            {
                Component neighbourComponent = components.GetObject(component.indexX - 1, component.indexY);
                RemoveConnectingNodes(component, neighbourComponent);
                ClearInternalPaths(neighbourComponent);
                CheckVertical(component, new Vector2Int(component.indexX * componentWidth, component.indexY * componentHeight), -1);
                CalculatePaths(neighbourComponent);
            }

            Debug.Log("calculating new paths");
            ClearInternalPaths(component);
            CalculatePaths(component);
        }
    }

    //a method which sets nodes as walkable or not
    public void UpdateComponent(Component component)
    {
        Debug.Log("updating component: "+component.indexX+", "+ component.indexY);
        int maxX = component.indexX*componentWidth + componentWidth;
        int maxY = component.indexY*componentWidth + componentHeight;

        for (int x = component.indexX*componentWidth; x < maxX; x++)
        {
            for (int y = component.indexY*componentHeight; y < maxY; y++)
            {
                Vector2 position = tileMap.GetWorldPositionFromIndex(x, y);
                if (Physics2D.OverlapPointAll(position + new Vector2(0, tileSize / 4f), terrainMask, 1f, -1f).Length > 0)
                {
                    //Debug.Log("Updated cost");
                    tileMap.GetObject(x, y).cost = 255;
                }
            }
        }
    }

    //checks a vertical line of a component
    private void CheckVertical(Component component, Vector2Int startPosIndex, int xModifier)
    {
        int length = 0;
        for (int y = startPosIndex.y; y < startPosIndex.y + componentHeight; y++)
        {
            Node currentNode = tileMap.GetObject(startPosIndex.x, y);
            Node neighbourNode = tileMap.GetObject(startPosIndex.x + xModifier, y);

            if (currentNode.cost < 255 && neighbourNode.cost < 255)
            {
                length++;
            }
            else if (length > 0)
            {
                int midPointY = y - Mathf.FloorToInt(length/2f) - 1;
                HierarchicalNode newHNode = new HierarchicalNode(startPosIndex.x, midPointY, component);
                HierarchicalNode neighbourHNode = new HierarchicalNode(startPosIndex.x + xModifier, midPointY, components.GetObject(component.indexX + xModifier, component.indexY));

                newHNode.AddNode(neighbourHNode, 1);
                neighbourHNode.AddNode(newHNode, 1);
                
                component.AddNode(newHNode);
                neighbourHNode.component.AddNode(neighbourHNode);
                length = 0;
            }
        }
        if (length > 0)
        {
            int midPointY = startPosIndex.y + componentHeight - 1 - Mathf.FloorToInt(length / 2f);
            Debug.Log(midPointY);
            HierarchicalNode newHNode = new HierarchicalNode(startPosIndex.x, midPointY, component);
            HierarchicalNode neighbourHNode = new HierarchicalNode(startPosIndex.x + xModifier, midPointY, components.GetObject(component.indexX + xModifier, component.indexY));

            newHNode.AddNode(neighbourHNode, 1);
            neighbourHNode.AddNode(newHNode, 1);

            component.AddNode(newHNode);
            neighbourHNode.component.AddNode(neighbourHNode);
        }
    }

    //checks a horizontal line of a component
    private void CheckHorizontal(Component component, Vector2Int startPosIndex, int yModifier)
    {
        int length = 0;
        for (int x = startPosIndex.x; x < startPosIndex.x + componentWidth; x++)
        {
            Node currentNode = tileMap.GetObject(x, startPosIndex.y);
            Node neighbourNode = tileMap.GetObject(x,  startPosIndex.y + yModifier);

            if (currentNode.cost < 255 && neighbourNode.cost < 255)
            {
                length++;
            }
            else if (length > 0)
            {
                int midPointX = x - Mathf.FloorToInt(length/2f) - 1;
                HierarchicalNode newHNode = new HierarchicalNode(midPointX, startPosIndex.y, component);
                HierarchicalNode neighbourHNode = new HierarchicalNode(midPointX, startPosIndex.y + yModifier, components.GetObject(component.indexX, component.indexY + yModifier));

                newHNode.AddNode(neighbourHNode, 1);
                neighbourHNode.AddNode(newHNode, 1);

                component.AddNode(newHNode);
                neighbourHNode.component.AddNode(neighbourHNode);
                length = 0;
            }
        }
        if (length > 0)
        {
            int midPointX = startPosIndex.x + componentWidth - 1 - Mathf.FloorToInt(length / 2f);
            HierarchicalNode newHNode = new HierarchicalNode(midPointX, startPosIndex.y, component);
            HierarchicalNode neighbourHNode = new HierarchicalNode(midPointX, startPosIndex.y + yModifier, components.GetObject(component.indexX, component.indexY + yModifier));

            newHNode.AddNode(neighbourHNode, 1);
            neighbourHNode.AddNode(newHNode, 1);

            component.AddNode(newHNode);
            neighbourHNode.component.AddNode(neighbourHNode);
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
    private void ClearInternalPaths(Component component)
    {
        foreach (HierarchicalNode node in component.portalNodes)
        {
            foreach(HierarchicalNode node_ in component.portalNodes)
            {
                node.RemoveNode(node_);
            }
        }
    }


    public HierarchicalNode AddNodeToGraph(Vector2 position)
    {
        Component component = components.GetObject(position);

        if (component == null)
        {
            return null;
        }

        TileGrid<int> integrationField = CreateIntegrationField(component, position);
        Vector2Int temp = tileMap.GetIndexFromWorldPosition(position);
        HierarchicalNode newNode = new HierarchicalNode(temp.x, temp.y, component);
        component.AddNode(newNode);

        foreach (HierarchicalNode node in component.portalNodes)
        {
            int weight = integrationField.GetObject(tileMap.GetWorldPositionFromIndex(node.x, node.y));
            if (weight != -1)
            {
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

    public void RemoveConnectingNodes(Component component1, Component component2)
    {
        List<HierarchicalNode> nodesToRemove = new List<HierarchicalNode>();
        foreach(HierarchicalNode node in component1.portalNodes)
        {
            foreach(HierarchicalNode node_ in node.connectedNodes.Keys)
            {
                if(node_.component == component2)
                {
                    nodesToRemove.Add(node);
                    component2.RemoveNode(node_);
                }
            }
        }

        foreach(HierarchicalNode node in nodesToRemove)
        {
            component1.RemoveNode(node);
        }
    }

    public bool CheckBuildingPlacement(Vector2 position, GameObject buildingPrefab)
    {
        Vector2Int positionIndex = tileMap.GetIndexFromWorldPosition(position);
        Building building = buildingPrefab.GetComponent<Building>();

        if(positionIndex.x > width * componentWidth || positionIndex.x < 0 || positionIndex.y > height * componentHeight || positionIndex.y < 0)
        {
            return false;
        }

        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector2Int pos = new Vector2Int(positionIndex.x + x, positionIndex.y + y);
                if(tileMap.GetObject(pos.x, pos.y).cost == 255)
                {
                    return false;
                }
            }
        }
        //Debug.Log("cannot place");
        return true;
    }

    public void PlaceBuilding(Vector2 position, GameObject buildingPrefab)
    {
        Vector2Int positionIndex = tileMap.GetIndexFromWorldPosition(position);
        Vector2 gridPosition = tileMap.GetWorldPositionFromIndex(positionIndex.x, positionIndex.y);

        Instantiate(buildingPrefab, gridPosition, buildingPrefab.transform.rotation);

        Building building = buildingPrefab.GetComponent<Building>();

        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector2Int pos = new Vector2Int(positionIndex.x + x, positionIndex.y + y);
                positions.Add(pos);
                tileMap.GetObject(pos.x, pos.y).cost = 255;
            }
        }

        UpdateNodes(positions);
    }

    public Vector2 WorldToGridPosition(Vector2 position)
    {
        Vector2Int index = tileMap.GetIndexFromWorldPosition(position);
        Vector2 output = tileMap.GetWorldPositionFromIndex(index.x, index.y);
        return output;
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
        //gets the component of the destination
        Component destinationComponent = components.GetObject(new Vector2(destination.x, destination.y));
        //sets the bounds for the integration field
        int minX = Mathf.Min(component.indexX, destinationComponent.indexX)* componentWidth;
        int maxX = Mathf.Max(component.indexX, destinationComponent.indexX)* componentWidth + componentWidth;
        int minY = Mathf.Min(component.indexY, destinationComponent.indexY)*componentHeight;
        int maxY = Mathf.Max(component.indexY, destinationComponent.indexY)*componentHeight + componentHeight;

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

                int currentNode_ = integrationField.GetObject(tileMap.GetWorldPositionFromIndex(currentNode.x, currentNode.y));
                int neighbourNode_ = integrationField.GetObject(tileMap.GetWorldPositionFromIndex(neighbourNode.x, neighbourNode.y));

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

    /*
     *  A function which converts a TileGrid of ints to a TileGrid of Vector2s where they each point at their lowest cost neighnour
     *  
     *  TileGrid<int> integrationField - the integration field to be converted
    */
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

    /*
     *  A function which returns a flowfield (TileGrid<Vector2>) to destination, including the destination and source components
     *  It will check if there is a cached flowfield which works, if not, it will make a new one
     *  
     *  Vector2 source - the source position
     *  Vector2Int destination - the destination index 
    */
    public TileGrid<Vector2> GetFlowField(Vector2 source, Vector2Int destination)
    {
        //if the dictionary containing our cached flowfields is at the set limit
        
        if (cachedFlowfields.Keys.Count == maxCachedComponents)
        {
            //remove an entry
            cachedFlowfields.Remove(cachedFlowfields.Keys.FirstOrDefault());
        }
        

        //get our new/existing flowfield
        Component component = components.GetObject(source);
        //checks if the flowfield is not cached
        if(!cachedFlowfields.ContainsKey((component, destination)))
        {
            //creates a new flowfield
            TileGrid<int> intField = CreateIntegrationField(component, tileMap.GetWorldPositionFromIndex(destination.x, destination.y));
            //caches the flowfield in the dictionary
            cachedFlowfields.Add((component, destination), CreateFlowField(intField));
        }
        
        return cachedFlowfields[(component, destination)];
    }

    /*
     * A function which calculates the h value between 2 given positions
     * 
     *  Vector2 source - the first position
     *  Vector2 destination - the second position
    */
    private int CalculateH(Vector2 source, Vector2 destination)
    {
        //logic for calculating the heuristic
        return Mathf.FloorToInt(Mathf.Abs(source.x - destination.x)) + Mathf.FloorToInt(Mathf.Abs(source.y - destination.y)/2f);
    }

    /*
     * 
     * Flocking 
     * 
     */

    public Vector2 GetSeperation(UnitClass unit)
    {
        Vector2 position = unit.transform.position;
        Vector2 totalForce = new Vector2();
        int nearby = 0;

        foreach (UnitClass other in allUnits)
        {
            Vector2 currentForce = position - (Vector2)other.transform.position;
            if (new Vector2(currentForce.x, currentForce.y * 2f).magnitude < unit.seperationRadius)
            {
                nearby++;
                float direction = currentForce.magnitude;
                currentForce.Normalize();
                float radius = other.radius + unit.radius;

                totalForce += currentForce * (1 - ((direction - radius) / unit.seperationRadius - radius));
            }
        }

        if(nearby == 0)
        {
            return Vector2.zero;
        }

        return totalForce * (unit.maxForce / nearby);
    }

    public void AlertNeighbours(UnitClass unit)
    {
        Vector2 position = unit.transform.position;

        foreach (UnitClass other in allUnits)
        {
            Vector2 currentForce = position - (Vector2)other.transform.position;
            if (new Vector2(currentForce.x, currentForce.y * 2f).magnitude < unit.seperationRadius && other.moving == true && unit.flock == other.flock)
            {
                other.StopMoving();
            }
        }
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
                Gizmos.matrix = Matrix4x4.Translate(new Vector2((componentHeight * tileSize * height) / 2f, (tileSize / 4f)));
                foreach (Component c_ in components.tileArray)
                {
                    List<HierarchicalNode> n = c_.portalNodes;
                    foreach (HierarchicalNode n_ in n)
                    {
                        Gizmos.color = Color.red;

                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) - (tileSize / 2f), (n_.x + n_.y) * tileSize / 4f), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / 4f) + (tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) + (tileSize / 2f), (n_.x + n_.y) * tileSize / 4f), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / 4f) + (tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) - (tileSize / 2f), (n_.x + n_.y) * tileSize / 4f), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / 4f) - (tileSize / 4f)));
                        Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f) + (tileSize / 2f), (n_.x + n_.y) * tileSize / 4f), new Vector2(((n_.x - n_.y) * tileSize / 2f), ((n_.x + n_.y) * tileSize / 4f) - (tileSize / 4f)));

                        //Gizmos.color = Color.white;
                        foreach (HierarchicalNode n__ in n_.connectedNodes.Keys)
                        {
                            Gizmos.DrawLine(new Vector2(((n_.x - n_.y) * tileSize / 2f), (n_.x + n_.y) * tileSize / 4f), new Vector2((n__.x - n__.y) * tileSize / 2f, (n__.x + n__.y) * tileSize / 4f));
                            Handles.Label(Vector2.Lerp(new Vector2(((n_.x - n_.y) * tileSize / 2f), (n_.x + n_.y) * tileSize / 4f) + tileMap.GetStartPosition(), new Vector2((n__.x - n__.y) * tileSize / 2f, (n__.x + n__.y) * tileSize / 4f) + tileMap.GetStartPosition(), 0.5f), n_.connectedNodes[n__].ToString());
                        }
                    }
                }
                Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, 0));
            }

            if (showGrid)
            {
                Gizmos.color = Color.gray;
                Gizmos.matrix = Matrix4x4.Translate(new Vector2((componentHeight * tileSize * height )/2f, (tileSize/4f)));
                foreach (Node n in tileMap.tileArray)
                {
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) - (tileSize / 2f), (n.x + n.y) * tileSize / 4f), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / 4f) + (tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) + (tileSize / 2f), (n.x + n.y) * tileSize / 4f), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / 4f) + (tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) - (tileSize / 2f), (n.x + n.y) * tileSize / 4f), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / 4f) - (tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(((n.x - n.y) * tileSize / 2f) + (tileSize / 2f), (n.x + n.y) * tileSize / 4f), new Vector2(((n.x - n.y) * tileSize / 2f), ((n.x + n.y) * tileSize / 4f) - (tileSize / 4f)));
                }
                Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, 0));
            }

            if (showComponents)
            {
                Gizmos.color = Color.black;
                Gizmos.matrix = Matrix4x4.Translate(new Vector2((componentHeight * tileSize * height)/2f, (componentHeight * tileSize / 4f)));
                foreach (Component c in components.tileArray)
                {
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) - (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f) + (componentHeight * tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) + (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f) + (componentHeight * tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) - (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f) - (componentHeight * tileSize / 4f)));
                    Gizmos.DrawLine(new Vector2(((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f) + (componentWidth * tileSize / 2f), (c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f), new Vector2((c.indexX * componentWidth * tileSize - c.indexY * componentHeight * tileSize) / 2f, ((c.indexX * componentWidth * tileSize + c.indexY * componentHeight * tileSize) / 4f) - (componentHeight * tileSize / 4f)));

                }
                Gizmos.matrix = Matrix4x4.Translate(new Vector2(0, 0));
            }
        }
    }
}
