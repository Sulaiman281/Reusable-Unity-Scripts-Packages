using UnityEngine;
using WitShells.DesignPatterns.Core;

public class DraggableUI : DraggableItem<string>
{
    void Start()
    {
        SetData("Hello Drag TEst");
    }

    public override bool CanReturnToOriginalPosition()
    {
        return true;
    }

    public override bool CanSwapWith(IDraggable<string> other)
    {
        return true;
    }

    public override void SwapWith(IDraggable<string> other)
    {
        Debug.Log($"Swapped {GetData()} with {other.GetData()}");
    }
}