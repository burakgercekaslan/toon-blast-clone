using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class GameManager : MonoBehaviour
{
    public int M, N, K, A, B, C;// given variables at pdf file. They are changable from GameManager.
    [SerializeField] private AudioSource PopAudio,ShuffleAudio;
    [SerializeField] private GameObject Borders; 
    [SerializeField] private GameObject[] DefaultCubes; // prefabrics for default cubes.
    [SerializeField] private Transform Cubes; // transform to make it easy for checking blocks.
    [SerializeField] private GameObject Ground;//invisible ground making blocks not to fall.
    [SerializeField] private Sprite[] BlockSprites; // all block sprites ordered.
    public static Dictionary<Tuple<int,int>, GameObject> DictofBlocks = new Dictionary<Tuple<int, int>, GameObject>(); // all positions of block (x,y) and corresponding GameObject(block).
    public static List <GameObject> toPop = new List<GameObject>(); // static list to hold elements to destroy.
    private static List<GameObject> toChange = new List<GameObject>(); // static list to hold elements to change (and also used for checking how much moves is available).
    public static int maxTogetherCount = 0;
    // Start is called before the first frame update.
    void Start()
    {
        Borders.transform.localScale = new Vector3(15,M/2,0); // orient borders. 
        InitializeGrid(M, N, K);
        ChangeSprites();
        CheckAvailableMoves();
        if (maxTogetherCount == 1)
        {
            ShuffleDeck();
        }
    }

    // Update is called once per frame
    void Update() // this part is mostly for making the game smoother with less bugs.
    {
        Invoke("ChangeSprites", 0.1f);
        Invoke("CheckAvailableMoves", 0.1f);
        if (maxTogetherCount == 1)
        {
            Invoke("ShuffleDeck", 0.1f);
        }
    }

    private void createBlock(int startingX, int startingY, int x, int y, int color, int droppingHeight) // function to create block GameObjects.
    {
        var block = Instantiate(DefaultCubes[color], new Vector2((startingX + x * 2) * 0.225f, (startingY + y * 2) * 0.225f + droppingHeight), Quaternion.identity);
        block.GetComponent<SpriteRenderer>().sortingOrder = y; // to better look objects on top should be on front.
        block.AddComponent<BoxCollider2D>();
        block.GetComponentInChildren<BoxCollider2D>().size = new Vector2(2, 2.25f);
        block.transform.SetParent(Cubes);
        block.AddComponent<Block>(); // adding block script to all blocks.
        block.GetComponent<Block>().x = x;
        block.GetComponent<Block>().y = y;
        block.GetComponent<Block>().color = color;
        block.name = x.ToString() + "." + y.ToString();
        block.tag = "Block";
        DictofBlocks.Add(new Tuple<int, int>(x, y), block);
    }
    private void InitializeGrid(int M, int N, int K)
    {
        int startingX = (-N + 1); //to dynamically allocate grid x position for start
        int startingY = (-M + 1); //to dynamically allocate grid y position for start
        Ground.transform.position = new Vector2(0, -M * 0.225f-0.25f); // allocating ground for object to not fall.
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++) 
            {
                int r = UnityEngine.Random.Range(0, K); // choosing random color 
                createBlock(startingX, startingY, x, y, r, 0);
            }
        }
    }
    public static void BlockPop(Tuple<int, int> coordinates, int color)
    {
        var Top = new Tuple<int, int>(coordinates.Item1, coordinates.Item2 + 1); // top of current location
        var Down = new Tuple<int, int>(coordinates.Item1, coordinates.Item2 - 1);// bottom of current location
        var Left = new Tuple<int, int>(coordinates.Item1 - 1, coordinates.Item2);// left of current location
        var Right = new Tuple<int, int>(coordinates.Item1 + 1, coordinates.Item2);// right of current location

        if (DictofBlocks.ContainsKey(Top) && color == DictofBlocks[Top].GetComponent<Block>().color)//checks if this blocks exist first and then check if colors match or not.
        {
            if (!toPop.Contains(DictofBlocks[coordinates]))
            {
                toPop.Add(DictofBlocks[coordinates]); // if atleast one match then we can add our initial block otherwise we should not pop it since it is only 1 block.
            }
            if (!toPop.Contains(DictofBlocks[Top])) //for not adding another block second time while searching simultaneously.
            {
                toPop.Add(DictofBlocks[Top]);
                DictofBlocks[Top].GetComponent<Block>().BlockTopRecursive(); // recursive call for block on top. It was more convenient to call it like this instead of calling blockpop again directly.
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
                DictofBlocks[Down].GetComponent<Block>().BlockTopRecursive();
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
                DictofBlocks[Left].GetComponent<Block>().BlockTopRecursive();
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
                DictofBlocks[Right].GetComponent<Block>().BlockTopRecursive();
            }
        }
    }
    public static void BlockChange(Tuple<int, int> coordinates, int color) // same method as blockPop. Only difference is we should add our initial block to toChange list. 
    {
        var Top = new Tuple<int, int>(coordinates.Item1, coordinates.Item2 + 1);
        var Down = new Tuple<int, int>(coordinates.Item1, coordinates.Item2 - 1);
        var Left = new Tuple<int, int>(coordinates.Item1 - 1, coordinates.Item2);
        var Right = new Tuple<int, int>(coordinates.Item1 + 1, coordinates.Item2);
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
                DictofBlocks[Top].GetComponent<Block>().BlockChangeRecursive();
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
                DictofBlocks[Down].GetComponent<Block>().BlockChangeRecursive();
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
                DictofBlocks[Left].GetComponent<Block>().BlockChangeRecursive();
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
                DictofBlocks[Right].GetComponent<Block>().BlockChangeRecursive();
            }
        }
    }
    // call functions to call from block scripts. It works better in game rather than directly calling. Also that way my functions can stay private.
    public void DestroyListCall() 
    {
        Invoke("DestroyList", 0.1f);
    }
    public void ChangeSpritesCall()
    {
        Invoke("ChangeSprites", 0f);
    }
    public void UpdateDictCall()
    {
        Invoke("UpdateDict", 0.1f);
    }
    public void UpdateGridCall()
    {
        Invoke("UpdateGrid", 0.1f);
    }
    public void PlayPopAudioCall()
    {
        PopAudio.Play();
    }
    public void ShuffleDeckCall()
    {
            Invoke("ShuffleDeck", 0.1f);
    }
    public void CheckAvailableMovesCall()
    {
        Invoke("CheckAvailableMoves", 0f);
    }
    public void PlayShuffleAudioCall()
    {
        ShuffleAudio.Play();
    }
    private void DestroyList() //destroy all the elements from toPop and remove them from DictofBlocks.
    {
        for (int i = 0; i < toPop.Count; i++)
        {
            DictofBlocks.Remove(new Tuple<int, int>(toPop[i].GetComponent<Block>().x, toPop[i].GetComponent<Block>().y));
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
                Tuple<int, int> coordinates = new Tuple<int, int>(x, y);
                if (DictofBlocks.ContainsKey(coordinates))
                {
                    objects.Add(DictofBlocks[coordinates]); // get all the remaining blocks in column.
                }    
            }
            int index = 0;
            int nullCounter = 0;
            for (int y = 0; y<M; y++)
            {
                Tuple<int, int> coordinates = new Tuple<int, int>(x, y);
                if (!DictofBlocks.ContainsKey(coordinates))
                {
                    nullCounter++; // when null we need to drop the y coordinate of the blocks above it. 
                }
                else
                {
                    DictofBlocks.Remove(coordinates); // remove from dict starting from bottom to reorder them. 
                    Tuple<int, int> new_coordinates = new Tuple<int, int>(x, y-nullCounter); // add to the dict again with corrected coordinates.
                    DictofBlocks.Add(new_coordinates, objects[index++]);//getting the blocks from objects list.
                    DictofBlocks[new_coordinates].GetComponent<Block>().x = new_coordinates.Item1;
                    DictofBlocks[new_coordinates].GetComponent<Block>().y = new_coordinates.Item2;
                    DictofBlocks[new_coordinates].GetComponent<Block>().name = new_coordinates.Item1.ToString() + "." + new_coordinates.Item2.ToString();
                    DictofBlocks[new_coordinates].GetComponent<SpriteRenderer>().sortingOrder = new_coordinates.Item2;
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
                Tuple<int, int> coordinates = new Tuple<int, int>(x, y);
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
                Tuple<int,int> coordinates = new Tuple<int, int>(x,y);
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
                Tuple<int, int> coordinates = new Tuple<int, int>(x, y);
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
                Tuple<int, int> coordinates = new Tuple<int, int> (x, y);
                colors.Add(DictofBlocks[new Tuple<int, int>(x, indexofy--)].GetComponent<Block>().color); // simultaneously get the colors of bottom half to transpose it after.
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
