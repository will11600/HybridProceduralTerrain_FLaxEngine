#nullable enable
using FlaxEngine;
using ProceduralGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AdvancedTerrainToolsEditor;

internal readonly struct TerrainGenerator : IGenerator<TerrainGenerator>
{
    private static readonly BoundedChannelOptions _channelOptions = new(500)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    };

    private readonly Channel<Patch> _channel;

    public Terrain Target { get; }
    public Int2 Size { get; }
    public TerrainPatches Patches { get; }
    public IEnumerable<GraphModel> Models { get; }

    public TerrainGenerator(Terrain terrain, IEnumerable<GraphModel> models)
    {
        Target = terrain ?? throw new ArgumentNullException(nameof(terrain));
        Models = models ?? throw new ArgumentNullException(nameof(models));

        _channel = Channel.CreateBounded<Patch>(_channelOptions);

        Int2 patchCount = CountPatches(terrain);
        int patchStride = terrain.ChunkSize * Terrain.PatchEdgeChunksCount;
        Size = (patchCount * patchStride) + Int2.One;
        Patches = new TerrainPatches(patchStride + 1, patchCount);
    }

    public async Task BuildAsync(CancellationToken cancellationToken)
    {
        Int2Enumerable patches = new(Patches.Count);
        Task write = WritePatchesAsync(patches, cancellationToken);

        await foreach (Patch patch in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                if (patch.SetupHeightmap(Target))
                {
                    continue;
                }

                Debug.LogErrorFormat(Target, "Failed to setup patch heightmap ({0}).", patch.Index);
            }
            finally
            {
                patch.Dispose();
            }
        }

        await write;
    }

    public static TerrainGenerator Create(Actor actor, IEnumerable<GraphModel> models)
    {
        return new((Terrain)actor, models);
    }

    private async Task WritePatchesAsync(Int2Enumerable patches, CancellationToken cancellationToken)
    {
        Exception? exception = null;
        try
        {
            await Parallel.ForEachAsync(patches, cancellationToken, GeneratePatchAsync);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            _channel.Writer.Complete(exception);
        }
    }

    private async ValueTask GeneratePatchAsync(Int2 index, CancellationToken cancellationToken)
    {
        Patch patch = new(index, Patches.Size);
        Span<float> heightmap = patch.Heightmap.Span;

        Int2 offset = index * (Patches.Size - 1);

        try
        {
            for (int i = 0; i < patch.Heightmap.Length; i++)
            {
                int x = i % Patches.Size;
                int z = i / Patches.Size;

                if (x == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                float u = (offset.X + x) / (float)Size.X;
                float v = (offset.Y + z) / (float)Size.Y;

                ref float height = ref heightmap[i];
                height = default;
                foreach (ITopographyProvider layer in Models.OfType<ITopographyProvider>())
                {
                    layer.GetHeight(u, v, ref height);
                }
            }

            foreach (ITopographyPostProcessor effect in Models.OfType<ITopographyPostProcessor>())
            {
                effect.Apply(patch.Heightmap, Patches.Size);
            }

            await _channel.Writer.WriteAsync(patch, cancellationToken);
        }
        catch
        {
            patch.Dispose();
            throw;
        }
    }

    private static Int2 CountPatches(Terrain terrain)
    {
        if (terrain.PatchesCount == 0)
        {
            return Int2.Zero;
        }

        Int2 min = Int2.Maximum;
        Int2 max = Int2.Minimum;

        for (int i = 0; i < terrain.PatchesCount; i++)
        {
            terrain.GetPatchCoord(i, out Int2 coord);

            min.X = coord.X < min.X ? coord.X : min.X;
            min.Y = coord.Y < min.Y ? coord.Y : min.Y;

            max.X = coord.X > max.X ? coord.X : max.X;
            max.Y = coord.Y > max.Y ? coord.Y : max.Y;
        }

        max += Int2.One;

        return max - min;
    }
}
