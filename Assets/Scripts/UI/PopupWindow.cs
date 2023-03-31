using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupWindow : MonoBehaviour
{
    [SerializeField]
    private TMP_Text title;
    [SerializeField]
    private TMP_Text info;

    /*
     * A function which updates the text components of the popup window to that of the selected building
     * 
     * string title - the tile/name of the building
     * string info - the info text of the building
     */
    public void PopulateWindow(string title, string info)
    {
        this.title.text = title;
        this.info.text = info;
        gameObject.SetActive(true);
    }
}
