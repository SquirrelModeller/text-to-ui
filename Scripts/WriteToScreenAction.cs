
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class WriteToScreenAction : ButtonActionBase
{
    [SerializeField] private TextMeshProUGUI targetText;

    public override void ExecuteAction(string buttonText)
    {
        if (targetText != null)
        {
            targetText.text = buttonText;
        }
    }
}