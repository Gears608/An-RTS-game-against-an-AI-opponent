using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Core : Building
{
    protected override void Update()
    {
        if (health == 0)
        {
            worldController.EndGame(owner);
        }

        base.Update();
    }
}
