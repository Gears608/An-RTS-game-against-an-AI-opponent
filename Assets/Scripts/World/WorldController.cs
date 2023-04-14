using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldController : MonoBehaviour
{
    public TileGrid<int> tileMap;
    public int width;
    public int height;

    public float tileSize;

    public Vector3 startPos;

    public TileGrid<Component> components;

    public int componentWidth;
    public int componentHeight;

    public static bool isPaused;

    private int terrainMask;

    private Dictionary<(Component, Vector2Int), TileGrid<Vector2>> cachedFlowfields;

    [SerializeField]
    private PlayerClass playerClass;

    [SerializeField] 
    private bool displayCost = false;
    [SerializeField] 
    private bool showportals = false;
    [SerializeField] 
    private bool showComponents = false;

    [SerializeField]
    private GameObject pause;
    [SerializeField]
    private TMP_Text winText;

    [SerializeField]
    private int maxCachedComponents;

    [SerializeField]
    private List<UnitClass> allUnits;

    private void Start()
    {
        isPaused = false;

        allUnits = new List<UnitClass>();
        GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("PlayerUnit");
        foreach(GameObject unit in foundObjects)
        {
            allUnits.Add(unit.GetComponent<UnitClass>());
        }
        foundObjects = GameObject.FindGameObjectsWithTag("EnemyUnit");
        foreach (GameObject unit in foundObjects)
        {
            allUnits.Add(unit.GetComponent<UnitClass>());
        }

        tileMap = new TileGrid<int>(width * componentWidth, height * componentHeight, tileSize, tileSize, new Vector2((componentHeight * tileSize * height) / 2f, 0));
        terrainMask = LayerMask.GetMask("Impassable", "EnemyBuilding", "PlayerBuilding");

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
                        tileMap.SetObject(x1 + x * componentWidth, y1 + y * componentHeight, 1);
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

    #region GameState

    /*
     * A function which returns the current paused state of the game
     * 
     * Returns bool; true if paused, false if not paused
    */
    public bool IsGamePaused()
    {
        return isPaused;
    }

    /*
     * A function which changes the paused state of the game; pause/unpause
    */
    public void PauseGame()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
        else
        {
            Time.timeScale = 0f;
        }
        isPaused = !isPaused;
        pause.SetActive(!pause.activeSelf);
    }

    /*
     * A function which ends the game
     * 
     * PlayerClass loser - the PlayerClass of the loser
    */
    public void EndGame(PlayerClass loser)
    {
        Time.timeScale = 0f;
        isPaused = true;
        if (loser is NonPlayerAgent)
        {
            winText.text = "You Win!";
        }
        else
        {
            winText.text = "Game Over.";
        }
        winText.gameObject.SetActive(true);
    }

    #endregion

    #region WorldRepresentation

    /*
     * 
     *  World Representation
     * 
     */

    /*
     * A function which updates the node graph and components based on a changed tile at a position
     * 
     *  Vector2 position - the position of the changed tile
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

    /*
     * A function which updates the node graph and components based on a list of updated tiles
     * 
     *  List<Vector2Int> positionsAsIndex - the indexes of the changed tiles
    */
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

            ClearInternalPaths(component);
            CalculatePaths(component);
        }
    }

    /*
     * A function which checks all the tiles in a given component and updates their costs
     * 
     *  Component component - the component to check the nodes of 
    */
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
                    tileMap.SetObject(x, y, 255);
                }
            }
        }
    }

    /*
     * A function which checks a vertical edge of a component and adds nodes at the midpoint of spaces
     * 
     *  Component component - the component to check the edge of
     *  Vector2Int startPosIndex - the starting position of the edge
     *  int xModifier - the modifier to the x index of the neighbouring component
    */
    private void CheckVertical(Component component, Vector2Int startPosIndex, int xModifier)
    {
        int length = 0;
        for (int y = startPosIndex.y; y < startPosIndex.y + componentHeight; y++)
        {
            int currentNodeCost = tileMap.GetObject(startPosIndex.x, y);
            int neighbourNodeCost = tileMap.GetObject(startPosIndex.x + xModifier, y);

            if (currentNodeCost < 255 && neighbourNodeCost < 255)
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
            HierarchicalNode newHNode = new HierarchicalNode(startPosIndex.x, midPointY, component);
            HierarchicalNode neighbourHNode = new HierarchicalNode(startPosIndex.x + xModifier, midPointY, components.GetObject(component.indexX + xModifier, component.indexY));

            newHNode.AddNode(neighbourHNode, 1);
            neighbourHNode.AddNode(newHNode, 1);

            component.AddNode(newHNode);
            neighbourHNode.component.AddNode(neighbourHNode);
        }
    }
    /*
     * A function which checks a horizontal edge of a component and adds nodes at the midpoint of spaces
     * 
     *  Component component - the component to check the edge of
     *  Vector2Int startPosIndex - the starting position of the edge
     *  int yModifier - the modifier to the y index of the neighbouring component
    */
    private void CheckHorizontal(Component component, Vector2Int startPosIndex, int yModifier)
    {
        int length = 0;
        for (int x = startPosIndex.x; x < startPosIndex.x + componentWidth; x++)
        {
            int currentNodeCost = tileMap.GetObject(x, startPosIndex.y);
            int neighbourNodeCost = tileMap.GetObject(x,  startPosIndex.y + yModifier);

            if (currentNodeCost < 255 && neighbourNodeCost < 255)
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

    /*
     * A function which will calculate all internal paths on a given component
     * 
     *  Component component - the component to calculate paths for
    */
    private void CalculatePaths(Component component)
    {
        foreach(HierarchicalNode node in component.portalNodes)
        {
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

    /*
     * A function which returns a node at a given position
     * 
     *  Vector2 position - the position of the node to find
     *  
     *  Returns the cost at the position or null
    */
    public int GetNode(Vector2 position)
    {
        return tileMap.GetObject(position);
    }

    /*
     * A function which returns a component at a given position
     * 
     *  Vector2 position - the position of the component to find
     *  
     *  Returns the Component at the position or null
    */
    public Component GetComponent(Vector2 position) 
    {
        return components.GetObject(position);
    }

    /*
     * A function which will clear all the internal paths of a component; this includes any path which connects two nodes on the given component
     * 
     *  Component component - the component to remove paths from
    */
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

    /*
     * A function which adds a new node to the node graph at a given position and returns it
     * 
     *  Vector2 position - the position of the new node
     *  
     *  Returns a HierarchicalNode - the newly added graph node
    */
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

    /*
     * A function which removes a node from the node graph
     * 
     *  HierarchicalNode node - the node to be removed
    */
    public void RemoveNodeFromGraph(HierarchicalNode node)
    {
        
        Component component = components.GetObject(tileMap.GetWorldPositionFromIndex(node.x, node.y));
        component.RemoveNode(node);
    }

    /*
     * A function which removes nodes connecting two given components
     * 
     *  Component component1 - the first component
     *  Component component2 - the second component
    */
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

    /*
     * A function which takes a position and converts it to the origin of the grid square the position is within
     * 
     *  Vector2 position - the position to be converted
     *  
     *  Returns a Vector2 which is the new position
    */
    public Vector2 WorldToGridPosition(Vector2 position)
    {
        Vector2Int index = tileMap.GetIndexFromWorldPosition(position);
        Vector2 gridPosition = tileMap.GetWorldPositionFromIndex(index.x, index.y);
        return gridPosition;
    }

    /*
     *  A function which finds out if a position is a valid position in the world space
     *  
     *  Vector2 position - the position to check
    */
    public bool IsValidPosition(Vector2 position)
    {
        return tileMap.IsValidPosition(position);
    }

    #endregion

    #region Building

    /*
     * A function which checks if a given position can accomodate a given building
     * 
     *  Vector2 position - the position to place the building at
     *  GameObjet buildingPrefab - the prefab of the building
     *  
     *  Returns bool if the placement is valid or not
    */
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
                if(tileMap.GetObject(pos.x, pos.y) == 255)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /*
     * A function which places a building at a given position
     * 
     *  Vector2 position - the position to place the building at
     *  GameObject buildingPrefab - the prefab of the building to be placed
    */
    public Building PlaceBuilding(Vector2 position, GameObject buildingPrefab, PlayerClass owner)
    {
        Vector2Int positionIndex = tileMap.GetIndexFromWorldPosition(position);
        Vector2 gridPosition = tileMap.GetWorldPositionFromIndex(positionIndex.x, positionIndex.y);

        GameObject buildingObject = Instantiate(buildingPrefab);
        buildingObject.transform.position = gridPosition;

        Building building = buildingObject.GetComponent<Building>();

        building.owner = owner;

        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector2Int pos = new Vector2Int(positionIndex.x + x, positionIndex.y + y);
                positions.Add(pos);
                tileMap.SetObject(pos.x, pos.y, 255);
            }
        }

        UpdateNodes(positions);

        return building;
    }

    /*
     * A function which destroys a building at a given position
     * 
     *  Vector2 position - the position of the building
    */
    public Building DestroyBuilding(Vector2 position)
    {
        //checks the position is in the world space
        if (!IsValidPosition(position))
        {
            return null;
        }

        Vector2Int positionIndex = tileMap.GetIndexFromWorldPosition(position);

        Collider2D buildingCollider = Physics2D.OverlapPoint(position);
        //checks that there is a collision
        if(buildingCollider == null)
        {
            return null;
        }

        Building building = buildingCollider.gameObject.GetComponentInParent<Building>();

        //gets the positions of grid spaces the building occupies
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector2Int pos = new Vector2Int(positionIndex.x + x, positionIndex.y + y);
                positions.Add(pos);
                tileMap.SetObject(pos.x, pos.y, 1);
            }
        }
        //updates the node graph
        UpdateNodes(positions);

        return building;
    }

    #endregion

    #region Pathfinding

    /*
     * 
     *  Pathfinding Methods
     * 
     */



    /*
     *  A function which finds an a* path over the hierarchical nodes in the world
     *  
     *  Vector2 startPos - the starting position of the path
     *  Vector2 destination - the destination given as a position
     *  
     *  Returns a list of hierarchical nodes if a path is found
     *  Returns null if no path is found
    */
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

    /*
     *  A function which finds an a* path over the hierarchical nodes in the world
     *  
     *  Vector2 startPos - the starting position of the path
     *  HierarchicalNode destinationNode - the destination as a given node in the node graph
     *  
     *  Returns a list of hierarchical nodes if a path is found
     *  Returns null if no path is found
    */
    public List<HierarchicalNode> FindHierarchicalPath(Vector2 startPos, Vector2 destination)
    {
        if(tileMap.GetObject(destination) == 255)
        {
            return null;
        }

        HierarchicalNode destinationNode = AddNodeToGraph(destination);

        //an open and closed list to hold the nodes to be searched
        List<HierarchicalNode> openList = new List<HierarchicalNode>();
        List<HierarchicalNode> closedList = new List<HierarchicalNode>();

        //finds the nodes accessible to the unit
        Component startComponent = components.GetObject(startPos);
        TileGrid<int> integrationField = CreateIntegrationField(startComponent, startPos);

        foreach (HierarchicalNode node in startComponent.portalNodes)
        {
            int weight = integrationField.GetObject(new Vector2(node.x, node.y) * tileSize);
            if (weight != -1)
            {
                node.g = weight;
                node.h = CalculateH(new Vector2(node.x, node.y), destination);
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

            if (currentNode == destinationNode)
            {
                List<HierarchicalNode> output = new List<HierarchicalNode>();
                output.Add(currentNode);
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                RemoveNodeFromGraph(destinationNode);

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
                        neighbour.h = CalculateH(new Vector2(neighbour.x, neighbour.y), destination);
                        neighbour.CalculateF();
                        neighbour.previousNode = currentNode;
                        openList.Add(neighbour);
                    }
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);
        }

        Debug.Log(destinationNode);
        RemoveNodeFromGraph(destinationNode);

        return null;
    }

    /*
     *  A function which finds a 'merging' a* path over the hierarchical nodes in the world
     *  
     *  Vector2 startPos - the starting position of the path
     *  HierarchicalNode destinationNode - the destination as a given node in the node graph
     *  List<HierarchicalNode> path - the existing path to merge with
     *  
     *  Returns a list of hierarchical nodes if a path is found
     *  Returns null if no path is found
    */
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

    /*
     *  A function which finds a 'merging' a* path over the hierarchical nodes in the world
     *  
     *  Vector2 startPos - the starting position of the path
     *  Vector2 destinationPos - the destination position of the path
     *  List<HierarchicalNode> path - the existing path to merge with
     *  
     *  Returns a list of hierarchical nodes if a path is found
     *  Returns null if no path is found
    */
    public List<HierarchicalNode> FindHierarchicalPathMerging(Vector2 startPos, Vector2 destinationPos, List<HierarchicalNode> path)
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
                node.h = CalculateH(new Vector2(node.x, node.y), destinationPos);
                node.CalculateF();
                node.previousNode = null;
                openList.Add(node);
            }
        }

        HierarchicalNode destinationNode = AddNodeToGraph(destinationPos);
        //Debug.Log("Temp destination added: "+destinationNode.x +", "+destinationNode.y);

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
                //Debug.Log("Merging Path Found");
                List<HierarchicalNode> output = path.GetRange(0, path.IndexOf(currentNode));
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                RemoveNodeFromGraph(destinationNode);
                //Debug.Log("Temp destination removed: " + destinationNode.x + ", " + destinationNode.y);
                return output;
            }

            //if destination node is reached without merging then return the path anyway
            if(currentNode == destinationNode)
            {
                //Debug.Log("Alternate Path Found");
                List<HierarchicalNode> output = new List<HierarchicalNode>();
                output.Add(currentNode);
                while (currentNode.previousNode != null)
                {
                    currentNode = currentNode.previousNode;
                    output.Add(currentNode);
                }

                RemoveNodeFromGraph(destinationNode);
                //Debug.Log("Temp destination removed: " + destinationNode.x + ", " + destinationNode.y);
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
                        neighbour.h = CalculateH(new Vector2(neighbour.x, neighbour.y), destinationPos);
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
        //Debug.Log("Temp destination removed");
        return null;
    }

    /*
     *  A function which creates an integgration field, given a source component and a destination point
     *  
     *  Component component - the source component
     *  Vector2 destination - the destination point
     *  
     *  retuns a TileGrid<int> which represents each position as the sum of all the costs of tiles between it and the destination
    */
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
                if (tileMap.GetObject(neighbourNode.x, neighbourNode.y) == 255)
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
                if (currentNode_ + 2 < neighbourNode_ || neighbourNode_ == -1)
                {
                    neighbourNode_ = currentNode_ + 2;
                    integrationField.SetObject(tileMap.GetWorldPositionFromIndex(neighbourNode.x, neighbourNode.y), neighbourNode_);
                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
            
            //gets a list of the nodes neighbours
            neighbours = tileMap.GetIntercardinalNeighbours(currentNode.x, currentNode.y, 255);

            //loops over all the nodes neighbours
            foreach (Vector2Int neighbourNode in neighbours)
            {

                if (neighbourNode.x >= maxX || neighbourNode.x < minX || neighbourNode.y >= maxY || neighbourNode.y < minY)
                {
                    continue;
                }
                if (tileMap.GetObject(neighbourNode.x, neighbourNode.y) == 255)
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
                if (currentNode_ + 3 < neighbourNode_ || neighbourNode_ == -1)
                {
                    neighbourNode_ = currentNode_ + 3;
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
     *  A function which converts a TileGrid of ints to a TileGrid of Vector2s where they each point at their lowest cost neighbour
     *  
     *  TileGrid<int> integrationField - the integration field to be converted
     *  
     *  Returns a TileGrid<Vector2> in which each tile in the grid is a Vector2 which points in the direction of the route to the destination
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
     *  
     *  Returns a TileGrid<Vector2> in which each tile in the grid is a Vector2 which points in the direction of the route to the destination
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
     *  
     *  Returns an int which is H cost between the two positions
    */
    private int CalculateH(Vector2 source, Vector2 destination)
    {
        //logic for calculating the heuristic
        return Mathf.FloorToInt(Mathf.Abs(source.x - destination.x)) + Mathf.FloorToInt(Mathf.Abs(source.y - destination.y)/2f);
    }

    #endregion

    #region Flocking

    /*
     * 
     * Flocking 
     * 
     */


    /*
     * A function which calculates the direction a given unit should move to be at distance from nearby units
     * 
     *  UnitClass unit - the unit in question
     *  
     *  Returns a Vector2 which is the direction of movement
    */
    public Vector2 GetSeperation(UnitClass unit)
    {
        Vector2 position = unit.transform.position;
        Vector2 totalForce = new Vector2();
        int nearby = 0;

        foreach (UnitClass other in allUnits)
        {
            Vector2 currentForce = position - (Vector2)other.transform.position;
            //checks if the unit is within the nearby radius
            if (new Vector2(currentForce.x, currentForce.y * 2f).magnitude < unit.seperationRadius)
            {
                nearby++;
                float direction = currentForce.magnitude;
                currentForce.Normalize();
                float radius = other.radius + unit.radius;

                totalForce += currentForce * (1 - ((direction - radius) / unit.seperationRadius - radius));
            }
        }

        //if there are no nearby units
        if(nearby == 0)
        {
            return Vector2.zero;
        }

        return totalForce.normalized;
    }

    /*
     * A function which alerts all nearby units to stop moving
     * 
     *  UnitClass unit - the unit in question
    */
    public void AlertNeighbours(UnitClass unit)
    {
        Vector2 position = unit.transform.position;

        foreach (UnitClass other in allUnits)
        {
            Vector2 displacement = position - (Vector2)other.transform.position;
            if (new Vector2(displacement.x, displacement.y * 2f).magnitude < unit.seperationRadius && other.IsMoving() && unit.flock == other.flock)
            {
                other.StopMoving();
            }
        }
    }

    /*
     * A function which adds a unit to the allUnits list
     * 
     *  UnitClass unit - the unit in question
    */
    public void AddUnit(UnitClass unit)
    {
        allUnits.Add(unit);
    }

    /*
     * A function which removes a unit from the allUnits list
     * 
     *  UnitClass unit - the unit in question
    */
    public void RemoveUnit(UnitClass unit)
    {
        allUnits.Remove(unit);
    }

    #endregion

    #region Debug
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
                        Handles.Label(tileMap.GetWorldPositionFromIndex(x, y) + new Vector2(0, tileSize/4f), tileMap.GetObject(x, y).ToString(), style);
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
    #endregion
}