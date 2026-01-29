using System.Collections.Generic;
using UnityEngine;

public class BoardState
{
    private readonly Dictionary<Vector2Int, GameObject> _cells;

    public BoardState(Dictionary<Vector2Int, GameObject> cells)
    {
        _cells = cells;
    }

    public bool Contains(Vector2Int pos)
    {
        return _cells != null && _cells.ContainsKey(pos);
    }

    public bool TryGetObject(Vector2Int pos, out GameObject obj)
    {
        obj = null;
        if (_cells == null)
        {
            return false;
        }

        return _cells.TryGetValue(pos, out obj);
    }

    public bool TryGetNormalBlock(Vector2Int pos, out Block block)
    {
        block = null;
        if (!TryGetObject(pos, out var obj) || obj == null)
        {
            return false;
        }

        block = obj.GetComponent<Block>();
        return block != null;
    }

    public bool TryGetBox(Vector2Int pos, out BoxBlock box)
    {
        box = null;
        if (!TryGetObject(pos, out var obj) || obj == null)
        {
            return false;
        }

        box = obj.GetComponent<BoxBlock>();
        return box != null;
    }

    public IEnumerable<Vector2Int> Neighbors4(Vector2Int pos)
    {
        yield return new Vector2Int(pos.x, pos.y + 1);
        yield return new Vector2Int(pos.x, pos.y - 1);
        yield return new Vector2Int(pos.x - 1, pos.y);
        yield return new Vector2Int(pos.x + 1, pos.y);
    }
}
