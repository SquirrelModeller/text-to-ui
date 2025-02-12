using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class NavButton : UdonSharpBehaviour
{
    [HideInInspector] public GameObject groupContainer;
    private UIGenerator uiGenerator;

    public void Initialize(UIGenerator generator)
    {
        uiGenerator = generator;
    }

    public override void Interact()
    {
        if (uiGenerator != null)
        {
            uiGenerator.HandleNavButtonPress(this);
        }
    }
}