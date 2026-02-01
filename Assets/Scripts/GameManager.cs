using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int M, N, K, A, B, C;// given variables at pdf file. They are changable from GameManager.
    [SerializeField] private AudioSource PopAudio,ShuffleAudio;
    [SerializeField] private GameObject Borders; 
    [SerializeField] private GameObject[] DefaultCubes; // prefabrics for default cubes.
    [SerializeField] private GameObject BoxPrefab;
    [SerializeField] private Transform Cubes; // transform to make it easy for checking blocks.
    [SerializeField] private GameObject Ground;//invisible ground making blocks not to fall.
    [SerializeField] private Sprite[] BlockSprites; // all block sprites ordered.
    [SerializeField] private Sprite Box1Sprite;
    [SerializeField] private Sprite Box0Sprite;
    [SerializeField] private bool UseSelectedBoxRows;
    [SerializeField] private bool UseSelectedBoxColumns;
    [SerializeField] private bool[] SelectedBoxRows;
    [SerializeField] private bool[] SelectedBoxColumns;
    public static Dictionary<Vector2Int, GameObject> DictofBlocks = new Dictionary<Vector2Int, GameObject>(); // all positions of block (x,y) and corresponding GameObject(block).
    public static List <GameObject> toPop = new List<GameObject>(); // static list to hold elements to destroy.
    private static List<GameObject> toChange = new List<GameObject>(); // static list to hold elements to change (and also used for checking how much moves is available).
    public static int maxTogetherCount = 0;
    private BlockFactory _blockFactory;

    [SerializeField] private float InputLockMinDuration = 0.05f;
    [SerializeField] private float InputLockMaxDuration = 1.5f;
    [SerializeField] private float SettleVelocityThreshold = 0.01f;
    [SerializeField] private GameUI gameUI;

    [SerializeField] private float FallMoveSpeed = 6f;
    [SerializeField] private float SnapDistance = 0.001f;
    [SerializeField] private float FallAcceleration = 40f;
    [SerializeField] private float MinFallSpeed = 1.5f;
    [SerializeField] private float MaxFallSpeed = 20f;

    private bool _inputLocked;
    private Coroutine _unlockRoutine;
    private int _lastClickFrame = -1;
    private readonly Dictionary<int, float> _fallSpeedById = new Dictionary<int, float>();

    private void OnValidate()
    {
        int rowCount = Mathf.Max(0, M);
        int columnCount = Mathf.Max(0, N);

        if (SelectedBoxRows == null || SelectedBoxRows.Length != rowCount)
        {
            bool[] resized = new bool[rowCount];
            if (SelectedBoxRows != null)
            {
                Array.Copy(SelectedBoxRows, resized, Mathf.Min(SelectedBoxRows.Length, resized.Length));
            }
            SelectedBoxRows = resized;
        }

        if (SelectedBoxColumns == null || SelectedBoxColumns.Length != columnCount)
        {
            bool[] resized = new bool[columnCount];
            if (SelectedBoxColumns != null)
            {
                Array.Copy(SelectedBoxColumns, resized, Mathf.Min(SelectedBoxColumns.Length, resized.Length));
            }
            SelectedBoxColumns = resized;
        }
    }

    // Start is called before the first frame update.
    void Start()
    {
        DictofBlocks.Clear();
        toPop.Clear();
        toChange.Clear();
        maxTogetherCount = 0;

        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<GameUI>();
        }

        if (Borders != null)
        {
            Borders.transform.localScale = new Vector3(15, M / 2f, 0); // orient borders. 
        }

        _blockFactory = new BlockFactory(this, DefaultCubes, BoxPrefab, Cubes, Box1Sprite, Box0Sprite);
        InitializeGrid(M, N, K);
        InitializeBoxes();
        AfterBoardChanged();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(world);
        if (hit == null)
        {
            hit = Physics2D.OverlapCircle(world, 0.05f);
        }
        if (hit == null)
        {
            return;
        }

        var block = hit.GetComponentInParent<Block>();
        if (block == null)
        {
            return;
        }

        OnBlockClicked(block);
    }

    private void FixedUpdate()
    {
        ApplyGridPositions();
    }

    private Vector2 GetWorldPositionForCell(int x, int y)
    {
        int startingX = (-N + 1);
        int startingY = (-M + 1);
        return new Vector2((startingX + x * 2) * CellSize, (startingY + y * 2) * CellSize);
    }

    private void ApplyGridPositions()
    {
        if (DictofBlocks == null || DictofBlocks.Count == 0)
        {
            return;
        }

        float step = 2f * CellSize;
        float dt = Time.fixedDeltaTime;

        for (int x = 0; x < N; x++)
        {
            List<(int y, GameObject obj)> column = null;
            List<int> boxYs = null;
            foreach (var kv in DictofBlocks)
            {
                if (kv.Key.x != x)
                {
                    continue;
                }

                if (kv.Value == null)
                {
                    continue;
                }

                if (IsBox(kv.Value))
                {
                    if (boxYs == null)
                    {
                        boxYs = new List<int>();
                    }
                    boxYs.Add(kv.Key.y);
                    continue;
                }

                if (!IsNormalBlock(kv.Value))
                {
                    continue;
                }

                if (column == null)
                {
                    column = new List<(int y, GameObject obj)>();
                }
                column.Add((kv.Key.y, kv.Value));
            }

            if (column == null)
            {
                continue;
            }

            column.Sort((a, b) => a.y.CompareTo(b.y));

            if (boxYs != null)
            {
                boxYs.Sort();
            }

            int boxIndex = 0;
            float floorY = float.NegativeInfinity;

            float minY = float.NegativeInfinity;
            for (int i = 0; i < column.Count; i++)
            {
                var obj = column[i].obj;
                if (obj == null)
                {
                    continue;
                }

                while (boxYs != null && boxIndex < boxYs.Count && boxYs[boxIndex] < column[i].y)
                {
                    Vector2Int boxKey = new Vector2Int(x, boxYs[boxIndex]);
                    if (DictofBlocks.TryGetValue(boxKey, out GameObject boxObj) && boxObj != null)
                    {
                        var boxComp = boxObj.GetComponent<BoxBlock>();
                        if (boxComp != null)
                        {
                            if (boxComp.x != x || boxComp.y != boxKey.y)
                            {
                                boxComp.x = x;
                                boxComp.y = boxKey.y;
                            }

                            Vector2 boxTarget = GetWorldPositionForCell(boxComp.x, boxComp.y);
                            var boxRb = boxObj.GetComponent<Rigidbody2D>();
                            if (boxRb != null)
                            {
                                boxRb.MovePosition(boxTarget);
                            }
                            else
                            {
                                boxObj.transform.position = boxTarget;
                            }
                        }
                    }

                    floorY = GetWorldPositionForCell(x, boxYs[boxIndex]).y + step;
                    minY = floorY;
                    boxIndex++;
                }

                var b = obj.GetComponent<Block>();
                if (b == null)
                {
                    continue;
                }

                if (b.x != x || b.y != column[i].y)
                {
                    b.x = x;
                    b.y = column[i].y;
                }

                Vector2 target = GetWorldPositionForCell(b.x, b.y);
                var rb = obj.GetComponent<Rigidbody2D>();

                Vector2 current = rb != null ? rb.position : (Vector2)obj.transform.position;
                int id = obj.GetInstanceID();
                float speed;
                if (!_fallSpeedById.TryGetValue(id, out speed))
                {
                    speed = 0f;
                }

                float dy = current.y - target.y;
                if (dy > SnapDistance)
                {
                    if (speed < MinFallSpeed)
                    {
                        speed = MinFallSpeed;
                    }
                    speed = Mathf.Min(speed + FallAcceleration * dt, MaxFallSpeed);
                }
                else
                {
                    speed = 0f;
                }

                float moveSpeed = Mathf.Min(speed + FallMoveSpeed, MaxFallSpeed);
                float newY = Mathf.MoveTowards(current.y, target.y, moveSpeed * dt);
                Vector2 next = new Vector2(target.x, newY);

                if (!float.IsNegativeInfinity(floorY) && next.y < floorY)
                {
                    next.y = floorY;
                }

                if (!float.IsNegativeInfinity(minY) && next.y < minY)
                {
                    next.y = minY;
                }

                if ((next - target).sqrMagnitude <= SnapDistance * SnapDistance)
                {
                    next = target;
                    speed = 0f;
                }

                if (rb != null)
                {
                    rb.MovePosition(next);
                }
                else
                {
                    obj.transform.position = next;
                }

                if (speed <= 0f)
                {
                    _fallSpeedById.Remove(id);
                }
                else
                {
                    _fallSpeedById[id] = speed;
                }

                minY = next.y + step;
            }

            while (boxYs != null && boxIndex < boxYs.Count)
            {
                Vector2Int boxKey = new Vector2Int(x, boxYs[boxIndex]);
                if (DictofBlocks.TryGetValue(boxKey, out GameObject boxObj) && boxObj != null)
                {
                    var boxComp = boxObj.GetComponent<BoxBlock>();
                    if (boxComp != null)
                    {
                        if (boxComp.x != x || boxComp.y != boxKey.y)
                        {
                            boxComp.x = x;
                            boxComp.y = boxKey.y;
                        }

                        Vector2 boxTarget = GetWorldPositionForCell(boxComp.x, boxComp.y);
                        var boxRb = boxObj.GetComponent<Rigidbody2D>();
                        if (boxRb != null)
                        {
                            boxRb.MovePosition(boxTarget);
                        }
                        else
                        {
                            boxObj.transform.position = boxTarget;
                        }
                    }
                }

                boxIndex++;
            }
        }
    }

    private const float CellSize = 0.225f;
    private const float PositionTolerance = 0.1f;
    private const float VelocityThreshold = 0.5f;

    private static void CleanupNullEntries()
    {
        if (DictofBlocks == null || DictofBlocks.Count == 0)
        {
            return;
        }

        List<Vector2Int> toRemove = null;
        foreach (var kv in DictofBlocks)
        {
            if (kv.Value == null)
            {
                if (toRemove == null)
                {
                    toRemove = new List<Vector2Int>();
                }
                toRemove.Add(kv.Key);
            }
        }

        if (toRemove == null)
        {
            return;
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            DictofBlocks.Remove(toRemove[i]);
        }
    }

    public void OnBlockClicked(Block block)
    {
        if (block == null)
        {
            return;
        }

        if (_inputLocked)
        {
            return;
        }

        if (_lastClickFrame == Time.frameCount)
        {
            return;
        }

        _inputLocked = true;
        _lastClickFrame = Time.frameCount;

        try
        {

        CleanupNullEntries();

        Block resolvedBlock = null;

        Vector2Int clickedPos = new Vector2Int(block.x, block.y);
        if (DictofBlocks.TryGetValue(clickedPos, out GameObject byCoord) && byCoord != null)
        {
            resolvedBlock = byCoord.GetComponent<Block>();
        }

        if (resolvedBlock == null)
        {
            foreach (var kv in DictofBlocks)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                if (kv.Value == block.gameObject)
                {
                    resolvedBlock = kv.Value.GetComponent<Block>();
                    break;
                }

                if (block.transform != null && kv.Value.transform != null && block.transform.IsChildOf(kv.Value.transform))
                {
                    resolvedBlock = kv.Value.GetComponent<Block>();
                    break;
                }
            }
        }

        if (resolvedBlock == null)
        {
            return;
        }

        clickedPos = new Vector2Int(resolvedBlock.x, resolvedBlock.y);

        // Clear toPop at START to prevent race conditions from rapid clicks
        toPop.Clear();
        
        BlockPop(clickedPos, resolvedBlock.color);
        if (toPop.Count > 0) // If something is going to pop:
        {
            if (PopAudio != null)
            {
                PopAudio.Play();
            }

            DamageAdjacentBoxes(toPop);
            
            if (gameUI != null)
            {
                gameUI.AddScore(toPop.Count);
            }
            
            DestroyList();
            UpdateDict();
            UpdateGrid();
        }

        AfterBoardChanged();
        }
        finally
        {
            _inputLocked = false;
        }
    }

    private void LockInput()
    {
        _inputLocked = true;
        if (_unlockRoutine != null)
        {
            StopCoroutine(_unlockRoutine);
        }
        _unlockRoutine = StartCoroutine(UnlockWhenSettled());
    }

    private IEnumerator UnlockWhenSettled()
    {
        float minUnlockTime = Time.time + InputLockMinDuration;
        float timeoutTime = Time.time + InputLockMaxDuration;

        while (Time.time < timeoutTime)
        {
            if (Time.time >= minUnlockTime && IsBoardSettled())
            {
                break;
            }

            yield return null;
        }

        _inputLocked = false;
        _unlockRoutine = null;
    }

    private bool IsBoardSettled()
    {
        float thresholdSqr = SettleVelocityThreshold * SettleVelocityThreshold;
        foreach (var kv in DictofBlocks)
        {
            var obj = kv.Value;
            if (obj == null)
            {
                continue;
            }

            var rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                if (!rb.IsSleeping() && rb.linearVelocity.sqrMagnitude > thresholdSqr)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void AfterBoardChanged()
    {
        ChangeSprites();
        CheckAvailableMoves();

        if (maxTogetherCount == 1)
        {
            ShuffleDeck();
            ChangeSprites();
            CheckAvailableMoves();
        }
    }

    private void createBox(int startingX, int startingY, int x, int y, int droppingHeight)
    {
        if (_blockFactory == null)
        {
            return;
        }

        var box = _blockFactory.CreateBox(startingX, startingY, x, y, droppingHeight);
        if (box == null)
        {
            return;
        }

        DictofBlocks[new Vector2Int(x, y)] = box;
    }

    private void createBlock(int startingX, int startingY, int x, int y, int color, int droppingHeight) // function to create block GameObjects.
    {
        if (_blockFactory == null)
        {
            return;
        }

        var block = _blockFactory.CreateNormalBlock(startingX, startingY, x, y, color, droppingHeight);
        if (block == null)
        {
            return;
        }

        DictofBlocks.Add(new Vector2Int(x, y), block);
    }

    private void InitializeBoxes()
    {
        if (BoxPrefab == null)
        {
            return;
        }

        HashSet<Vector2Int> boxCoordinates = new HashSet<Vector2Int>();

        bool useSelections = UseSelectedBoxRows || UseSelectedBoxColumns;
        if (!useSelections)
        {
            return;
        }

        if (UseSelectedBoxRows && SelectedBoxRows != null)
        {
            for (int y = 0; y < Mathf.Min(M, SelectedBoxRows.Length); y++)
            {
                if (!SelectedBoxRows[y])
                {
                    continue;
                }

                for (int x = 0; x < N; x++)
                {
                    boxCoordinates.Add(new Vector2Int(x, y));
                }
            }
        }

        if (UseSelectedBoxColumns && SelectedBoxColumns != null)
        {
            for (int x = 0; x < Mathf.Min(N, SelectedBoxColumns.Length); x++)
            {
                if (!SelectedBoxColumns[x])
                {
                    continue;
                }

                for (int y = 0; y < M; y++)
                {
                    boxCoordinates.Add(new Vector2Int(x, y));
                }
            }
        }

        if (boxCoordinates.Count == 0)
        {
            return;
        }

        int startingX = (-N + 1);
        int startingY = (-M + 1);

        foreach (var coordinates in boxCoordinates)
        {
            if (DictofBlocks.ContainsKey(coordinates))
            {
                Destroy(DictofBlocks[coordinates]);
                DictofBlocks.Remove(coordinates);
            }

            createBox(startingX, startingY, coordinates.x, coordinates.y, 0);
        }
    }
    private void InitializeGrid(int M, int N, int K)
    {
        int startingX = (-N + 1); //to dynamically allocate grid x position for start
        int startingY = (-M + 1); //to dynamically allocate grid y position for start
        if (Ground != null)
        {
            Ground.transform.position = new Vector2(0, (startingY * 0.225f) - 0.25f); // allocating ground for object to not fall.
        }
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++) 
            {
                int r = UnityEngine.Random.Range(0, K); // choosing random color 
                createBlock(startingX, startingY, x, y, r, 0);
            }
        }
    }

    private static bool IsBox(GameObject obj)
    {
        return obj != null && obj.GetComponent<BoxBlock>() != null;
    }

    private static bool IsNormalBlock(GameObject obj)
    {
        return obj != null && obj.GetComponent<Block>() != null;
    }

    private bool IsBlockStationary(GameObject obj)
    {
        if (obj == null) return false;
        
        var block = obj.GetComponent<Block>();
        if (block == null) return false;
        
        var rb = obj.GetComponent<Rigidbody2D>();
        
        // If no Rigidbody2D, treat as stationary (movement is handled by transform/parenting, not physics)
        if (rb == null)
        {
            return true;
        }
        
        // Static rigidbodies are always stationary
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return true;
        }

        if (rb.IsSleeping())
        {
            return true;
        }
        
        // For dynamic rigidbodies, check velocity
        if (rb.linearVelocity.sqrMagnitude > VelocityThreshold * VelocityThreshold)
        {
            return false;
        }

        return true;
    }

    private void DamageAdjacentBoxes(List<GameObject> poppedBlocks)
    {
        if (poppedBlocks == null || poppedBlocks.Count == 0)
        {
            return;
        }

        HashSet<BoxBlock> damaged = new HashSet<BoxBlock>();
        foreach (var popped in poppedBlocks)
        {
            if (!IsNormalBlock(popped))
            {
                continue;
            }

            var b = popped.GetComponent<Block>();
            if (b == null)
            {
                continue;
            }

            Vector2Int p = new Vector2Int(b.x, b.y);
            Vector2Int[] neighbors =
            {
                new Vector2Int(p.x, p.y + 1),
                new Vector2Int(p.x, p.y - 1),
                new Vector2Int(p.x - 1, p.y),
                new Vector2Int(p.x + 1, p.y)
            };

            foreach (var n in neighbors)
            {
                if (!DictofBlocks.ContainsKey(n))
                {
                    continue;
                }

                var box = DictofBlocks[n].GetComponent<BoxBlock>();
                if (box != null)
                {
                    damaged.Add(box);
                }
            }
        }

        foreach (var box in damaged)
        {
            box.ApplyDamage(1);
        }
    }
    public void BlockPop(Vector2Int coordinates, int color)
    {
        // Validate current block exists, is valid, and is stationary
        if (!DictofBlocks.TryGetValue(coordinates, out GameObject currentBlock) || currentBlock == null || !IsNormalBlock(currentBlock))
        {
            return;
        }

        var Top = new Vector2Int(coordinates.x, coordinates.y + 1); // top of current location
        var Down = new Vector2Int(coordinates.x, coordinates.y - 1);// bottom of current location
        var Left = new Vector2Int(coordinates.x - 1, coordinates.y);// left of current location
        var Right = new Vector2Int(coordinates.x + 1, coordinates.y);// right of current location

        if (DictofBlocks.TryGetValue(Top, out GameObject topBlock) && topBlock != null && IsNormalBlock(topBlock))//checks if this blocks exist first and then check if colors match or not.
        {
            var topBlockComp = topBlock.GetComponent<Block>();
            if (topBlockComp != null && color == topBlockComp.color)
            {
                if (!toPop.Contains(currentBlock))
                {
                    toPop.Add(currentBlock); // if atleast one match then we can add our initial block otherwise we should not pop it since it is only 1 block.
                }
                if (!toPop.Contains(topBlock)) //for not adding another block second time while searching simultaneously.
                {
                    toPop.Add(topBlock);
                    BlockPop(Top, color);
                }
            }
        }

        if (DictofBlocks.TryGetValue(Down, out GameObject downBlock) && downBlock != null && IsNormalBlock(downBlock)) // same process for other directions.
        {
            var downBlockComp = downBlock.GetComponent<Block>();
            if (downBlockComp != null && color == downBlockComp.color)
            {
                if (!toPop.Contains(currentBlock))
                {
                    toPop.Add(currentBlock);
                }
                if (!toPop.Contains(downBlock))
                {
                    toPop.Add(downBlock);
                    BlockPop(Down, color);
                }
            }
        }

        if (DictofBlocks.TryGetValue(Left, out GameObject leftBlock) && leftBlock != null && IsNormalBlock(leftBlock))
        {
            var leftBlockComp = leftBlock.GetComponent<Block>();
            if (leftBlockComp != null && color == leftBlockComp.color)
            {
                if (!toPop.Contains(currentBlock))
                {
                    toPop.Add(currentBlock);
                }
                if (!toPop.Contains(leftBlock))
                {
                    toPop.Add(leftBlock);
                    BlockPop(Left, color);
                }
            }
        }

        if (DictofBlocks.TryGetValue(Right, out GameObject rightBlock) && rightBlock != null && IsNormalBlock(rightBlock))
        {
            var rightBlockComp = rightBlock.GetComponent<Block>();
            if (rightBlockComp != null && color == rightBlockComp.color)
            {
                if (!toPop.Contains(currentBlock))
                {
                    toPop.Add(currentBlock);
                }
                if (!toPop.Contains(rightBlock))
                {
                    toPop.Add(rightBlock);
                    BlockPop(Right, color);
                }
            }
        }
    }
    public static void BlockChange(Vector2Int coordinates, int color) // same method as blockPop. Only difference is we should add our initial block to toChange list. 
    {
        var Top = new Vector2Int(coordinates.x, coordinates.y + 1);
        var Down = new Vector2Int(coordinates.x, coordinates.y - 1);
        var Left = new Vector2Int(coordinates.x - 1, coordinates.y);
        var Right = new Vector2Int(coordinates.x + 1, coordinates.y);
        if (!toChange.Contains(DictofBlocks[coordinates]))
        {
            toChange.Add(DictofBlocks[coordinates]);
        }
        if (DictofBlocks.ContainsKey(Top) && IsNormalBlock(DictofBlocks[Top]) && color == DictofBlocks[Top].GetComponent<Block>().color)
        {
            if (!toChange.Contains(DictofBlocks[coordinates]))
            {
                toChange.Add(DictofBlocks[coordinates]);
            }
            if (!toChange.Contains(DictofBlocks[Top]))
            {
                toChange.Add(DictofBlocks[Top]);
                BlockChange(Top, color);
            }
        }

        if (DictofBlocks.ContainsKey(Down) && IsNormalBlock(DictofBlocks[Down]) && color == DictofBlocks[Down].GetComponent<Block>().color)
        {
            if (!toChange.Contains(DictofBlocks[coordinates]))
            {
                toChange.Add(DictofBlocks[coordinates]);
            }
            if (!toChange.Contains(DictofBlocks[Down]))
            {
                toChange.Add(DictofBlocks[Down]);
                BlockChange(Down, color);
            }
        }

        if (DictofBlocks.ContainsKey(Left) && IsNormalBlock(DictofBlocks[Left]) && color == DictofBlocks[Left].GetComponent<Block>().color)
        {
            if (!toChange.Contains(DictofBlocks[coordinates]))
            {
                toChange.Add(DictofBlocks[coordinates]);
            }
            if (!toChange.Contains(DictofBlocks[Left]))
            {
                toChange.Add(DictofBlocks[Left]);
                BlockChange(Left, color);
            }
        }

        if (DictofBlocks.ContainsKey(Right) && IsNormalBlock(DictofBlocks[Right]) && color == DictofBlocks[Right].GetComponent<Block>().color)
        {
            if (!toChange.Contains(DictofBlocks[coordinates]))
            {
                toChange.Add(DictofBlocks[coordinates]);
            }
            if (!toChange.Contains(DictofBlocks[Right]))
            {
                toChange.Add(DictofBlocks[Right]);
                BlockChange(Right, color);
            }
        }
    }
    // call functions to call from block scripts. It works better in game rather than directly calling. Also that way my functions can stay private.
    public void DestroyListCall() 
    {
        DestroyList();
    }
    public void ChangeSpritesCall()
    {
        ChangeSprites();
    }
    public void UpdateDictCall()
    {
        UpdateDict();
    }
    public void UpdateGridCall()
    {
        UpdateGrid();
    }
    public void PlayPopAudioCall()
    {
        if (PopAudio != null)
        {
            PopAudio.Play();
        }
    }
    public void ShuffleDeckCall()
    {
        ShuffleDeck();
    }
    public void CheckAvailableMovesCall()
    {
        CheckAvailableMoves();
    }
    public void PlayShuffleAudioCall()
    {
        if (ShuffleAudio != null)
        {
            ShuffleAudio.Play();
        }
    }
    private void DestroyList() //destroy all the elements from toPop and remove them from DictofBlocks.
    {
        for (int i = 0; i < toPop.Count; i++)
        {
            var obj = toPop[i];
            if (obj == null)
            {
                continue;
            }
            
            var blockComp = obj.GetComponent<Block>();
            if (blockComp != null)
            {
                DictofBlocks.Remove(new Vector2Int(blockComp.x, blockComp.y));
            }
            Destroy(obj);
        }
        toPop.Clear(); //clear toPop for later calls since it is a global variable.

    }
    private void UpdateDict() // make dictionary update because the elements drop below when some blocks under them are popped.
    {
        for (int x = 0; x < N; x++)
        {
            int startY = 0;
            while (startY < M)
            {
                int endYExclusive = M;
                for (int y = startY; y < M; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (DictofBlocks.ContainsKey(c) && IsBox(DictofBlocks[c]))
                    {
                        endYExclusive = y;
                        break;
                    }
                }

                List<GameObject> segmentBlocks = new List<GameObject>();
                for (int y = startY; y < endYExclusive; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (DictofBlocks.ContainsKey(c) && IsNormalBlock(DictofBlocks[c]))
                    {
                        segmentBlocks.Add(DictofBlocks[c]);
                    }
                }

                for (int y = startY; y < endYExclusive; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (DictofBlocks.ContainsKey(c) && IsNormalBlock(DictofBlocks[c]))
                    {
                        DictofBlocks.Remove(c);
                    }
                }

                int writeY = startY;
                foreach (var obj in segmentBlocks)
                {
                    Vector2Int newPos = new Vector2Int(x, writeY++);
                    DictofBlocks[newPos] = obj;
                    var b = obj.GetComponent<Block>();
                    b.x = newPos.x;
                    b.y = newPos.y;
                    obj.name = newPos.x.ToString() + "." + newPos.y.ToString();
                    var renderer = obj.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = newPos.y;
                    }
                }

                if (endYExclusive == M)
                {
                    break;
                }

                startY = endYExclusive + 1;
            }
        }
    }
    private void UpdateGrid() // create additional blocks if there are some gaps in the grid.
    {
        CleanupNullEntries();
        int startingX = (-N + 1);
        int startingY = (-M + 1);
        for (int x = 0; x < N; x++)
        {
            int highestBoxY = -1;
            for (int y = M - 1; y >= 0; y--)
            {
                Vector2Int c = new Vector2Int(x, y);
                if (DictofBlocks.TryGetValue(c, out GameObject obj) && obj != null && IsBox(obj))
                {
                    highestBoxY = y;
                    break;
                }
            }

            int spawnStartY = highestBoxY + 1;
            for (int y = spawnStartY; y < M; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                if (!DictofBlocks.TryGetValue(coordinates, out GameObject existing) || existing == null)
                {
                    int r = UnityEngine.Random.Range(0, K);
                    createBlock(startingX, startingY, x, y, r, 5);
                }
            }
        }
    }
    private void ChangeSprites() //change sprites according to how many objects are next to each other.
    {
        List<GameObject> visited = new List<GameObject>(); // to make it more efficient, when we get a group we add blocks inside of it to visited list.
        for (int x = 0; x < N; x++) { 
            for (int y = 0; y < M; y++) {
                Vector2Int coordinates = new Vector2Int(x,y);
                if (!DictofBlocks.ContainsKey(coordinates) || !IsNormalBlock(DictofBlocks[coordinates]))
                {
                    continue;
                }
                int color = DictofBlocks[coordinates].GetComponent<Block>().color;
                if (!visited.Contains(DictofBlocks[coordinates])){ // if it is not checked before.
                    BlockChange(coordinates, color); // makes toChange list a list of grouped objects.
                    if (toChange.Count > C)
                    {
                        foreach (GameObject k in toChange)
                        {
                            k.GetComponent<SpriteRenderer>().sprite = BlockSprites[(k.GetComponent<Block>().color * 4) + 3];
                            visited.Add(k);
                        }
                    }
                    else if (toChange.Count > B)
                    {
                        foreach (GameObject k in toChange)
                        {
                            k.GetComponent<SpriteRenderer>().sprite = BlockSprites[(k.GetComponent<Block>().color * 4) + 2];
                            visited.Add(k);
                        }
                    }
                    else if (toChange.Count > A)
                    {
                        foreach (GameObject k in toChange)
                        {
                            k.GetComponent<SpriteRenderer>().sprite = BlockSprites[(k.GetComponent<Block>().color * 4) + 1];
                            visited.Add(k);
                        }
                    }
                    else
                    {
                        foreach (GameObject k in toChange)
                        {
                            k.GetComponent<SpriteRenderer>().sprite = BlockSprites[k.GetComponent<Block>().color * 4];
                            visited.Add(k);
                        }
                    }
                    toChange.Clear(); // clear toChange list after sprite changes are done.
                }
            }
        }
    }
    private void CheckAvailableMoves() // checks the biggest group of blocks and makes maxTogetherCount equal to it.
    {
        maxTogetherCount = 0;
        List<GameObject> visited = new List<GameObject>();// to make it more efficient, when we get a group we add blocks inside of it to visited list.
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                if (!DictofBlocks.ContainsKey(coordinates) || !IsNormalBlock(DictofBlocks[coordinates]))
                {
                    continue;
                }
                int color = DictofBlocks[coordinates].GetComponent<Block>().color;
                if (!visited.Contains(DictofBlocks[coordinates]))// if it is not checked before.
                {
                    BlockChange(coordinates, color);  // makes toChange list a list of grouped objects.
                    if (toChange.Count > maxTogetherCount)
                    {
                        maxTogetherCount = toChange.Count; // check what is the maximum blocks in a group
                    }
                    foreach (GameObject k in toChange)
                    {
                        visited.Add(k);
                    }
                    toChange.Clear ();
                }
            }
        }
    }
    private void ShuffleDeck() // shuffling algorith which removes the top half of the grid and create the transpose of the bottom half to the top. That way there is always multiple moves to play when shuffled.
    {
        PlayShuffleAudioCall();

        MoveFinder.ShuffleColorsInPlace(DictofBlocks);
        var moveFinder = new MoveFinder(new BoardState(DictofBlocks), N, M);
        if (!moveFinder.HasPlayableMove())
        {
            moveFinder.TryForceCreateMove();
        }

        maxTogetherCount = 0; // reset maxTogetherCount
    }

}
