using UnityEngine;

namespace WitShells.SplineRuntime
{
    /// <summary>
    /// Defines the types of meshes that can be generated from splines
    /// </summary>
    public enum SplineMeshType
    {
        /// <summary>
        /// A flat road mesh with adjustable width
        /// </summary>
        Road,

        /// <summary>
        /// A road mesh with elevated sides/walls
        /// </summary>
        ElevatedRoad,

        /// <summary>
        /// A tube/tunnel shaped mesh surrounding the spline
        /// </summary>
        Tunnel,

        /// <summary>
        /// A ribbon mesh with variable width along the spline
        /// </summary>
        Ribbon
    }

    /// <summary>
    /// Base settings shared by all spline mesh types
    /// </summary>
    [System.Serializable]
    public struct SplineMeshBaseSettings
    {
        /// <summary>
        /// Index of the spline to use in the container (default: 0)
        /// </summary>
        public int splineIndex;

        /// <summary>
        /// Number of segments along the spline (higher = smoother but more vertices)
        /// </summary>
        public int segments;

        /// <summary>
        /// Multiplier for texture tiling (higher = more repetitions)
        /// </summary>
        public float materialTilingMultiplier;

        /// <summary>
        /// Create with default values
        /// </summary>
        public static SplineMeshBaseSettings Default => new SplineMeshBaseSettings
        {
            splineIndex = 0,
            segments = 100,
            materialTilingMultiplier = 1f
        };
    }

    /// <summary>
    /// Settings for basic road mesh generation
    /// </summary>
    [System.Serializable]
    public struct RoadMeshSettings
    {
        /// <summary>
        /// Base settings for the mesh
        /// </summary>
        public SplineMeshBaseSettings baseSettings;

        /// <summary>
        /// Width of the road
        /// </summary>
        public float width;

        /// <summary>
        /// Whether to flip the road direction
        /// </summary>
        public bool flipDirection;

        /// <summary>
        /// Create with default values
        /// </summary>
        public static RoadMeshSettings Default => new RoadMeshSettings
        {
            baseSettings = SplineMeshBaseSettings.Default,
            width = 5f,
            flipDirection = false
        };
    }

    /// <summary>
    /// Settings for elevated road mesh generation
    /// </summary>
    [System.Serializable]
    public struct ElevatedRoadMeshSettings
    {
        /// <summary>
        /// Base settings for the mesh
        /// </summary>
        public SplineMeshBaseSettings baseSettings;

        /// <summary>
        /// Width of the road
        /// </summary>
        public float width;

        /// <summary>
        /// Height of the side walls
        /// </summary>
        public float height;

        /// <summary>
        /// Create with default values
        /// </summary>
        public static ElevatedRoadMeshSettings Default => new ElevatedRoadMeshSettings
        {
            baseSettings = SplineMeshBaseSettings.Default,
            width = 5f,
            height = 0.5f
        };
    }

    /// <summary>
    /// Settings for tunnel mesh generation
    /// </summary>
    [System.Serializable]
    public struct TunnelMeshSettings
    {
        /// <summary>
        /// Base settings for the mesh
        /// </summary>
        public SplineMeshBaseSettings baseSettings;

        /// <summary>
        /// Radius of the tunnel
        /// </summary>
        public float radius;

        /// <summary>
        /// Number of vertices around the circumference
        /// </summary>
        public int circleVertices;

        /// <summary>
        /// Create with default values
        /// </summary>
        public static TunnelMeshSettings Default => new TunnelMeshSettings
        {
            baseSettings = SplineMeshBaseSettings.Default,
            radius = 3f,
            circleVertices = 12
        };
    }

    /// <summary>
    /// Settings for ribbon mesh generation
    /// </summary>
    [System.Serializable]
    public struct RibbonMeshSettings
    {
        /// <summary>
        /// Base settings for the mesh
        /// </summary>
        public SplineMeshBaseSettings baseSettings;

        /// <summary>
        /// Width at the start of the spline
        /// </summary>
        public float startWidth;

        /// <summary>
        /// Width at the end of the spline
        /// </summary>
        public float endWidth;

        /// <summary>
        /// Whether to conform to terrain height
        /// </summary>
        public bool conformToTerrain;

        /// <summary>
        /// Height offset from terrain when conforming
        /// </summary>
        public float terrainOffset;

        /// <summary>
        /// Create with default values
        /// </summary>
        public static RibbonMeshSettings Default => new RibbonMeshSettings
        {
            baseSettings = SplineMeshBaseSettings.Default,
            startWidth = 5f,
            endWidth = 5f,
            conformToTerrain = false,
            terrainOffset = 0.1f
        };
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshCreator : ASplineRuntime
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        public MeshFilter MeshFilter
        {
            get
            {
                if (meshFilter == null)
                    meshFilter = GetComponent<MeshFilter>();
                return meshFilter;
            }
        }

        public MeshRenderer MeshRenderer
        {
            get
            {
                if (meshRenderer == null)
                    meshRenderer = GetComponent<MeshRenderer>();
                return meshRenderer;
            }
        }

        public MeshCollider MeshCollider
        {
            get
            {
                if (meshCollider == null)
                {
                    if (TryGetComponent<MeshCollider>(out var mc))
                        meshCollider = mc;
                    else
                        meshCollider = gameObject.AddComponent<MeshCollider>();

                }
                return meshCollider;
            }
        }

        [Header("Mesh Type")]
        public SplineMeshType meshType = SplineMeshType.Road;

        [Header("Road Settings")]
        public RoadMeshSettings roadSettings = RoadMeshSettings.Default;

        [Header("Elevated Road Settings")]
        public ElevatedRoadMeshSettings elevatedRoadSettings = ElevatedRoadMeshSettings.Default;

        [Header("Tunnel Settings")]
        public TunnelMeshSettings tunnelSettings = TunnelMeshSettings.Default;

        [Header("Ribbon Settings")]
        public RibbonMeshSettings ribbonSettings = RibbonMeshSettings.Default;

        [Header("Mesh Components")]
        public bool generateCollider = true;

        [ContextMenu("Generate Mesh")]
        public void GenerateMesh()
        {
            Mesh mesh = null;

            if (SplineContainer.Spline == null || SplineContainer.Spline.Count == 0)
            {
                Debug.LogWarning("No splines found in the container.");
                return;
            }

            // Generate the appropriate mesh type based on the selected enum
            switch (meshType)
            {
                case SplineMeshType.Road:
                    mesh = SplineContainer.GenerateRoadMesh(roadSettings, transform);
                    break;

                case SplineMeshType.ElevatedRoad:
                    mesh = SplineContainer.GenerateElevatedRoadMesh(elevatedRoadSettings, transform);
                    break;

                case SplineMeshType.Tunnel:
                    mesh = SplineContainer.GenerateTunnelMesh(tunnelSettings, transform);
                    break;

                case SplineMeshType.Ribbon:
                    mesh = SplineContainer.GenerateRibbonMesh(ribbonSettings, transform);
                    break;
            }

            if (mesh == null) return;

            // Assign the generated mesh to the mesh filter
            MeshFilter.sharedMesh = mesh;

            // Update the mesh collider if needed
            if (generateCollider)
            {
                MeshCollider.sharedMesh = mesh;
            }
        }

        public void SetMaterial(Material material)
        {
            if (MeshRenderer != null)
                MeshRenderer.sharedMaterial = material;
        }

    }
}