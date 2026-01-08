using System;
using System.Collections;
using System.Collections.Generic;
using FlaxEngine;

namespace ProceduralGraph.Terrain;

/// <summary>
/// Int2Enumerator struct.
/// </summary>
internal struct Int2Enumerator : IEnumerator<Int2>
{
    public Int2 Start { get; }

    public Int2 End { get; }

    private int _x;
    private int _y;
    public readonly Int2 Current => new(_x, _y);
    readonly object IEnumerator.Current => Current;

    public Int2Enumerator(int endX, int endY, int startX = default, int startY = default)
    {
        Start = new(startX, startY);
        End = new(endX, endY);

        _x = startX - 1;
        _y = startY;
    }

    public Int2Enumerator(Int2 end, Int2 start = default)
    {
        Start = start;
        End = end;

        _x = start.X - 1;
        _y = start.Y;
    }
 
    public bool MoveNext()
    {
        if (++_x >= End.X)
        {
            _x = Start.X;
            _y++;
        }
        
        return _y < End.Y && _x < End.X;
    }

    public void Reset()
    {
        _x = Start.X - 1;
        _y = Start.Y;
    }

    readonly void IDisposable.Dispose() { }
}
