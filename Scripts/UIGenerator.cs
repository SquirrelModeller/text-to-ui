using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
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

    [UdonSynced] private bool[] activeStates;
    private VRCPlayerApi localPlayer;
    private GameObject[] uiContainers;
    private int containerCount;

    private void Start()
    {
        if (configFile == null) return;

        localPlayer = Networking.LocalPlayer;
        InitializeArrays();
        ParseConfigFile();
        CacheAllContainers();

        if (!Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    private void InitializeArrays()
    {
        allUIElements = new GameObject[MAX_ELEMENTS];
        elementDepths = new int[MAX_ELEMENTS];
        activeStates = new bool[MAX_ELEMENTS];
        uiContainers = new GameObject[MAX_ELEMENTS];
    }

    private void CacheAllContainers()
    {
        containerCount = 0;

        for (int i = 0; i < contentParent.childCount; i++)
        {
            GameObject child = contentParent.GetChild(i).gameObject;
            NavButton navButton = child.GetComponent<NavButton>();
            if (navButton != null)
            {
                uiContainers[containerCount] = child;
                containerCount++;
            }
        }

        foreach (GameObject element in allUIElements)
        {
            if (element == null) continue;

            NavButton navButton = element.GetComponent<NavButton>();
            if (navButton != null && navButton.groupContainer != null)
            {
                uiContainers[containerCount] = navButton.groupContainer;
                containerCount++;
            }
        }
    }

    public void UpdateUIState(GameObject targetElement, bool state)
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(localPlayer, gameObject);
        }

        if (targetElement != null)
        {
            targetElement.SetActive(state);
        }

        for (int i = 0; i < containerCount; i++)
        {
            if (uiContainers[i] == targetElement)
            {
                activeStates[i] = state;
                break;
            }
        }

        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        for (int i = 0; i < containerCount; i++)
        {
            if (uiContainers[i] != null)
            {
                uiContainers[i].SetActive(activeStates[i]);
            }
        }
    }

    public void HandleNavButtonPress(NavButton button)
    {
        if (button.groupContainer == null) return;

        bool isRootButton = button.transform.parent == contentParent;

        if (isRootButton)
        {
            for (int i = 0; i < contentParent.childCount; i++)
            {
                Transform child = contentParent.GetChild(i);
                NavButton navButton = child.GetComponent<NavButton>();
                if (navButton != null)
                {
                    UpdateUIState(child.gameObject, false);
                }
            }
        }
        else
        {
            Transform contentParent = button.transform.parent;
            if (contentParent != null)
            {
                for (int i = 0; i < contentParent.childCount; i++)
                {
                    Transform child = contentParent.GetChild(i);
                    NavButton siblingNav = child.GetComponent<NavButton>();
                    if (siblingNav != null && siblingNav.groupContainer != null)
                    {
                        UpdateUIState(siblingNav.groupContainer, false);
                    }
                }
            }
        }

        GroupContainer groupScript = button.groupContainer.GetComponent<GroupContainer>();
        if (groupScript != null && groupScript.parentGroup != null)
        {
            UpdateUIState(groupScript.parentGroup, false);
        }

        UpdateUIState(button.groupContainer, true);
    }


    public void HandleBackButtonPress(BackButtonHandler backButton)
    {
        if (backButton.currentGroup == null) return;

        GroupContainer groupScript = backButton.currentGroup.GetComponent<GroupContainer>();
        if (groupScript != null)
        {
            if (groupScript.parentGroup != null)
            {
                UpdateUIState(groupScript.parentGroup, true);

                Transform parentContent = groupScript.parentGroup.transform.Find("Content");
                if (parentContent != null)
                {
                    for (int i = 0; i < parentContent.childCount; i++)
                    {
                        GameObject child = parentContent.GetChild(i).gameObject;
                        UpdateUIState(child, true);
                    }
                }
            }
            else
            {
                Transform contentParent = backButton.transform.parent.parent;
                for (int i = 0; i < contentParent.childCount; i++)
                {
                    Transform child = contentParent.GetChild(i);
                    if (child.GetComponent<NavButton>() != null)
                    {
                        UpdateUIState(child.gameObject, true);
                    }
                }
            }
        }

        UpdateUIState(backButton.currentGroup, false);
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
                backHandler.Initialize(gameObject.GetComponentInChildren<UIGenerator>());
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
                NavButton tmp = newElement.GetComponentInChildren<NavButton>();
                tmp.Initialize(gameObject.GetComponentInChildren<UIGenerator>());
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