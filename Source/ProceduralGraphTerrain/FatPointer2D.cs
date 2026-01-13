namespace ProceduralGraph.Terrain;

public sealed class FatPointer2D<T>(int width, int height) : FatPointer<T>(width * height) where T : unmanaged
{
    public int Width { get; } = width;
    public int Height { get; } = height;
}
