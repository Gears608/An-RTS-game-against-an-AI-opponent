using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntegrationField
{
    public int[,] array;

    public IntegrationField(int x, int y)
    {
        array = new int[x, y];
    }
}
