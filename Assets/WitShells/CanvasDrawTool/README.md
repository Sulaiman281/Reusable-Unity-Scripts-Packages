# WitShells Canvas Draw Tool

A complete professional drawing tool for Unity runtime applications. Create custom textures, paint on canvas, manage layers, and export images — all within your game at runtime.

## Features

### 🎨 Drawing System
- **Multiple Brush Types**: Round, Square, Soft, Airbrush, Pencil, Marker, and Custom brushes
- **Adjustable Settings**: Size, opacity, hardness, spacing, and flow
- **Brush Dynamics**: Pressure-based size and opacity variations
- **Eraser Tool**: Precise erasing with all brush types
- **Flood Fill**: Fill areas with a single click
- **Shape Tools**: Lines, rectangles, and ellipses

### 📚 Layer System
- **Unlimited Layers**: Create, delete, duplicate, and reorder layers
- **Blend Modes**: Normal, Multiply, Screen, Overlay, Add, Subtract
- **Layer Controls**: Visibility toggle, lock, opacity, and naming
- **Merge Operations**: Merge down, merge visible, flatten all
- **Per-Layer Import**: Import images directly to layers

### ✒️ Pen Tablet Support (New Input System)
- **Pressure Sensitivity**: Conditional opacity based on pen pressure
- **Tilt Support**: Use pen angle for brush effects
- **Barrel Button**: Configurable for Eraser, Eyedropper, or Pan
- **Eraser Tip**: Automatic detection of pen eraser end
- **Works With**: Wacom, Surface Pen, Apple Pencil, and more

### 🎭 Color System
- **Color Picker**: HSV square with hue bar and alpha slider
- **RGB/Hex Input**: Direct value entry
- **Color History**: Quick access to recently used colors
- **Eyedropper**: Pick colors from canvas

### 💾 Export & Import
- **Export PNG**: With full alpha transparency
- **Export JPG**: With quality settings
- **Import Images**: Load external images to layers
- **Custom Resolution**: Create canvas at any size

## Installation

### Via Git URL (Recommended)
Open Package Manager → Add package from git URL:
```
https://github.com/WitShells/CanvasDrawTool.git
```

### Via Local Package
Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.witshells.canvasdrawtool": "file:../Assets/WitShells/CanvasDrawTool"
  }
}
```

## Requirements

- Unity 2021.3 or higher
- Input System Package (`com.unity.inputsystem`)
- TextMeshPro (`com.unity.textmeshpro`)

## Quick Start

### 1. Basic Setup

```csharp
using WitShells.CanvasDrawTool;
using UnityEngine;

public class DrawingSetup : MonoBehaviour
{
    public RawImage canvasImage;
    
    private LayerManager layerManager;
    private DrawingEngine drawingEngine;
    private Brush brush;
    
    void Start()
    {
        // Initialize layer manager with canvas size
        layerManager = gameObject.AddComponent<LayerManager>();
        layerManager.Initialize(1024, 1024);
        
        // Create a drawing layer
        layerManager.CreateLayer("Drawing Layer");
        layerManager.SetActiveLayer(1);
        
        // Initialize drawing engine
        drawingEngine = gameObject.AddComponent<DrawingEngine>();
        drawingEngine.Initialize(layerManager);
        
        // Set up brush
        brush = Brush.Default;
        
        // Display composite texture
        canvasImage.texture = layerManager.CompositeTexture;
    }
}
```

### 2. Handle Drawing Input

```csharp
using WitShells.CanvasDrawTool;
using UnityEngine;

public class SimpleDrawing : MonoBehaviour
{
    public DrawingEngine drawingEngine;
    public LayerManager layerManager;
    public Color drawColor = Color.black;
    public Brush brush;
    
    private bool isDrawing;
    private Vector2Int lastPos;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int pos = GetCanvasPosition();
            drawingEngine.BeginDraw(pos, brush, drawColor);
            lastPos = pos;
            isDrawing = true;
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            Vector2Int pos = GetCanvasPosition();
            if (pos != lastPos)
            {
                drawingEngine.ContinueDraw(pos, brush, drawColor);
                lastPos = pos;
            }
        }
        else if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            Vector2Int pos = GetCanvasPosition();
            drawingEngine.EndDraw(pos, brush, drawColor);
            isDrawing = false;
        }
        
        // Update display
        layerManager.UpdateComposite();
    }
    
    Vector2Int GetCanvasPosition()
    {
        // Convert screen to canvas coordinates
        // Implementation depends on your UI setup
        return Vector2Int.zero;
    }
}
```

### 3. Using Pen Input with Pressure

```csharp
using WitShells.CanvasDrawTool;
using UnityEngine;

public class PenDrawing : MonoBehaviour
{
    public PenInput penInput;
    public DrawingEngine drawingEngine;
    public Brush brush;
    
