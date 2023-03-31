using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldMine : Building
{
    private int delay = 1;
    public int increase = 1;

    float timer = 0f;

    protected override void Start()
    {
        base.Start();
        buildingName = "Gold Mine";
        infoText = "This is a gold mine, it will produce currency for you over time. The current production rate of this gold mine is " + increase.ToString() + " every " + delay.ToString() + " second(s).";
    }

    protected override void Update()
    {
        base.Update();

        timer += Time.deltaTime;

        if (timer >= delay)
        {
            timer = 0f;
            owner.gold += increase;
        }
    }

    /*
     * A function which upgrades the production of a gold mine
     * 
     * int increase - the increase in production
     */
    public void UpgradeProduction(int increase)
    {
        this.increase += increase;
        infoText = "This is a gold mine, it will produce currency for you over time. The current production rate of this gold mine is: " + this.increase.ToString();
    }

}
