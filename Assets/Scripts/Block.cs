using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Threading.Tasks;

public class Block : MonoBehaviour
{
    public int x, y, color; //blocks position as x,y and its color as an integer.
    private void OnMouseDown() 
    {
        GameManager.BlockPop(new Tuple<int, int>(x, y), color); //calls the blockpop function from GameManager to make a toPop list which contains the game objects that we should destroys.
        if (GameManager.toPop.Count > 0) // If something is going to pop:
        {
            FindObjectOfType<GameManager>().PlayPopAudioCall(); // call from GameManager to play audio.
            FindObjectOfType<GameManager>().DestroyListCall();// call from GameManager to destroy all the elements in the list toPop.
            FindObjectOfType<GameManager>().UpdateDictCall();// // call from GameManager to update dictOfBlocks.
            FindObjectOfType<GameManager>().UpdateGridCall();// call from GameManager to update grid (game objects) and their positions respectfully.
        }
        FindObjectOfType<GameManager>().ChangeSpritesCall(); // check which block sprites to change.
        FindObjectOfType<GameManager>().CheckAvailableMovesCall();//check if there is a playable move.
        if (GameManager.maxTogetherCount == 1)
        {
            FindObjectOfType<GameManager>().ShuffleDeckCall(); //shuffle deck if no moves are available.
        }
    }
    public void BlockTopRecursive() // to call block pop again.
    {
        GameManager.BlockPop(new Tuple<int, int>(x, y), color); 
    }
    public void BlockChangeRecursive()// to call block change again.
    {
        GameManager.BlockChange(new Tuple<int, int>(x, y), color);
    }
}
    