    void Start()
    {
        // Configure pressure settings
        penInput.UsePressureForOpacity = true;  // Pressure affects opacity
        penInput.UsePressureForSize = false;    // Pressure doesn't affect size
        
        // Subscribe to events
        penInput.OnDrawStart.AddListener(HandleDrawStart);
        penInput.OnDrawMove.AddListener(HandleDrawMove);
        penInput.OnDrawEnd.AddListener(HandleDrawEnd);
    }
    
    void HandleDrawStart(Vector2 canvasPos, float pressure)
    {
        // Pressure is automatically applied to opacity
        float effectiveOpacity = penInput.GetEffectiveOpacity(brush.Opacity, pressure);
        int effectiveSize = penInput.GetEffectiveSize(brush.Size, pressure);
        
        Debug.Log($"Pressure: {pressure}, Opacity: {effectiveOpacity}");
    }
    
    void HandleDrawMove(Vector2 canvasPos, float pressure) { }
    void HandleDrawEnd(Vector2 canvasPos, float pressure) { }
}
```

### 4. Layer Management

```csharp
// Create layers
int layer1 = layerManager.CreateLayer("Sketch");
int layer2 = layerManager.CreateLayer("Colors");
int layer3 = layerManager.CreateLayer("Details");

// Set active layer
layerManager.SetActiveLayer(layer2);

// Toggle visibility
layerManager.GetLayer(layer1).IsVisible = false;

// Set blend mode
layerManager.GetLayer(layer2).BlendMode = BlendMode.Multiply;

// Set opacity
layerManager.GetLayer(layer3).Opacity = 0.5f;

// Merge layers
layerManager.MergeDown(layer3);  // Merge layer3 into layer2

// Flatten all
layerManager.Flatten();
```

### 5. Export Image

```csharp
// Export as PNG (with transparency)
layerManager.ExportAsPNG("Assets/MyDrawing.png");

// Export as JPG (with quality setting)
layerManager.ExportAsJPG("Assets/MyDrawing.jpg", 90);

// Get texture data for runtime use
Texture2D finalTexture = layerManager.CompositeTexture;
byte[] pngData = finalTexture.EncodeToPNG();
```

## Components Overview

| Component | Description |
|-----------|-------------|
| `DrawToolController` | Main controller that wires everything together |
| `LayerManager` | Manages layers and composite texture |
| `DrawLayer` | Individual layer with pixel manipulation |
| `DrawingEngine` | Core drawing operations |
| `Brush` | Brush configuration and dynamics |
| `DrawingInput` | Mouse/touch input handling |
| `PenInput` | Tablet pen input with pressure |
| `DrawCanvasUI` | Canvas display and zoom/pan |
| `ColorPickerUI` | HSV color picker interface |
| `LayerPanelUI` | Layer list management UI |
| `BrushSettingsUI` | Brush parameter controls |

## Brush Types

```csharp
// Use preset brushes
Brush defaultBrush = Brush.Default;
Brush softBrush = Brush.Soft;
Brush airbrush = Brush.Airbrush;
Brush pencil = Brush.Pencil;
Brush eraser = Brush.Eraser;

// Create custom brush
Brush customBrush = new Brush
{
    Name = "Custom",
    Type = BrushType.Round,
    Size = 50,
    Hardness = 0.7f,
    Opacity = 0.8f,
    Spacing = 0.15f,
    Flow = 1f
};
```

## Blend Modes

- **Normal**: Standard opacity blending
- **Multiply**: Darkens underlying colors
- **Screen**: Lightens underlying colors  
- **Overlay**: Combines multiply and screen
- **Add**: Adds color values (brightens)
- **Subtract**: Subtracts color values (darkens)

## Events

```csharp
// Drawing events
drawingInput.OnDrawStart.AddListener((pos, pressure) => { });
drawingInput.OnDrawMove.AddListener((pos, pressure) => { });
drawingInput.OnDrawEnd.AddListener((pos, pressure) => { });

// Pen-specific events
penInput.OnPressureChanged.AddListener((pressure) => { });
penInput.OnTiltChanged.AddListener((tilt) => { });
penInput.OnBarrelButtonPressed.AddListener(() => { });
penInput.OnEraserTipActive.AddListener(() => { });
penInput.OnPenTipActive.AddListener(() => { });

// Layer events
layerManager.OnLayerAdded.AddListener((index) => { });
layerManager.OnLayerRemoved.AddListener((index) => { });
layerManager.OnActiveLayerChanged.AddListener((index) => { });

// Color picker events
colorPicker.OnColorChanged.AddListener((color) => { });
colorPicker.OnColorConfirmed.AddListener((color) => { });
```

## Support

- **Website**: [witshells.com](https://witshells.com)
- **Documentation**: [witshells.com/docs/canvas-draw-tool](https://witshells.com/docs/canvas-draw-tool)
- **Issues**: [GitHub Issues](https://github.com/WitShells/CanvasDrawTool/issues)

## License

MIT License - see [LICENSE](LICENSE) for details.
