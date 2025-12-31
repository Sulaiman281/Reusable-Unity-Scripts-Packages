# WitShells Spline Runtime

A comprehensive Unity package for working with splines at runtime. Create, modify, and utilize splines for roads, tunnels, object placement, mesh generation, and procedural content creation.

---

## Table of Contents

1. [Introduction](#chapter-1-introduction)
2. [Installation](#chapter-2-installation)
3. [Getting Started](#chapter-3-getting-started)
4. [Core Components](#chapter-4-core-components)
   - [ASplineRuntime](#asplineruntime)
   - [MeshCreator](#meshcreator)
   - [SplinePathCreator](#splinepathcreator)
5. [Spline Creation](#chapter-5-spline-creation)
6. [Spline Modification](#chapter-6-spline-modification)
7. [Mesh Generation](#chapter-7-mesh-generation)
8. [Object Placement](#chapter-8-object-placement)
9. [Spline Sampling & Querying](#chapter-9-spline-sampling--querying)
10. [Transform Utilities](#chapter-10-transform-utilities)
11. [Extension Methods](#chapter-11-extension-methods)
12. [Example Scenarios](#chapter-12-example-scenarios)
13. [AI Prompt Guide](#chapter-13-ai-prompt-guide)
14. [API Reference](#chapter-14-api-reference)
15. [Troubleshooting](#chapter-15-troubleshooting)

---

## Chapter 1: Introduction

**WitShells Spline Runtime** is a powerful Unity package that extends Unity's built-in Spline system with runtime capabilities. It provides tools for:

- **Procedural Spline Creation**: Create splines from positions, arcs, spirals, grids, and terrain-following paths
- **Dynamic Mesh Generation**: Generate roads, elevated roads, tunnels, and ribbons from splines
- **Object Placement**: Spawn and distribute objects along splines with precise control
- **Spline Manipulation**: Merge, offset, subdivide, and reverse splines
- **Runtime Updates**: Update splines dynamically based on child object positions

### Key Features

| Feature | Description |
|---------|-------------|
| **Multiple Mesh Types** | Road, Elevated Road, Tunnel, Ribbon |
| **Object Pooling Support** | Efficient object spawning with built-in pooling |
| **Terrain Conforming** | Splines and meshes that follow terrain height |
| **Auto-Smoothing** | Automatic Bezier tangent smoothing |
| **Ground Snapping** | Snap spawned objects to ground layers |
| **Rotation Control** | Full rotation control for spawned objects |

### Dependencies

- Unity 2021.3+
- Unity Splines Package (`com.unity.splines`)
- WitShells Design Patterns (for ObjectPool)

---

## Chapter 2: Installation

### Via Package Manager

1. Open **Window > Package Manager**
2. Click the **+** button and select **"Add package from disk"**
3. Navigate to the package folder and select `package.json`

### Manual Installation

Copy the `SplineRuntime` folder to your `Assets` directory.

### Required Setup

Ensure the Unity Splines package is installed:
```
Window > Package Manager > Unity Registry > Splines
```

---

## Chapter 3: Getting Started

### Basic Workflow

1. **Create a GameObject** with a `SplineContainer` component
2. **Add control points** to define your spline path
3. **Use SplineUtils** to generate meshes or place objects
4. **Attach components** like `MeshCreator` for visual results

### Quick Start Example

```csharp
using UnityEngine;
using UnityEngine.Splines;
using WitShells.SplineRuntime;

public class QuickStartExample : MonoBehaviour
{
    void Start()
    {
        // Create a spline from positions
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(10, 0, 10),
            new Vector3(20, 0, 5),
            new Vector3(30, 0, 15)
        };

        // Create the spline container
        SplineContainer spline = SplineUtils.CreateSplineFromPositions(positions);

        // Generate a road mesh
        Mesh roadMesh = SplineUtils.CreateRoadMesh(spline, width: 5f);

        // Apply to a MeshFilter
        GetComponent<MeshFilter>().mesh = roadMesh;
    }
}
```

---

## Chapter 4: Core Components

### ASplineRuntime

**Base class for all spline runtime components.**

```csharp
// ASplineRuntime automatically manages SplineContainer reference
// and provides position update functionality

public class MySplineComponent : ASplineRuntime
{
    public override void Update()
    {
        base.Update(); // Handles automatic position updates
        
        // Your custom logic here
    }
}
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SplineContainer` | `SplineContainer` | Auto-cached reference to the SplineContainer |
| `updateInterval` | `float` | How often to update positions (seconds) |
| `updateWithChildren` | `bool` | Auto-update spline from child positions |

#### Key Method

```csharp
// Updates the spline based on child object positions
public void UpdateSplinePositionWithChildren()
```

---

### MeshCreator

**Component for generating procedural meshes from splines.**

```csharp
// Add to a GameObject with SplineContainer
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshCreator : ASplineRuntime
```

#### Inspector Settings

```csharp
[Header("Mesh Type")]
public SplineMeshType meshType = SplineMeshType.Road;

// Choose from:
// - Road: Flat road surface
// - ElevatedRoad: Road with side walls
// - Tunnel: Cylindrical tunnel
// - Ribbon: Variable-width ribbon

[Header("Road Settings")]
public RoadMeshSettings roadSettings = RoadMeshSettings.Default;
// - width: Road width
// - flipDirection: Reverse the road direction
// - baseSettings.segments: Mesh resolution

[Header("Elevated Road Settings")]
public ElevatedRoadMeshSettings elevatedRoadSettings;
// - width: Road width
// - height: Wall height

[Header("Tunnel Settings")]
public TunnelMeshSettings tunnelSettings;
// - radius: Tunnel radius
// - circleVertices: Resolution around circumference

[Header("Ribbon Settings")]
public RibbonMeshSettings ribbonSettings;
// - startWidth / endWidth: Variable width
// - conformToTerrain: Follow terrain height

[Header("Mesh Components")]
public bool generateCollider = true; // Auto-generate MeshCollider
```

#### Usage Example

```csharp
using UnityEngine;
using WitShells.SplineRuntime;

public class RoadBuilder : MonoBehaviour
{
    [SerializeField] private MeshCreator meshCreator;
    [SerializeField] private Material roadMaterial;

    void Start()
    {
        // Configure road settings
        meshCreator.meshType = SplineMeshType.Road;
        meshCreator.roadSettings = new RoadMeshSettings
        {
            baseSettings = new SplineMeshBaseSettings
            {
                segments = 100,
                materialTilingMultiplier = 1f
            },
            width = 8f,
            flipDirection = false
        };

        // Generate the mesh
        meshCreator.GenerateMesh();

        // Apply material
        meshCreator.SetMaterial(roadMaterial);
    }
}
```

---

### SplinePathCreator

**Spawns objects along a spline with object pooling support.**

```csharp
[RequireComponent(typeof(SplineContainer))]
public class SplinePathCreator : ASplineRuntime
```

#### Inspector Settings

```csharp
[Header("Reference")]
public GameObject nodePrefab;       // Prefab to spawn

[Header("Settings")]
public LayerMask LayerMask;         // Ground layers for snapping
public bool updateNodes = false;    // Continuously update positions
public float spacing = 1f;          // Distance between spawned objects
public Vector3 rotationOffset;      // Additional rotation to apply
```

#### Usage Example

```csharp
using UnityEngine;
using WitShells.SplineRuntime;

public class FenceBuilder : MonoBehaviour
{
    [SerializeField] private SplinePathCreator pathCreator;
    [SerializeField] private GameObject fencePostPrefab;

    void Start()
    {
        pathCreator.nodePrefab = fencePostPrefab;
        pathCreator.spacing = 2f; // 2 units between posts
        pathCreator.rotationOffset = new Vector3(0, 90, 0);
        pathCreator.updateNodes = true;
    }

    public void ClearFence()
    {
        pathCreator.ClearNodes();
    }
}
```

---

## Chapter 5: Spline Creation

### Create from Positions

```csharp
// Create a basic spline from world positions
Vector3[] waypoints = new Vector3[]
{
    new Vector3(0, 0, 0),
    new Vector3(5, 2, 10),
    new Vector3(10, 0, 15),
    new Vector3(15, 1, 20)
};

SplineContainer spline = SplineUtils.CreateSplineFromPositions(
    waypoints,
    parent: transform,     // Optional parent
    autoSmooth: true       // Auto-smooth tangents
);
```

### Create Closed Loop

```csharp
// Create a closed loop (polygon) spline
Vector3[] loopPoints = new Vector3[]
{
    new Vector3(0, 0, 0),
    new Vector3(10, 0, 0),
    new Vector3(10, 0, 10),
    new Vector3(0, 0, 10)
};

SplineContainer closedSpline = SplineUtils.CreateClosedLoopSpline(
    loopPoints,
    parent: transform
);
```

### Create Arc Between Points

```csharp
// Create an arcing path (useful for bridges, jumps)
Vector3 startPoint = new Vector3(0, 0, 0);
Vector3 endPoint = new Vector3(20, 0, 0);
float arcHeight = 5f;

SplineContainer arcSpline = SplineUtils.CreateArcBetweenPoints(
    startPoint,
    endPoint,
    arcHeight,
    pointCount: 10,
    parent: transform
);
```

### Create Spiral

```csharp
// Create a spiral spline (helixes, staircases)
SplineContainer spiral = SplineUtils.CreateSpiralSpline(
    center: Vector3.zero,
    startRadius: 5f,
    endRadius: 10f,
    height: 20f,
    rotations: 3f,
    segments: 36,
    parent: transform
);
```

### Create Grid Network

```csharp
// Create a grid of interconnected splines
List<SplineContainer> gridSplines = SplineUtils.CreateSplineGrid(
    origin: Vector3.zero,
    width: 50f,
    length: 50f,
    rows: 5,
    columns: 5,
    parent: transform
);

// Result: 12 splines forming a 5x5 grid
```

### Create Terrain-Following Spline

```csharp
// Create a spline that follows terrain height
SplineContainer terrainSpline = SplineUtils.CreateTerrainFollowingSpline(
    startPoint: new Vector3(0, 0, 0),
    endPoint: new Vector3(100, 0, 100),
    heightOffset: 0.5f,    // Height above terrain
    segments: 50,
    parent: transform
);
```

### Create Spline Object Directly

```csharp
// Create a Spline (not SplineContainer) from local positions
Vector3[] localPositions = new Vector3[]
{
    Vector3.zero,
    new Vector3(5, 0, 5),
    new Vector3(10, 0, 0)
};

Spline spline = SplineUtils.CreateSplineFromLocalPositions(
    localPositions,
    autoSmooth: true
);

// Or from world positions with a reference transform
Spline worldSpline = SplineUtils.CreateSplineFromWorldPositions(
    worldPositions,
    referenceTransform,
    autoSmooth: true
);
```

---

## Chapter 6: Spline Modification

### Auto-Smooth Tangents

```csharp
// Smooth the tangents of an existing spline
SplineUtils.AutoSmoothTangents(
    spline: splineContainer.Spline,
    tangentScale: 0.33f  // Tangent length multiplier
);
```

### Add Points to Existing Spline

```csharp
// Add new points to an existing spline
Vector3[] newPoints = new Vector3[]
{
    new Vector3(25, 0, 25),
    new Vector3(30, 0, 30)
};

SplineUtils.AddPointsToSpline(
    splineContainer,
    newPoints,
    splineIndex: 0,
    autoSmooth: true
);
```

### Offset a Spline

```csharp
// Create a parallel spline at a distance
SplineContainer leftLane = SplineUtils.OffsetSpline(
    sourceSpline: mainRoad,
    offset: -3f,           // Negative = left side
    splineIndex: 0,
    sampleCount: 50,
    parent: transform
);

SplineContainer rightLane = SplineUtils.OffsetSpline(
    sourceSpline: mainRoad,
    offset: 3f,            // Positive = right side
    splineIndex: 0,
    sampleCount: 50,
    parent: transform
);
```

### Merge Multiple Splines

```csharp
// Combine multiple splines into one
List<SplineContainer> roadSegments = new List<SplineContainer>
{
    segment1,
    segment2,
    segment3
};

SplineContainer mergedRoad = SplineUtils.MergeSplines(
    roadSegments,
    connectEndpoints: true,  // Smoothly connect end-to-start
    parent: transform
);
```

### Subdivide a Spline

```csharp
// Add more points between existing knots for higher detail
SplineUtils.SubdivideSpline(
    splineContainer,
    subdivisions: 3,         // Add 3 points between each knot
    splineIndex: 0,
    preserveTangents: false  // Recalculate tangents
);
```

### Reverse a Spline

```csharp
// Reverse the direction of a spline
SplineUtils.ReverseSpline(
    splineContainer,
    splineIndex: 0
);
```

---

## Chapter 7: Mesh Generation

### Road Mesh

```csharp
// Simple flat road
Mesh roadMesh = SplineUtils.CreateRoadMesh(
    splineContainer,
    width: 6f,
    materialTilingMultiplier: 1f,
    flipDirection: false,
    splineIndex: 0,
    segments: 100,
    transform: transform  // Convert to local space
);

// Apply to MeshFilter
GetComponent<MeshFilter>().mesh = roadMesh;
```

### Elevated Road Mesh

```csharp
// Road with side walls (elevated highways, bridges)
Mesh elevatedRoad = SplineUtils.CreateElevatedRoadMesh(
    splineContainer,
    width: 8f,
    height: 0.5f,         // Wall height
    materialTilingMultiplier: 1f,
    splineIndex: 0,
    segments: 100,
    transform: transform
);
```

### Tunnel Mesh

```csharp
// Cylindrical tunnel around the spline
Mesh tunnel = SplineUtils.CreateTunnelMesh(
    splineContainer,
    radius: 4f,
    segments: 100,
    circleVertices: 16,   // Resolution of circle
    materialTilingMultiplier: 1f,
    splineIndex: 0,
    transform: transform
);
```

### Ribbon Mesh

```csharp
// Variable-width ribbon (rivers, paths)
Mesh riverMesh = SplineUtils.CreateRibbonMesh(
    splineContainer,
    startWidth: 2f,
    endWidth: 10f,        // Widens towards end
    materialTilingMultiplier: 1f,
    splineIndex: 0,
    segments: 100,
    conformToTerrain: true,
    terrainOffset: 0.1f,
    transform: transform
);
```

### Using MeshCreator Component

```csharp
// Configure via code
MeshCreator creator = GetComponent<MeshCreator>();

creator.meshType = SplineMeshType.Tunnel;
creator.tunnelSettings = new TunnelMeshSettings
{
    baseSettings = new SplineMeshBaseSettings
    {
        segments = 100,
        materialTilingMultiplier = 2f
    },
    radius = 5f,
    circleVertices = 24
};

creator.GenerateMesh();
```

### Mesh Utilities

```csharp
// Check if normals need flipping
bool shouldFlip = SplineUtils.NeedToFlipNormals(mesh, transform);

// Flip normals if needed
if (shouldFlip)
{
    SplineUtils.FlipNormals(mesh);
}
```

---

## Chapter 8: Object Placement

### Basic Object Spawning

```csharp
// Spawn objects along a spline
SplineUtils.SpawnObjectsAlongSpline(
    splineContainer,
    prefab: treePrefab,
    objectContainer: transform,
    groundLayers: LayerMask.GetMask("Ground"),
    snapToGround: true,
    alignToSpline: true,
    groundOffset: 0f,
    spacing: 5f,
    splineIndex: 0
);
```

### Advanced Object Spawning with Rotation

```csharp
// Spawn with custom rotation offset
SplineUtils.SpawnObjectsAlongSplineAdvanced(
    splineContainer,
    prefab: fencePostPrefab,
    objectContainer: transform,
    groundLayers: LayerMask.GetMask("Ground"),
    snapToGround: true,
    alignToSpline: true,
    groundOffset: 0f,
    spacing: 2f,
    rotationOffset: new Vector3(0, 90, 0),
    splineIndex: 0
);
```

### Object Spawning with Object Pool

```csharp
using WitShells.DesignPatterns.Core;

// Create object pool
ObjectPool<GameObject> treePool = new ObjectPool<GameObject>(
    () => Instantiate(treePrefab)
);

// Spawn using pool (more efficient for runtime updates)
SplineUtils.SpawnObjectsAlongSplineAdvanced(
    splineContainer,
    prefabPool: treePool,
    objectContainer: transform,
    groundLayers: LayerMask.GetMask("Ground"),
    snapToGround: true,
    alignToSpline: true,
    groundOffset: 0.1f,
    spacing: 3f,
    rotationOffset: Vector3.zero,
    splineIndex: 0
);
```

### Place Single Object at Spline Point

```csharp
// Place an object at a specific point along the spline
GameObject placed = SplineUtils.PlaceObjectAtSplinePoint(
    splineContainer,
    prefab: markerPrefab,
    parent: transform,
    normalizeDistance: true,  // Use 0-1 range
    distance: 0.5f,           // Middle of spline
    rotationOffset: Vector3.zero,
    forwardAxis: Vector3.forward,
    upAxis: Vector3.up,
    splineIndex: 0
);

// Or use world units
GameObject placedWorldUnits = SplineUtils.PlaceObjectAtSplinePoint(
    splineContainer,
    prefab: markerPrefab,
    parent: transform,
    normalizeDistance: false,  // Use world units
    distance: 25f,             // 25 units along spline
    rotationOffset: new Vector3(0, 90, 0),
    forwardAxis: Vector3.forward,
    upAxis: Vector3.up,
    splineIndex: 0
);
```

### Distribute Pooled Objects

```csharp
// Efficient distribution using object pooling
List<GameObject> placedObjects = SplineUtils.DistributePooledObjectsAlongSpline(
    splineContainer,
    getPooledObject: () => objectPool.Get(),
    releasePooledObject: (obj) => objectPool.Release(obj),
    container: transform,
    objectCount: 20,
    existingObjects: previouslyPlaced,
    splineIndex: 0,
    alignToSpline: true,
    rotationOffset: Vector3.zero,
    forwardAxis: Vector3.forward,
    upAxis: Vector3.up
);
```

### Update Objects Along Spline

```csharp
// Update existing objects to follow new positions
List<Vector3> newPositions = new List<Vector3>
{
    new Vector3(0, 0, 0),
    new Vector3(10, 5, 10),
    new Vector3(20, 0, 20)
};

List<GameObject> objectsToUpdate = GetMyObjects();

SplineUtils.UpdateObjectsAlongSplineAndCleanup(
    newPositions,
    objectsToUpdate,
    alignToSpline: true,
    rotationOffset: Vector3.zero,
    distributeEvenly: true,
    forwardAxis: Vector3.forward,
    upAxis: Vector3.up
);
```

---

## Chapter 9: Spline Sampling & Querying

### Sample Points at Regular Intervals

```csharp
// Get evenly-spaced points along the spline
Vector3[] sampledPoints = SplineUtils.SamplePointsAlongSpline(
    splineContainer,
    spacing: 2f,      // 2 units between samples
    splineIndex: 0
);

// Use for AI pathfinding, particle effects, etc.
foreach (Vector3 point in sampledPoints)
{
    Debug.DrawRay(point, Vector3.up, Color.green, 5f);
}
```

### Find Closest Point on Spline

```csharp
// Find where on the spline is closest to a position
Vector3 targetPosition = player.transform.position;

float t = SplineUtils.GetClosestPointOnSpline(
    splineContainer,
    targetPosition,
    splineIndex: 0,
    resolution: 100   // Higher = more accurate
);

// t is 0-1, representing position along spline
Debug.Log($"Closest point is at {t * 100}% of the spline");

// Get the actual position at t
float3 position, tangent, up;
splineContainer.Evaluate(0, t, out position, out tangent, out up);
```

### Get Knot Positions

```csharp
// Extract all knot positions from a spline
Vector3[] knotPositions = SplineUtils.GetPositionsFromSpline(
    splineContainer.Spline
);
```

---

## Chapter 10: Transform Utilities

### Convert Child Objects to Position List

```csharp
// Get positions from child GameObjects
// Useful for creating splines from placed waypoints

List<Vector3> childPositions = SplineUtils.ContainerChildrenToPositionList(
    container: waypointParent,
    includeInactive: false,
    localPositions: true
);

// Create spline from child positions
Spline spline = SplineUtils.CreateSplineFromPositionsList(childPositions);
```

### Convert Transforms to Positions

```csharp
// From a list of transforms
List<Transform> waypoints = GetWaypointTransforms();
List<Vector3> positions = SplineUtils.TransformsToPositionList(waypoints);

// From an array
Transform[] waypointArray = GetWaypointArray();
List<Vector3> positionsFromArray = SplineUtils.TransformsToPositionList(waypointArray);
```

---

## Chapter 11: Extension Methods

The `SplineMeshExtensions` class provides convenient extension methods for `SplineContainer`:

### Generate Mesh by Type

```csharp
using WitShells.SplineRuntime;

// Generic mesh generation with settings object
Mesh mesh = splineContainer.GenerateMesh(
    SplineMeshType.Road,
    RoadMeshSettings.Default,
    transform
);
```

### Type-Specific Extensions

```csharp
// Road mesh
Mesh road = splineContainer.GenerateRoadMesh(
    new RoadMeshSettings
    {
        baseSettings = SplineMeshBaseSettings.Default,
        width = 6f,
        flipDirection = false
    },
    transform
);

// Elevated road mesh
Mesh elevated = splineContainer.GenerateElevatedRoadMesh(
    new ElevatedRoadMeshSettings
    {
        baseSettings = SplineMeshBaseSettings.Default,
        width = 8f,
        height = 0.5f
    },
    transform
);

// Tunnel mesh
Mesh tunnel = splineContainer.GenerateTunnelMesh(
    new TunnelMeshSettings
    {
        baseSettings = SplineMeshBaseSettings.Default,
        radius = 4f,
        circleVertices = 16
    },
    transform
);

// Ribbon mesh
Mesh ribbon = splineContainer.GenerateRibbonMesh(
    new RibbonMeshSettings
    {
        baseSettings = SplineMeshBaseSettings.Default,
        startWidth = 2f,
        endWidth = 8f,
        conformToTerrain = true,
        terrainOffset = 0.1f
    },
    transform
);
```

### JSON-Based Settings

```csharp
// For serialization/network transmission
string settingsJson = JsonUtility.ToJson(roadSettings);

Mesh mesh = splineContainer.GenerateMesh(
    SplineMeshType.Road,
    settingsJson,
    transform
);
```

---

## Chapter 12: Example Scenarios

### Scenario 1: Procedural Race Track

```csharp
using UnityEngine;
using UnityEngine.Splines;
using WitShells.SplineRuntime;

public class RaceTrackGenerator : MonoBehaviour
{
    [SerializeField] private Material trackMaterial;
    [SerializeField] private Material barrierMaterial;
    [SerializeField] private GameObject barrierPrefab;

    private SplineContainer trackSpline;
    private MeshFilter trackMeshFilter;
    private MeshRenderer trackRenderer;

    void Start()
    {
        GenerateTrack();
    }

    void GenerateTrack()
    {
        // Create a closed loop track
        Vector3[] trackPoints = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(50, 0, 20),
            new Vector3(100, 0, 0),
            new Vector3(100, 0, 80),
            new Vector3(50, 0, 100),
            new Vector3(0, 0, 80)
        };

        trackSpline = SplineUtils.CreateClosedLoopSpline(trackPoints, transform);

        // Generate track surface
        Mesh trackMesh = SplineUtils.CreateRoadMesh(
            trackSpline,
            width: 12f,
            materialTilingMultiplier: 0.1f,
            segments: 200
        );

        // Setup mesh components
        trackMeshFilter = trackSpline.gameObject.AddComponent<MeshFilter>();
        trackRenderer = trackSpline.gameObject.AddComponent<MeshRenderer>();
        trackSpline.gameObject.AddComponent<MeshCollider>().sharedMesh = trackMesh;

        trackMeshFilter.mesh = trackMesh;
        trackRenderer.material = trackMaterial;

        // Add barriers on both sides
        SplineContainer leftBarrier = SplineUtils.OffsetSpline(trackSpline, -6.5f);
        SplineContainer rightBarrier = SplineUtils.OffsetSpline(trackSpline, 6.5f);

        SplineUtils.SpawnObjectsAlongSpline(
            leftBarrier, barrierPrefab, transform,
            LayerMask.GetMask("Ground"), false, true, 0f, 3f
        );

        SplineUtils.SpawnObjectsAlongSpline(
            rightBarrier, barrierPrefab, transform,
            LayerMask.GetMask("Ground"), false, true, 0f, 3f
        );
    }
}
```

### Scenario 2: River System with Variable Width

```csharp
using UnityEngine;
using UnityEngine.Splines;
using WitShells.SplineRuntime;

public class RiverGenerator : MonoBehaviour
{
    [SerializeField] private Material waterMaterial;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject treePrefab;

    void Start()
    {
        GenerateRiver();
    }

    void GenerateRiver()
    {
        // Create terrain-following river path
        SplineContainer riverSpline = SplineUtils.CreateTerrainFollowingSpline(
            new Vector3(0, 10, 0),
            new Vector3(200, 5, 150),
            heightOffset: -0.5f,  // Slightly below terrain
            segments: 100,
            transform
        );

        // Generate river mesh with widening effect
        Mesh riverMesh = SplineUtils.CreateRibbonMesh(
            riverSpline,
            startWidth: 3f,
            endWidth: 15f,
            materialTilingMultiplier: 0.5f,
            conformToTerrain: true,
            terrainOffset: 0.1f
        );

        // Setup river visuals
        MeshFilter mf = riverSpline.gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = riverSpline.gameObject.AddComponent<MeshRenderer>();
        mf.mesh = riverMesh;
        mr.material = waterMaterial;

        // Add rocks along the river banks
        SplineContainer leftBank = SplineUtils.OffsetSpline(riverSpline, -8f);
        SplineContainer rightBank = SplineUtils.OffsetSpline(riverSpline, 8f);

        SplineUtils.SpawnObjectsAlongSplineAdvanced(
            leftBank, rockPrefab, transform,
            LayerMask.GetMask("Terrain"), true, false, 0f, 10f,
            new Vector3(0, Random.Range(0, 360), 0)
        );

        // Add trees along banks
        SplineUtils.SpawnObjectsAlongSplineAdvanced(
            rightBank, treePrefab, transform,
            LayerMask.GetMask("Terrain"), true, false, 0f, 8f,
            Vector3.zero
        );
    }
}
```

### Scenario 3: Dynamic Rope/Cable System

```csharp
using UnityEngine;
using UnityEngine.Splines;
using WitShells.SplineRuntime;
using System.Collections.Generic;

public class DynamicRope : MonoBehaviour
{
    [SerializeField] private Transform startAnchor;
    [SerializeField] private Transform endAnchor;
    [SerializeField] private float sagAmount = 2f;
    [SerializeField] private int ropeSegments = 20;
    [SerializeField] private Material ropeMaterial;

    private SplineContainer ropeSpline;
    private MeshFilter meshFilter;
    private LineRenderer lineRenderer;

    void Start()
    {
        CreateRope();
    }

    void Update()
    {
        UpdateRope();
    }

    void CreateRope()
    {
        // Create initial arc between anchors
        ropeSpline = SplineUtils.CreateArcBetweenPoints(
            startAnchor.position,
            endAnchor.position,
            -sagAmount,  // Negative for downward sag
            ropeSegments,
            transform
        );

        // Setup line renderer for visualization
        lineRenderer = ropeSpline.gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = ropeMaterial;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        UpdateLineRenderer();
    }

    void UpdateRope()
    {
        // Recreate spline if anchors moved
        if (ropeSpline != null)
        {
            Destroy(ropeSpline.gameObject);
        }

        ropeSpline = SplineUtils.CreateArcBetweenPoints(
            startAnchor.position,
            endAnchor.position,
            -sagAmount,
            ropeSegments,
            transform
        );

        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        Vector3[] points = SplineUtils.SamplePointsAlongSpline(
            ropeSpline, 0.1f
        );

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }
}
```

### Scenario 4: Tunnel/Cave System

```csharp
using UnityEngine;
using UnityEngine.Splines;
using WitShells.SplineRuntime;

public class CaveGenerator : MonoBehaviour
{
    [SerializeField] private Material caveMaterial;
    [SerializeField] private GameObject stalagmitePrefab;
    [SerializeField] private GameObject torchPrefab;

    void Start()
    {
        GenerateCave();
    }

    void GenerateCave()
    {
        // Create winding cave path
        Vector3[] cavePath = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(10, -2, 15),
            new Vector3(25, -5, 20),
            new Vector3(40, -3, 35),
            new Vector3(50, -8, 50),
            new Vector3(60, -5, 70)
        };

        SplineContainer caveSpline = SplineUtils.CreateSplineFromPositions(
            cavePath, transform
        );

        // Generate tunnel mesh
        Mesh tunnelMesh = SplineUtils.CreateTunnelMesh(
            caveSpline,
            radius: 4f,
            segments: 100,
            circleVertices: 24,
            materialTilingMultiplier: 0.5f
        );

        // Need to flip normals for interior view
        SplineUtils.FlipNormals(tunnelMesh);

        // Setup cave visuals
        MeshFilter mf = caveSpline.gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = caveSpline.gameObject.AddComponent<MeshRenderer>();
        MeshCollider mc = caveSpline.gameObject.AddComponent<MeshCollider>();

        mf.mesh = tunnelMesh;
        mr.material = caveMaterial;
        mc.sharedMesh = tunnelMesh;

        // Add torches along the cave
        SplineUtils.SpawnObjectsAlongSplineAdvanced(
            caveSpline, torchPrefab, transform,
            LayerMask.GetMask("Default"), false, true, 
            3f,  // Offset from center (on wall)
            15f, // Spacing
            new Vector3(0, 0, 90) // Rotated to face outward
        );
    }
}
```

### Scenario 5: Highway System with Multiple Lanes

```csharp
using UnityEngine;
using UnityEngine.Splines;
using WitShells.SplineRuntime;
using System.Collections.Generic;

public class HighwayGenerator : MonoBehaviour
{
    [SerializeField] private Material asphaltMaterial;
    [SerializeField] private Material lineMarkingMaterial;
    [SerializeField] private GameObject lightPolePrefab;
    [SerializeField] private float laneWidth = 3.5f;
    [SerializeField] private int numberOfLanes = 3;

    private List<SplineContainer> lanes = new List<SplineContainer>();

    void Start()
    {
        GenerateHighway();
    }

    void GenerateHighway()
    {
        // Create main highway path
        Vector3[] highwayPath = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(100, 0, 50),
            new Vector3(200, 5, 40),
            new Vector3(300, 5, 100),
            new Vector3(400, 0, 150)
        };

        SplineContainer mainSpline = SplineUtils.CreateSplineFromPositions(
            highwayPath, transform
        );

        float totalWidth = laneWidth * numberOfLanes * 2; // Both directions

        // Generate road surface
        Mesh roadMesh = SplineUtils.CreateElevatedRoadMesh(
            mainSpline,
            width: totalWidth,
            height: 0.3f,
            materialTilingMultiplier: 0.1f,
            segments: 200
        );

        // Setup road
        MeshFilter mf = mainSpline.gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = mainSpline.gameObject.AddComponent<MeshRenderer>();
        mf.mesh = roadMesh;
        mr.material = asphaltMaterial;

        // Create lane center lines for AI pathfinding
        for (int i = 0; i < numberOfLanes; i++)
        {
            // Left side lanes (one direction)
            float leftOffset = -laneWidth * (i + 0.5f);
            SplineContainer leftLane = SplineUtils.OffsetSpline(
                mainSpline, leftOffset, sampleCount: 100
            );
            leftLane.name = $"Lane_Left_{i}";
            lanes.Add(leftLane);

            // Right side lanes (opposite direction)
            float rightOffset = laneWidth * (i + 0.5f);
            SplineContainer rightLane = SplineUtils.OffsetSpline(
                mainSpline, rightOffset, sampleCount: 100
            );
            SplineUtils.ReverseSpline(rightLane); // Reverse for opposite direction
            rightLane.name = $"Lane_Right_{i}";
            lanes.Add(rightLane);
        }

        // Add light poles along the highway
        SplineContainer leftEdge = SplineUtils.OffsetSpline(
            mainSpline, -(totalWidth / 2 + 1f)
        );
        
        SplineUtils.SpawnObjectsAlongSplineAdvanced(
            leftEdge, lightPolePrefab, transform,
            LayerMask.GetMask("Ground"), true, true, 0f, 30f,
            new Vector3(0, 0, 0)
        );
    }

    // Get lane spline for AI vehicles
    public SplineContainer GetLane(int laneIndex, bool leftSide)
    {
        int index = leftSide ? laneIndex : numberOfLanes + laneIndex;
        return lanes[index];
    }
}
```

### Scenario 6: Animated Path Following

```csharp
using UnityEngine;
using UnityEngine.Splines;
using WitShells.SplineRuntime;

public class SplineFollower : MonoBehaviour
{
    [SerializeField] private SplineContainer spline;
    [SerializeField] private float speed = 5f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool alignToPath = true;

    private float currentT = 0f;
    private float splineLength;

    void Start()
    {
        if (spline != null)
        {
            splineLength = spline.CalculateLength(0);
        }
    }

    void Update()
    {
        if (spline == null || splineLength <= 0) return;

        // Move along spline
        float distancePerFrame = speed * Time.deltaTime;
        float tDelta = distancePerFrame / splineLength;
        currentT += tDelta;

        if (loop)
        {
            currentT = currentT % 1f;
        }
        else
        {
            currentT = Mathf.Clamp01(currentT);
        }

        // Evaluate spline
        float3 position, tangent, up;
        if (spline.Evaluate(0, currentT, out position, out tangent, out up))
        {
            transform.position = new Vector3(position.x, position.y, position.z);

            if (alignToPath)
            {
                Vector3 forward = new Vector3(tangent.x, tangent.y, tangent.z).normalized;
                Vector3 upDir = new Vector3(up.x, up.y, up.z).normalized;
                transform.rotation = Quaternion.LookRotation(forward, upDir);
            }
        }
    }

    public void SetProgress(float t)
    {
        currentT = Mathf.Clamp01(t);
    }

    public float GetProgress()
    {
        return currentT;
    }
}
```

---

## Chapter 13: AI Prompt Guide

Use these prompts when working with AI assistants to get help with SplineRuntime:

### General Usage

```
"Using WitShells SplineRuntime in Unity, show me how to create a [road/tunnel/path] 
from a set of waypoints and generate a mesh for it."
```

### Mesh Generation

```
"I need to generate a [road/elevated road/tunnel/ribbon] mesh using SplineRuntime. 
The road should be [X] units wide with [Y] segments. Show me the code."
```

### Object Placement

```
"How do I spawn [prefab type] objects along a spline at regular intervals 
using SplineRuntime? I need them to be [X] units apart and aligned to the path."
```

### Dynamic Updates

```
"I want to update a spline at runtime based on the positions of child objects. 
How do I use ASplineRuntime or SplineUtils to do this?"
```

### Terrain Following

```
"Create a spline that follows terrain height between two points using 
SplineRuntime's CreateTerrainFollowingSpline function."
```

### Complex Scenarios

```
"Using WitShells SplineRuntime, I need to create a race track with:
- A closed loop spline
- Lane markings using offset splines
- Barriers placed along both sides
Show me the complete implementation."
```

### Troubleshooting

```
"My SplineRuntime mesh has inverted normals / wrong facing. 
How do I check and fix this using SplineUtils?"
```

### Object Pooling

```
"I need to efficiently spawn and update many objects along a spline at runtime. 
Show me how to use SplineUtils with object pooling."
```

### Extension Methods

```
"Show me how to use SplineMeshExtensions to generate a mesh 
with custom settings using the extension method pattern."
```

---

## Chapter 14: API Reference

### SplineUtils Static Methods

#### Spline Creation
| Method | Description |
|--------|-------------|
| `CreateSplineFromPositions()` | Create SplineContainer from world positions |
| `CreateClosedLoopSpline()` | Create closed loop spline |
| `CreateArcBetweenPoints()` | Create arcing path between two points |
| `CreateSplineGrid()` | Create grid network of splines |
| `CreateSpiralSpline()` | Create spiral/helix spline |
| `CreateTerrainFollowingSpline()` | Create terrain-conforming spline |
| `CreateSplineFromLocalPositions()` | Create Spline from local positions |
| `CreateSplineFromWorldPositions()` | Create Spline from world positions |
| `CreateSplineFromPositionsList()` | Create Spline from List<Vector3> |

#### Spline Modification
| Method | Description |
|--------|-------------|
| `AutoSmoothTangents()` | Smooth Bezier tangents |
| `AddPointsToSpline()` | Add points to existing spline |
| `OffsetSpline()` | Create parallel offset spline |
| `MergeSplines()` | Combine multiple splines |
| `SubdivideSpline()` | Add detail to spline |
| `ReverseSpline()` | Reverse spline direction |

#### Mesh Generation
| Method | Description |
|--------|-------------|
| `CreateRoadMesh()` | Generate flat road mesh |
| `CreateElevatedRoadMesh()` | Generate road with walls |
| `CreateTunnelMesh()` | Generate cylindrical tunnel |
| `CreateRibbonMesh()` | Generate variable-width ribbon |
| `FlipNormals()` | Flip mesh normals |
| `NeedToFlipNormals()` | Check if normals need flipping |

#### Object Placement
| Method | Description |
|--------|-------------|
| `SpawnObjectsAlongSpline()` | Basic object spawning |
| `SpawnObjectsAlongSplineAdvanced()` | Spawning with rotation control |
| `PlaceObjectAtSplinePoint()` | Place single object at t position |
| `DistributePooledObjectsAlongSpline()` | Pooled object distribution |
| `UpdateObjectsAlongSpline()` | Update existing objects |
| `UpdateObjectsAlongSplineAndCleanup()` | Update and cleanup temp spline |

#### Sampling & Querying
| Method | Description |
|--------|-------------|
| `SamplePointsAlongSpline()` | Get evenly-spaced points |
| `GetClosestPointOnSpline()` | Find closest t value |
| `GetPositionsFromSpline()` | Extract knot positions |

#### Transform Utilities
| Method | Description |
|--------|-------------|
| `ContainerChildrenToPositionList()` | Convert children to positions |
| `TransformsToPositionList()` | Convert transforms to positions |

### Settings Structs

#### SplineMeshBaseSettings
```csharp
public struct SplineMeshBaseSettings
{
    public int splineIndex;              // Spline index in container
    public int segments;                 // Mesh resolution
    public float materialTilingMultiplier; // UV tiling
}
```

#### RoadMeshSettings
```csharp
public struct RoadMeshSettings
{
    public SplineMeshBaseSettings baseSettings;
    public float width;
    public bool flipDirection;
}
```

#### ElevatedRoadMeshSettings
```csharp
public struct ElevatedRoadMeshSettings
{
    public SplineMeshBaseSettings baseSettings;
    public float width;
    public float height;
}
```

#### TunnelMeshSettings
```csharp
public struct TunnelMeshSettings
{
    public SplineMeshBaseSettings baseSettings;
    public float radius;
    public int circleVertices;
}
```

#### RibbonMeshSettings
```csharp
public struct RibbonMeshSettings
{
    public SplineMeshBaseSettings baseSettings;
    public float startWidth;
    public float endWidth;
    public bool conformToTerrain;
    public float terrainOffset;
}
```

---

## Chapter 15: Troubleshooting

### Common Issues

#### Mesh appears inside-out
```csharp
// Check and fix inverted normals
if (SplineUtils.NeedToFlipNormals(mesh, transform))
{
    SplineUtils.FlipNormals(mesh);
}
```

#### Objects not aligning to spline
```csharp
// Ensure alignToSpline is true and check axis alignment
SplineUtils.SpawnObjectsAlongSplineAdvanced(
    spline, prefab, container,
    groundLayers, snapToGround,
    alignToSpline: true,  // Must be true
    groundOffset, spacing,
    rotationOffset: Vector3.zero  // Try adjusting this
);
```

#### Spline updates too frequently
```csharp
// Increase update interval in ASplineRuntime
[SerializeField] private float updateInterval = 0.5f; // Increase from 0.1f
```

#### Ground snapping not working
```csharp
// Verify LayerMask includes your ground layer
LayerMask groundLayers = LayerMask.GetMask("Ground", "Terrain");

// Ensure ground objects have colliders
```

#### Spline not smooth enough
```csharp
// Increase segment count
RoadMeshSettings settings = new RoadMeshSettings
{
    baseSettings = new SplineMeshBaseSettings
    {
        segments = 200  // Increase from 100
    }
};

// Or subdivide existing spline
SplineUtils.SubdivideSpline(splineContainer, subdivisions: 3);
```

#### Performance issues with many objects
```csharp
// Use object pooling
ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
    () => Instantiate(prefab)
);

SplineUtils.SpawnObjectsAlongSplineAdvanced(
    spline, pool, container, ...
);
```

### Best Practices

1. **Use Object Pooling** for runtime-updated spawning
2. **Cache SplineContainer references** instead of GetComponent every frame
3. **Adjust segments** based on spline length and visual needs
4. **Use terrain layers** for efficient ground snapping raycasts
5. **Subdivide splines** before generating high-detail meshes
6. **Test with lower settings** first, then increase quality

---

## License

Copyright © WitShells. All rights reserved.

---

*Documentation Version: 1.0.2*
*Package Version: 1.0.2*
*Last Updated: December 2025*
