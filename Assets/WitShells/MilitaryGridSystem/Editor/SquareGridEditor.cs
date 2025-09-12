using UnityEditor;
using UnityEngine;
using WitShells.MilitaryGridSystem;

[CustomEditor(typeof(SquareGridLayout))]
public class SquareGridEditor : Editor
{
    private bool showFixedSettings = true;
    private bool showAreaSettings = false;
    private bool showDimensionSettings = false;
    private bool showLabelSettings = false;
    private bool showPresets = false;

    // SerializedProperty references for private fields
    private SerializedProperty gridTypeProp;
    private SerializedProperty cellSizeProp;
    private SerializedProperty maintainAspectRatioProp;
    private SerializedProperty labelTypeProp;
    private SerializedProperty sequentialStartingNumberProp;
    private SerializedProperty totalAreaKmSquareProp;
    private SerializedProperty boxSizeKmSquareProp;
    private SerializedProperty horizontalDistanceKmProp;
    private SerializedProperty verticalDistanceKmProp;
    private SerializedProperty gridCellSizeKmProp;

    private void OnEnable()
    {
        // Initialize SerializedProperty references
        gridTypeProp = serializedObject.FindProperty("gridType");
        cellSizeProp = serializedObject.FindProperty("cellSize");
        maintainAspectRatioProp = serializedObject.FindProperty("maintainAspectRatio");
        labelTypeProp = serializedObject.FindProperty("labelType");
        sequentialStartingNumberProp = serializedObject.FindProperty("sequentialStartingNumber");
        totalAreaKmSquareProp = serializedObject.FindProperty("totalAreaKmSquare");
        boxSizeKmSquareProp = serializedObject.FindProperty("boxSizeKmSquare");
        horizontalDistanceKmProp = serializedObject.FindProperty("horizontalDistanceKm");
        verticalDistanceKmProp = serializedObject.FindProperty("verticalDistanceKm");
        gridCellSizeKmProp = serializedObject.FindProperty("gridCellSizeKm");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SquareGridLayout grid = (SquareGridLayout)target;

        if (GUILayout.Button("Regenerate Grid"))
        {
            Undo.RecordObject(grid, "Regenerate Grid");
            grid.RegenerateGrid();
        }

        // // Draw default inspector for common settings
        // serializedObject.Update();

        // // We'll draw our own custom inspector
        // // DrawDefaultInspector();

        // EditorGUILayout.Space();
        // EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

        // // Grid type selection
        // EditorGUI.BeginChangeCheck();
        // EditorGUILayout.PropertyField(gridTypeProp, new GUIContent("Grid Type"));
        // if (EditorGUI.EndChangeCheck())
        // {
        //     // Update foldout states based on grid type
        //     SquareGridLayout.GridType selectedType = (SquareGridLayout.GridType)gridTypeProp.enumValueIndex;
        //     showFixedSettings = (selectedType == SquareGridLayout.GridType.Fixed);
        //     showAreaSettings = (selectedType == SquareGridLayout.GridType.AreaBased);
        //     showDimensionSettings = (selectedType == SquareGridLayout.GridType.DimensionBased);

        //     serializedObject.ApplyModifiedProperties();
        // }

        // // Line Settings
        // EditorGUILayout.PropertyField(serializedObject.FindProperty("linePrefab"));
        // EditorGUILayout.PropertyField(serializedObject.FindProperty("lineColor"));
        // EditorGUILayout.PropertyField(serializedObject.FindProperty("lineThickness"));

        // // Aspect Ratio Toggle
        // EditorGUI.BeginChangeCheck();
        // EditorGUILayout.PropertyField(maintainAspectRatioProp, new GUIContent("Maintain Aspect Ratio"));
        // if (EditorGUI.EndChangeCheck())
        // {
        //     serializedObject.ApplyModifiedProperties();
        //     grid.SetMaintainAspectRatio(maintainAspectRatioProp.boolValue);
        // }

        // EditorGUILayout.Space();

        // // Fixed Grid Settings
        // showFixedSettings = EditorGUILayout.Foldout(showFixedSettings, "Fixed Grid Settings", true);
        // if (showFixedSettings)
        // {
        //     EditorGUI.indentLevel++;

        //     EditorGUI.BeginChangeCheck();
        //     EditorGUILayout.PropertyField(cellSizeProp, new GUIContent("Cell Size (pixels)"));
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         serializedObject.ApplyModifiedProperties();
        //         grid.SetFixedCellSize(cellSizeProp.floatValue);
        //     }

        //     EditorGUI.indentLevel--;
        // }

        // // Area-Based Grid Settings
        // showAreaSettings = EditorGUILayout.Foldout(showAreaSettings, "Area-Based Grid Settings", true);
        // if (showAreaSettings)
        // {
        //     EditorGUI.indentLevel++;

        //     EditorGUI.BeginChangeCheck();
        //     EditorGUILayout.PropertyField(totalAreaKmSquareProp, new GUIContent("Total Area (km²)"));
        //     EditorGUILayout.PropertyField(boxSizeKmSquareProp, new GUIContent("Box Size (km²)"));
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         serializedObject.ApplyModifiedProperties();
        //         grid.SetAreaBasedGrid(totalAreaKmSquareProp.floatValue, boxSizeKmSquareProp.floatValue);
        //     }

        //     EditorGUI.indentLevel--;
        // }

        // // Dimension-Based Grid Settings
        // showDimensionSettings = EditorGUILayout.Foldout(showDimensionSettings, "Dimension-Based Grid Settings", true);
        // if (showDimensionSettings)
        // {
        //     EditorGUI.indentLevel++;

        //     EditorGUI.BeginChangeCheck();
        //     EditorGUILayout.PropertyField(horizontalDistanceKmProp, new GUIContent("Horizontal Distance (km)"));
        //     EditorGUILayout.PropertyField(verticalDistanceKmProp, new GUIContent("Vertical Distance (km)"));
        //     EditorGUILayout.PropertyField(gridCellSizeKmProp, new GUIContent("Cell Size (km)"));
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         serializedObject.ApplyModifiedProperties();
        //         grid.SetDimensionBasedGrid(horizontalDistanceKmProp.floatValue, verticalDistanceKmProp.floatValue, gridCellSizeKmProp.floatValue);
        //     }

        //     EditorGUI.indentLevel--;
        // }

        // // Label Settings
        // EditorGUILayout.Space();
        // showLabelSettings = EditorGUILayout.Foldout(showLabelSettings, "Label Settings", true);
        // if (showLabelSettings)
        // {
        //     EditorGUI.indentLevel++;

        //     EditorGUILayout.PropertyField(serializedObject.FindProperty("labelPrefab"));

        //     EditorGUI.BeginChangeCheck();
        //     EditorGUILayout.PropertyField(labelTypeProp, new GUIContent("Label Type"));
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         serializedObject.ApplyModifiedProperties();
        //         grid.SetLabelType((SquareGridLayout.LabelType)labelTypeProp.enumValueIndex);
        //     }

        //     SquareGridLayout.LabelType currentLabelType = (SquareGridLayout.LabelType)labelTypeProp.enumValueIndex;
        //     if (currentLabelType == SquareGridLayout.LabelType.Sequential)
        //     {
        //         EditorGUI.BeginChangeCheck();
        //         EditorGUILayout.PropertyField(sequentialStartingNumberProp, new GUIContent("Starting Number"));
        //         if (EditorGUI.EndChangeCheck())
        //         {
        //             serializedObject.ApplyModifiedProperties();
        //             grid.SetSequentialNumbering(sequentialStartingNumberProp.intValue);
        //         }
        //     }

        //     EditorGUILayout.PropertyField(serializedObject.FindProperty("labelSize"));
        //     EditorGUILayout.PropertyField(serializedObject.FindProperty("labelIndexOffset"));
        //     EditorGUILayout.PropertyField(serializedObject.FindProperty("labelOffset"));

        //     if (currentLabelType == SquareGridLayout.LabelType.TransformBased)
        //     {
        //         EditorGUILayout.PropertyField(serializedObject.FindProperty("customUtmReminder"));
        //         EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetPosition"));
        //     }

        //     EditorGUI.BeginChangeCheck();
        //     bool showLabels = EditorGUILayout.Toggle("Show Labels", grid.ShowLabels);
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         Undo.RecordObject(grid, "Toggle Label Visibility");
        //         grid.ShowLabels = showLabels;
        //     }

        //     EditorGUI.indentLevel--;
        // }

        // serializedObject.ApplyModifiedProperties();

        // // Grid Info Display
        // EditorGUILayout.Space();
        // EditorGUILayout.LabelField("Grid Information", EditorStyles.boldLabel);
        // EditorGUILayout.HelpBox(grid.GetGridInfo(), MessageType.Info);

        // // Quick Presets Section
        // EditorGUILayout.Space();
        // showPresets = EditorGUILayout.Foldout(showPresets, "Quick Presets", true);
        // if (showPresets)
        // {
        //     EditorGUILayout.LabelField("Cell Size Presets (pixels)", EditorStyles.boldLabel);
        //     EditorGUILayout.BeginHorizontal();
        //     if (GUILayout.Button("30px")) grid.SetFixedCellSize(30f);
        //     if (GUILayout.Button("50px")) grid.SetFixedCellSize(50f);
        //     if (GUILayout.Button("100px")) grid.SetFixedCellSize(100f);
        //     if (GUILayout.Button("200px")) grid.SetFixedCellSize(200f);
        //     EditorGUILayout.EndHorizontal();

        //     EditorGUILayout.Space();
        //     EditorGUILayout.LabelField("Area-Based Presets", EditorStyles.boldLabel);
        //     EditorGUILayout.BeginHorizontal();
        //     if (GUILayout.Button("1km² (100×100m)")) grid.SetAreaBasedGrid(1f, 0.01f);
        //     if (GUILayout.Button("10km² (500×500m)")) grid.SetAreaBasedGrid(10f, 0.25f);
        //     if (GUILayout.Button("100km² (1×1km)")) grid.SetAreaBasedGrid(100f, 1f);
        //     if (GUILayout.Button("1000km² (10×10km)")) grid.SetAreaBasedGrid(1000f, 100f);
        //     EditorGUILayout.EndHorizontal();

        //     EditorGUILayout.Space();
        //     EditorGUILayout.LabelField("Dimension-Based Presets", EditorStyles.boldLabel);
        //     EditorGUILayout.BeginHorizontal();
        //     if (GUILayout.Button("1×1km (100m)")) grid.SetDimensionBasedGrid(1f, 1f, 0.1f);
        //     if (GUILayout.Button("10×10km (1km)")) grid.SetDimensionBasedGrid(10f, 10f, 1f);
        //     if (GUILayout.Button("100×100km (10km)")) grid.SetDimensionBasedGrid(100f, 100f, 10f);
        //     EditorGUILayout.EndHorizontal();
        // }

        // // Force Regenerate Button
        // EditorGUILayout.Space();
        // if (GUILayout.Button("Regenerate Grid"))
        // {
        //     Undo.RecordObject(grid, "Regenerate Grid");
        //     grid.RegenerateGrid();
        // }
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