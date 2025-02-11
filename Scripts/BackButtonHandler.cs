using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BackButtonHandler : UdonSharpBehaviour
{
    [HideInInspector] public GameObject currentGroup;
    public override void Interact()
    {
        if (currentGroup == null) return;

        GroupContainer groupScript = currentGroup.GetComponent<GroupContainer>();
        if (groupScript != null)
        {
            if (groupScript.parentGroup != null)
            {
                groupScript.parentGroup.SetActive(true);

                Transform parentContent = groupScript.parentGroup.transform.Find("Content");
                if (parentContent != null)
                {
                    for (int i = 0; i < parentContent.childCount; i++)
                    {
                        parentContent.GetChild(i).gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                Transform contentParent = transform.parent.parent;
                for (int i = 0; i < contentParent.childCount; i++)
                {
                    Transform child = contentParent.GetChild(i);
                    if (child.GetComponent<NavButton>() != null)
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
        currentGroup.SetActive(false);
    }
}