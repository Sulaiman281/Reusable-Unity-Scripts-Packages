using UnityEngine;
using UnityEngine.Splines;

namespace WitShells.SplineRuntime
{
    /// <summary>
    /// Extension methods for easy spline mesh generation
    /// </summary>
    public static class SplineMeshExtensions
    {
        /// <summary>
        /// Generate a mesh based on the specified mesh type and settings
        /// </summary>
        /// <param name="splineContainer">The spline container to generate from</param>
        /// <param name="meshType">Type of mesh to generate</param>
        /// <param name="settings">Settings object as a JSON string</param>
        /// <param name="transform">Optional transform for local space conversion</param>
        /// <returns>The generated mesh</returns>
        public static Mesh GenerateMesh(this SplineContainer splineContainer, SplineMeshType meshType, string settings, Transform transform = null)
        {
            switch (meshType)
            {
                case SplineMeshType.Road:
                    RoadMeshSettings roadSettings = JsonUtility.FromJson<RoadMeshSettings>(settings);
                    return SplineUtils.CreateRoadMesh(
                        splineContainer,
                        roadSettings.width,
                        roadSettings.baseSettings.materialTilingMultiplier,
                        roadSettings.flipDirection,
                        roadSettings.baseSettings.splineIndex,
                        roadSettings.baseSettings.segments,
                        transform
                    );

                case SplineMeshType.ElevatedRoad:
                    ElevatedRoadMeshSettings elevatedSettings = JsonUtility.FromJson<ElevatedRoadMeshSettings>(settings);
                    return SplineUtils.CreateElevatedRoadMesh(
                        splineContainer,
                        elevatedSettings.width,
                        elevatedSettings.height,
                        elevatedSettings.baseSettings.materialTilingMultiplier,
                        elevatedSettings.baseSettings.splineIndex,
                        elevatedSettings.baseSettings.segments,
                        transform
                    );

                case SplineMeshType.Tunnel:
                    TunnelMeshSettings tunnelSettings = JsonUtility.FromJson<TunnelMeshSettings>(settings);
                    return SplineUtils.CreateTunnelMesh(
                        splineContainer,
                        tunnelSettings.radius,
                        tunnelSettings.baseSettings.segments,
                        tunnelSettings.circleVertices,
                        tunnelSettings.baseSettings.materialTilingMultiplier,
                        tunnelSettings.baseSettings.splineIndex,
                        transform
                    );

                case SplineMeshType.Ribbon:
                    RibbonMeshSettings ribbonSettings = JsonUtility.FromJson<RibbonMeshSettings>(settings);
                    return SplineUtils.CreateRibbonMesh(
                        splineContainer,
                        ribbonSettings.startWidth,
                        ribbonSettings.endWidth,
                        ribbonSettings.baseSettings.materialTilingMultiplier,
                        ribbonSettings.baseSettings.splineIndex,
                        ribbonSettings.baseSettings.segments,
                        ribbonSettings.conformToTerrain,
                        ribbonSettings.terrainOffset,
                        transform
                    );

                default:
                    return null;
            }
        }

        /// <summary>
        /// Generate a mesh based on the specified mesh type and settings object
        /// </summary>
        /// <param name="splineContainer">The spline container to generate from</param>
        /// <param name="meshType">Type of mesh to generate</param>
        /// <param name="settings">Settings struct instance</param>
        /// <param name="transform">Optional transform for local space conversion</param>
        /// <returns>The generated mesh</returns>
        public static Mesh GenerateMesh(this SplineContainer splineContainer, SplineMeshType meshType, object settings, Transform transform = null)
        {
            string json = JsonUtility.ToJson(settings);
            return GenerateMesh(splineContainer, meshType, json, transform);
        }

        /// <summary>
        /// Generate a road mesh with the specified settings
        /// </summary>
        public static Mesh GenerateRoadMesh(this SplineContainer splineContainer, RoadMeshSettings settings, Transform transform = null)
        {
            return SplineUtils.CreateRoadMesh(
                splineContainer,
                settings.width,
                settings.baseSettings.materialTilingMultiplier,
                settings.flipDirection,
                settings.baseSettings.splineIndex,
                settings.baseSettings.segments,
                transform
            );
        }

        /// <summary>
        /// Generate an elevated road mesh with the specified settings
        /// </summary>
        public static Mesh GenerateElevatedRoadMesh(this SplineContainer splineContainer, ElevatedRoadMeshSettings settings, Transform transform = null)
        {
            return SplineUtils.CreateElevatedRoadMesh(
                splineContainer,
                settings.width,
                settings.height,
                settings.baseSettings.materialTilingMultiplier,
                settings.baseSettings.splineIndex,
                settings.baseSettings.segments,
                transform
            );
        }

        /// <summary>
        /// Generate a tunnel mesh with the specified settings
        /// </summary>
        public static Mesh GenerateTunnelMesh(this SplineContainer splineContainer, TunnelMeshSettings settings, Transform transform = null)
        {
            return SplineUtils.CreateTunnelMesh(
                splineContainer,
                settings.radius,
                settings.baseSettings.segments,
                settings.circleVertices,
                settings.baseSettings.materialTilingMultiplier,
                settings.baseSettings.splineIndex,
                transform
            );
        }

        /// <summary>
        /// Generate a ribbon mesh with the specified settings
        /// </summary>
        public static Mesh GenerateRibbonMesh(this SplineContainer splineContainer, RibbonMeshSettings settings, Transform transform = null)
        {
            return SplineUtils.CreateRibbonMesh(
                splineContainer,
                settings.startWidth,
                settings.endWidth,
                settings.baseSettings.materialTilingMultiplier,
                settings.baseSettings.splineIndex,
                settings.baseSettings.segments,
                settings.conformToTerrain,
                settings.terrainOffset,
                transform
            );
        }
    }
}