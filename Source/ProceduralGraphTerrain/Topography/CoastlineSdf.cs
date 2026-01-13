using FlaxEngine;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Terrain.Topography;

internal sealed class CoastlineSdf(FatPointer2D<float> heightMap, float seaLevel) : IDisposable
{
    private const float Infinity = 1e9f;
    private const float Threshold = 1e8f;

    private bool _disposed;

    private readonly float _seaLevel = seaLevel;
    public float SeaLevel => _seaLevel;

    private readonly FatPointer2D<float> _heightMap = heightMap ?? throw new ArgumentNullException(nameof(heightMap));

    private readonly FatPointer<float> _sdfMap = new(heightMap.Length);
    public unsafe float* Buffer => _sdfMap.Buffer;

    public unsafe void ProcessRow(int rowIndex)
    {
        int rowOffset = rowIndex * _width;
        ReadOnlySpan<float> heightMapRow = new(_heightMap.Buffer + rowOffset, _width);
        Span<float> sdfMapRow = new(_sdfMap.Buffer + rowOffset, _width);
        Sweep(heightMapRow, sdfMapRow, _seaLevel);
        ForwardPass(sdfMapRow);
        BackwardPass(sdfMapRow);
        SquareValues(sdfMapRow);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Sweep(ReadOnlySpan<float> heights, Span<float> values, float seaLevel)
    {
        ref readonly float previousSample = ref heights[0];
        values[0] = InitialDistance(previousSample, heights[1], seaLevel);
        for (int x = 1; x < heights.Length; x++)
        {
            ref readonly float currentSample = ref heights[x];
            values[x] = InitialDistance(currentSample, previousSample, seaLevel);
            previousSample = ref currentSample;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float InitialDistance(float a, float b, float seaLevel)
    {
        return (a < seaLevel) != (b < seaLevel) ? 0.0f : Infinity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ForwardPass(Span<float> values)
    {
        ref readonly float previousValue = ref values[0];
        for (int x = 1; x < values.Length; x++)
        {
            ref float currentValue = ref values[x];
            float distanceFromLeft = previousValue + 1.0f;
            if (distanceFromLeft < currentValue)
            {
                currentValue = distanceFromLeft;
            }
            previousValue = ref currentValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BackwardPass(Span<float> values)
    {
        int lastIndex = values.Length - 1;
        ref readonly float nextValue = ref values[lastIndex];
        for (int x = values.Length - 1; x >= 0; x--)
        {
            ref float currentValue = ref values[x];
            float distanceFromRight = nextValue + 1.0f;
            if (distanceFromRight < currentValue)
            {
                currentValue = distanceFromRight;
            }
            nextValue = ref currentValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SquareValues(Span<float> values)
    {
        for (int x = 0; x < values.Length; x++)
        {
            ref float value = ref values[x];
            if (value < Threshold)
            {
                value *= value;
            }
        }
    }

    public unsafe void ProcessColumn(int columnIndex)
    {
        int* envelopeVertices = (int*)NativeMemory.Alloc((nuint)_width, sizeof(int));
        float* envelopeIntersections = (float*)NativeMemory.Alloc((nuint)(_width + 1), sizeof(float));

        float* sdfBuffer = _sdfMap.Buffer;

        try
        {
            BuildLowerEnvelope(columnIndex, envelopeVertices, envelopeIntersections, sdfBuffer);
            SampleEnvelope(columnIndex, envelopeVertices, envelopeIntersections, sdfBuffer);
        }
        finally
        {
            NativeMemory.Free(envelopeVertices);
            NativeMemory.Free(envelopeIntersections);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void BuildLowerEnvelope(int columnIndex, int* envelopeVertices, float* envelopeIntersections, float* sdfBuffer)
    {
        int stackTop = 0;
        envelopeVertices[0] = 0;
        envelopeIntersections[0] = -Infinity;
        envelopeIntersections[1] = Infinity;

        for (int candidateRow = 1; candidateRow < _height; candidateRow++)
        {
            float intersectionY;
            do
            {
                int vertexRow = envelopeVertices[stackTop];

                float distSqCandidate = sdfBuffer[candidateRow * _width + columnIndex];
                float distSqVertex = sdfBuffer[vertexRow * _width + columnIndex];

                float num = distSqCandidate + candidateRow * candidateRow - (distSqVertex + vertexRow * vertexRow);
                float den = 2 * candidateRow - 2 * vertexRow;

                intersectionY = num / den;

                if (intersectionY > envelopeIntersections[stackTop])
                {
                    break;
                }

                stackTop--;
            }
            while (stackTop >= 0);

            stackTop++;
            envelopeVertices[stackTop] = candidateRow;
            envelopeIntersections[stackTop] = intersectionY;
            envelopeIntersections[stackTop + 1] = Infinity;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SampleEnvelope(int columnIndex, int* envelopeVertices, float* envelopeIntersections, float* sdfBuffer)
    {
        int envelopeScanner = 0;

        for (int rowIndex = 0; rowIndex < _height; rowIndex++)
        {
            while (envelopeIntersections[envelopeScanner + 1] < rowIndex)
            {
                envelopeScanner++;
            }

            int nearestRow = envelopeVertices[envelopeScanner];

            float dy = rowIndex - nearestRow;
            float distSqVertical = dy * dy;
            float distSqHorizontal = sdfBuffer[nearestRow * _width + columnIndex];

            sdfBuffer[rowIndex * _width + columnIndex] = Mathf.Sqrt(distSqVertical + distSqHorizontal);
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
            _sdfMap.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
