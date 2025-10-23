using UnityEditor;
using UnityEngine;
using WitShells.MilitaryGridSystem;
public static class EditorSquareGridLayoutExtension
{
    [MenuItem("WitShells/MilitaryGridSystem/Create")]
    public static void CreateSquareGridLayout()
    {
        var selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject in the hierarchy.", "OK");
            return;
        }

        GameObject gridObject = new GameObject("SquareGridLayout");
        Undo.RegisterCreatedObjectUndo(gridObject, "Create SquareGridLayout");
        Undo.AddComponent<RectTransform>(gridObject);
        Undo.AddComponent<SquareGridLayout>(gridObject);
        gridObject.transform.SetParent(selectedObject.transform, false);
        RectTransform rectTransform = gridObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        Selection.activeGameObject = gridObject;
    }
}