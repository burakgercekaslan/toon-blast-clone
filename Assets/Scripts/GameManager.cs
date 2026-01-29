using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int M, N, K, A, B, C;// given variables at pdf file. They are changable from GameManager.
    [SerializeField] private AudioSource PopAudio,ShuffleAudio;
    [SerializeField] private GameObject Borders; 
    [SerializeField] private GameObject[] DefaultCubes; // prefabrics for default cubes.
    [SerializeField] private Transform Cubes; // transform to make it easy for checking blocks.
    [SerializeField] private GameObject Ground;//invisible ground making blocks not to fall.
    [SerializeField] private Sprite[] BlockSprites; // all block sprites ordered.
    public static Dictionary<Vector2Int, GameObject> DictofBlocks = new Dictionary<Vector2Int, GameObject>(); // all positions of block (x,y) and corresponding GameObject(block).
    public static List <GameObject> toPop = new List<GameObject>(); // static list to hold elements to destroy.
    private static List<GameObject> toChange = new List<GameObject>(); // static list to hold elements to change (and also used for checking how much moves is available).
    public static int maxTogetherCount = 0;
    // Start is called before the first frame update.
    void Start()
    {
        DictofBlocks.Clear();
        toPop.Clear();
        toChange.Clear();
        maxTogetherCount = 0;

        if (Borders != null)
        {
            Borders.transform.localScale = new Vector3(15, M / 2f, 0); // orient borders. 
        }
        InitializeGrid(M, N, K);
        AfterBoardChanged();
    }

    public void OnBlockClicked(Block block)
    {
        if (block == null)
        {
            return;
        }

        BlockPop(new Vector2Int(block.x, block.y), block.color); //calls the blockpop function to make a toPop list which contains the game objects that we should destroy.
        if (toPop.Count > 0) // If something is going to pop:
        {
            if (PopAudio != null)
            {
                PopAudio.Play();
            }

            DestroyList();
            UpdateDict();
            UpdateGrid();
        }

        AfterBoardChanged();
    }

    private void AfterBoardChanged()
    {
        ChangeSprites();
        CheckAvailableMoves();

        int shuffleSafety = 0;
        while (maxTogetherCount == 1 && shuffleSafety++ < 3)
        {
            ShuffleDeck();
            ChangeSprites();
            CheckAvailableMoves();
        }
    }

    private void createBlock(int startingX, int startingY, int x, int y, int color, int droppingHeight) // function to create block GameObjects.
    {
        var block = Instantiate(DefaultCubes[color], new Vector2((startingX + x * 2) * 0.225f, (startingY + y * 2) * 0.225f + droppingHeight), Quaternion.identity);
        block.GetComponent<SpriteRenderer>().sortingOrder = y; // to better look objects on top should be on front.
        block.AddComponent<BoxCollider2D>();
        block.GetComponentInChildren<BoxCollider2D>().size = new Vector2(2, 2.25f);
        block.transform.SetParent(Cubes);
        var blockComponent = block.AddComponent<Block>(); // adding block script to all blocks.
        blockComponent.x = x;
        blockComponent.y = y;
        blockComponent.color = color;
        blockComponent.SetGameManager(this);
        block.name = x.ToString() + "." + y.ToString();
        block.tag = "Block";
        DictofBlocks.Add(new Vector2Int(x, y), block);
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
    public static void BlockPop(Vector2Int coordinates, int color)
    {
        var Top = new Vector2Int(coordinates.x, coordinates.y + 1); // top of current location
        var Down = new Vector2Int(coordinates.x, coordinates.y - 1);// bottom of current location
        var Left = new Vector2Int(coordinates.x - 1, coordinates.y);// left of current location
        var Right = new Vector2Int(coordinates.x + 1, coordinates.y);// right of current location

        if (DictofBlocks.ContainsKey(Top) && color == DictofBlocks[Top].GetComponent<Block>().color)//checks if this blocks exist first and then check if colors match or not.
        {
            if (!toPop.Contains(DictofBlocks[coordinates]))
            {
                toPop.Add(DictofBlocks[coordinates]); // if atleast one match then we can add our initial block otherwise we should not pop it since it is only 1 block.
            }
            if (!toPop.Contains(DictofBlocks[Top])) //for not adding another block second time while searching simultaneously.
            {
                toPop.Add(DictofBlocks[Top]);
                BlockPop(Top, color);
            }
        }

        if (DictofBlocks.ContainsKey(Down) && color == DictofBlocks[Down].GetComponent<Block>().color) // same process for other directions.
        {
            if (!toPop.Contains(DictofBlocks[coordinates]))
            {
                toPop.Add(DictofBlocks[coordinates]);
            }
            if (!toPop.Contains(DictofBlocks[Down]))
            {
                toPop.Add(DictofBlocks[Down]);
                BlockPop(Down, color);
            }
        }

        if (DictofBlocks.ContainsKey(Left) && color == DictofBlocks[Left].GetComponent<Block>().color)
        {
            if (!toPop.Contains(DictofBlocks[coordinates]))
            {
                toPop.Add(DictofBlocks[coordinates]);
            }
            if (!toPop.Contains(DictofBlocks[Left]))
            {
                toPop.Add(DictofBlocks[Left]);
                BlockPop(Left, color);
            }
        }

        if (DictofBlocks.ContainsKey(Right) && color == DictofBlocks[Right].GetComponent<Block>().color)
        {
            if (!toPop.Contains(DictofBlocks[coordinates]))
            {
                toPop.Add(DictofBlocks[coordinates]);
            }
            if (!toPop.Contains(DictofBlocks[Right]))
            {
                toPop.Add(DictofBlocks[Right]);
                BlockPop(Right, color);
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
        if (DictofBlocks.ContainsKey(Top) && color == DictofBlocks[Top].GetComponent<Block>().color)
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

        if (DictofBlocks.ContainsKey(Down) && color == DictofBlocks[Down].GetComponent<Block>().color)
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

        if (DictofBlocks.ContainsKey(Left) && color == DictofBlocks[Left].GetComponent<Block>().color)
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

        if (DictofBlocks.ContainsKey(Right) && color == DictofBlocks[Right].GetComponent<Block>().color)
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
            DictofBlocks.Remove(new Vector2Int(toPop[i].GetComponent<Block>().x, toPop[i].GetComponent<Block>().y));
            Destroy(toPop[i]);
        }
        toPop.Clear(); //clear toPop for later calls since it is a global variable.

    }
    private void UpdateDict() // make dictionary update because the elements drop below when some blocks under them are popped.
    {
        for (int x = 0; x < N; x++)
        {
            List<GameObject> objects = new List<GameObject>();
            for(int y = 0;y < M; y++) {
                Vector2Int coordinates = new Vector2Int(x, y);
                if (DictofBlocks.ContainsKey(coordinates))
                {
                    objects.Add(DictofBlocks[coordinates]); // get all the remaining blocks in column.
                }    
            }
            int index = 0;
            int nullCounter = 0;
            for (int y = 0; y<M; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                if (!DictofBlocks.ContainsKey(coordinates))
                {
                    nullCounter++; // when null we need to drop the y coordinate of the blocks above it. 
                }
                else
                {
                    DictofBlocks.Remove(coordinates); // remove from dict starting from bottom to reorder them. 
                    Vector2Int new_coordinates = new Vector2Int(x, y-nullCounter); // add to the dict again with corrected coordinates.
                    DictofBlocks.Add(new_coordinates, objects[index++]);//getting the blocks from objects list.
                    DictofBlocks[new_coordinates].GetComponent<Block>().x = new_coordinates.x;
                    DictofBlocks[new_coordinates].GetComponent<Block>().y = new_coordinates.y;
                    DictofBlocks[new_coordinates].GetComponent<Block>().name = new_coordinates.x.ToString() + "." + new_coordinates.y.ToString();
                    DictofBlocks[new_coordinates].GetComponent<SpriteRenderer>().sortingOrder = new_coordinates.y;
                }
            }
            objects.Clear();// clear the list for next column.
        }
    }
    private void UpdateGrid() // create additional blocks if there are some gaps in the grid.
    {
        int startingX = (-N + 1);
        int startingY = (-M + 1);
        for (int x = 0; x < N; x++)
        {
            for (int y = 0;y < M; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                if (!DictofBlocks.ContainsKey(coordinates)){ // if there is not any blocks in given coordinates, create one.
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
        int deleteUntilRow = M - M / 2;
        for (int x = 0;x < N; x++)
        {
            List<int> colors = new List<int>();
            int index = 0;
            int indexofy = deleteUntilRow-1; // y coordinate of blocks on top after destroying top half.
            for (int y = M-1; y >= deleteUntilRow; y--)
            {
                Vector2Int coordinates = new Vector2Int (x, y);
                colors.Add(DictofBlocks[new Vector2Int(x, indexofy--)].GetComponent<Block>().color); // simultaneously get the colors of bottom half to transpose it after.
                if (DictofBlocks.ContainsKey(coordinates))
                {
                    Destroy(DictofBlocks[coordinates]); // destroy the block.
                    DictofBlocks.Remove(coordinates); // delete from dictionary.
                }
            }
            for (int y = deleteUntilRow; y < M; y++) // to create the transpose.
            {
                int startingX = (-N + 1);
                int startingY = (-M + 1);
                int r = colors[index++]; // get the colors from top to bottom.
                createBlock(startingX, startingY, x, y, r, 5);
            }
        }
        maxTogetherCount = 0; // reset maxTogetherCount
    }

}
