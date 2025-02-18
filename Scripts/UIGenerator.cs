using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;
using VRC.SDK3.Data;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UIGenerator : UdonSharpBehaviour
{
    [SerializeField] private TextAsset configFile;
    [SerializeField] private GameObject navPrefab;
    [SerializeField] private GameObject btnPrefab;
    [SerializeField] private GameObject backPrefab;
    [SerializeField] private GameObject groupPrefab;
    [SerializeField] private GameObject textNavTitlePrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private string[] actionIdentifiers;
    [SerializeField] private ButtonActionBase[] actionManagers;
    [SerializeField] private bool shouldSyncUIAcrossClients;

    [SerializeField]
    [Tooltip("Path to the header transform")]
    private string headerPath = "";

    [SerializeField]
    [Tooltip("Path to the content transform")]
    private string contentPath = "";
    private const int MAX_ELEMENTS = 100;
    private const int INDENT_SIZE = 2;

    [UdonSynced] private bool[] activeStates;
    private DataList allUIElements;
    private DataList uiContainers;
    private int groupCounter = 0;
    private DataList elementDepths;
    private UIGenerator cachedUIGenerator;
    private DataList cachedGroupContainerScripts;
    private DataDictionary nameIdToIndex;

    private void Start()
    {
        if (configFile == null) return;

        InitializeArrays();
        ParseConfigFile();
        CacheAllContainers();

        if (!Networking.IsOwner(gameObject) && shouldSyncUIAcrossClients)
        {
            RequestSerialization();
        }
    }

    private void InitializeArrays()
    {
        allUIElements = new DataList();
        elementDepths = new DataList();
        activeStates = new bool[MAX_ELEMENTS];
        uiContainers = new DataList();
        cachedGroupContainerScripts = new DataList();
        cachedUIGenerator = gameObject.GetComponentInChildren<UIGenerator>();
        nameIdToIndex = new DataDictionary();
    }

    private void CacheAllContainers()
    {
        int containerCount = 0;

        GameObject rootGroup = contentParent.Find("Root_Group").gameObject;
        uiContainers.Add(rootGroup);
        nameIdToIndex[new DataToken(((GameObject)uiContainers[containerCount].Reference).name)] = new DataToken(containerCount);
        containerCount++;

        for (int i = 0; i < allUIElements.Count; i++) 
        {
            NavButton navButton = ((GameObject)allUIElements[i].Reference).GetComponent<NavButton>();
            if (navButton == null || navButton.groupContainer == null) continue;

            uiContainers.Add(navButton.groupContainer);
            cachedGroupContainerScripts.Add(navButton.groupContainer.GetComponent<GroupContainer>());
            nameIdToIndex[new DataToken(((GameObject)uiContainers[containerCount].Reference).name)] = new DataToken(containerCount);
            containerCount++;
        }
    }

    public void UpdateUIState(GameObject targetElement, bool state)
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        if (targetElement != null)
        {
            targetElement.SetActive(state);
        }

        DataToken indexToken;
        if (nameIdToIndex.TryGetValue(new DataToken(targetElement.name), out indexToken))
        {
            activeStates[indexToken.Int] = state;
        }
        if (shouldSyncUIAcrossClients)
        {
            RequestSerialization();
        }
    }

    public void HandleNavButtonPress(NavButton button)
    {
        if (button.groupContainer == null) return;

        DataToken indexToken;
        if (nameIdToIndex.TryGetValue(new DataToken(button.groupContainer.name), out indexToken))
        {
            UpdateUIState(((GroupContainer)cachedGroupContainerScripts[indexToken.Int-1].Reference).parentGroup, false);
        }

        UpdateUIState(button.groupContainer, true);
    }

    public void HandleBackButtonPress(BackButtonHandler backButton)
    {
        if (backButton.currentGroup == null) return;

        GroupContainer groupScript = null;
        DataToken indexToken;
        if (nameIdToIndex.TryGetValue(new DataToken(backButton.currentGroup.name), out indexToken))
        {
            groupScript = (GroupContainer)cachedGroupContainerScripts[indexToken.Int-1].Reference;
        }
        if (groupScript != null && groupScript.parentGroup != null)
        {
            UpdateUIState(groupScript.parentGroup, true);

            Transform parentContent = groupScript.parentGroup.transform.Find(contentPath);
            if (parentContent != null)
            {
                for (int i = 0; i < parentContent.childCount; i++)
                {
                    GameObject child = parentContent.GetChild(i).gameObject;
                    UpdateUIState(child, true);
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
        elementDepths.Add(lineDepth);

        string[] parts = line.Trim().Split(new[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return;

        string elementName = parts[0].Trim();
        string elementType = parts[1].Trim().ToLower();
        string actionType = parts.Length > 2 ? parts[2].Trim() : "";

        GameObject newElement = CreateUIElement(elementType, elementName, actionType);
        if (newElement == null) return;

        allUIElements.Add(newElement);
        SetElementParent(newElement, currentIndex, lineDepth, actionType);
    }

    // Root elements are at depth 0, root elements do not have a parent nav
    // A parent gorup is therfore created, all navs can then be treated the same
    private void SetElementParent(GameObject element, int currentIndex, int depth, string navTitle)
    {
        GameObject parentGroup = null;

        if (depth == 0)
        {
            Transform rootGroupTransform = contentParent.Find("Root_Group");
            if (rootGroupTransform == null)
            {
                GameObject rootGroup = Instantiate(groupPrefab);
                rootGroup.name = "Root_Group";
                rootGroup.transform.SetParent(contentParent, false);
                rootGroup.SetActive(true);
                rootGroup.GetComponent<GroupContainer>().parentGroup = null;
                parentGroup = rootGroup;
            }
            else
            {
                parentGroup = rootGroupTransform.gameObject;
            }
        }
        else
        {
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                if (elementDepths[i].Int == depth - 1)
                {
                    NavButton parentNav = ((GameObject)allUIElements[i].Reference).GetComponent<NavButton>();
                    parentGroup = parentNav.groupContainer;
                    break;
                }
            }
        }

        Transform contentArea = parentGroup.transform.Find(contentPath);
        element.transform.SetParent(contentArea, false);

        NavButton navButton = element.GetComponent<NavButton>();
        if (navButton != null)
        {
            GameObject existingGroup = GameObject.Find($"{element.name}_Group_{groupCounter}");
            groupCounter++;
            if (existingGroup != null)
            {
                navButton.groupContainer = existingGroup;
            }
            else
            {
                GameObject groupContainer = CreateGroupContainer(element, currentIndex, depth, navTitle);
                navButton.groupContainer = groupContainer;

                GroupContainer groupScript = groupContainer.GetComponent<GroupContainer>();
                groupScript.parentGroup = parentGroup;
            }
        }
    }

    private GameObject CreateGroupContainer(GameObject navElement, int index, int depth, string navTitle)
    {
        GameObject groupContainer = Instantiate(groupPrefab);
        groupContainer.name = $"{navElement.name}_Group";
        groupContainer.transform.SetParent(contentParent, false);
        groupContainer.SetActive(false);

        GroupContainer groupScript = groupContainer.GetComponent<GroupContainer>();

        groupScript.parentGroup = FindParentGroup(index, depth);

        GameObject backButton = Instantiate(backPrefab);
        backButton.transform.SetParent(groupContainer.transform.Find(headerPath), false);
        backButton.transform.SetAsFirstSibling();

        BackButtonHandler backHandler = backButton.GetComponent<BackButtonHandler>();

        backHandler.currentGroup = groupContainer;
        backHandler.Initialize(cachedUIGenerator);


        if (navTitle != "" && textNavTitlePrefab != null)
        {
            GameObject textNavTitle = Instantiate(textNavTitlePrefab);
            textNavTitle.transform.SetParent(groupContainer.transform.Find(headerPath), false);
            textNavTitle.GetComponentInChildren<TextMeshProUGUI>().text = navTitle == "-" ? navElement.name : navTitle;

        }

        return groupContainer;
    }

    private GameObject FindParentGroup(int currentIndex, int currentDepth)
    {
        if (currentDepth == 0) return null;

        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (elementDepths[i].Int == currentDepth - 1)
            {
                NavButton parentNav = ((GameObject)allUIElements[i].Reference).GetComponent<NavButton>();
                if (parentNav != null)
                {
                    return parentNav.groupContainer;
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
                NavButton navButton = newElement.GetComponentInChildren<NavButton>();
                navButton.Initialize(cachedUIGenerator);
                SetElementText(newElement, elementName);
                break;

            case "btn":
                newElement = Instantiate(btnPrefab);
                SetElementText(newElement, elementName);

                if (!string.IsNullOrEmpty(actionType))
                {
                    ButtonExecute buttonExecute = newElement.GetComponent<ButtonExecute>();
                    if (buttonExecute != null)
                    {
                        AssignActionToButton(buttonExecute, actionType);
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

    public override void OnDeserialization()
    {
        for (int i = 0; i < uiContainers.Count; i++)
        {
            ((GameObject)uiContainers[i].Reference).SetActive(activeStates[i]);
        }
    }
}