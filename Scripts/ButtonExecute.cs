using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ButtonExecute : UdonSharpBehaviour
{
    [HideInInspector] public ButtonActionBase actionScript;
    private TextMeshProUGUI textObject;

    public override void Interact()
    {
        if (textObject == null)
        {
            textObject = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (actionScript != null && textObject != null)
        {
            actionScript.ExecuteAction(textObject.text);
        }
    }
}
