using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Page", menuName = "Assets/Page")]
public class Page : ScriptableObject
{
    public string PageTitle; //ID use for generating
    public Texture PageTexture; //required image to display page
    public PageType Type; //Type of page basicly the tab of it

    public Page(string _title, Texture _PageTexture, PageType _type)
    {
        PageTitle = _title;
        PageTexture = _PageTexture;
        Type = _type;
    }

    public Page(string _title, PageType _type)
    {
        PageTitle = _title;
        Type = _type;
    }
}

public enum PageType
{
    Contents,
    Help,
    Quests,
    Metals,
    Combat,
    Enemies
}
