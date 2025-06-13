using UnityEditor;
using UnityEngine;
using WitShells.ApiIntegration;
using System.Collections.Generic;

[CustomEditor(typeof(ApiExecutor))]
public class ApiExecutorEditor : Editor
{
    private SerializedProperty endpointsProp;
    private List<bool> endpointFoldouts = new List<bool>();

    private void OnEnable()
    {
        endpointsProp = serializedObject.FindProperty("endpoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("API Executor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (endpointsProp != null)
        {
            while (endpointFoldouts.Count < endpointsProp.arraySize)
                endpointFoldouts.Add(false);
            while (endpointFoldouts.Count > endpointsProp.arraySize)
                endpointFoldouts.RemoveAt(endpointFoldouts.Count - 1);

            int removeIndex = -1;

            for (int i = 0; i < endpointsProp.arraySize; i++)
            {
                var endpointProp = endpointsProp.GetArrayElementAtIndex(i);
                var endpointNameProp = endpointProp.FindPropertyRelative("endpointName");
                string displayName = endpointNameProp != null && !string.IsNullOrEmpty(endpointNameProp.stringValue)
                    ? endpointNameProp.stringValue
                    : $"Endpoint {i + 1}";

                endpointFoldouts[i] = EditorGUILayout.Foldout(endpointFoldouts[i], displayName, true);

                if (endpointFoldouts[i])
                {
                    EditorGUILayout.BeginVertical("box");

                    // Endpoint Heading
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        removeIndex = i;
                    }
                    EditorGUILayout.EndHorizontal();

                    // Endpoint Name Input Field
                    EditorGUILayout.PropertyField(endpointNameProp, new GUIContent("Endpoint Name"));

                    // Endpoint Settings
                    var endpointSettingsProp = endpointProp.FindPropertyRelative("endpoint");
                    if (endpointSettingsProp != null)
                    {
                        EditorGUILayout.LabelField("Endpoint Settings", EditorStyles.miniBoldLabel);
                        EditorGUILayout.PropertyField(endpointSettingsProp.FindPropertyRelative("Method"), new GUIContent("HTTP Method"));

                        // Content Type field with change detection
                        var contentTypeProp = endpointSettingsProp.FindPropertyRelative("ContentType");
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(contentTypeProp, new GUIContent("Content Type"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Debug.Log($"[ApiExecutorEditor] Content Type changed to: {((ContentType)contentTypeProp.enumValueIndex)} for endpoint '{displayName}'");
                        }

                        EditorGUILayout.PropertyField(endpointSettingsProp.FindPropertyRelative("Path"), new GUIContent("Path"));
                        EditorGUILayout.PropertyField(endpointSettingsProp.FindPropertyRelative("IsSecure"), new GUIContent("Is Secure"));
                        EditorGUILayout.PropertyField(endpointSettingsProp.FindPropertyRelative("responseType"), new GUIContent("Response Type"));
                    }

                    // Default Headers
                    EditorGUILayout.PropertyField(endpointProp.FindPropertyRelative("includeDefaultHeaders"), new GUIContent("Include Default Headers"));

                    // Custom Headers
                    EditorGUILayout.LabelField("Custom Headers", EditorStyles.miniBoldLabel);
                    var customHeadersProp = endpointProp.FindPropertyRelative("customHeaders");
                    EditorGUILayout.PropertyField(customHeadersProp, true);

                    // Body Settings
                    var includeBodyProp = endpointProp.FindPropertyRelative("includeBody");
                    EditorGUILayout.PropertyField(includeBodyProp, new GUIContent("Include Body"));

                    if (includeBodyProp.boolValue)
                    {
                        EditorGUILayout.LabelField("Body Fields", EditorStyles.miniBoldLabel);
                        var bodyFieldsProp = endpointProp.FindPropertyRelative("bodyFields");
                        for (int j = 0; j < bodyFieldsProp.arraySize; j++)
                        {
                            var bodyFieldProp = bodyFieldsProp.GetArrayElementAtIndex(j);
                            EditorGUILayout.BeginHorizontal();

                            // Key field
                            EditorGUILayout.LabelField("Key", GUILayout.Width(30));
                            EditorGUILayout.PropertyField(bodyFieldProp.FindPropertyRelative("key"), GUIContent.none, GUILayout.Width(100));

                            // Type field
                            var typeProp = bodyFieldProp.FindPropertyRelative("type");
                            if (typeProp == null)
                            {
                                // Add type property if missing (for migration)
                                bodyFieldProp.serializedObject.Update();
                                bodyFieldProp.FindPropertyRelative("type").enumValueIndex = 0;
                                bodyFieldProp.serializedObject.ApplyModifiedProperties();
                                typeProp = bodyFieldProp.FindPropertyRelative("type");
                            }
                            EditorGUILayout.PropertyField(typeProp, GUIContent.none, GUILayout.Width(70));
                            GUILayout.Space(10);

                            // Value field
                            EditorGUILayout.LabelField("Value", GUILayout.Width(40));
                            DrawTypedValueField(typeProp, bodyFieldProp);

                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                bodyFieldsProp.DeleteArrayElementAtIndex(j);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        if (GUILayout.Button("Add Body Field"))
                        {
                            bodyFieldsProp.InsertArrayElementAtIndex(bodyFieldsProp.arraySize);
                        }
                    }

                    // Show only the relevant UnityEvent for the selected response type
                    var onSuccessProp = endpointProp.FindPropertyRelative("onSuccess");
                    var responseTypeProp = endpointSettingsProp.FindPropertyRelative("responseType");
                    if (onSuccessProp != null && responseTypeProp != null)
                    {
                        ResponseType responseType = (ResponseType)responseTypeProp.enumValueIndex;
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("On Success", EditorStyles.boldLabel);

                        if (responseType == ResponseType.Authorize)
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Authorization Keys", EditorStyles.boldLabel);

                            var accessTokenKeyProp = onSuccessProp.FindPropertyRelative("accessTokenKey");
                            var refreshTokenKeyProp = onSuccessProp.FindPropertyRelative("refreshTokenKey");

                            EditorGUILayout.PropertyField(accessTokenKeyProp, new GUIContent("Access Token Key"));
                            EditorGUILayout.PropertyField(refreshTokenKeyProp, new GUIContent("Refresh Token Key (Optional)"));
                        }
                        else
                        {

                            switch (responseType)
                            {
                                case ResponseType.Json:
                                    EditorGUILayout.PropertyField(onSuccessProp.FindPropertyRelative("onJsonData"), new GUIContent("On Json Data"));
                                    break;
                                case ResponseType.Bytes:
                                    EditorGUILayout.PropertyField(onSuccessProp.FindPropertyRelative("onBytesData"), new GUIContent("On Bytes Data"));
                                    break;
                                case ResponseType.Text:
                                    EditorGUILayout.PropertyField(onSuccessProp.FindPropertyRelative("onTextData"), new GUIContent("On Text Data"));
                                    break;
                                default:
                                    EditorGUILayout.HelpBox("Unhandled response type.", MessageType.Warning);
                                    break;
                            }
                        }
                    }

                    // On Fail Event
                    EditorGUILayout.PropertyField(endpointProp.FindPropertyRelative("onFail"), new GUIContent("On Fail"));

                    // Execute Button
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Execute"))
                    {
                        if (Application.isPlaying)
                        {
                            ((ApiExecutor)target).Execute(i);
                        }
                        else
                        {
                            Debug.LogWarning("API execution is only available in Play Mode.");
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }

            // Remove endpoint if requested
            if (removeIndex >= 0)
            {
                endpointsProp.DeleteArrayElementAtIndex(removeIndex);
            }
        }

        if (GUILayout.Button("Add Endpoint"))
        {
            endpointsProp.InsertArrayElementAtIndex(endpointsProp.arraySize);
        }

        if (GUILayout.Button("Remove Initial Slash From All Endpoints"))
        {
            for (int i = 0; i < endpointsProp.arraySize; i++)
            {
                var endpointProp = endpointsProp.GetArrayElementAtIndex(i);
                var endpointField = endpointProp.FindPropertyRelative("endpoint");
                if (endpointField != null)
                {
                    var pathProp = endpointField.FindPropertyRelative("Path");
                    if (pathProp != null && pathProp.stringValue.StartsWith("/"))
                    {
                        pathProp.stringValue = pathProp.stringValue.TrimStart('/');
                        EditorUtility.SetDirty(target);
                        Debug.Log($"Removed initial slash from endpoint path: {pathProp.stringValue}");
                    }
                }
            }
        }

        if (GUILayout.Button("Add Initial Slash To All Endpoints"))
        {
            for (int i = 0; i < endpointsProp.arraySize; i++)
            {
                var endpointProp = endpointsProp.GetArrayElementAtIndex(i);
                var endpointField = endpointProp.FindPropertyRelative("endpoint");
                if (endpointField != null)
                {
                    var pathProp = endpointField.FindPropertyRelative("Path");
                    if (pathProp != null)
                    {
                        string path = pathProp.stringValue ?? "";
                        if (!string.IsNullOrEmpty(path))
                        {
                            path = "/" + path.TrimStart('/');
                            pathProp.stringValue = path;
                            EditorUtility.SetDirty(target);
                            Debug.Log($"Added initial slash to endpoint path: {pathProp.stringValue}");
                        }
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Draws the value field according to the selected type
    private void DrawTypedValueField(SerializedProperty typeProp, SerializedProperty bodyFieldProp)
    {
        if (typeProp == null || bodyFieldProp == null)
            return;

        BodyFieldType type = (BodyFieldType)typeProp.enumValueIndex;
        switch (type)
        {
            case BodyFieldType.String:
                EditorGUILayout.PropertyField(bodyFieldProp.FindPropertyRelative("stringValue"), GUIContent.none, GUILayout.Width(120));
                break;
            case BodyFieldType.Integer:
                EditorGUILayout.PropertyField(bodyFieldProp.FindPropertyRelative("intValue"), GUIContent.none, GUILayout.Width(120));
                break;
            case BodyFieldType.Float:
                EditorGUILayout.PropertyField(bodyFieldProp.FindPropertyRelative("floatValue"), GUIContent.none, GUILayout.Width(120));
                break;
            case BodyFieldType.Boolean:
                EditorGUILayout.PropertyField(bodyFieldProp.FindPropertyRelative("boolValue"), GUIContent.none, GUILayout.Width(120));
                break;
            case BodyFieldType.Image:
            case BodyFieldType.Audio:
                // Show current file path (if any)
                var mediaPathProp = bodyFieldProp.FindPropertyRelative("mediaPath");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(mediaPathProp.stringValue, GUILayout.Width(180));
                if (GUILayout.Button("Upload File", GUILayout.Width(90)))
                {
                    string filter = type == BodyFieldType.Image
                        ? "Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif"
                        : "Audio files (*.wav;*.mp3;*.ogg)|*.wav;*.mp3;*.ogg";
                    string path = EditorUtility.OpenFilePanel("Select File", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        mediaPathProp.stringValue = path;
                    }
                }
                EditorGUILayout.EndHorizontal();
                break;
        }
    }
}