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

    public void PopulateWindow(string title, string info)
    {
        this.title.text = title;
        this.info.text = info;
        gameObject.SetActive(true);
    }
}
