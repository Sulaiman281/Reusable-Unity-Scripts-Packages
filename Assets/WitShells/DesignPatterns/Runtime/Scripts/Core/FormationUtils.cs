using System.Collections.Generic;
using UnityEngine;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// Static utility class that generates world-space <see cref="Pose"/> lists for common
    /// tactical/game unit formations. Each method returns a list of positions and rotations
    /// that can be assigned to agents, NPCs, or any Unity objects.
    /// </summary>
    /// <remarks>
    /// All methods are allocation-friendly – they return a new <c>List&lt;Pose&gt;</c> that the
    /// caller owns. None of the methods move objects; callers apply the poses themselves.
    /// </remarks>
    public static class FormationUtils
    {
        /// <summary>
        /// Generates a circular formation centred on <paramref name="center"/>.
        /// Each entity faces the centre of the circle.
        /// </summary>
        /// <param name="center">The world-space centre of the circle.</param>
        /// <param name="radius">Radius of the circle in world units.</param>
        /// <param name="numberOfEntities">How many evenly-spaced poses to produce.</param>
        /// <returns>A list of <see cref="Pose"/> values arranged on the perimeter of the circle.</returns>
        public static List<Pose> GenerateCircleFormation(Vector3 center, float radius, int numberOfEntities)
        {
            List<Pose> positions = new List<Pose>();
            float angleStep = 360f / numberOfEntities;

            for (int i = 0; i < numberOfEntities; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 position = new Vector3(
                    center.x + radius * Mathf.Cos(angle),
                    center.y,
                    center.z + radius * Mathf.Sin(angle)
                );
                Quaternion rotation = Quaternion.LookRotation(center - position);
                positions.Add(new Pose(position, rotation));
            }

            return positions;
        }

        /// <summary>
        /// Generates a straight-line formation relative to a <see cref="Transform"/>.
        /// The line runs along the right axis (horizontal) or forward axis (vertical) of the transform.
        /// </summary>
        /// <param name="transform">Reference transform that defines position and orientation.</param>
        /// <param name="numberOfEntities">Total number of poses to place in the line.</param>
        /// <param name="spacing">Gap between consecutive entities in world units.</param>
        /// <param name="isCentered">If <c>true</c>, the line is centred on the transform's position.</param>
        /// <param name="isVertical">If <c>true</c>, the line runs along the forward axis; otherwise the right axis.</param>
        public static List<Pose> GenerateLineFormation(Transform transform, int numberOfEntities, int spacing, bool isCentered = false, bool isVertical = false)
        {
            int width = (numberOfEntities - 1) * spacing;

            Vector3 startPosition = transform.position;
            Vector3 finalPosition;
            if (isVertical)
            {
                startPosition += -transform.forward * (width * (isCentered ? .5f : 1));
                finalPosition = startPosition + transform.forward * (numberOfEntities * spacing);
            }
            else
            {
                startPosition += -transform.right * (width * (isCentered ? .5f : 1));
                finalPosition = startPosition + transform.right * (numberOfEntities * spacing);
            }


            return GenerateLineFormation(startPosition, finalPosition, numberOfEntities);
        }

        /// <summary>
        /// Generates a straight-line formation between two explicit world positions.
        /// All entities face the direction from <paramref name="start"/> to <paramref name="end"/>.
        /// </summary>
        /// <param name="start">World-space start point of the line.</param>
        /// <param name="end">World-space end point of the line.</param>
        /// <param name="numberOfEntities">Number of evenly-spaced poses along the line.</param>
        public static List<Pose> GenerateLineFormation(Vector3 start, Vector3 end, int numberOfEntities)
        {
            List<Pose> positions = new List<Pose>();
            Vector3 direction = (end - start).normalized;
            float totalLength = Vector3.Distance(start, end);
            float step = totalLength / (numberOfEntities - 1);

            for (int i = 0; i < numberOfEntities; i++)
            {
                Vector3 position = start + direction * step * i;
                Quaternion rotation = Quaternion.LookRotation(direction);
                positions.Add(new Pose(position, rotation));
            }

            return positions;
        }

        /// <summary>
        /// Generates a V-shaped formation (two diagonal lines diverging from the front).
        /// The first entity is placed at the tip; subsequent entities fan out on each side.
        /// </summary>
        /// <param name="transform">Reference transform providing position and orientation.</param>
        /// <param name="numberOfEntities">Total number of entities in the V.</param>
        /// <param name="spacingX">Horizontal spacing between consecutive entities on each arm.</param>
        /// <param name="spacingZ">Depth offset per step along the forward axis.</param>
        public static List<Pose> GenerateVFormation(Transform transform, int numberOfEntities, float spacingX, float spacingZ)
        {
            List<Pose> positions = new List<Pose>();

            Vector3 startPosition = transform.position;
            int midPoint = numberOfEntities / 2;
            for (int i = 0; i < numberOfEntities; i++)
            {
                Vector3 finalPosition;
                if (i <= midPoint)
                {
                    finalPosition = startPosition + -transform.right * (i * spacingX) + -transform.forward * (i * spacingZ);
                }
                else
                {
                    int rightIndex = i - midPoint;
                    finalPosition = startPosition + transform.right * (rightIndex * spacingX) + -transform.forward * (rightIndex * spacingZ);
                }

                positions.Add(new Pose(finalPosition, transform.rotation));
            }


            return positions;

        }

        /// <summary>
        /// Generates a wedge (arrowhead) formation with a single leader at the front
        /// and two diverging lines trailing behind.
        /// </summary>
        /// <param name="transform">Reference transform providing position and orientation.</param>
        /// <param name="numberOfEntities">Total number of entities including the leader.</param>
        /// <param name="spacingX">Horizontal gap per step along each trailing arm.</param>
        /// <param name="spacingZ">Depth gap per step behind the leader.</param>
        public static List<Pose> GenerateWedgeFormation(Transform transform, int numberOfEntities, float spacingX, float spacingZ)
        {
            List<Pose> positions = new List<Pose>();
            Vector3 startPosition = transform.position;
            int midPoint = numberOfEntities / 2;

            // Leader position
            positions.Add(new Pose(startPosition, transform.rotation));

            // Generate two diagonal lines behind the leader
            for (int i = 1; i < numberOfEntities; i++)
            {
                Vector3 finalPosition;
                if (i <= midPoint)
                {
                    finalPosition = startPosition + transform.right * (i * spacingX) + -transform.forward * (i * spacingZ);
                }
                else
                {
                    int leftIndex = i - midPoint;
                    finalPosition = startPosition + -transform.right * (leftIndex * spacingX) + -transform.forward * (leftIndex * spacingZ);
                }
                positions.Add(new Pose(finalPosition, transform.rotation));
            }

            return positions;
        }

        /// <summary>
        /// Generates a square/rectangular box formation.
        /// Entities are arranged in a grid as close to square as possible.
        /// </summary>
        /// <param name="transform">Reference transform providing position and orientation.</param>
        /// <param name="numberOfEntities">Total number of entities in the box.</param>
        /// <param name="spacing">Gap between entities both horizontally and vertically.</param>
        public static List<Pose> GenerateBoxFormation(Transform transform, int numberOfEntities, float spacing)
        {
            List<Pose> positions = new List<Pose>();
            Vector3 startPosition = transform.position;

            // Calculate the size of the box (try to make it as square as possible)
            int sideLength = Mathf.CeilToInt(Mathf.Sqrt(numberOfEntities));

            for (int i = 0; i < numberOfEntities; i++)
            {
                int row = i / sideLength;
                int col = i % sideLength;

                Vector3 finalPosition = startPosition +
                    transform.right * (col * spacing) +
                    -transform.forward * (row * spacing);

                positions.Add(new Pose(finalPosition, transform.rotation));
            }

            return positions;
        }

        /// <summary>
        /// Generates a triangle formation where each successive row has one more entity
        /// than the previous, starting from a single entity at the front.
        /// </summary>
        /// <param name="transform">Reference transform providing position and orientation.</param>
        /// <param name="numberOfEntities">Total number of entities to place.</param>
        /// <param name="spacing">Gap between entities within a row and between rows.</param>
        public static List<Pose> GenerateTriangleFormation(Transform transform, int numberOfEntities, float spacing)
        {
            List<Pose> positions = new List<Pose>();
            Vector3 startPosition = transform.position;

            int currentRow = 0;
            int entitiesPlaced = 0;

            while (entitiesPlaced < numberOfEntities)
            {
                int entitiesInThisRow = currentRow + 1;
                float rowOffset = currentRow * spacing;
                float rowWidth = (entitiesInThisRow - 1) * spacing;

                for (int i = 0; i < entitiesInThisRow && entitiesPlaced < numberOfEntities; i++)
                {
                    Vector3 finalPosition = startPosition +
                        -transform.forward * rowOffset +
                        transform.right * (i * spacing - rowWidth / 2);

                    positions.Add(new Pose(finalPosition, transform.rotation));
                    entitiesPlaced++;
                }

                currentRow++;
            }

            return positions;
        }

        /// <summary>
        /// Generates an echelon (staircase) formation where each entity is offset diagonally
        /// to the right or left and behind the previous one.
        /// </summary>
        /// <param name="transform">Reference transform providing position and orientation.</param>
        /// <param name="numberOfEntities">Total number of entities in the echelon.</param>
        /// <param name="spacingX">Horizontal offset per step.</param>
        /// <param name="spacingZ">Depth offset per step.</param>
        /// <param name="rightEchelon">If <c>true</c>, the echelon trails to the right; otherwise to the left.</param>
        public static List<Pose> GenerateEchelonFormation(Transform transform, int numberOfEntities, float spacingX, float spacingZ, bool rightEchelon = true)
        {
            List<Pose> positions = new List<Pose>();
            Vector3 startPosition = transform.position;

            for (int i = 0; i < numberOfEntities; i++)
            {
                Vector3 finalPosition = startPosition +
                    (rightEchelon ? transform.right : -transform.right) * (i * spacingX) +
                    -transform.forward * (i * spacingZ);

                positions.Add(new Pose(finalPosition, transform.rotation));
            }

            return positions;
        }

        /// <summary>
        /// Generates a column formation where entities are arranged in multiple rows,
        /// each row holding <paramref name="elementsPerRow"/> entities side by side.
        /// </summary>
        /// <param name="transform">Reference transform providing position and orientation.</param>
        /// <param name="numberOfEntities">Total number of entities in the column.</param>
        /// <param name="spacing">Gap between entities in all directions.</param>
        /// <param name="elementsPerRow">Number of entities per row (default 2).</param>
        public static List<Pose> GenerateColumnFormation(Transform transform, int numberOfEntities, float spacing, int elementsPerRow = 2)
        {
            List<Pose> positions = new List<Pose>();
            Vector3 startPosition = transform.position;

            for (int i = 0; i < numberOfEntities; i++)
            {
                int row = i / elementsPerRow;
                int col = i % elementsPerRow;

                Vector3 finalPosition = startPosition +
                    transform.right * (col * spacing - (spacing * (elementsPerRow - 1) / 2f)) +
                    -transform.forward * (row * spacing);

                positions.Add(new Pose(finalPosition, transform.rotation));
            }

            return positions;
        }

        /// <summary>
        /// Generates a diamond formation with a single leader at the tip and expanding
        /// diagonal lines of entities behind. Additional entities beyond the first four
        /// continue the diamond pattern at each successive depth level.
        /// </summary>
        /// <param name="transform">Reference transform providing position and orientation.</param>
        /// <param name="numberOfEntities">Total number of entities including the leader.</param>
        /// <param name="spacing">Gap between adjacent entities in the diamond.</param>
        public static List<Pose> GenerateDiamondFormation(Transform transform, int numberOfEntities, float spacing)
        {
            List<Pose> positions = new List<Pose>();
            Vector3 startPosition = transform.position;

            // Leader position (top of diamond)
            positions.Add(new Pose(startPosition, transform.rotation));

            if (numberOfEntities <= 1) return positions;

            // Calculate remaining positions
            float halfSpacing = spacing / 2f;

            // Left and right positions
            if (numberOfEntities > 2)
            {
                positions.Add(new Pose(startPosition + -transform.right * spacing + -transform.forward * spacing, transform.rotation));
                positions.Add(new Pose(startPosition + transform.right * spacing + -transform.forward * spacing, transform.rotation));
            }

            // Rear position
            if (numberOfEntities > 3)
            {
                positions.Add(new Pose(startPosition + -transform.forward * (spacing * 2), transform.rotation));
            }

            // Add additional units in expanding diamond pattern if needed
            if (numberOfEntities > 4)
            {
                float currentSpacing = spacing * 2;
                int currentIndex = 4;

                while (currentIndex < numberOfEntities)
                {
                    // Add left side
                    if (currentIndex < numberOfEntities)
                    {
                        positions.Add(new Pose(
                            startPosition + -transform.right * currentSpacing + -transform.forward * currentSpacing,
                            transform.rotation
                        ));
                        currentIndex++;
                    }

                    // Add right side
                    if (currentIndex < numberOfEntities)
                    {
                        positions.Add(new Pose(
                            startPosition + transform.right * currentSpacing + -transform.forward * currentSpacing,
                            transform.rotation
                        ));
                        currentIndex++;
                    }

                    currentSpacing += spacing;
                }
            }

            return positions;
        }
    }
}