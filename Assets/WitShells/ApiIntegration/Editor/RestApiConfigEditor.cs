using UnityEditor;
using UnityEngine;
using WitShells.ApiIntegration;
using System.Collections.Generic;

public class RestApiConfigEditor : EditorWindow
{
    private RestApiConfig config;

    // Fields for editing
    private ApiEnvironment environment;
    private string localUrl;
    private string localTestUrl;
    private string productionUrl;
    private bool showLog;
    private string accessToken;
    private List<Header> defaultHeaders = new List<Header>();

    [MenuItem("WitShells/API/RestApiConfig")]
    public static void ShowWindow()
    {
        GetWindow<RestApiConfigEditor>("RestApiConfig Editor");
    }

    private void OnEnable()
    {
        config = RestApiConfig.Instance;
        if (config != null)
        {
            environment = config.environment;
            localUrl = config.localUrl;
            localTestUrl = config.localTestUrl;
            productionUrl = config.productionUrl;
            showLog = config.showLog;
            accessToken = config.accessToken;
            defaultHeaders = new List<Header>(config.defaultHeaders);
        }
    }

    private void OnGUI()
    {
        if (config == null)
        {
            EditorGUILayout.HelpBox("RestApiConfig asset not found in Resources folder.", MessageType.Error);
            if (GUILayout.Button("Create RestApiConfig"))
            {
                CreateConfigAsset();
            }
            return;
        }

        EditorGUILayout.LabelField("Environment Settings", EditorStyles.boldLabel);
        environment = (ApiEnvironment)EditorGUILayout.EnumPopup("Environment", environment);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Base API URLs", EditorStyles.boldLabel);
        localUrl = EditorGUILayout.TextField("Local URL", localUrl);
        localTestUrl = EditorGUILayout.TextField("Local Test URL", localTestUrl);
        productionUrl = EditorGUILayout.TextField("Production URL", productionUrl);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Default Headers", EditorStyles.boldLabel);

        int removeIndex = -1;
        for (int i = 0; i < defaultHeaders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            defaultHeaders[i].key = EditorGUILayout.TextField("Key", defaultHeaders[i].key);
            defaultHeaders[i].value = EditorGUILayout.TextField("Value", defaultHeaders[i].value);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0)
        {
            defaultHeaders.RemoveAt(removeIndex);
        }
        if (GUILayout.Button("Add Header"))
        {
            defaultHeaders.Add(new Header());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
        showLog = EditorGUILayout.Toggle("Show Log", showLog);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Access Token (runtime only)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(accessToken);

        EditorGUILayout.Space();
        if (GUILayout.Button("Save"))
        {
            SaveConfig();
        }
    }

    private void SaveConfig()
    {
        config.environment = environment;
        config.localUrl = localUrl;
        config.localTestUrl = localTestUrl;
        config.productionUrl = productionUrl;
        config.showLog = showLog;
        config.defaultHeaders = new List<Header>(defaultHeaders);

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("RestApiConfig saved!"));
    }

    private void CreateConfigAsset()
    {
        var asset = ScriptableObject.CreateInstance<RestApiConfig>();
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        AssetDatabase.CreateAsset(asset, "Assets/Resources/RestApiConfig.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        config = asset;
        OnEnable();
    }
}