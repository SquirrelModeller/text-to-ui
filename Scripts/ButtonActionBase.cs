using UnityEngine;
using UdonSharp;

public abstract class ButtonActionBase : UdonSharpBehaviour
{
    public abstract void ExecuteAction(string buttonText);
}
