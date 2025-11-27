using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    
    private void Start()
    {
        // Ensure AudioManager exists
        if (AudioManager.Instance == null)
        {
            GameObject audioManagerObj = new GameObject("AudioManager");
            audioManagerObj.AddComponent<AudioManager>();
        }
        
        // Show main menu, hide options
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }
    
    public void OnPlayButtonClicked()
    {
        PlayButtonSound();
        SceneManager.LoadScene("SampleScene");
    }
    
    public void OnOptionsButtonClicked()
    {
        PlayButtonSound();
        
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }
    
    public void OnExitButtonClicked()
    {
        PlayButtonSound();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    public void OnBackButtonClicked()
    {
        PlayButtonSound();
        
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
        
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
    
    private void PlayButtonSound()
    {
        if (buttonClickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound);
        }
    }
}


