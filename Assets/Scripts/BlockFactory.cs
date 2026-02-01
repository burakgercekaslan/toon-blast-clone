using UnityEngine;

public class BlockFactory
{
    private readonly GameManager _gameManager;
    private readonly GameObject[] _defaultCubes;
    private readonly GameObject _boxPrefab;
    private readonly Transform _parent;
    private readonly Sprite _box1Sprite;
    private readonly Sprite _box0Sprite;

    private const float CellSize = 0.225f;
    private static readonly Vector2 ColliderSize = new Vector2(2, 2.25f);

    public BlockFactory(
        GameManager gameManager,
        GameObject[] defaultCubes,
        GameObject boxPrefab,
        Transform parent,
        Sprite box1Sprite,
        Sprite box0Sprite)
    {
        _gameManager = gameManager;
        _defaultCubes = defaultCubes;
        _boxPrefab = boxPrefab;
        _parent = parent;
        _box1Sprite = box1Sprite;
        _box0Sprite = box0Sprite;
    }

    public GameObject CreateNormalBlock(int startingX, int startingY, int x, int y, int color, int droppingHeight)
    {
        if (_defaultCubes == null || color < 0 || color >= _defaultCubes.Length)
        {
            return null;
        }

        var prefab = _defaultCubes[color];
        if (prefab == null)
        {
            return null;
        }

        var obj = Object.Instantiate(
            prefab,
            new Vector2((startingX + x * 2) * CellSize, (startingY + y * 2) * CellSize + droppingHeight),
            Quaternion.identity);

        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = y;
        }

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        if (obj.GetComponent<BoxCollider2D>() == null)
        {
            obj.AddComponent<BoxCollider2D>();
        }
        var collider = obj.GetComponent<BoxCollider2D>();
        collider.size = ColliderSize;
        collider.isTrigger = true;

        if (_parent != null)
        {
            obj.transform.SetParent(_parent);
        }

        var block = obj.GetComponent<Block>();
        if (block == null)
        {
            block = obj.AddComponent<Block>();
        }

        block.x = x;
        block.y = y;
        block.color = color;
        block.SetGameManager(_gameManager);

        obj.name = x + "." + y;
        obj.tag = "Block";

        return obj;
    }

    public GameObject CreateBox(int startingX, int startingY, int x, int y, int droppingHeight)
    {
        if (_boxPrefab == null)
        {
            return null;
        }

        var obj = Object.Instantiate(
            _boxPrefab,
            new Vector2((startingX + x * 2) * CellSize, (startingY + y * 2) * CellSize + droppingHeight),
            Quaternion.identity);

        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = y;
        }

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (obj.GetComponent<BoxCollider2D>() == null)
        {
            obj.AddComponent<BoxCollider2D>();
        }
        var boxCollider = obj.GetComponent<BoxCollider2D>();
        boxCollider.size = ColliderSize;
        boxCollider.isTrigger = true;

        if (_parent != null)
        {
            obj.transform.SetParent(_parent);
        }

        var box = obj.GetComponent<BoxBlock>();
        if (box == null)
        {
            box = obj.AddComponent<BoxBlock>();
        }

        box.Initialize(x, y, _box1Sprite, _box0Sprite);
        box.SetGameManager(_gameManager);

        obj.name = $"Box.{x}.{y}";

        return obj;
    }
}
