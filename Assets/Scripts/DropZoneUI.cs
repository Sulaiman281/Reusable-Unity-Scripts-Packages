using UnityEngine;
using WitShells.DesignPatterns.Core;

public class DropZoneUI : DropZone<string>
{
    protected override bool CanAcceptDrop(string data)
    {
        return true;
    }

    protected override void HandleDrop(string data, IDraggable<string> draggable)
    {
        Debug.Log($"Dropped data: {data}");
    }
}