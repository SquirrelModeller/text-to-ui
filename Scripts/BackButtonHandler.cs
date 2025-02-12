using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BackButtonHandler : UdonSharpBehaviour
{
    [HideInInspector] public GameObject currentGroup;
    private UIGenerator uiGenerator;

    public void Initialize(UIGenerator generator)
    {
        uiGenerator = generator;
    }

    public override void Interact()
    {
        if (uiGenerator != null)
        {
            uiGenerator.HandleBackButtonPress(this);
        }
    }
}