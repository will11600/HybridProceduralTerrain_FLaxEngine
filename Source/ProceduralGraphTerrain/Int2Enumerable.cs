using System.Collections;
using System.Collections.Generic;
using FlaxEngine;

namespace AdvancedTerrainToolsEditor;

internal readonly struct Int2Enumerable : IEnumerable<Int2>
{
    public Int2 Start { get; }

    public Int2 End { get; }

    public Int2Enumerable(int endX, int endY, int startX = default, int startY = default)
    {
        Start = new(startX, startY);
        End = new(endX, endY);
    }

    public Int2Enumerable(Int2 end, Int2 start = default)
    {
        Start = start;
        End = end;
    }

    public IEnumerator<Int2> GetEnumerator()
    {
        return new Int2Enumerator(End, Start);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
