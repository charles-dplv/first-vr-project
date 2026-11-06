using System.Collections.Generic;
using UnityEngine;

// Poisson disk sampling with Bridson's algorithm ("Fast Poisson Disk Sampling in Arbitrary Dimensions", 2007).
// Gives random points in a rectangle that are never closer than minDistance to each other:
// the furniture is spread over the whole room without overlapping, and without looking like a grid
public static class PoissonDisk
{
    // Candidates tried around a point before giving up on it (k in the paper)
    private const int attempts = 30;

    public static List<Vector2> Sample(Vector2 size, float minDistance)
    {
        float cellSize = minDistance / Mathf.Sqrt(2f);
        
        // Index + 1 of the point in each cell, 0 if the cell is empty
        int[,] grid = new int[Mathf.CeilToInt(size.x / cellSize), Mathf.CeilToInt(size.y / cellSize)];
        List<Vector2> points = new List<Vector2>();
        // Points that may still have room for a neighbour
        List<Vector2> active = new List<Vector2>();

        Vector2Int GetCell(Vector2 point)
        {
            int x = Mathf.Min((int)(point.x / cellSize), grid.GetLength(0) - 1);
            int y = Mathf.Min((int)(point.y / cellSize), grid.GetLength(1) - 1);
            return new Vector2Int(x, y);
        }

        void AddPoint(Vector2 point)
        {
            points.Add(point);
            active.Add(point);
            Vector2Int cell = GetCell(point);
            grid[cell.x, cell.y] = points.Count;
        }

        bool IsFarEnough(Vector2 candidate)
        {
            // A point closer than r can only be in the 5 x 5 cells around the candidate
            Vector2Int cell = GetCell(candidate);
            for (int x = Mathf.Max(0, cell.x - 2); x <= Mathf.Min(grid.GetLength(0) - 1, cell.x + 2); x++)
            {
                for (int y = Mathf.Max(0, cell.y - 2); y <= Mathf.Min(grid.GetLength(1) - 1, cell.y + 2); y++)
                {
                    int other = grid[x, y] - 1;
                    if (other >= 0 && Vector2.Distance(candidate, points[other]) < minDistance)
                        return false;
                }
            }
            return true;
        }

        AddPoint(new Vector2(Random.Range(0f, size.x), Random.Range(0f, size.y)));
        while (active.Count > 0)
        {
            int index = Random.Range(0, active.Count);
            bool found = false;

            for (int i = 0; i < attempts && !found; i++)
            {
                // Random point in the ring between r and 2r around the active point
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float distance = Random.Range(minDistance, 2f * minDistance);
                Vector2 candidate = active[index] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                bool inside = candidate.x >= 0f && candidate.y >= 0f && candidate.x <= size.x && candidate.y <= size.y;
                if (inside && IsFarEnough(candidate))
                {
                    AddPoint(candidate);
                    found = true;
                }
            }

            // No room left around this point
            if (!found)
                active.RemoveAt(index);
        }
        return points;
    }
}
