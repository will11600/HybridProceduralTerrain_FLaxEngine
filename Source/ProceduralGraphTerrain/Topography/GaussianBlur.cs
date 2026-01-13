using FlaxEngine;
using System;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Terrain.Topography;

internal unsafe sealed class GaussianBlur(FatPointer2D<float> heightMap, int radius, float sigma) : IDisposable
{
    public readonly FatPointer2D<float> heightMap = heightMap ?? throw new ArgumentNullException(nameof(heightMap));

    public readonly int radius = radius;

    private readonly float* _tempPtr = (float*)NativeMemory.Alloc((nuint)(heightMap.Length * sizeof(float)));

    private readonly float* _kernelPtr = CreateGaussianKernel(radius, sigma);

    private bool _disposed;
        
    ~GaussianBlur()
    {
        Dispose(disposing: false);
    }

    public unsafe void ProcessRow(int y)
    {
        int width = heightMap.Width;
        float* buffer = heightMap.Buffer;

        int rowOffset = y * width;

        for (int x = 0; x < width; x++)
        {
            float sum = 0f;
            float weightSum = 0f;

            for (int k = -radius; k <= radius; k++)
            {
                int sampleX = Math.Clamp(x + k, 0, width - 1);

                float sample = buffer[rowOffset + sampleX];
                float weight = _kernelPtr[k + radius];

                sum += sample * weight;
                weightSum += weight;
            }

            _tempPtr[rowOffset + x] = sum / weightSum;
        }
    }

    public unsafe void ProcessColumn(int x)
    {
        int width = heightMap.Width;
        int height = heightMap.Height;
        float* buffer = heightMap.Buffer;

        for (int y = 0; y < width; y++)
        {
            float sum = 0f;
            float weightSum = 0f;

            for (int k = -radius; k <= radius; k++)
            {
                int sampleY = Math.Clamp(y + k, 0, height - 1);

                int sampleIndex = (sampleY * width) + x;

                float sample = _tempPtr[sampleIndex];
                float weight = _kernelPtr[k + radius];

                sum += sample * weight;
                weightSum += weight;
            }

            buffer[(y * width) + x] = sum / weightSum;
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
