using System.Collections.Generic;
using UnityEngine;

namespace WitShells.DesignPatterns.Core
{
    public static class FormationUtils
    {
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