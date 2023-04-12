using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingButtonConroller : MonoBehaviour
{
    [SerializeField]
    private PlayerAgent playerClass;
    [SerializeField]
    private GameObject buildingPrefab;

    /*
     * A function which updates the active building prefab in the player class
     */
    public void SetBuildingPrefab()
    {
        playerClass.SetBuildingPrefab(buildingPrefab);
    }
}
