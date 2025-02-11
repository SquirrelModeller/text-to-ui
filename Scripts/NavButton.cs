using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class NavButton : UdonSharpBehaviour
{
    [HideInInspector] public GameObject groupContainer;
    private Transform contentParent;

    void Start()
    {
        contentParent = transform.parent;
    }

    public override void Interact()
    {
        if (groupContainer == null) return;

        if (contentParent != null)
        {
            for (int i = 0; i < contentParent.childCount; i++)
            {
                Transform child = contentParent.GetChild(i);
                if (child.gameObject != groupContainer)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        GroupContainer groupScript = groupContainer.GetComponent<GroupContainer>();
        if (groupScript != null && groupScript.parentGroup != null)
        {
            groupScript.parentGroup.SetActive(false);
        }

        groupContainer.SetActive(true);
    }
}
