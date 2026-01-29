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
        if (_gameManager == null)
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        if (_gameManager != null)
        {
            _gameManager.OnBlockClicked(this);
        }
    }
}