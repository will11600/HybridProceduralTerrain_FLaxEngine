using FlaxEngine;
using System;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Terrain.Topography;

internal unsafe sealed class GaussianBlur(float* pSource, int length, int radius, float sigma) : IDisposable
{
    public float* SourcePtr { get; } = pSource;

    public required int Width { get; init; }

    public required int Height { get; init; }

    public int Radius { get; } = radius;

    private readonly float* _tempPtr = (float*)NativeMemory.Alloc((nuint)(length * sizeof(float)));

    private readonly float* _kernelPtr = CreateGaussianKernel(radius, sigma);

    private bool _disposed;
        
    ~GaussianBlur()
    {
        Dispose(disposing: false);
    }

    public void ProcessRow(int y)
    {
        int rowOffset = y * Width;

        for (int x = 0; x < Width; x++)
        {
            float sum = 0f;
            float weightSum = 0f;

            for (int k = -Radius; k <= Radius; k++)
            {
                int sampleX = Math.Clamp(x + k, 0, Width - 1);

                float sample = SourcePtr[rowOffset + sampleX];
                float weight = _kernelPtr[k + Radius];

                sum += sample * weight;
                weightSum += weight;
            }

            _tempPtr[rowOffset + x] = sum / weightSum;
        }
    }

    public void ProcessColumn(int x)
    {
        for (int y = 0; y < Height; y++)
        {
            float sum = 0f;
            float weightSum = 0f;

            for (int k = -Radius; k <= Radius; k++)
            {
                int sampleY = Math.Clamp(y + k, 0, Height - 1);

                int sampleIndex = (sampleY * Width) + x;

                float sample = _tempPtr[sampleIndex];
                float weight = _kernelPtr[k + Radius];

                sum += sample * weight;
                weightSum += weight;
            }

            SourcePtr[(y * Width) + x] = sum / weightSum;
        }
    }

    /// <summary>
    /// Generates a normalized 1D Gaussian kernel.
    /// </summary>
    private static float* CreateGaussianKernel(int radius, float sigma)
    {
        int size = 2 * radius + 1;
        float* kernel = (float*)NativeMemory.Alloc((nuint)(size * sizeof(float)));
        try
        {
            float sum = 0f;
            float calculatedSigma = sigma > 0 ? sigma : 1.0f;
            float twoSigmaSquare = 2.0f * calculatedSigma * calculatedSigma;
            float rootTwoPiSigma = Mathf.Sqrt(Mathf.TwoPi) * calculatedSigma;

            for (int i = -radius; i <= radius; i++)
            {
                float distance = i * i;
                int index = i + radius;
                kernel[index] = Mathf.Exp(-distance / twoSigmaSquare) / rootTwoPiSigma;
                sum += kernel[index];
            }

            for (int i = 0; i < size; i++)
            {
                kernel[i] /= sum;
            }

            return kernel;
        }
        catch
        {
            NativeMemory.Free(kernel);
            throw;
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Dispose of managed resources if any.
        }

        NativeMemory.Free(_tempPtr);
        NativeMemory.Free(_kernelPtr);

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
