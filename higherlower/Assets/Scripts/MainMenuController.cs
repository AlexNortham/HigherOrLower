using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using DG.Tweening;    

public class MainMenuController : MonoBehaviour
{
    public Image fadePanel; 

    void Start()
    {
        //panel to fade in
        fadePanel.color = new Color(0, 0, 0, 1); 
        fadePanel.raycastTarget = true;

        fadePanel.DOFade(0, 1.0f).OnComplete(() => {
            fadePanel.raycastTarget = false;
        });
    }

    public void PlayGame()
    {
        fadePanel.raycastTarget = true;
        //loads higher or lower scene
        fadePanel.DOFade(1, 1.0f).OnComplete(() => {
            SceneManager.LoadScene(1); 
        });
    }
    
    public void PlayBlackjack()
    {
        fadePanel.raycastTarget = true;
        //loads blackjack scene
        fadePanel.DOFade(1, 1.0f).OnComplete(() => SceneManager.LoadScene(2)); 
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
}