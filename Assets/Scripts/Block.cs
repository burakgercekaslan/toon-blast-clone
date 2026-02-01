using UnityEngine;

public class Block : MonoBehaviour
{
    public int x, y, color; //blocks position as x,y and its color as an integer.
    private GameManager _gameManager;

    public void SetGameManager(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    private void OnMouseDown() 
    {
        Block target = this;
        while (target.transform.parent != null)
        {
            var parentBlock = target.transform.parent.GetComponent<Block>();
            if (parentBlock == null)
            {
                break;
            }
            target = parentBlock;
        }

        if (_gameManager == null)
        {
            _gameManager = FindFirstObjectByType<GameManager>();
        }

        if (_gameManager != null)
        {
            _gameManager.OnBlockClicked(target);
        }
    }
}