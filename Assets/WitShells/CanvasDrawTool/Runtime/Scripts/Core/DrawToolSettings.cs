namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// ScriptableObject that holds all drawing tool settings.
    /// This is the single source of truth for brush, color, and canvas settings.
    /// </summary>
    [CreateAssetMenu(fileName = "DrawToolSettings", menuName = "WitShells/Canvas Draw Tool/Settings")]
    public class DrawToolSettings : ScriptableObject
    {
        [Header("Brush Settings")]
        [SerializeField] private string _brushName = "Default";
        [SerializeField] private Brush.BrushType _brushType = Brush.BrushType.Round;
        [SerializeField] private int _brushSize = 10;
        [SerializeField] private float _brushHardness = 1f;
        [SerializeField] private float _brushOpacity = 1f;
        [SerializeField] private float _brushSpacing = 0.25f;
        [SerializeField] private float _brushJitter = 0f;
        [SerializeField] private float _brushScatter = 0f;
        [SerializeField] private bool _brushAntiAlias = true;

        [Header("Pressure Settings")]
        [SerializeField] private bool _usePressureForSize = false;
        [SerializeField] private bool _usePressureForOpacity = true;
        [SerializeField] private float _minPressureSize = 0.1f;
        [SerializeField] private float _minPressureOpacity = 0.1f;

        [Header("Color Settings")]
        [SerializeField] private Color _primaryColor = Color.black;
        [SerializeField] private Color _secondaryColor = Color.white;

        [Header("Canvas Settings")]
        [SerializeField] private int _defaultCanvasWidth = 1024;
        [SerializeField] private int _defaultCanvasHeight = 1024;
        [SerializeField] private Color _defaultBackgroundColor = Color.white;

        [Header("Tool Settings")]
        [SerializeField] private DrawingTool _currentTool = DrawingTool.Brush;

        [Header("Brush Presets")]
        [SerializeField] private List<BrushPreset> _brushPresets = new List<BrushPreset>();

        // Events for settings changes
        public event Action OnSettingsChanged;
        public event Action<Brush.BrushType> OnBrushTypeChanged;
        public event Action<int> OnBrushSizeChanged;
        public event Action<float> OnBrushOpacityChanged;
        public event Action<float> OnBrushHardnessChanged;
        public event Action<Color> OnPrimaryColorChanged;
        public event Action<Color> OnSecondaryColorChanged;
        public event Action<DrawingTool> OnToolChanged;

        public enum DrawingTool
        {
            Brush,
            Eraser,
            Eyedropper,
            Fill,
            Line,
            Rectangle,
            Ellipse,
            Pan,
            Zoom,
            Transform  // Select and transform imported images
        }

        [System.Serializable]
        public class BrushPreset
        {
            public string Name = "Preset";
            public Brush.BrushType Type = Brush.BrushType.Round;
            public int Size = 10;
            public float Hardness = 1f;
            public float Opacity = 1f;
            public float Spacing = 0.25f;
        }

        // ============ Brush Properties ============

        public string BrushName
        {
            get => _brushName;
            set
            {
                if (_brushName != value)
                {
                    _brushName = value;
                    NotifySettingsChanged();
                }
            }
        }

        public Brush.BrushType BrushType
        {
            get => _brushType;
            set
            {
                if (_brushType != value)
                {
                    _brushType = value;
                    OnBrushTypeChanged?.Invoke(value);
                    NotifySettingsChanged();
                }
            }
        }

        public int BrushSize
        {
            get => _brushSize;
            set
            {
                int clamped = Mathf.Max(1, value);
                if (_brushSize != clamped)
                {
                    _brushSize = clamped;
                    OnBrushSizeChanged?.Invoke(clamped);
                    NotifySettingsChanged();
                }
            }
        }

        public float BrushHardness
        {
            get => _brushHardness;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (!Mathf.Approximately(_brushHardness, clamped))
                {
                    _brushHardness = clamped;
                    OnBrushHardnessChanged?.Invoke(clamped);
                    NotifySettingsChanged();
                }
            }
        }

        public float BrushOpacity
        {
            get => _brushOpacity;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (!Mathf.Approximately(_brushOpacity, clamped))
                {
                    _brushOpacity = clamped;
                    OnBrushOpacityChanged?.Invoke(clamped);
                    NotifySettingsChanged();
                }
            }
        }

        public float BrushSpacing
        {
            get => _brushSpacing;
            set
            {
                float clamped = Mathf.Max(0.01f, value);
                if (!Mathf.Approximately(_brushSpacing, clamped))
                {
                    _brushSpacing = clamped;
                    NotifySettingsChanged();
                }
            }
        }

        public float BrushJitter
        {
            get => _brushJitter;
            set
            {
                if (!Mathf.Approximately(_brushJitter, value))
                {
                    _brushJitter = value;
                    NotifySettingsChanged();
                }
            }
        }

        public float BrushScatter
        {
            get => _brushScatter;
            set
            {
                if (!Mathf.Approximately(_brushScatter, value))
                {
                    _brushScatter = value;
                    NotifySettingsChanged();
                }
            }
        }

        public bool BrushAntiAlias
        {
            get => _brushAntiAlias;
            set
            {
                if (_brushAntiAlias != value)
                {
                    _brushAntiAlias = value;
                    NotifySettingsChanged();
                }
            }
        }

        // ============ Pressure Properties ============

        public bool UsePressureForSize
        {
            get => _usePressureForSize;
            set
            {
                if (_usePressureForSize != value)
                {
                    _usePressureForSize = value;
                    NotifySettingsChanged();
                }
            }
        }

        public bool UsePressureForOpacity
        {
            get => _usePressureForOpacity;
            set
            {
                if (_usePressureForOpacity != value)
                {
                    _usePressureForOpacity = value;
                    NotifySettingsChanged();
                }
            }
        }

        public float MinPressureSize
        {
            get => _minPressureSize;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (!Mathf.Approximately(_minPressureSize, clamped))
                {
                    _minPressureSize = clamped;
                    NotifySettingsChanged();
                }
            }
        }

        public float MinPressureOpacity
        {
            get => _minPressureOpacity;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (!Mathf.Approximately(_minPressureOpacity, clamped))
                {
                    _minPressureOpacity = clamped;
                    NotifySettingsChanged();
                }
            }
        }

        // ============ Color Properties ============

        public Color PrimaryColor
        {
            get => _primaryColor;
            set
            {
                if (_primaryColor != value)
                {
                    _primaryColor = value;
                    OnPrimaryColorChanged?.Invoke(value);
                    NotifySettingsChanged();
                }
            }
        }

        public Color SecondaryColor
        {
            get => _secondaryColor;
            set
            {
                if (_secondaryColor != value)
                {
                    _secondaryColor = value;
                    OnSecondaryColorChanged?.Invoke(value);
                    NotifySettingsChanged();
                }
            }
        }

        // ============ Canvas Properties ============

        public int DefaultCanvasWidth
        {
            get => _defaultCanvasWidth;
            set
            {
                if (_defaultCanvasWidth != value)
                {
                    _defaultCanvasWidth = Mathf.Max(1, value);
                    NotifySettingsChanged();
                }
            }
        }

        public int DefaultCanvasHeight
        {
            get => _defaultCanvasHeight;
            set
            {
                if (_defaultCanvasHeight != value)
                {
                    _defaultCanvasHeight = Mathf.Max(1, value);
                    NotifySettingsChanged();
                }
            }
        }

        public Color DefaultBackgroundColor
        {
            get => _defaultBackgroundColor;
            set
            {
                if (_defaultBackgroundColor != value)
                {
                    _defaultBackgroundColor = value;
                    NotifySettingsChanged();
                }
            }
        }

        // ============ Tool Properties ============

        public DrawingTool CurrentTool
        {
            get => _currentTool;
            set
            {
                if (_currentTool != value)
                {
                    _currentTool = value;
                    OnToolChanged?.Invoke(value);
                    NotifySettingsChanged();
                }
            }
        }

        // ============ Presets ============

        public List<BrushPreset> BrushPresets => _brushPresets;

        // ============ Methods ============

        /// <summary>
        /// Create a Brush object from current settings.
        /// </summary>
        public Brush CreateBrush()
        {
            return new Brush
            {
                Name = _brushName,
                Type = _brushType,
                Size = _brushSize,
                Hardness = _brushHardness,
                Opacity = _brushOpacity,
                Spacing = _brushSpacing,
                Color = _primaryColor,
                UsePressureForSize = _usePressureForSize,
                UsePressureForOpacity = _usePressureForOpacity,
                MinPressureSize = _minPressureSize,
                MinPressureOpacity = _minPressureOpacity,
                Jitter = _brushJitter,
                Scatter = _brushScatter,
                AntiAlias = _brushAntiAlias
            };
        }

        /// <summary>
        /// Apply a brush preset to current settings.
        /// </summary>
        public void ApplyPreset(BrushPreset preset)
        {
            if (preset == null) return;

            _brushName = preset.Name;
            _brushType = preset.Type;
            _brushSize = preset.Size;
            _brushHardness = preset.Hardness;
            _brushOpacity = preset.Opacity;
            _brushSpacing = preset.Spacing;

            OnBrushTypeChanged?.Invoke(_brushType);
            OnBrushSizeChanged?.Invoke(_brushSize);
            OnBrushOpacityChanged?.Invoke(_brushOpacity);
            OnBrushHardnessChanged?.Invoke(_brushHardness);
            NotifySettingsChanged();
        }

        /// <summary>
        /// Apply settings from a Brush object.
        /// </summary>
        public void ApplyBrush(Brush brush)
        {
            if (brush == null) return;

            _brushName = brush.Name;
            _brushType = brush.Type;
            _brushSize = brush.Size;
            _brushHardness = brush.Hardness;
            _brushOpacity = brush.Opacity;
            _brushSpacing = brush.Spacing;
            _brushJitter = brush.Jitter;
            _brushScatter = brush.Scatter;
            _brushAntiAlias = brush.AntiAlias;
            _usePressureForSize = brush.UsePressureForSize;
            _usePressureForOpacity = brush.UsePressureForOpacity;
            _minPressureSize = brush.MinPressureSize;
            _minPressureOpacity = brush.MinPressureOpacity;

            OnBrushTypeChanged?.Invoke(_brushType);
            OnBrushSizeChanged?.Invoke(_brushSize);
            OnBrushOpacityChanged?.Invoke(_brushOpacity);
            OnBrushHardnessChanged?.Invoke(_brushHardness);
            NotifySettingsChanged();
        }

        /// <summary>
        /// Swap primary and secondary colors.
        /// </summary>
        public void SwapColors()
        {
            Color temp = _primaryColor;
            _primaryColor = _secondaryColor;
            _secondaryColor = temp;

            OnPrimaryColorChanged?.Invoke(_primaryColor);
            OnSecondaryColorChanged?.Invoke(_secondaryColor);
            NotifySettingsChanged();
        }

        /// <summary>
        /// Get effective brush size based on pressure.
        /// </summary>
        public int GetEffectiveSize(float pressure)
        {
            if (!_usePressureForSize) return _brushSize;
            float t = Mathf.Lerp(_minPressureSize, 1f, pressure);
            return Mathf.Max(1, Mathf.RoundToInt(_brushSize * t));
        }

        /// <summary>
        /// Get effective opacity based on pressure.
        /// </summary>
        public float GetEffectiveOpacity(float pressure)
        {
            if (!_usePressureForOpacity) return _brushOpacity;
            return Mathf.Lerp(_minPressureOpacity * _brushOpacity, _brushOpacity, pressure);
        }

        /// <summary>
        /// Reset to default settings.
        /// </summary>
        public void ResetToDefaults()
        {
            _brushName = "Default";
            _brushType = Brush.BrushType.Round;
            _brushSize = 10;
            _brushHardness = 1f;
            _brushOpacity = 1f;
            _brushSpacing = 0.25f;
            _brushJitter = 0f;
            _brushScatter = 0f;
            _brushAntiAlias = true;
            _usePressureForSize = false;
            _usePressureForOpacity = true;
            _minPressureSize = 0.1f;
            _minPressureOpacity = 0.1f;
            _primaryColor = Color.black;
            _secondaryColor = Color.white;
            _currentTool = DrawingTool.Brush;

            NotifySettingsChanged();
        }

        /// <summary>
        /// Create default presets.
        /// </summary>
        public void CreateDefaultPresets()
        {
            _brushPresets.Clear();
            
            _brushPresets.Add(new BrushPreset
            {
                Name = "Default",
                Type = Brush.BrushType.Round,
                Size = 10,
                Hardness = 1f,
                Opacity = 1f,
                Spacing = 0.25f
            });

            _brushPresets.Add(new BrushPreset
            {
                Name = "Soft Brush",
                Type = Brush.BrushType.Soft,
                Size = 20,
                Hardness = 0.5f,
                Opacity = 0.8f,
                Spacing = 0.25f
            });

            _brushPresets.Add(new BrushPreset
            {
                Name = "Airbrush",
                Type = Brush.BrushType.Airbrush,
                Size = 30,
                Hardness = 0.2f,
                Opacity = 0.3f,
                Spacing = 0.1f
            });

            _brushPresets.Add(new BrushPreset
            {
                Name = "Pencil",
                Type = Brush.BrushType.Pencil,
                Size = 2,
                Hardness = 1f,
                Opacity = 1f,
                Spacing = 0.25f
            });

            _brushPresets.Add(new BrushPreset
            {
                Name = "Eraser",
                Type = Brush.BrushType.Round,
                Size = 20,
                Hardness = 1f,
                Opacity = 1f,
                Spacing = 0.25f
            });
        }

        private void NotifySettingsChanged()
        {
            OnSettingsChanged?.Invoke();
        }

        private void OnValidate()
        {
            // Clamp values when edited in inspector
            _brushSize = Mathf.Max(1, _brushSize);
            _brushHardness = Mathf.Clamp01(_brushHardness);
            _brushOpacity = Mathf.Clamp01(_brushOpacity);
            _brushSpacing = Mathf.Max(0.01f, _brushSpacing);
            _minPressureSize = Mathf.Clamp01(_minPressureSize);
            _minPressureOpacity = Mathf.Clamp01(_minPressureOpacity);
            _defaultCanvasWidth = Mathf.Max(1, _defaultCanvasWidth);
            _defaultCanvasHeight = Mathf.Max(1, _defaultCanvasHeight);
        }
    }
}
