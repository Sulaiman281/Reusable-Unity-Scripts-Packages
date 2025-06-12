using UnityEditor;
using UnityEngine;
using WitShells.ApiIntegration;

public static class ApiManagerCreator
{
    [MenuItem("WitShells/API/Create ApiManager")]
    public static void CreateApiManager()
    {
        GameObject go = new GameObject("ApiManager");
        go.AddComponent<Api>();
        go.AddComponent<ApiExecutor>();
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }
}