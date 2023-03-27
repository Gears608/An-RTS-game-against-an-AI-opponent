using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingButtonConroller : MonoBehaviour
{
    [SerializeField]
    private PlayerClass playerClass;
    [SerializeField]
    private GameObject buildingPrefab;

    public void SetBuildingPrefab()
    {
        playerClass.SetBuildingPrefab(buildingPrefab);
    }
}
