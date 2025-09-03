using UnityEditor;
using UnityEngine;
using WitShells.MilitaryGridSystem;

[CustomEditor(typeof(SquareGridLayout))]
public class SquareGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SquareGridLayout grid = (SquareGridLayout)target;

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // Add regenerate button
        if (GUILayout.Button("Regenerate Grid"))
        {
            grid.RegenerateGrid();
        }

        // Preset buttons
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Cell Size Presets", EditorStyles.boldLabel);

        if (GUILayout.Button("80x80"))
        {
            grid.SetCellSize(80f);
        }
        if (GUILayout.Button("100x100"))
        {
            grid.SetCellSize(100f);
        }
        if (GUILayout.Button("120x120"))
        {
            grid.SetCellSize(120f);
        }
        if (GUILayout.Button("150x150"))
        {
            grid.SetCellSize(150f);
        }
        if (GUILayout.Button("200x200"))
        {
            grid.SetCellSize(200f);
        }
        if (GUILayout.Button("300x300"))
        {
            grid.SetCellSize(300f);
        }
        if (GUILayout.Button("500x500"))
        {
            grid.SetCellSize(500f);
        }
        if (GUILayout.Button("800x800"))
        {
            grid.SetCellSize(800f);
        }

        if (GUILayout.Button("Generate All Labels"))
        {
            int rows = grid.Rows;
            int cols = grid.Columns;
            grid.SetLabels(GenerateAlphabetLabels(rows), SquareGridLayout.GridLabel.Left);
            grid.SetLabels(GenerateAlphabetLabels(rows), SquareGridLayout.GridLabel.Right);
            grid.SetLabels(GenerateAlphabetLabels(cols), SquareGridLayout.GridLabel.Top);
            grid.SetLabels(GenerateAlphabetLabels(cols), SquareGridLayout.GridLabel.Bottom);
        }

        if (GUILayout.Button("Left Row Labels"))
        {
            int rows = grid.Rows;
            grid.SetLabels(GenerateAlphabetLabels(rows), SquareGridLayout.GridLabel.Left);
        }

        if (GUILayout.Button("Right Row Labels"))
        {
            int rows = grid.Rows;
            grid.SetLabels(GenerateAlphabetLabels(rows), SquareGridLayout.GridLabel.Right);
        }

        if (GUILayout.Button("Top Row Labels"))
        {
            int cols = grid.Columns;
            grid.SetLabels(GenerateAlphabetLabels(cols), SquareGridLayout.GridLabel.Top);
        }

        if (GUILayout.Button("Bottom Row Labels"))
        {
            int cols = grid.Columns;
            grid.SetLabels(GenerateAlphabetLabels(cols), SquareGridLayout.GridLabel.Bottom);
        }

        if (GUILayout.Button("Toggle Labels"))
        {
            grid.ShowLabels = !grid.ShowLabels;
        }
    }

    private string[] GenerateAlphabetLabels(int count)
    {
        string[] labels = new string[count];
        for (int i = 0; i < count; i++)
        {
            labels[i] = i.ToString("00");
        }
        return labels;
    }
}

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