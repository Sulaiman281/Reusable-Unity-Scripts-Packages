namespace WitShells.CanvasDrawTool.Editor
{
    using UnityEngine;
    using UnityEditor;
    using WitShells.CanvasDrawTool;  // Import runtime types

    /// <summary>
    /// Custom inspector for LayerManager with visual layer preview.
    /// Updated for LayerObject-based system with individual RawImage per layer.
    /// </summary>
    [CustomEditor(typeof(LayerManager))]
    public class LayerManagerEditor : Editor
    {
        private LayerManager _layerManager;
        private bool _showLayers = true;
        private bool _showPreview = true;
        private bool _showTransform = true;
        private Vector2 _scrollPos;

        private void OnEnable()
        {
            _layerManager = (LayerManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawLayerList();
            DrawLayerTransformControls();
            DrawCompositePreview();
        }

        private void DrawLayerList()
        {
            _showLayers = EditorGUILayout.Foldout(_showLayers, $"Layers ({_layerManager.LayerCount})", true, EditorStyles.foldoutHeader);
            if (!_showLayers) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(200));

            for (int i = _layerManager.LayerCount - 1; i >= 0; i--)
            {
                var layer = _layerManager.Layers[i];
                if (layer == null) continue;
                
                bool isActive = i == _layerManager.ActiveLayerIndex;

                EditorGUILayout.BeginHorizontal(isActive ? EditorStyles.selectionRect : EditorStyles.helpBox);

                // Layer thumbnail
                if (layer.Texture != null)
                {
                    GUILayout.Label(layer.Texture, GUILayout.Width(32), GUILayout.Height(32));
                }

                EditorGUILayout.BeginVertical();

                // Layer name
                EditorGUILayout.BeginHorizontal();
                if (isActive)
                    EditorGUILayout.LabelField("► " + layer.LayerName, EditorStyles.boldLabel);
                else
                    EditorGUILayout.LabelField("   " + layer.LayerName);
                EditorGUILayout.EndHorizontal();

                // Layer info
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Opacity: {layer.Opacity:P0}", EditorStyles.miniLabel, GUILayout.Width(80));
                GUILayout.Label(layer.IsVisible ? "👁" : "🚫", GUILayout.Width(20));
                GUILayout.Label(layer.IsLocked ? "🔒" : "🔓", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                // Controls
                EditorGUILayout.BeginVertical(GUILayout.Width(60));
                if (!isActive && GUILayout.Button("Select", EditorStyles.miniButton))
                {
                    _layerManager.SetActiveLayer(i);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLayerTransformControls()
        {
            LayerObject activeLayer = _layerManager.ActiveLayer;
            if (activeLayer == null) return;

            _showTransform = EditorGUILayout.Foldout(_showTransform, "Layer Transform", true, EditorStyles.foldoutHeader);
            if (!_showTransform) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Position
            EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
            Vector2 newPos = EditorGUILayout.Vector2Field("", activeLayer.Position);
            if (newPos != activeLayer.Position)
            {
                activeLayer.SetPosition(newPos);
            }

            // Scale
            EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
            Vector2 newScale = EditorGUILayout.Vector2Field("", activeLayer.Scale);
            if (newScale != activeLayer.Scale)
            {
                activeLayer.SetScale(newScale);
            }

            // Rotation
            float newRotation = EditorGUILayout.Slider("Rotation", activeLayer.Rotation, -180f, 180f);
            if (newRotation != activeLayer.Rotation)
            {
                activeLayer.SetRotation(newRotation);
            }

            // Pivot
            EditorGUILayout.LabelField("Pivot", EditorStyles.boldLabel);
            Vector2 newPivot = EditorGUILayout.Vector2Field("", activeLayer.Pivot);
            if (newPivot != activeLayer.Pivot)
            {
                activeLayer.Pivot = newPivot;
                activeLayer.ApplyTransform();
            }

            EditorGUILayout.Space(5);

            // Crop controls
            EditorGUILayout.LabelField("Crop (Pixel Rect)", EditorStyles.boldLabel);
            RectInt currentCrop = activeLayer.CropRect;
            int cropX = EditorGUILayout.IntSlider("X", currentCrop.x, 0, activeLayer.TextureWidth);
            int cropY = EditorGUILayout.IntSlider("Y", currentCrop.y, 0, activeLayer.TextureHeight);
            int cropW = EditorGUILayout.IntSlider("Width", currentCrop.width, 1, activeLayer.TextureWidth);
            int cropH = EditorGUILayout.IntSlider("Height", currentCrop.height, 1, activeLayer.TextureHeight);
            
            RectInt newCrop = new RectInt(cropX, cropY, cropW, cropH);
            if (newCrop.x != currentCrop.x || newCrop.y != currentCrop.y || 
                newCrop.width != currentCrop.width || newCrop.height != currentCrop.height)
            {
                activeLayer.SetCrop(newCrop);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Crop"))
            {
                activeLayer.ResetCrop();
            }
            if (GUILayout.Button("Reset Transform"))
            {
                activeLayer.SetPosition(Vector2.zero);
                activeLayer.SetScale(Vector2.one);
                activeLayer.SetRotation(0);
                activeLayer.Pivot = new Vector2(0.5f, 0.5f);
                activeLayer.ApplyTransform();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCompositePreview()
        {
            _showPreview = EditorGUILayout.Foldout(_showPreview, "Composite Preview", true, EditorStyles.foldoutHeader);
            if (!_showPreview) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_layerManager.CompositeTexture != null)
            {
                float aspectRatio = (float)_layerManager.CanvasWidth / _layerManager.CanvasHeight;
                float previewWidth = EditorGUIUtility.currentViewWidth - 40;
                float previewHeight = previewWidth / aspectRatio;

                if (previewHeight > 300)
                {
                    previewHeight = 300;
                    previewWidth = previewHeight * aspectRatio;
                }

                Rect rect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                EditorGUI.DrawPreviewTexture(rect, _layerManager.CompositeTexture, null, ScaleMode.ScaleToFit);

                EditorGUILayout.LabelField($"Size: {_layerManager.CanvasWidth} x {_layerManager.CanvasHeight}", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No composite texture available", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        public override bool HasPreviewGUI()
        {
            return Application.isPlaying && _layerManager.CompositeTexture != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (_layerManager.CompositeTexture != null)
            {
                EditorGUI.DrawPreviewTexture(r, _layerManager.CompositeTexture, null, ScaleMode.ScaleToFit);
            }
        }
    }
}
