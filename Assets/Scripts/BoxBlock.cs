using UnityEngine;

public class BoxBlock : MonoBehaviour
{
    public int x, y;
    public int health = 2;

    [SerializeField] private Sprite Box1Sprite;
    [SerializeField] private Sprite Box0Sprite;

    private GameManager _gameManager;

    public void SetGameManager(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public void Initialize(int x, int y, Sprite box1Sprite, Sprite box0Sprite)
    {
        this.x = x;
        this.y = y;
        if (box1Sprite != null)
        {
            Box1Sprite = box1Sprite;
        }
        if (box0Sprite != null)
        {
            Box0Sprite = box0Sprite;
        }

        UpdateVisual();
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || health <= 0)
        {
            return;
        }

        health -= amount;

        if (health <= 0)
        {
            if (GameManager.DictofBlocks != null)
            {
                GameManager.DictofBlocks.Remove(new Vector2Int(x, y));
            }
            Destroy(gameObject);
            return;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        if (health >= 2 && Box1Sprite != null)
        {
            renderer.sprite = Box1Sprite;
        }
        else if (health == 1 && Box0Sprite != null)
        {
            renderer.sprite = Box0Sprite;
        }
    }
}
