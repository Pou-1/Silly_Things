using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Silly_Things.codes.BountyContract
{
    public static class BountyPathSystem
    {
        public static void SpawnPath(Vector3 start, Vector3 end, GameObject footprintPrefab, float spacing, float lifetime)
        {
            if (footprintPrefab == null)
                return;

            NavMeshPath path = new NavMeshPath();

            if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
                return;

            Vector3[] corners = path.corners;

            bool left = true;
            float footOffset = 0.15f;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 from = corners[i];
                Vector3 to = corners[i + 1];

                Vector3 dir = (to - from).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;

                float distance = Vector3.Distance(from, to);
                int steps = Mathf.FloorToInt(distance / spacing);

                for (int j = 0; j < steps; j++)
                {
                    float t = j / (float)steps;
                    Vector3 pos = Vector3.Lerp(from, to, t);

                    Vector3 offset = (left ? -side : side) * footOffset;

                    GameObject footprint = Object.Instantiate(
                        footprintPrefab,
                        pos + offset,
                        Quaternion.LookRotation(dir)
                    );

                    Object.Destroy(footprint, lifetime);

                    left = !left;
                }
            }
        }

        public static Queue<Vector3> GeneratePathPoints(Vector3 start, Vector3 end, float spacing)
        {
            Queue<Vector3> points = new Queue<Vector3>();

            NavMeshPath path = new NavMeshPath();

            if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
                return points;

            var corners = path.corners;

            bool left = true;
            float baseOffset = 0.18f;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 from = corners[i];
                Vector3 to = corners[i + 1];

                Vector3 dir = (to - from).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;

                float dist = Vector3.Distance(from, to);

                float step = spacing;

                float traveled = 0f;

                while (traveled < dist)
                {
                    float t = traveled / dist;

                    Vector3 pos = Vector3.Lerp(from, to, t);

                    float offsetNoise = Random.Range(-0.04f, 0.04f);
                    float sideOffset = baseOffset + offsetNoise;

                    Vector3 offset = (left ? -side : side) * sideOffset;

                    float forwardNoise = Random.Range(-0.05f, 0.05f);
                    pos += dir * forwardNoise;

                    points.Enqueue(pos + offset);

                    left = !left;

                    step = spacing + Random.Range(-0.12f, 0.12f);
                    traveled += step;
                }
            }

            return points;
        }
    }
}