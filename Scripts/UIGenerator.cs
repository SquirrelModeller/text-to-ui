using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UIGenerator : UdonSharpBehaviour
{
    [SerializeField] private TextAsset configFile;
    [SerializeField] private GameObject navPrefab;
    [SerializeField] private GameObject btnPrefab;
    [SerializeField] private GameObject groupPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private string[] actionIdentifiers;
    [SerializeField] private ButtonActionBase[] actionManagers;

    private GameObject[] allUIElements;
    private int[] elementDepths;
    private const int MAX_ELEMENTS = 100;
    private const int INDENT_SIZE = 2;

    private void Start()
    {
        if (configFile == null) return;

        InitializeArrays();
        ParseConfigFile();
    }

    private void InitializeArrays()
    {
        allUIElements = new GameObject[MAX_ELEMENTS];
        elementDepths = new int[MAX_ELEMENTS];
    }

    private void ParseConfigFile()
    {
        string[] lines = configFile.text.Split('\n');
        int currentIndex = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line.Trim())) continue;
            if (currentIndex >= MAX_ELEMENTS) break;

            ProcessConfigLine(line, currentIndex);
            currentIndex++;
        }
    }

    private void ProcessConfigLine(string line, int currentIndex)
    {
        int lineDepth = CountLeadingSpaces(line) / INDENT_SIZE;
        elementDepths[currentIndex] = lineDepth;

        string[] parts = line.Trim().Split(new[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return;

        string elementName = parts[0].Trim();
        string elementType = parts[1].Trim().ToLower();
        string actionType = parts.Length > 2 ? parts[2].Trim() : "";

        GameObject newElement = CreateUIElement(elementType, elementName, actionType);
        if (newElement == null) return;

        allUIElements[currentIndex] = newElement;
        SetElementParent(newElement, currentIndex, lineDepth);
    }


    private void SetElementParent(GameObject element, int currentIndex, int depth)
    {
        if (depth == 0)
        {
            element.transform.SetParent(contentParent, false);
            return;
        }

        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (elementDepths[i] != depth - 1) continue;

            NavButton parentToggle = allUIElements[i].GetComponent<NavButton>();
            if (parentToggle == null) break;

            GameObject groupContainer = GetOrCreateGroupContainer(parentToggle, i, elementDepths[i]);
            Transform contentArea = groupContainer.transform.Find("Content");
            if (contentArea != null)
            {
                element.transform.SetParent(contentArea, false);
            }
            break;
        }
    }

    private GameObject GetOrCreateGroupContainer(NavButton parentToggle, int index, int depth)
    {
        if (parentToggle.groupContainer != null) return parentToggle.groupContainer;

        GameObject groupContainer = Instantiate(groupPrefab);
        groupContainer.name = $"{allUIElements[index].name}_Group";
        groupContainer.transform.SetParent(contentParent, false);
        groupContainer.SetActive(false);

        SetupGroupContainer(groupContainer, index, depth);
        parentToggle.groupContainer = groupContainer;

        return groupContainer;
    }

    private void SetupGroupContainer(GameObject container, int index, int depth)
    {
        GroupContainer groupScript = container.GetComponent<GroupContainer>();
        if (groupScript != null)
        {
            groupScript.parentGroup = FindParentGroup(index, depth);
        }

        Transform backButton = container.transform.Find("BackButton");
        if (backButton != null)
        {
            BackButtonHandler backHandler = backButton.GetComponent<BackButtonHandler>();
            if (backHandler != null)
            {
                backHandler.currentGroup = container;
            }
        }
    }

    private GameObject FindParentGroup(int currentIndex, int currentDepth)
    {
        if (currentDepth == 0) return null;

        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (elementDepths[i] == currentDepth - 1)
            {
                NavButton parentToggle = allUIElements[i].GetComponent<NavButton>();
                if (parentToggle != null)
                {
                    return parentToggle.groupContainer;
                }
                return null;
            }
        }
        return null;
    }

    private GameObject CreateUIElement(string elementType, string elementName, string actionType)
    {
        GameObject newElement = null;

        switch (elementType)
        {
            case "nav":
                newElement = Instantiate(navPrefab);
                SetElementText(newElement, elementName);
                break;

            case "btn":
                newElement = Instantiate(btnPrefab);
                SetElementText(newElement, elementName);

                if (!string.IsNullOrEmpty(actionType))
                {
                    ButtonExecute buttonWord = newElement.GetComponent<ButtonExecute>();
                    if (buttonWord != null)
                    {
                        AssignActionToButton(buttonWord, actionType);
                    }
                }
                break;
        }

        if (newElement != null)
        {
            newElement.name = elementName;
        }

        return newElement;
    }

    private void AssignActionToButton(ButtonExecute button, string actionType)
    {
        for (int i = 0; i < actionIdentifiers.Length; i++)
        {
            if (actionIdentifiers[i] == actionType && i < actionManagers.Length)
            {
                button.actionScript = actionManagers[i];
                break;
            }
        }
    }

    private void SetElementText(GameObject element, string text)
    {
        TextMeshProUGUI tmp = element.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
        }
    }

    private int CountLeadingSpaces(string line)
    {
        int count = 0;
        while (count < line.Length && line[count] == ' ')
        {
            count++;
        }
        return count;
    }
}