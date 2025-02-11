
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CustomAction : ButtonActionBase
{
    [SerializeField] private GameObject targetObject;

    public override void ExecuteAction(string buttonText)
    {
        // Custom logic here
    }
}