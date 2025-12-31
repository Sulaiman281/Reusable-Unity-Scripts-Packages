namespace WitShells.CanvasDrawTool
{
    using UnityEngine;

    /// <summary>
    /// Defines brush settings and properties for drawing.
    /// </summary>
    [System.Serializable]
    public class Brush
    {
        [Header("Basic Settings")]
        public string Name = "Default Brush";
        public BrushType Type = BrushType.Round;
        public int Size = 10;
        public float Hardness = 1f;
        public float Opacity = 1f;
        public float Spacing = 0.25f;

        [Header("Color")]
        public Color Color = Color.black;

        [Header("Dynamics")]
        public bool UsePressureForSize = false;
        public bool UsePressureForOpacity = true;
        public float MinPressureSize = 0.1f;
        public float MinPressureOpacity = 0.1f;

        [Header("Advanced")]
        public float Jitter = 0f;
        public float Scatter = 0f;
        public bool AntiAlias = true;

        public enum BrushType
        {
            Round,
            Square,
            Soft,
            Airbrush,
            Pencil,
            Marker,
            Custom
        }

        /// <summary>
        /// Create a default brush.
        /// </summary>
        public static Brush Default => new Brush
        {
            Name = "Default",
            Type = BrushType.Round,
            Size = 10,
            Hardness = 1f,
            Opacity = 1f,
            Color = Color.black
        };

        /// <summary>
        /// Create a soft brush.
        /// </summary>
        public static Brush Soft => new Brush
        {
            Name = "Soft Brush",
            Type = BrushType.Soft,
            Size = 20,
            Hardness = 0.5f,
            Opacity = 0.8f,
            Color = Color.black
        };

        /// <summary>
        /// Create an airbrush.
        /// </summary>
        public static Brush Airbrush => new Brush
        {
            Name = "Airbrush",
            Type = BrushType.Airbrush,
            Size = 30,
            Hardness = 0.2f,
            Opacity = 0.3f,
            Color = Color.black,
            Spacing = 0.1f
        };

        /// <summary>
        /// Create a pencil brush.
        /// </summary>
        public static Brush Pencil => new Brush
        {
            Name = "Pencil",
            Type = BrushType.Pencil,
            Size = 2,
            Hardness = 1f,
            Opacity = 1f,
            Color = Color.black,
            AntiAlias = false
        };

        /// <summary>
        /// Create an eraser brush.
        /// </summary>
        public static Brush Eraser => new Brush
        {
            Name = "Eraser",
            Type = BrushType.Round,
            Size = 20,
            Hardness = 1f,
            Opacity = 1f,
            Color = Color.clear
        };

        /// <summary>
        /// Clone this brush.
        /// </summary>
        public Brush Clone()
        {
            return new Brush
            {
                Name = Name,
                Type = Type,
                Size = Size,
                Hardness = Hardness,
                Opacity = Opacity,
                Spacing = Spacing,
                Color = Color,
                UsePressureForSize = UsePressureForSize,
                UsePressureForOpacity = UsePressureForOpacity,
                MinPressureSize = MinPressureSize,
                MinPressureOpacity = MinPressureOpacity,
                Jitter = Jitter,
                Scatter = Scatter,
                AntiAlias = AntiAlias
            };
        }

        /// <summary>
        /// Get the effective size based on pressure.
        /// </summary>
        public int GetEffectiveSize(float pressure)
        {
            if (!UsePressureForSize) return Size;
            float t = Mathf.Lerp(MinPressureSize, 1f, pressure);
            return Mathf.Max(1, Mathf.RoundToInt(Size * t));
        }

        /// <summary>
        /// Get the effective opacity based on pressure.
        /// </summary>
        public float GetEffectiveOpacity(float pressure)
        {
            if (!UsePressureForOpacity) return Opacity;
            return Mathf.Lerp(MinPressureOpacity * Opacity, Opacity, pressure);
        }

        /// <summary>
        /// Get the brush alpha at a given distance from center (0-1).
        /// </summary>
        public float GetAlphaAtDistance(float normalizedDistance)
        {
            switch (Type)
            {
                case BrushType.Round:
                case BrushType.Pencil:
                    if (Hardness >= 1f)
                    {
                        return normalizedDistance <= 1f ? 1f : 0f;
                    }
                    else
                    {
                        float edge = 1f - Hardness;
                        if (normalizedDistance <= 1f - edge)
                            return 1f;
                        else if (normalizedDistance <= 1f)
                            return 1f - (normalizedDistance - (1f - edge)) / edge;
                        else
                            return 0f;
                    }

                case BrushType.Soft:
                    return Mathf.Clamp01(1f - normalizedDistance) * Hardness;

                case BrushType.Airbrush:
                    float falloff = 1f - normalizedDistance * normalizedDistance;
                    return Mathf.Clamp01(falloff * Hardness * 0.5f);

                case BrushType.Square:
                    return normalizedDistance <= 1f ? 1f : 0f;

                case BrushType.Marker:
                    return normalizedDistance <= 0.8f ? 0.7f : (normalizedDistance <= 1f ? 0.3f : 0f);

                default:
                    return normalizedDistance <= 1f ? 1f : 0f;
            }
        }
    }
}
