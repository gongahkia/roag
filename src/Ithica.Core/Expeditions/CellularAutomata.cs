namespace Ithica.Core.Expeditions;

public static class CellularAutomata
{
    // All cells read the previous generation. Edges count as occupied.
    public static bool[] Smooth(bool[] initial, int width, int height, int passes, int birth = 5, int survive = 4)
    {
        if (width <= 0 || height <= 0 || initial.Length != width * height || passes < 0)
            throw new ArgumentException("Invalid cellular-automata dimensions or pass count.");
        var current = (bool[])initial.Clone();
        var next = new bool[current.Length];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int neighbors = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height || current[ny * width + nx]) neighbors++;
                        }
                    int i = y * width + x;
                    next[i] = neighbors >= (current[i] ? survive : birth);
                }
            (current, next) = (next, current);
        }
        return current;
    }
}
