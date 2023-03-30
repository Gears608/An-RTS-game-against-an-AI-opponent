using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public int height;
    public int width;

    public int cost;
    public string buildingName;

    [SerializeField]
    public PlayerClass playerClass;

    public string infoText;

    public virtual bool EnableUnitMenu()
    {
        return false;
    }
}
