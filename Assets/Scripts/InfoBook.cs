using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//orgignal thought was to use tabs then sections but a better thought it to have pages and tabs that are jsut like a contents 
//it could be refrenced in the contents at the begining and the back button would bring you to it

//I'm also thinking adding pages would be very easy but we would still need to find another way to track quests

//I also want the palyer to manually be adding how to create metals and do things so maybe we add a way to add quests

//the conents can use begining tab page finder
public class InfoBook : MonoBehaviour
{
    [SerializeField] private GameObject BookGuiBase;
    [SerializeField] private GameObject ContentsText;
    [SerializeField] private GameObject LeftPage;
    [SerializeField] private GameObject RightPage;
    [SerializeField] private Texture BlankPageTexture;

    [SerializeField] private Button backButton;
    [SerializeField] private Button NextPageButton;
    [SerializeField] private Button PreviousPageButton;

    [SerializeField] private List<Page> StartingPages;

    private List<Page> BookPages;

    public List<int> startingPageNums = new List<int>() { 0, 1, 2, 3, 4, 5 };
    /// <summary>
    /// Currently order is contents page (always 0), Quests, Help, Metals, Combat, Eneimes  
    /// might add more later
    /// </summary>
    private int currentPage = 0;

    private void Start()
    {
        //sets starting vars
        startingPageNums = new List<int>() { 0, 1, 2, 3, 4, 5 };
        currentPage = 0;

        BookPages = StartingPages;

        addStartingPages();
        BookGuiBase.SetActive(false);

        backButton.onClick.AddListener(BackToContents);
        NextPageButton.onClick.AddListener(NextPage);
        PreviousPageButton.onClick.AddListener(PreviousPage);
    }

    private void addStartingPages()
    { 
        //this would include help, combat, and one metal
    }


    private void BackToContents()
    {
        currentPage = 0;
        DisplayPage(0);
    }
    public void DisplayBook()
    {
        BookGuiBase.SetActive(true);
        DisplayCurrentPage();
    }

    public void DisplayContents()
    {
        string tempT = "";
        for(int i = 1; i < startingPageNums.Count; i++)
        {
            tempT = tempT + BookPages[startingPageNums[i]].PageTitle + " ----------- Page " + startingPageNums[i] + "\n";
        }

        //cannot find it
        ContentsText.GetComponent<TextMeshProUGUI>().text = tempT;
    }

    public void NextPage()
    {
        if (currentPage + 2 < BookPages.Count)
            currentPage += 2;
        else if(currentPage + 1 < BookPages.Count)
            currentPage++;
        DisplayPage(currentPage);
    }

    public void PreviousPage()
    {
        if(currentPage >= 2)
            currentPage = currentPage - 2;
        DisplayPage(currentPage);
    }
    private void DisplayPage(int pageNum)
    {
        if(pageNum < 0)
        {
            Debug.Log("ERROR page number is less than 0");
            
        }
        LeftPage.SetActive(true);
        RightPage.SetActive(true);
        if(pageNum > BookPages.Count)
        {
            LeftPage.GetComponent<RawImage>().texture = BookPages[pageNum - 1].PageTexture;
            RightPage.GetComponent<RawImage>().texture = BlankPageTexture;
        }
        else
        {
            if (pageNum % 2 == 0)
            {
                LeftPage.GetComponent<RawImage>().texture = BookPages[pageNum].PageTexture;
                RightPage.GetComponent<RawImage>().texture = BookPages[pageNum + 1].PageTexture;
            }
            else
            {
                LeftPage.GetComponent<RawImage>().texture = BookPages[pageNum - 1].PageTexture;
                RightPage.GetComponent<RawImage>().texture = BookPages[pageNum].PageTexture;
            }
        }
        if (pageNum <= 1)
        {
            DisplayContents();
            currentPage = 0;
        }
        else
        {
            ContentsText.GetComponent<TextMeshProUGUI>().text = "";
        }
    }

    public void DisplayCurrentPage() { DisplayPage(currentPage); }
    public void addPage(Page page)
    {
        BookPages.Insert(FindFirstPageType(page.Type), page);
        switch (page.Type)
        {
            case PageType.Contents:
                //This should never happen
                break;
            case PageType.Quests:
                startingPageNums[1] = FindFirstPageType(page.Type);
                break;
            case PageType.Help:
                startingPageNums[2] = FindFirstPageType(page.Type);
                break;
            case PageType.Metals:
                startingPageNums[3] = FindFirstPageType(page.Type);
                break;
            case PageType.Combat:
                startingPageNums[4] = FindFirstPageType(page.Type);
                break;
            case PageType.Enemies:
                startingPageNums[5] = FindFirstPageType(page.Type);
                break;
            default:
                //this might happen if the contents page is wierd or it does not have a tab
                startingPageNums[5] = FindFirstPageType(page.Type);
                break;
        }
    }

    //returns the index of the first page of the given type
    private int FindFirstPageType(PageType type)
    {
        for (int i = 0; i < BookPages.Count; i++)
        {
            if(BookPages[i].Type == type)
            {
                return i;
            }
        }
        return BookPages.Count - 1;
    }
}
