using System.Collections.Generic;
using UnityEngine;

public class MoveFinder
{
    private readonly BoardState _board;
    private readonly int _width;
    private readonly int _height;

    public MoveFinder(BoardState board, int width, int height)
    {
        _board = board;
        _width = width;
        _height = height;
    }

    public bool HasPlayableMove()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var p = new Vector2Int(x, y);
                if (!_board.TryGetNormalBlock(p, out var b) || b == null)
                {
                    continue;
                }

                var right = new Vector2Int(x + 1, y);
                if (_board.TryGetNormalBlock(right, out var r) && r != null && r.color == b.color)
                {
                    return true;
                }

                var up = new Vector2Int(x, y + 1);
                if (_board.TryGetNormalBlock(up, out var u) && u != null && u.color == b.color)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryForceCreateMove()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var p = new Vector2Int(x, y);
                if (!_board.TryGetNormalBlock(p, out var a) || a == null)
                {
                    continue;
                }

                var right = new Vector2Int(x + 1, y);
                if (_board.TryGetNormalBlock(right, out var b) && b != null)
                {
                    b.color = a.color;
                    return true;
                }

                var up = new Vector2Int(x, y + 1);
                if (_board.TryGetNormalBlock(up, out var c) && c != null)
                {
                    c.color = a.color;
                    return true;
                }
            }
        }

        return false;
    }

    public static void ShuffleColorsInPlace(Dictionary<Vector2Int, GameObject> dict)
    {
        if (dict == null)
        {
            return;
        }

        List<Block> blocks = new List<Block>();
        List<int> colors = new List<int>();
        foreach (var kvp in dict)
        {
            var obj = kvp.Value;
            if (obj == null)
            {
                continue;
            }

            var b = obj.GetComponent<Block>();
            if (b == null)
            {
                continue;
            }

            blocks.Add(b);
            colors.Add(b.color);
        }

        var rng = new System.Random();
        for (int i = colors.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (colors[i], colors[j]) = (colors[j], colors[i]);
        }

        for (int i = 0; i < blocks.Count; i++)
        {
            blocks[i].color = colors[i];
        }
    }
}
