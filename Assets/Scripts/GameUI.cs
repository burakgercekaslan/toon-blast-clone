using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    
    private int score = 0;
    private float elapsedTime = 0f;
    private bool timerRunning = true;

    private void Awake()
    {
        SetupAnchors();
    }

    private void Start()
    {
        score = 0;
        elapsedTime = 0f;
        timerRunning = true;
        UpdateScoreText();
        UpdateTimerText();
    }

    private void SetupAnchors()
    {
        if (scoreText != null)
        {
            RectTransform rt = scoreText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(70, 120);
            rt.sizeDelta = new Vector2(600, 80);
            scoreText.fontSize = 78;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.alignment = TextAlignmentOptions.BottomLeft;
            scoreText.textWrappingMode = TextWrappingModes.NoWrap;
            scoreText.overflowMode = TextOverflowModes.Overflow;
        }

        if (timerText != null)
        {
            RectTransform rt = timerText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -130);
            rt.sizeDelta = new Vector2(500, 80);
            timerText.fontSize = 78;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.textWrappingMode = TextWrappingModes.NoWrap;
            timerText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void Update()
    {
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerText();
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    public int GetScore()
    {
        return score;
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        timerRunning = true;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
