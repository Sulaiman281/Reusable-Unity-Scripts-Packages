namespace WitShells.SplineRuntime
{
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Splines;
    using System.Collections.Generic;
    using System;
    using WitShells.DesignPatterns.Core;
    using System.Linq;

    public static class SplineUtils
    {
        #region Spline Creation

        /// <summary>
        /// Creates a new SplineContainer with a spline from a list of world positions
        /// </summary>
        /// <param name="positions">List of world positions to form the spline</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="autoSmooth">Whether to automatically smooth the tangents</param>
        /// <returns>A new SplineContainer GameObject</returns>
        public static SplineContainer CreateSplineFromPositions(Vector3[] positions, Transform parent = null, bool autoSmooth = true)
        {
            if (positions == null || positions.Length < 2)
                return null;

            // Create a new GameObject with a SplineContainer
            GameObject splineObject = new GameObject("SplineContainer");
            if (parent != null)
                splineObject.transform.SetParent(parent);

            SplineContainer splineContainer = splineObject.AddComponent<SplineContainer>();

            // Add knots to the spline
            for (int i = 0; i < positions.Length; i++)
            {
                // Convert to local space
                Vector3 localPos = splineObject.transform.InverseTransformPoint(positions[i]);
                splineContainer.Spline.Add(new BezierKnot(localPos));
            }

            // Auto-smooth the tangents if requested
            if (autoSmooth)
                AutoSmoothTangents(splineContainer.Spline);

            return splineContainer;
        }

        /// <summary>
        /// Creates a closed loop spline from positions
        /// </summary>
        /// <param name="positions">Positions to form the loop</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>A new SplineContainer with a closed loop</returns>
        public static SplineContainer CreateClosedLoopSpline(Vector3[] positions, Transform parent = null)
        {
            if (positions == null || positions.Length < 3)
                return null;

            SplineContainer splineContainer = CreateSplineFromPositions(positions, parent, false);
            splineContainer.Spline.Closed = true;
            AutoSmoothTangents(splineContainer.Spline);
            return splineContainer;
        }

        /// <summary>
        /// Creates a path between two points with an arc
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="arcHeight">Height of the arc</param>
        /// <param name="pointCount">Number of points to generate (min 3)</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>SplineContainer with an arcing path</returns>
        public static SplineContainer CreateArcBetweenPoints(Vector3 start, Vector3 end, float arcHeight, int pointCount = 5, Transform parent = null)
        {
            if (pointCount < 3)
                pointCount = 3;

            Vector3[] points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);

                // Linear interpolation on X and Z
                float x = Mathf.Lerp(start.x, end.x, t);
                float z = Mathf.Lerp(start.z, end.z, t);

                // Parabolic arc on Y
                float y = Mathf.Lerp(start.y, end.y, t);
                y += arcHeight * Mathf.Sin(t * Mathf.PI); // Add arc using sine wave

                points[i] = new Vector3(x, y, z);
            }

            return CreateSplineFromPositions(points, parent);
        }

        /// <summary>
        /// Creates a grid of splines forming a network
        /// </summary>
        /// <param name="origin">Bottom-left corner of the grid</param>
        /// <param name="width">Width of the grid (X axis)</param>
        /// <param name="length">Length of the grid (Z axis)</param>
        /// <param name="rows">Number of rows</param>
        /// <param name="columns">Number of columns</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>List of SplineContainer GameObjects forming the grid</returns>
        public static List<SplineContainer> CreateSplineGrid(Vector3 origin, float width, float length, int rows, int columns, Transform parent = null)
        {
            if (rows < 2 || columns < 2)
                return new List<SplineContainer>();

            List<SplineContainer> splines = new List<SplineContainer>();

            // Create horizontal rows
            for (int row = 0; row <= rows; row++)
            {
                float z = origin.z + (length * row / rows);
                Vector3 start = new Vector3(origin.x, origin.y, z);
                Vector3 end = new Vector3(origin.x + width, origin.y, z);

                SplineContainer rowSpline = CreateSplineFromPositions(new Vector3[] { start, end }, parent);
                rowSpline.name = $"Row_{row}";
                splines.Add(rowSpline);
            }

            // Create vertical columns
            for (int col = 0; col <= columns; col++)
            {
                float x = origin.x + (width * col / columns);
                Vector3 start = new Vector3(x, origin.y, origin.z);
                Vector3 end = new Vector3(x, origin.y, origin.z + length);

                SplineContainer colSpline = CreateSplineFromPositions(new Vector3[] { start, end }, parent);
                colSpline.name = $"Column_{col}";
                splines.Add(colSpline);
            }

            return splines;
        }

        /// <summary>
        /// Creates a spiral spline
        /// </summary>
        /// <param name="center">Center of the spiral</param>
        /// <param name="startRadius">Starting radius</param>
        /// <param name="endRadius">Ending radius</param>
        /// <param name="height">Height difference from start to end</param>
        /// <param name="rotations">Number of full rotations</param>
        /// <param name="segments">Number of segments per rotation (higher = smoother)</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>SplineContainer with a spiral shape</returns>
        public static SplineContainer CreateSpiralSpline(Vector3 center, float startRadius, float endRadius, float height,
                                                        float rotations, int segments, Transform parent = null)
        {
            int totalPoints = Mathf.Max(3, Mathf.FloorToInt(segments * rotations));
            Vector3[] points = new Vector3[totalPoints];

            for (int i = 0; i < totalPoints; i++)
            {
                float t = i / (float)(totalPoints - 1);
                float angle = t * rotations * 2f * Mathf.PI;
                float radius = Mathf.Lerp(startRadius, endRadius, t);

                float x = center.x + radius * Mathf.Cos(angle);
                float z = center.z + radius * Mathf.Sin(angle);
                float y = center.y + height * t;

                points[i] = new Vector3(x, y, z);
            }

            SplineContainer spiral = CreateSplineFromPositions(points, parent);
            spiral.name = "Spiral";
            return spiral;
        }

        /// <summary>
        /// Creates a spline that follows terrain height
        /// </summary>
        /// <param name="startPoint">Start position in world space</param>
        /// <param name="endPoint">End position in world space</param>
        /// <param name="heightOffset">Offset from terrain height</param>
        /// <param name="segments">Number of segments (higher = better terrain following)</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>SplineContainer that follows terrain height</returns>
        public static SplineContainer CreateTerrainFollowingSpline(Vector3 startPoint, Vector3 endPoint,
                                                                 float heightOffset, int segments, Transform parent = null)
        {
            if (segments < 2)
                segments = 2;

            Vector3[] points = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);

                // Interpolate XZ position
                float x = Mathf.Lerp(startPoint.x, endPoint.x, t);
                float z = Mathf.Lerp(startPoint.z, endPoint.z, t);

                // Find terrain height at this position
                float terrainHeight = 0f;
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(x, 1000f, z), Vector3.down, out hit, 2000f, LayerMask.GetMask("Terrain")))
                {
                    terrainHeight = hit.point.y;
                }
                else
                {
                    // If no terrain found, linearly interpolate Y
                    terrainHeight = Mathf.Lerp(startPoint.y, endPoint.y, t);
                }

                points[i] = new Vector3(x, terrainHeight + heightOffset, z);
            }

            SplineContainer spline = CreateSplineFromPositions(points, parent);
            spline.name = "TerrainFollowingSpline";
            return spline;
        }

        /// <summary>
        /// Creates a Spline object directly from a list of positions
        /// </summary>
        /// <param name="positions">Array of positions in local space</param>
        /// <param name="autoSmooth">Whether to automatically smooth the tangents</param>
        /// <returns>A new Spline object</returns>
        public static Spline CreateSplineFromLocalPositions(Vector3[] positions, bool autoSmooth = true)
        {
            if (positions == null || positions.Length < 2)
                return null;

            Spline spline = new Spline();

            // Add knots to the spline
            foreach (Vector3 position in positions)
            {
                spline.Add(new BezierKnot(position));
            }

            // Auto-smooth the tangents if requested
            if (autoSmooth)
                AutoSmoothTangents(spline);

            return spline;
        }

        /// <summary>
        /// Creates a Spline object directly from a list of world positions
        /// </summary>
        /// <param name="worldPositions">Array of positions in world space</param>
        /// <param name="referenceTransform">Transform used to convert world to local positions</param>
        /// <param name="autoSmooth">Whether to automatically smooth the tangents</param>
        /// <returns>A new Spline object</returns>
        public static Spline CreateSplineFromWorldPositions(Vector3[] worldPositions, Transform referenceTransform, bool autoSmooth = true)
        {
            if (worldPositions == null || worldPositions.Length < 2 || referenceTransform == null)
                return null;

            Spline spline = new Spline();

            // Add knots to the spline (convert from world to local space)
            foreach (Vector3 worldPos in worldPositions)
            {
                Vector3 localPos = referenceTransform.InverseTransformPoint(worldPos);
                spline.Add(new BezierKnot(localPos));
            }

            // Auto-smooth the tangents if requested
            if (autoSmooth)
                AutoSmoothTangents(spline);

            return spline;
        }

        /// <summary>
        /// Creates a spline from a List of Vector3 positions
        /// </summary>
        /// <param name="positionsList">List of positions in local space</param>
        /// <param name="autoSmooth">Whether to auto-smooth the tangents</param>
        /// <returns>A new Spline object</returns>
        public static Spline CreateSplineFromPositionsList(List<Vector3> positionsList, bool autoSmooth = true)
        {
            if (positionsList == null || positionsList.Count < 2)
                return null;

            return CreateSplineFromLocalPositions(positionsList.ToArray(), autoSmooth);
        }

        #endregion

        #region Spline Modification

        /// <summary>
        /// Automatically smooths the tangents of a spline
        /// </summary>
        /// <param name="spline">The spline to smooth</param>
        /// <param name="tangentScale">Scale factor for tangent length (default: 0.33)</param>
        public static void AutoSmoothTangents(Spline spline, float tangentScale = 0.33f)
        {
            if (spline == null || spline.Count < 2)
                return;

            for (int i = 0; i < spline.Count; i++)
            {
                BezierKnot knot = spline[i];

                // Get previous and next points considering if the spline is closed
                Vector3 prevPos;
                Vector3 nextPos;

                if (spline.Closed)
                {
                    prevPos = i > 0 ? spline[i - 1].Position : spline[spline.Count - 1].Position;
                    nextPos = i < spline.Count - 1 ? spline[i + 1].Position : spline[0].Position;
                }
                else
                {
                    prevPos = i > 0 ? spline[i - 1].Position : knot.Position;
                    nextPos = i < spline.Count - 1 ? spline[i + 1].Position : knot.Position;
                }

                // Calculate smooth tangents
                Vector3 dir = (nextPos - prevPos).normalized;
                float tangentLength = Vector3.Distance(prevPos, nextPos) * tangentScale;

                knot.TangentIn = -dir * tangentLength;
                knot.TangentOut = dir * tangentLength;

                // Apply the updated knot
                spline[i] = knot;
            }
        }

        /// <summary>
        /// Adds a sequence of points to an existing spline
        /// </summary>
        /// <param name="splineContainer">The spline container to modify</param>
        /// <param name="positions">Positions to add</param>
        /// <param name="splineIndex">Index of the spline to modify</param>
        /// <param name="autoSmooth">Whether to auto-smooth tangents</param>
        public static void AddPointsToSpline(SplineContainer splineContainer, Vector3[] positions, int splineIndex = 0, bool autoSmooth = true)
        {
            if (splineContainer == null || positions == null || positions.Length == 0)
                return;

            if (splineIndex >= splineContainer.Splines.Count)
                return;

            Spline targetSpline = splineContainer[splineIndex];

            foreach (Vector3 worldPos in positions)
            {
                // Convert to local space
                Vector3 localPos = splineContainer.transform.InverseTransformPoint(worldPos);
                targetSpline.Add(new BezierKnot(localPos));
            }

            if (autoSmooth)
                AutoSmoothTangents(targetSpline);
        }

        /// <summary>
        /// Offsets a spline by a given distance perpendicular to its direction
        /// </summary>
        /// <param name="sourceSpline">The source spline container</param>
        /// <param name="offset">Distance to offset (positive = right, negative = left)</param>
        /// <param name="splineIndex">Index of the spline to offset</param>
        /// <param name="sampleCount">Number of samples for accuracy</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>New SplineContainer with the offset path</returns>
        public static SplineContainer OffsetSpline(SplineContainer sourceSpline, float offset,
                                                 int splineIndex = 0, int sampleCount = 20, Transform parent = null)
        {
            if (sourceSpline == null || offset == 0f)
                return null;

            // Sample the source spline
            float splineLength = sourceSpline.CalculateLength(splineIndex);
            Vector3[] offsetPoints = new Vector3[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);

                float3 position;
                float3 tangent;
                float3 up;

                if (sourceSpline.Evaluate(splineIndex, t, out position, out tangent, out up))
                {
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                    Vector3 upDir = new Vector3(up.x, up.y, up.z);

                    // Calculate the right vector (perpendicular to the tangent and up)
                    Vector3 right = Vector3.Cross(upDir, tan.normalized).normalized;

                    // Offset the point perpendicular to the tangent
                    offsetPoints[i] = pos + right * offset;
                }
            }

            // Create a new spline with the offset points
            SplineContainer offsetSpline = CreateSplineFromPositions(offsetPoints, parent);
            offsetSpline.name = $"Offset_{sourceSpline.name}";
            return offsetSpline;
        }

        /// <summary>
        /// Merges multiple splines into a single spline container
        /// </summary>
        /// <param name="splines">List of spline containers to merge</param>
        /// <param name="connectEndpoints">Whether to connect the endpoints of consecutive splines</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>SplineContainer containing all the merged splines</returns>
        public static SplineContainer MergeSplines(List<SplineContainer> splines, bool connectEndpoints = false, Transform parent = null)
        {
            if (splines == null || splines.Count == 0)
                return null;

            // Create a new GameObject with a SplineContainer
            GameObject splineObject = new GameObject("MergedSpline");
            if (parent != null)
                splineObject.transform.SetParent(parent);

            SplineContainer mergedContainer = splineObject.AddComponent<SplineContainer>();
            Spline mergedSpline = mergedContainer.Spline;

            for (int s = 0; s < splines.Count; s++)
            {
                SplineContainer container = splines[s];
                if (container == null) continue;

                // Get the points from this spline (first one in container)
                Spline sourceSpline = container[0];
                if (sourceSpline == null || sourceSpline.Count == 0) continue;

                // If this isn't the first spline and we want to connect endpoints
                if (s > 0 && connectEndpoints && mergedSpline.Count > 0)
                {
                    // Add only the points after the first one
                    for (int i = 1; i < sourceSpline.Count; i++)
                    {
                        Vector3 worldPos = container.transform.TransformPoint(sourceSpline[i].Position);
                        Vector3 localPos = splineObject.transform.InverseTransformPoint(worldPos);

                        mergedSpline.Add(new BezierKnot(localPos));
                    }
                }
                else
                {
                    // Add all points
                    for (int i = 0; i < sourceSpline.Count; i++)
                    {
                        Vector3 worldPos = container.transform.TransformPoint(sourceSpline[i].Position);
                        Vector3 localPos = splineObject.transform.InverseTransformPoint(worldPos);

                        mergedSpline.Add(new BezierKnot(localPos));
                    }
                }
            }

            // Auto-smooth the tangents
            AutoSmoothTangents(mergedSpline);

            return mergedContainer;
        }

        /// <summary>
        /// Subdivides a spline by adding additional points between existing knots
        /// </summary>
        /// <param name="splineContainer">The spline container to subdivide</param>
        /// <param name="subdivisions">Number of points to add between each existing pair of knots</param>
        /// <param name="splineIndex">Index of the spline to subdivide</param>
        /// <param name="preserveTangents">Whether to preserve tangent information (true) or auto-smooth (false)</param>
        public static void SubdivideSpline(SplineContainer splineContainer, int subdivisions, int splineIndex = 0, bool preserveTangents = false)
        {
            if (splineContainer == null || subdivisions <= 0 || splineIndex >= splineContainer.Splines.Count)
                return;

            Spline spline = splineContainer[splineIndex];
            if (spline.Count < 2)
                return;

            // Create a copy of the original knots
            List<BezierKnot> originalKnots = new List<BezierKnot>();
            for (int i = 0; i < spline.Count; i++)
            {
                originalKnots.Add(spline[i]);
            }

            // Clear the spline
            spline.Clear();

            // Add back the original points with subdivisions between them
            for (int i = 0; i < originalKnots.Count - 1; i++)
            {
                // Add the original knot
                spline.Add(originalKnots[i]);

                // Add subdivision knots
                BezierKnot start = originalKnots[i];
                BezierKnot end = originalKnots[i + 1];

                for (int j = 1; j <= subdivisions; j++)
                {
                    float t = j / (float)(subdivisions + 1);

                    // Interpolate position
                    Vector3 position = Vector3.Lerp(start.Position, end.Position, t);

                    // Create a new knot
                    BezierKnot newKnot = new BezierKnot(position);

                    // If preserving tangents, interpolate them too
                    if (preserveTangents)
                    {
                        newKnot.TangentIn = Vector3.Lerp(start.TangentIn, end.TangentIn, t);
                        newKnot.TangentOut = Vector3.Lerp(start.TangentOut, end.TangentOut, t);
                    }

                    spline.Add(newKnot);
                }
            }

            // Add the last original knot
            spline.Add(originalKnots[originalKnots.Count - 1]);

            // Auto-smooth the tangents if not preserving them
            if (!preserveTangents)
            {
                AutoSmoothTangents(spline);
            }
        }

        /// <summary>
        /// Reverses the direction of a spline
        /// </summary>
        /// <param name="splineContainer">The spline container to modify</param>
        /// <param name="splineIndex">Index of the spline to reverse</param>
        public static void ReverseSpline(SplineContainer splineContainer, int splineIndex = 0)
        {
            if (splineContainer == null || splineIndex >= splineContainer.Splines.Count)
                return;

            Spline spline = splineContainer[splineIndex];
            if (spline.Count < 2)
                return;

            // Create a reversed copy of the knots
            List<BezierKnot> reversedKnots = new List<BezierKnot>();
            for (int i = spline.Count - 1; i >= 0; i--)
            {
                BezierKnot originalKnot = spline[i];

                // Swap the tangents when reversing
                BezierKnot reversedKnot = new BezierKnot(
                    originalKnot.Position,
                    originalKnot.TangentOut,  // Swap in/out tangents
                    originalKnot.TangentIn,   // Swap in/out tangents
                    originalKnot.Rotation
                );

                reversedKnots.Add(reversedKnot);
            }

            // Clear the spline and add the reversed knots
            spline.Clear();
            foreach (BezierKnot knot in reversedKnots)
            {
                spline.Add(knot);
            }
        }

        #endregion

        #region Spline Sampling

        /// <summary>
        /// Samples points along a spline at regular distance intervals
        /// </summary>
        /// <param name="splineContainer">The spline container to sample</param>
        /// <param name="spacing">Distance between samples</param>
        /// <param name="splineIndex">Index of the spline to sample</param>
        /// <returns>Array of sampled world positions</returns>
        public static Vector3[] SamplePointsAlongSpline(SplineContainer splineContainer, float spacing, int splineIndex = 0)
        {
            if (splineContainer == null || spacing <= 0)
                return new Vector3[0];

            float splineLength = splineContainer.CalculateLength(splineIndex);
            if (splineLength <= 0)
                return new Vector3[0];

            int sampleCount = Mathf.FloorToInt(splineLength / spacing) + 1;
            Vector3[] points = new Vector3[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);

                float3 position;
                if (splineContainer.Evaluate(splineIndex, t, out position, out _, out _))
                {
                    points[i] = new Vector3(position.x, position.y, position.z);
                }
            }

            return points;
        }

        /// <summary>
        /// Gets the closest point on a spline to a world position
        /// </summary>
        /// <param name="splineContainer">The spline container</param>
        /// <param name="worldPosition">The world position to find closest point to</param>
        /// <param name="splineIndex">Index of the spline</param>
        /// <param name="resolution">How many points to check (higher = more accurate but slower)</param>
        /// <returns>Normalized t value (0-1) of the closest point on the spline</returns>
        public static float GetClosestPointOnSpline(SplineContainer splineContainer, Vector3 worldPosition, int splineIndex = 0, int resolution = 100)
        {
            if (splineContainer == null)
                return 0f;

            float closestDistance = float.MaxValue;
            float closestT = 0f;

            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;

                float3 position;
                if (splineContainer.Evaluate(splineIndex, t, out position, out _, out _))
                {
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    float distance = Vector3.Distance(pos, worldPosition);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestT = t;
                    }
                }
            }

            return closestT;
        }

        /// <summary>
        /// Extracts a list of all knot positions from a spline
        /// </summary>
        /// <param name="spline">The spline to extract positions from</param>
        /// <returns>Array of Vector3 positions</returns>
        public static Vector3[] GetPositionsFromSpline(Spline spline)
        {
            if (spline == null || spline.Count == 0)
                return new Vector3[0];

            Vector3[] positions = new Vector3[spline.Count];

            for (int i = 0; i < spline.Count; i++)
            {
                positions[i] = spline[i].Position;
            }

            return positions;
        }

        #endregion

        #region Object Placement

        /// <summary>
        /// Spawns objects along a spline at regular intervals
        /// </summary>
        /// <param name="spline">The spline container to follow</param>
        /// <param name="prefab">The object to spawn</param>
        /// <param name="spacing">Distance between objects</param>
        /// <param name="splineIndex">Index of the spline to use (default: 0)</param>
        public static void SpawnObjectsAlongSpline(SplineContainer spline, GameObject prefab, Transform objectContainer, LayerMask groundLayers,
        bool snapToGround, bool alignToSpline, float groundOffset, float spacing, int splineIndex = 0)
        {
            if (spline == null || prefab == null || spacing <= 0) return;

            // Calculate spline length
            float splineLength = spline.CalculateLength(splineIndex);
            if (splineLength <= 0) return;

            // Create container if needed
            if (objectContainer == null)
            {
                GameObject container = new GameObject("SpawnedObjects");
                container.transform.SetParent(spline.transform);
                container.transform.localPosition = Vector3.zero;
                objectContainer = container.transform;
            }

            // Calculate how many objects to place
            int objectCount = Mathf.FloorToInt(splineLength / spacing);
            if (objectCount <= 0) objectCount = 1;

            // Place objects at regular intervals
            for (int i = 0; i <= objectCount; i++)
            {
                // Calculate position along spline (0 to 1)
                float t = i / (float)objectCount;

                // Get position and orientation from spline
                float3 position;
                float3 tangent;
                float3 up;

                if (spline.Evaluate(splineIndex, t, out position, out tangent, out up))
                {
                    // Convert from float3 to Vector3
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                    Vector3 upDir = new Vector3(up.x, up.y, up.z);

                    // Apply ground snapping if enabled
                    if (snapToGround)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayers))
                        {
                            pos = hit.point + Vector3.up * groundOffset;
                        }
                    }

                    // Instantiate the object
                    GameObject obj = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity, objectContainer);
                    obj.name = $"{prefab.name}_{i}";

                    // Align to spline direction if enabled
                    if (alignToSpline)
                    {
                        // Create a rotation that accurately aligns to the spline
                        // Using LookRotation to create a rotation from forward and up vectors
                        Vector3 normalizedTangent = tan.normalized;
                        Vector3 normalizedUp = upDir.normalized;

                        // Make sure up vector is perpendicular to tangent for proper orientation
                        Vector3 right = Vector3.Cross(normalizedTangent, normalizedUp).normalized;
                        Vector3 adjustedUp = Vector3.Cross(right, normalizedTangent).normalized;

                        // Create rotation from orthogonal vectors
                        Quaternion splineRotation = Quaternion.LookRotation(normalizedTangent, adjustedUp);
                        obj.transform.rotation = splineRotation;
                    }
                }
            }
        }

        /// <summary>
        /// Spawns objects along a spline at regular intervals with advanced rotation control
        /// </summary>
        /// <param name="spline">The spline container to follow</param>
        /// <param name="prefab">The object to spawn</param>
        /// <param name="objectContainer">Container to parent spawned objects to</param>
        /// <param name="groundLayers">Layers to raycast against for ground snapping</param>
        /// <param name="snapToGround">Whether to snap objects to ground</param>
        /// <param name="alignToSpline">Whether to align objects to spline direction</param>
        /// <param name="groundOffset">Height offset from ground when snapping</param>
        /// <param name="spacing">Distance between objects</param>
        /// <param name="rotationOffset">Additional rotation to apply to objects (in degrees)</param>
        /// <param name="splineIndex">Index of the spline to use (default: 0)</param>
        public static void SpawnObjectsAlongSplineAdvanced(SplineContainer spline, GameObject prefab, Transform objectContainer,
            LayerMask groundLayers, bool snapToGround, bool alignToSpline, float groundOffset, float spacing,
            Vector3 rotationOffset, int splineIndex = 0)
        {
            if (spline == null || prefab == null || spacing <= 0) return;

            // Calculate spline length
            float splineLength = spline.CalculateLength(splineIndex);
            if (splineLength <= 0) return;

            // Create container if needed
            if (objectContainer == null)
            {
                GameObject container = new GameObject("SpawnedObjects");
                container.transform.SetParent(spline.transform);
                container.transform.localPosition = Vector3.zero;
                objectContainer = container.transform;
            }

            // Calculate how many objects to place
            int objectCount = Mathf.FloorToInt(splineLength / spacing);
            if (objectCount <= 0) objectCount = 1;

            // Create rotation offset quaternion
            Quaternion rotOffset = Quaternion.Euler(rotationOffset);

            // Place objects at regular intervals
            for (int i = 0; i <= objectCount; i++)
            {
                // Calculate position along spline (0 to 1)
                float t = i / (float)objectCount;

                // Get position and orientation from spline
                float3 position;
                float3 tangent;
                float3 up;

                if (spline.Evaluate(splineIndex, t, out position, out tangent, out up))
                {
                    // Convert from float3 to Vector3
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                    Vector3 upDir = new Vector3(up.x, up.y, up.z);

                    // Apply ground snapping if enabled
                    if (snapToGround)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayers))
                        {
                            pos = hit.point + Vector3.up * groundOffset;
                        }
                    }

                    // Instantiate the object
                    GameObject obj = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity, objectContainer);
                    obj.name = $"{prefab.name}_{i}";

                    // Align to spline direction if enabled
                    if (alignToSpline)
                    {
                        // Create a rotation that accurately aligns to the spline
                        Vector3 normalizedTangent = tan.normalized;
                        Vector3 normalizedUp = upDir.normalized;

                        // Make sure up vector is perpendicular to tangent
                        Vector3 right = Vector3.Cross(normalizedTangent, normalizedUp).normalized;
                        Vector3 adjustedUp = Vector3.Cross(right, normalizedTangent).normalized;

                        // Create rotation from orthogonal vectors and apply offset
                        Quaternion splineRotation = Quaternion.LookRotation(normalizedTangent, adjustedUp) * rotOffset;
                        obj.transform.rotation = splineRotation;
                    }
                    else
                    {
                        // Apply just the rotation offset if not aligning to spline
                        obj.transform.rotation = rotOffset;
                    }
                }
            }
        }

        public static void SpawnObjectsAlongSplineAdvanced(SplineContainer spline, ObjectPool<GameObject> prefabPool, Transform objectContainer,
            LayerMask groundLayers, bool snapToGround, bool alignToSpline, float groundOffset, float spacing,
            Vector3 rotationOffset, int splineIndex = 0)
        {
            if (spline == null || prefabPool == null || spacing <= 0) return;

            // Calculate spline length
            float splineLength = spline.CalculateLength(splineIndex);
            if (splineLength <= 0) return;

            // Create container if needed
            if (objectContainer == null)
            {
                GameObject container = new GameObject("SpawnedObjects");
                container.transform.SetParent(spline.transform);
                container.transform.localPosition = Vector3.zero;
                objectContainer = container.transform;
            }

            // Calculate how many objects to place
            int objectCount = Mathf.FloorToInt(splineLength / spacing);
            if (objectCount <= 0) objectCount = 1;

            // Create rotation offset quaternion
            Quaternion rotOffset = Quaternion.Euler(rotationOffset);

            // release all objects in the pool first
            for (int i = 0; i < objectContainer.childCount; i++)
            {
                Transform child = objectContainer.GetChild(i);
                child.gameObject.SetActive(false);
                prefabPool.Release(child.gameObject);
            }

            // Place objects at regular intervals
            for (int i = 0; i <= objectCount; i++)
            {
                // Calculate position along spline (0 to 1)
                float t = i / (float)objectCount;

                // Get position and orientation from spline
                float3 position;
                float3 tangent;
                float3 up;

                if (spline.Evaluate(splineIndex, t, out position, out tangent, out up))
                {
                    // Convert from float3 to Vector3
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                    Vector3 upDir = new Vector3(up.x, up.y, up.z);

                    // Apply ground snapping if enabled
                    if (snapToGround)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayers))
                        {
                            pos = hit.point + Vector3.up * groundOffset;
                        }
                    }

                    // Instantiate the object
                    GameObject obj = prefabPool.Get();
                    obj.transform.SetParent(objectContainer);
                    obj.transform.position = pos;
                    obj.SetActive(true);

                    // Align to spline direction if enabled
                    if (alignToSpline)
                    {
                        // Create a rotation that accurately aligns to the spline
                        Vector3 normalizedTangent = tan.normalized;
                        Vector3 normalizedUp = upDir.normalized;

                        // Make sure up vector is perpendicular to tangent
                        Vector3 right = Vector3.Cross(normalizedTangent, normalizedUp).normalized;
                        Vector3 adjustedUp = Vector3.Cross(right, normalizedTangent).normalized;

                        // Create rotation from orthogonal vectors and apply offset
                        Quaternion splineRotation = Quaternion.LookRotation(normalizedTangent, adjustedUp) * rotOffset;
                        obj.transform.rotation = splineRotation;
                    }
                    else
                    {
                        // Apply just the rotation offset if not aligning to spline
                        obj.transform.rotation = rotOffset;
                    }
                }
            }
        }

        #endregion

        #region Mesh Generation

        /// <summary>
        /// Creates a road mesh from a spline path
        /// </summary>
        /// <param name="splineContainer">The spline to follow</param>
        /// <param name="width">Width of the road</param>
        /// <param name="materialTilingMultiplier">Multiplier for texture tiling (higher value = more tiling)</param>
        /// <param name="flipDirection">Whether to flip the road direction</param>
        /// <param name="splineIndex">Index of the spline to use</param>
        /// <param name="segments">Number of segments for the road mesh (higher = smoother)</param>
        /// <param name="transform">Optional transform to convert to local space</param>
        /// <returns>A generated road mesh</returns>
        public static Mesh CreateRoadMesh(SplineContainer splineContainer, float width, float materialTilingMultiplier = 1f,
                                        bool flipDirection = false, int splineIndex = 0, int segments = 100, Transform transform = null)
        {
            if (splineContainer == null || width <= 0)
                return null;

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = "RoadMesh";
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float splineLength = splineContainer.CalculateLength(splineIndex);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                // Get position and orientation from spline
                float3 position;
                float3 tangent;
                float3 upVector;

                if (splineContainer.Evaluate(splineIndex, t, out position, out tangent, out upVector))
                {
                    // Convert float3 to Vector3
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);

                    // Use world up vector for consistent orientation
                    Vector3 worldUp = Vector3.up;

                    // Calculate right vector properly
                    Vector3 right = Vector3.Cross(tan.normalized, worldUp).normalized;

                    if (flipDirection)
                        right = -right;

                    // Create road vertices
                    Vector3 leftEdge = pos - right * width * 0.5f;
                    Vector3 rightEdge = pos + right * width * 0.5f;

                    // Convert to local space if transform is provided
                    if (transform != null)
                    {
                        leftEdge = transform.InverseTransformPoint(leftEdge);
                        rightEdge = transform.InverseTransformPoint(rightEdge);
                    }

                    // Add to mesh
                    vertices.Add(leftEdge);
                    vertices.Add(rightEdge);

                    // UV scaling based on spline length for proper texture tiling
                    uvs.Add(new Vector2(0, t * splineLength * materialTilingMultiplier));
                    uvs.Add(new Vector2(1, t * splineLength * materialTilingMultiplier));

                    // Add triangles
                    if (i > 0)
                    {
                        int baseIndex = (i - 1) * 2;

                        // Keep triangle winding order consistent for proper normal direction
                        triangles.Add(baseIndex);
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 1);

                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 3);
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Creates an elevated road mesh with side walls
        /// </summary>
        /// <param name="splineContainer">The spline to follow</param>
        /// <param name="width">Width of the road</param>
        /// <param name="height">Height of the side walls</param>
        /// <param name="materialTilingMultiplier">Multiplier for texture tiling</param>
        /// <param name="splineIndex">Index of the spline to use</param>
        /// <param name="segments">Number of segments for the road mesh</param>
        /// <param name="transform">Optional transform to convert to local space</param>
        /// <returns>A generated elevated road mesh with walls</returns>
        public static Mesh CreateElevatedRoadMesh(SplineContainer splineContainer, float width, float height,
                                                float materialTilingMultiplier = 1f, int splineIndex = 0,
                                                int segments = 100, Transform transform = null)
        {
            if (splineContainer == null || width <= 0 || height <= 0)
                return null;

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = "ElevatedRoadMesh";
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float splineLength = splineContainer.CalculateLength(splineIndex);
            float halfWidth = width * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                // Get position and orientation from spline
                float3 position;
                float3 tangent;
                float3 upVector;

                if (splineContainer.Evaluate(splineIndex, t, out position, out tangent, out upVector))
                {
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                    Vector3 up = new Vector3(upVector.x, upVector.y, upVector.z);

                    // Calculate right vector
                    Vector3 right = Vector3.Cross(up, tan.normalized).normalized;

                    // Create road vertices (4 points for each cross-section: left wall top, left road, right road, right wall top)
                    Vector3 leftWallTop = pos - right * halfWidth;
                    Vector3 leftRoad = leftWallTop - up * height;
                    Vector3 rightRoad = pos + right * halfWidth - up * height;
                    Vector3 rightWallTop = rightRoad + up * height;

                    // Convert to local space if transform is provided
                    if (transform != null)
                    {
                        leftWallTop = transform.InverseTransformPoint(leftWallTop);
                        leftRoad = transform.InverseTransformPoint(leftRoad);
                        rightRoad = transform.InverseTransformPoint(rightRoad);
                        rightWallTop = transform.InverseTransformPoint(rightWallTop);
                    }

                    // Add vertices
                    vertices.Add(leftWallTop);  // 0: Left wall top
                    vertices.Add(leftRoad);     // 1: Left road
                    vertices.Add(rightRoad);    // 2: Right road
                    vertices.Add(rightWallTop); // 3: Right wall top

                    // UVs for proper texture mapping
                    float vCoord = t * splineLength * materialTilingMultiplier;
                    uvs.Add(new Vector2(0, vCoord + height));  // Left wall top
                    uvs.Add(new Vector2(0, vCoord));           // Left road
                    uvs.Add(new Vector2(1, vCoord));           // Right road
                    uvs.Add(new Vector2(1, vCoord + height));  // Right wall top

                    // Add triangles
                    if (i > 0)
                    {
                        int baseIndex = (i - 1) * 4;

                        // Left wall
                        triangles.Add(baseIndex + 0);
                        triangles.Add(baseIndex + 4);
                        triangles.Add(baseIndex + 1);

                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 4);
                        triangles.Add(baseIndex + 5);

                        // Road surface
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 5);
                        triangles.Add(baseIndex + 2);

                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 5);
                        triangles.Add(baseIndex + 6);

                        // Right wall
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 6);
                        triangles.Add(baseIndex + 3);

                        triangles.Add(baseIndex + 3);
                        triangles.Add(baseIndex + 6);
                        triangles.Add(baseIndex + 7);
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Creates a tunnel mesh along a spline
        /// </summary>
        /// <param name="splineContainer">The spline to follow</param>
        /// <param name="radius">Radius of the tunnel</param>
        /// <param name="segments">Number of segments along the spline</param>
        /// <param name="circleVertices">Number of vertices around the circumference</param>
        /// <param name="materialTilingMultiplier">Texture tiling multiplier</param>
        /// <param name="splineIndex">Index of the spline to use</param>
        /// <param name="transform">Optional transform to convert to local space</param>
        /// <returns>A generated tunnel mesh</returns>
        public static Mesh CreateTunnelMesh(SplineContainer splineContainer, float radius, int segments = 50,
                                          int circleVertices = 12, float materialTilingMultiplier = 1f,
                                          int splineIndex = 0, Transform transform = null)
        {
            if (splineContainer == null || radius <= 0)
                return null;

            Mesh mesh = new Mesh();
            mesh.name = "TunnelMesh";
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float splineLength = splineContainer.CalculateLength(splineIndex);

            // For each segment along the spline
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                float3 position;
                float3 tangent;
                float3 up;

                if (splineContainer.Evaluate(splineIndex, t, out position, out tangent, out up))
                {
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z).normalized;
                    Vector3 upDir = new Vector3(up.x, up.y, up.z).normalized;
                    Vector3 right = Vector3.Cross(upDir, tan).normalized;

                    // Create a circle of vertices around the spline point
                    for (int j = 0; j < circleVertices; j++)
                    {
                        float angle = j * (2 * Mathf.PI / circleVertices);
                        float x = Mathf.Cos(angle);
                        float y = Mathf.Sin(angle);

                        // Calculate position of vertex in circle
                        Vector3 vertexPos = pos + (right * x + upDir * y) * radius;

                        // Convert to local space if transform is provided
                        if (transform != null)
                        {
                            vertexPos = transform.InverseTransformPoint(vertexPos);
                        }

                        vertices.Add(vertexPos);

                        // UV mapping (u = position around circle, v = position along spline)
                        float u = j / (float)circleVertices;
                        float v = t * splineLength * materialTilingMultiplier;
                        uvs.Add(new Vector2(u, v));

                        // Add triangles (connect this circle to the previous one)
                        if (i > 0 && j < circleVertices)
                        {
                            int currentIndex = i * circleVertices + j;
                            int prevIndex = (i - 1) * circleVertices + j;

                            int nextJ = (j + 1) % circleVertices;
                            int currentIndexNext = i * circleVertices + nextJ;
                            int prevIndexNext = (i - 1) * circleVertices + nextJ;

                            // First triangle
                            triangles.Add(prevIndex);
                            triangles.Add(currentIndex);
                            triangles.Add(prevIndexNext);

                            // Second triangle
                            triangles.Add(prevIndexNext);
                            triangles.Add(currentIndex);
                            triangles.Add(currentIndexNext);
                        }
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Creates a ribbon mesh following a spline, useful for rivers, streams, or paths
        /// </summary>
        /// <param name="splineContainer">The spline to follow</param>
        /// <param name="startWidth">Width at the start of the spline</param>
        /// <param name="endWidth">Width at the end of the spline</param>
        /// <param name="materialTilingMultiplier">Texture tiling multiplier</param>
        /// <param name="splineIndex">Index of the spline to use</param>
        /// <param name="segments">Number of segments along the spline</param>
        /// <param name="conformToTerrain">Whether to conform to terrain height</param>
        /// <param name="terrainOffset">Height offset from terrain</param>
        /// <param name="transform">Optional transform to convert to local space</param>
        /// <returns>A ribbon mesh following the spline with varying width</returns>
        public static Mesh CreateRibbonMesh(SplineContainer splineContainer, float startWidth, float endWidth,
                                          float materialTilingMultiplier = 1f, int splineIndex = 0, int segments = 100,
                                          bool conformToTerrain = false, float terrainOffset = 0.1f, Transform transform = null)
        {
            if (splineContainer == null || startWidth <= 0 || endWidth <= 0)
                return null;

            Mesh mesh = new Mesh();
            mesh.name = "RibbonMesh";
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float splineLength = splineContainer.CalculateLength(splineIndex);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                // Interpolate width along the spline
                float width = Mathf.Lerp(startWidth, endWidth, t);
                float halfWidth = width * 0.5f;

                float3 position;
                float3 tangent;
                float3 upVector;

                if (splineContainer.Evaluate(splineIndex, t, out position, out tangent, out upVector))
                {
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);

                    // Use world up for consistent orientation
                    Vector3 worldUp = Vector3.up;
                    Vector3 right = Vector3.Cross(tan.normalized, worldUp).normalized;

                    // Create ribbon vertices
                    Vector3 leftEdge = pos - right * halfWidth;
                    Vector3 rightEdge = pos + right * halfWidth;

                    // Apply terrain conforming if requested
                    if (conformToTerrain)
                    {
                        RaycastHit hitLeft, hitRight;
                        if (Physics.Raycast(leftEdge + Vector3.up * 100f, Vector3.down, out hitLeft, 200f, LayerMask.GetMask("Terrain")))
                        {
                            leftEdge.y = hitLeft.point.y + terrainOffset;
                        }

                        if (Physics.Raycast(rightEdge + Vector3.up * 100f, Vector3.down, out hitRight, 200f, LayerMask.GetMask("Terrain")))
                        {
                            rightEdge.y = hitRight.point.y + terrainOffset;
                        }
                    }

                    // Convert to local space if transform is provided
                    if (transform != null)
                    {
                        leftEdge = transform.InverseTransformPoint(leftEdge);
                        rightEdge = transform.InverseTransformPoint(rightEdge);
                    }

                    // Add to mesh
                    vertices.Add(leftEdge);
                    vertices.Add(rightEdge);

                    // UV mapping
                    uvs.Add(new Vector2(0, t * splineLength * materialTilingMultiplier));
                    uvs.Add(new Vector2(1, t * splineLength * materialTilingMultiplier));

                    // Add triangles
                    if (i > 0)
                    {
                        int baseIndex = (i - 1) * 2;

                        triangles.Add(baseIndex);
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 1);

                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 3);
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Helper method to check if mesh normals need to be flipped
        /// </summary>
        /// <param name="mesh">The mesh to check</param>
        /// <param name="transform">Optional transform to convert normals to world space</param>
        /// <returns>True if normals should be flipped</returns>
        public static bool NeedToFlipNormals(Mesh mesh, Transform transform = null)
        {
            if (mesh == null || mesh.normals.Length == 0)
                return false;

            Vector3[] normals = mesh.normals;

            // Check if the majority of normals are pointing down
            int downCount = 0;
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 worldNormal = transform != null
                    ? transform.TransformDirection(normals[i])
                    : normals[i];

                if (worldNormal.y < 0)
                    downCount++;
            }

            return downCount > normals.Length / 2;
        }

        /// <summary>
        /// Flips the normals of a mesh
        /// </summary>
        /// <param name="mesh">The mesh to modify</param>
        public static void FlipNormals(Mesh mesh)
        {
            if (mesh == null) return;

            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            mesh.normals = normals;

            // Also reverse triangle order
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 2];
                triangles[i + 2] = temp;
            }
            mesh.triangles = triangles;
        }

        #endregion

        #region Transform Utilities

        /// <summary>
        /// Converts all child positions of a container transform to a list of Vector3 positions
        /// </summary>
        /// <param name="container">The parent transform containing child objects</param>
        /// <param name="includeInactive">Whether to include inactive child objects (default: false)</param>
        /// <returns>List of Vector3 positions of all children</returns>
        public static List<Vector3> ContainerChildrenToPositionList(Transform container, bool includeInactive = false, bool localPositions = true)
        {
            if (container == null)
                return new List<Vector3>();

            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);

                if (!includeInactive && !child.gameObject.activeSelf)
                    continue;

                positions.Add(localPositions ? child.localPosition : child.position);
            }

            return positions;
        }

        /// <summary>
        /// Converts a list of transforms to a list of their world positions
        /// </summary>
        /// <param name="transforms">List of transforms to convert</param>
        /// <returns>List of Vector3 positions</returns>
        public static List<Vector3> TransformsToPositionList(List<Transform> transforms)
        {
            if (transforms == null)
                return new List<Vector3>();

            List<Vector3> positions = new List<Vector3>();

            foreach (Transform transform in transforms)
            {
                if (transform == null)
                    continue;

                positions.Add(transform.position);
            }

            return positions;
        }

        /// <summary>
        /// Converts an array of transforms to a list of their world positions
        /// </summary>
        /// <param name="transforms">Array of transforms to convert</param>
        /// <returns>List of Vector3 positions</returns>
        public static List<Vector3> TransformsToPositionList(Transform[] transforms)
        {
            if (transforms == null)
                return new List<Vector3>();

            List<Vector3> positions = new List<Vector3>();

            foreach (Transform transform in transforms)
            {
                if (transform == null)
                    continue;

                positions.Add(transform.position);
            }

            return positions;
        }

        /// <summary>
        /// Places a single object at a specific point along a spline with precise rotation control
        /// </summary>
        /// <param name="spline">The spline container</param>
        /// <param name="prefab">The object to spawn</param>
        /// <param name="parent">Parent transform for the spawned object</param>
        /// <param name="normalizeDistance">Whether the distance is normalized (0-1) or in world units</param>
        /// <param name="distance">Distance along the spline</param>
        /// <param name="rotationOffset">Additional rotation to apply (in degrees)</param>
        /// <param name="forwardAxis">Which local axis should point forward along the spline</param>
        /// <param name="upAxis">Which local axis should align with the spline's up direction</param>
        /// <param name="splineIndex">Index of the spline to use (default: 0)</param>
        /// <returns>The instantiated GameObject or null if failed</returns>
        public static GameObject PlaceObjectAtSplinePoint(SplineContainer spline, GameObject prefab, Transform parent,
            bool normalizeDistance, float distance, Vector3 rotationOffset, Vector3 forwardAxis, Vector3 upAxis,
            int splineIndex = 0)
        {
            if (spline == null || prefab == null) return null;

            // Calculate t value (normalized position)
            float t;
            if (normalizeDistance)
            {
                // t is already normalized (0-1)
                t = Mathf.Clamp01(distance);
            }
            else
            {
                // Convert world distance to normalized value
                float splineLength = spline.CalculateLength(splineIndex);
                if (splineLength <= 0) return null;

                t = Mathf.Clamp01(distance / splineLength);
            }

            // Evaluate the spline at the specified position
            float3 position;
            float3 tangent;
            float3 up;

            if (spline.Evaluate(splineIndex, t, out position, out tangent, out up))
            {
                // Convert from float3 to Vector3
                Vector3 pos = new Vector3(position.x, position.y, position.z);
                Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                Vector3 upDir = new Vector3(up.x, up.y, up.z);

                // Instantiate the object
                GameObject obj = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity, parent);
                obj.name = $"{prefab.name}_SplinePoint";

                // Normalize vectors
                Vector3 normalizedTangent = tan.normalized;
                Vector3 normalizedUp = upDir.normalized;

                // Make sure up vector is perpendicular to tangent
                Vector3 right = Vector3.Cross(normalizedTangent, normalizedUp).normalized;
                Vector3 adjustedUp = Vector3.Cross(right, normalizedTangent).normalized;

                // Create base rotation from orthogonal vectors
                Quaternion splineRotation = Quaternion.LookRotation(normalizedTangent, adjustedUp);

                // Create and apply custom rotation to align specified axes
                Quaternion axisRotation = Quaternion.identity;

                // Only apply axis alignment if custom axes are provided
                if (forwardAxis != Vector3.forward || upAxis != Vector3.up)
                {
                    // Normalize custom axes
                    forwardAxis.Normalize();
                    upAxis.Normalize();

                    // Calculate the rotation needed to align the object's axes with the spline
                    Quaternion fromToRotation = Quaternion.Inverse(
                        Quaternion.LookRotation(forwardAxis, upAxis)
                    );

                    axisRotation = fromToRotation;
                }

                // Apply all rotations: spline direction, axis alignment, and custom offset
                obj.transform.rotation = splineRotation * axisRotation * Quaternion.Euler(rotationOffset);

                return obj;
            }

            return null;
        }

        /// <summary>
        /// Updates the positions and rotations of objects based on a spline created from updated positions
        /// </summary>
        /// <param name="positions">List of Vector3 positions to create the spline from</param>
        /// <param name="objects">List of GameObjects to update positions and rotations for</param>
        /// <param name="alignToSpline">Whether to align objects to the spline direction</param>
        /// <param name="rotationOffset">Additional rotation to apply (in degrees)</param>
        /// <param name="distributeEvenly">If true, distributes objects evenly along spline; if false, tries to match positions to closest spline points</param>
        /// <param name="forwardAxis">Which local axis should point forward along the spline</param>
        /// <param name="upAxis">Which local axis should align with the spline's up direction</param>
        /// <returns>The temporary SplineContainer created for the update (null if failed)</returns>
        public static SplineContainer UpdateObjectsAlongSpline(List<Vector3> positions, List<GameObject> objects,
            bool alignToSpline = true, Vector3 rotationOffset = default, bool distributeEvenly = true,
            Vector3 forwardAxis = default, Vector3 upAxis = default)
        {
            if (positions == null || positions.Count < 2 || objects == null || objects.Count == 0)
                return null;

            // Default axes if not specified
            if (forwardAxis == default) forwardAxis = Vector3.forward;
            if (upAxis == default) upAxis = Vector3.up;

            // Create a temporary GameObject to hold the SplineContainer
            GameObject tempGO = new GameObject("TempSplineContainer");
            tempGO.hideFlags = HideFlags.HideAndDontSave; // Hidden in hierarchy

            // Create spline from positions
            SplineContainer splineContainer = CreateSplineFromPositions(positions.ToArray(), tempGO.transform, true);
            if (splineContainer == null)
            {
                UnityEngine.Object.Destroy(tempGO);
                return null;
            }

            float splineLength = splineContainer.CalculateLength(0);
            if (splineLength <= 0)
            {
                UnityEngine.Object.Destroy(tempGO);
                return null;
            }

            // Create rotation offset quaternion
            Quaternion rotOffset = Quaternion.Euler(rotationOffset);

            if (distributeEvenly)
            {
                // Distribute objects evenly along the spline
                int objectCount = objects.Count;

                for (int i = 0; i < objectCount; i++)
                {
                    if (objects[i] == null) continue;

                    // Calculate normalized position along spline (0 to 1)
                    float t = (objectCount > 1) ? (float)i / (objectCount - 1) : 0f;

                    // Get position and orientation from spline
                    float3 position;
                    float3 tangent;
                    float3 up;

                    if (splineContainer.Evaluate(0, t, out position, out tangent, out up))
                    {
                        // Convert from float3 to Vector3
                        Vector3 pos = new Vector3(position.x, position.y, position.z);
                        Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                        Vector3 upDir = new Vector3(up.x, up.y, up.z);

                        // Update position
                        objects[i].transform.position = pos;

                        // Update rotation if needed
                        if (alignToSpline)
                        {
                            // Create a rotation that accurately aligns to the spline
                            Vector3 normalizedTangent = tan.normalized;
                            Vector3 normalizedUp = upDir.normalized;

                            // Make sure up vector is perpendicular to tangent
                            Vector3 right = Vector3.Cross(normalizedTangent, normalizedUp).normalized;
                            Vector3 adjustedUp = Vector3.Cross(right, normalizedTangent).normalized;

                            // Create base rotation from orthogonal vectors
                            Quaternion splineRotation = Quaternion.LookRotation(normalizedTangent, adjustedUp);

                            // Create and apply custom rotation to align specified axes
                            Quaternion axisRotation = Quaternion.identity;

                            // Only apply axis alignment if custom axes are provided
                            if (forwardAxis != Vector3.forward || upAxis != Vector3.up)
                            {
                                // Normalize custom axes
                                Vector3 normForwardAxis = forwardAxis.normalized;
                                Vector3 normUpAxis = upAxis.normalized;

                                // Calculate the rotation needed to align the object's axes with the spline
                                Quaternion fromToRotation = Quaternion.Inverse(
                                    Quaternion.LookRotation(normForwardAxis, normUpAxis)
                                );

                                axisRotation = fromToRotation;
                            }

                            // Apply all rotations: spline direction, axis alignment, and custom offset
                            objects[i].transform.rotation = splineRotation * axisRotation * rotOffset;
                        }
                        else if (rotOffset != Quaternion.identity)
                        {
                            // Apply just the rotation offset if not aligning to spline
                            objects[i].transform.rotation = rotOffset;
                        }
                    }
                }
            }
            else
            {
                // For each object, find the closest point on the spline
                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i] == null) continue;

                    // Find closest point on spline
                    float t = GetClosestPointOnSpline(splineContainer, objects[i].transform.position);

                    // Get position and orientation from spline
                    float3 position;
                    float3 tangent;
                    float3 up;

                    if (splineContainer.Evaluate(0, t, out position, out tangent, out up))
                    {
                        // Convert from float3 to Vector3
                        Vector3 pos = new Vector3(position.x, position.y, position.z);
                        Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                        Vector3 upDir = new Vector3(up.x, up.y, up.z);

                        // Update position
                        objects[i].transform.position = pos;

                        // Update rotation if needed
                        if (alignToSpline)
                        {
                            // Create a rotation that accurately aligns to the spline
                            Vector3 normalizedTangent = tan.normalized;
                            Vector3 normalizedUp = upDir.normalized;

                            // Make sure up vector is perpendicular to tangent
                            Vector3 right = Vector3.Cross(normalizedTangent, normalizedUp).normalized;
                            Vector3 adjustedUp = Vector3.Cross(right, normalizedTangent).normalized;

                            // Create base rotation from orthogonal vectors
                            Quaternion splineRotation = Quaternion.LookRotation(normalizedTangent, adjustedUp);

                            // Create and apply custom rotation to align specified axes
                            Quaternion axisRotation = Quaternion.identity;

                            // Only apply axis alignment if custom axes are provided
                            if (forwardAxis != Vector3.forward || upAxis != Vector3.up)
                            {
                                // Normalize custom axes
                                Vector3 normForwardAxis = forwardAxis.normalized;
                                Vector3 normUpAxis = upAxis.normalized;

                                // Calculate the rotation needed to align the object's axes with the spline
                                Quaternion fromToRotation = Quaternion.Inverse(
                                    Quaternion.LookRotation(normForwardAxis, normUpAxis)
                                );

                                axisRotation = fromToRotation;
                            }

                            // Apply all rotations: spline direction, axis alignment, and custom offset
                            objects[i].transform.rotation = splineRotation * axisRotation * rotOffset;
                        }
                        else if (rotOffset != Quaternion.identity)
                        {
                            // Apply just the rotation offset if not aligning to spline
                            objects[i].transform.rotation = rotOffset;
                        }
                    }
                }
            }

            return splineContainer;
        }

        /// <summary>
        /// Updates the positions and rotations of objects based on a spline created from updated positions, 
        /// and automatically cleans up the temporary spline
        /// </summary>
        /// <param name="positions">List of Vector3 positions to create the spline from</param>
        /// <param name="objects">List of GameObjects to update positions and rotations for</param>
        /// <param name="alignToSpline">Whether to align objects to the spline direction</param>
        /// <param name="rotationOffset">Additional rotation to apply (in degrees)</param>
        /// <param name="distributeEvenly">If true, distributes objects evenly along spline; if false, tries to match positions to closest spline points</param>
        /// <param name="forwardAxis">Which local axis should point forward along the spline</param>
        /// <param name="upAxis">Which local axis should align with the spline's up direction</param>
        public static void UpdateObjectsAlongSplineAndCleanup(List<Vector3> positions, List<GameObject> objects,
            bool alignToSpline = true, Vector3 rotationOffset = default, bool distributeEvenly = true,
            Vector3 forwardAxis = default, Vector3 upAxis = default)
        {
            SplineContainer splineContainer = UpdateObjectsAlongSpline(positions, objects, alignToSpline,
                                                                     rotationOffset, distributeEvenly,
                                                                     forwardAxis, upAxis);

            // Clean up the temporary GameObject and SplineContainer
            if (splineContainer != null)
            {
                UnityEngine.Object.Destroy(splineContainer.gameObject);
            }
        }

        /// <summary>
        /// Distributes GameObjects along a spline using object pooling
        /// </summary>
        /// <param name="spline">The spline to distribute objects along</param>
        /// <param name="getPooledObject">Function to get an object from a pool</param>
        /// <param name="releasePooledObject">Function to release an object back to the pool</param>
        /// <param name="container">Container transform to parent the objects to</param>
        /// <param name="objectCount">Number of objects to distribute along the spline</param>
        /// <param name="existingObjects">Optional list of existing objects to release back to the pool</param>
        /// <param name="splineIndex">Index of the spline to use (default: 0)</param>
        /// <param name="alignToSpline">Whether to align objects to the spline direction</param>
        /// <param name="rotationOffset">Additional rotation to apply (in degrees)</param>
        /// <param name="forwardAxis">Which local axis should point forward along the spline</param>
        /// <param name="upAxis">Which local axis should align with the spline's up direction</param>
        /// <returns>List of positioned GameObjects from the pool</returns>
        public static List<GameObject> DistributePooledObjectsAlongSpline(
            SplineContainer spline,
            Func<GameObject> getPooledObject,
            Action<GameObject> releasePooledObject,
            Transform container,
            int objectCount,
            List<GameObject> existingObjects = null,
            int splineIndex = 0,
            bool alignToSpline = true,
            Vector3 rotationOffset = default,
            Vector3 forwardAxis = default,
            Vector3 upAxis = default)
        {
            if (spline == null || getPooledObject == null || releasePooledObject == null || objectCount <= 0)
                return new List<GameObject>();

            // Default axes if not specified
            if (forwardAxis == default) forwardAxis = Vector3.forward;
            if (upAxis == default) upAxis = Vector3.up;

            // Create rotation offset quaternion
            Quaternion rotOffset = Quaternion.Euler(rotationOffset);

            // Release existing objects back to the pool if provided
            if (existingObjects != null)
            {
                foreach (var obj in existingObjects.Where(o => o != null))
                {
                    releasePooledObject(obj);
                }
                existingObjects.Clear();
            }
            else
            {
                existingObjects = new List<GameObject>(objectCount);
            }

            // Calculate the spline length
            float splineLength = spline.CalculateLength(splineIndex);
            if (splineLength <= 0) return existingObjects;

            // Distribute objects evenly along the spline
            for (int i = 0; i < objectCount; i++)
            {
                // Calculate normalized position along spline (0 to 1)
                float t = (objectCount > 1) ? (float)i / (objectCount - 1) : 0f;

                // Get position and orientation from spline
                float3 position;
                float3 tangent;
                float3 up;

                if (spline.Evaluate(splineIndex, t, out position, out tangent, out up))
                {
                    // Convert from float3 to Vector3
                    Vector3 pos = new Vector3(position.x, position.y, position.z);
                    Vector3 tan = new Vector3(tangent.x, tangent.y, tangent.z);
                    Vector3 upDir = new Vector3(up.x, up.y, up.z);

                    // Calculate rotation if needed
                    Quaternion finalRotation = Quaternion.identity;
                    if (alignToSpline)
                    {
                        // Create a rotation that accurately aligns to the spline
                        Vector3 normalizedTangent = tan.normalized;
                        Vector3 normalizedUp = upDir.normalized;

                        // Make sure up vector is perpendicular to tangent
                        Vector3 right = Vector3.Cross(normalizedTangent, normalizedUp).normalized;
                        Vector3 adjustedUp = Vector3.Cross(right, normalizedTangent).normalized;

                        // Create base rotation from orthogonal vectors
                        Quaternion splineRotation = Quaternion.LookRotation(normalizedTangent, adjustedUp);

                        // Create and apply custom rotation to align specified axes
                        Quaternion axisRotation = Quaternion.identity;

                        // Only apply axis alignment if custom axes are provided
                        if (forwardAxis != Vector3.forward || upAxis != Vector3.up)
                        {
                            // Normalize custom axes
                            Vector3 normForwardAxis = forwardAxis.normalized;
                            Vector3 normUpAxis = upAxis.normalized;

                            // Calculate the rotation needed to align the object's axes with the spline
                            Quaternion fromToRotation = Quaternion.Inverse(
                                Quaternion.LookRotation(normForwardAxis, normUpAxis)
                            );

                            axisRotation = fromToRotation;
                        }

                        // Apply all rotations: spline direction, axis alignment, and custom offset
                        finalRotation = splineRotation * axisRotation * rotOffset;
                    }
                    else if (rotOffset != Quaternion.identity)
                    {
                        // Apply just the rotation offset if not aligning to spline
                        finalRotation = rotOffset;
                    }

                    // Get object from pool and position it
                    GameObject pooledObject = getPooledObject();
                    if (pooledObject != null)
                    {
                        // First set position and rotation while object is inactive
                        // to avoid any unnecessary physics or visual calculations
                        pooledObject.SetActive(false);

                        // Set parent and transform properties
                        pooledObject.transform.SetParent(container);
                        pooledObject.transform.position = pos;
                        pooledObject.transform.rotation = finalRotation;

                        // Now activate the object
                        pooledObject.SetActive(true);

                        existingObjects.Add(pooledObject);
                    }
                }
            }

            return existingObjects;
        }

        /// <summary>
        /// Example method showing how to use DistributePooledObjectsAlongSpline with ObjectPoolPattern
        /// </summary>
        public static List<GameObject> DistributePooledPrefabsAlongSpline(
            SplineContainer spline,
            GameObject prefab,
            Transform container,
            int objectCount,
            List<GameObject> existingObjects = null,
            int splineIndex = 0,
            bool alignToSpline = true,
            Vector3 rotationOffset = default)
        {
            // This is an example of how you might use the method with your own object pool
            // In a real implementation, you would create and manage the object pool externally

            // Example simple object pool for demonstration purposes
            Stack<GameObject> poolStack = new Stack<GameObject>();

            // Get object from pool function
            Func<GameObject> getPooledObject = () =>
            {
                if (poolStack.Count > 0)
                {
                    var pooledOBject = poolStack.Pop();
                    // Don't activate here - the main method will handle activation after positioning
                    return pooledOBject;
                }
                else
                {
                    // Create new instance if pool is empty - keep it inactive
                    var newObject = UnityEngine.Object.Instantiate(prefab);
                    newObject.SetActive(false);
                    return newObject;
                }
            };

            // Release object to pool function
            Action<GameObject> releasePooledObject = (obj) =>
            {
                if (obj != null)
                {
                    // Ensure the object is inactive when returned to pool
                    obj.SetActive(false);

                    // Detach from parent to avoid transform issues
                    obj.transform.SetParent(null);

                    poolStack.Push(obj);
                }
            };

            // Use the main method with our pool functions
            return DistributePooledObjectsAlongSpline(
                spline, getPooledObject, releasePooledObject, container,
                objectCount, existingObjects, splineIndex,
                alignToSpline, rotationOffset);
        }

        public static List<GameObject> DistributePooledPrefabsAlongSpline(
            SplineContainer spline,
            GameObject prefab,
            Transform container,
            int objectCount,
            int splineIndex = 0,
            bool alignToSpline = true,
            Vector3 rotationOffset = default)
        {
            var existingObjects = new List<GameObject>();
            for (int i = 0; i < container.childCount; i++)
            {
                existingObjects.Add(container.GetChild(i).gameObject);
            }

            return DistributePooledPrefabsAlongSpline(
                spline, prefab, container, objectCount, existingObjects,
                splineIndex, alignToSpline, rotationOffset);
        }

        #endregion
    }
}