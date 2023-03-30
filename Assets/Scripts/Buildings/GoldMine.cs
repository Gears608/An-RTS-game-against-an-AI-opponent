using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldMine : Building
{
    private int delay = 1;
    public int increase = 1;

    float timer = 0f;

    private void Start()
    {
        buildingName = "Gold Mine";
        infoText = "This is a gold mine, it will produce currency for you over time. The current production rate of this gold mine is " + increase.ToString() + " every " + delay.ToString() + " second(s).";
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= delay)
        {
            timer = 0f;
            playerClass.gold += increase;
        }
    }

    public void UpgradeProduction(int increase)
    {
        this.increase += increase;
        infoText = "This is a gold mine, it will produce currency for you over time. The current production rate of this gold mine is: " + this.increase.ToString();
    }

}
