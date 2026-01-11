using UnityEngine;
using UnityEngine.UI; 
using DG.Tweening; 
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public DeckManager deckManager;
    public GameObject cardPrefab;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    
    [Header("UI Controls")]
    public Button higherBtn;
    public Button lowerBtn;
    public Button playAgainBtn; 
    public Button quitBtn; 
    public Image fadePanel;     

    [Header("Anchors")]
    public Transform deckSpawnPoint;    
    public Transform cameraInspectPoint;
    public Transform activeCardPoint;   
    public Transform discardPilePoint;  

    private List<GameObject> discardedCardsList = new List<GameObject>(); 
    private List<GameObject> deckPileList = new List<GameObject>();       

    private GameObject activeCardObj;   
    private GameObject pendingCardObj;  
    
    private CardData activeCardData;
    private CardData pendingCardData;

    private int score = 0;
    private bool isGuessingHigher;

    void Start()
    {
        //hide quit and play again buttons initially
        playAgainBtn.gameObject.SetActive(false); 
        quitBtn.gameObject.SetActive(false);
        
        fadePanel.gameObject.SetActive(true);     
        fadePanel.color = new Color(0, 0, 0, 1);
        fadePanel.raycastTarget = true;          

        //fade scene in
        fadePanel.DOFade(0, 1.0f).OnComplete(() => fadePanel.raycastTarget = false);

        deckManager.InitialiseDeck(); 
        StartGame();
    }

    void StartGame()
    {
        score = 0;
        scoreText.text = "Score: 0";
        resultText.text = "";
        
        playAgainBtn.gameObject.SetActive(false);
        quitBtn.gameObject.SetActive(false);

        //ensure no card data from previous games remains
        foreach (GameObject card in discardedCardsList) Destroy(card);
        discardedCardsList.Clear();

        foreach (GameObject card in deckPileList) Destroy(card);
        deckPileList.Clear();

        if (activeCardObj != null) Destroy(activeCardObj);
        if (pendingCardObj != null) Destroy(pendingCardObj);

        deckManager.InitialiseDeck(); 

        //create card stack
        for (int i = 0; i < 52; i++)
        {
            Vector3 stackPos = deckSpawnPoint.position + new Vector3(0, i * 0.005f, 0);
            GameObject newCard = Instantiate(cardPrefab, stackPos, deckSpawnPoint.rotation);
            newCard.transform.Rotate(180, 0, 0);
            deckPileList.Add(newCard);
        }

        //setup first card
        activeCardData = deckManager.Draw();
        activeCardObj = GetTopCardFromDeckPile();

        activeCardObj.transform.position = activeCardPoint.position;
        activeCardObj.transform.rotation = activeCardPoint.rotation;
        
        ApplyTexture(activeCardObj, activeCardData);

        SpawnNextCardAtDeck();
    }

   
    public void OnPlayAgainClicked()
    {
        playAgainBtn.interactable = false;
        quitBtn.interactable = false;

        fadePanel.raycastTarget = true; 
        //fades the scene in and out for a new game
        Sequence seq = DOTween.Sequence();
        seq.Append(fadePanel.DOFade(1, 1.0f));
        seq.AppendCallback(() => StartGame());
        seq.AppendInterval(1.0f);
        seq.Append(fadePanel.DOFade(0, 1.0f));
        seq.OnComplete(() => {
            fadePanel.raycastTarget = false;
            playAgainBtn.interactable = true; 
            quitBtn.interactable = true;
        });
    }

    
    public void OnQuitClicked()
    {
        playAgainBtn.interactable = false;
        quitBtn.interactable = false;

        fadePanel.raycastTarget = true;

        //fade out and load main menu
        fadePanel.DOFade(1, 1.0f).OnComplete(() => {
            SceneManager.LoadScene(0); 
        });
    }

    GameObject GetTopCardFromDeckPile()
    {
        if (deckPileList.Count == 0) return null;
        int lastIndex = deckPileList.Count - 1;
        GameObject card = deckPileList[lastIndex];
        deckPileList.RemoveAt(lastIndex);
        return card;
    }

    void SpawnNextCardAtDeck()
    {
        //gets the next card
        higherBtn.interactable = true;
        lowerBtn.interactable = true;
        
        resultText.text = "Higher or Lower?";

        pendingCardData = deckManager.Draw();
        
        if(pendingCardData == null) 
        { 
            resultText.text = "You Win!";
            GameOver(); 
            return; 
        }

        pendingCardObj = GetTopCardFromDeckPile();
        if (pendingCardObj == null) return;

        ApplyTexture(pendingCardObj, pendingCardData);

        Sequence seq = DOTween.Sequence();
        seq.Append(pendingCardObj.transform.DOMove(cameraInspectPoint.position, 0.6f).SetEase(Ease.OutQuad));
        Vector3 faceDownRot = cameraInspectPoint.rotation.eulerAngles + new Vector3(180, 0, 0);
        seq.Join(pendingCardObj.transform.DORotate(faceDownRot, 0.5f));
    }

    public void OnGuessHigher() { isGuessingHigher = true; RevealCard(); }
    public void OnGuessLower() { isGuessingHigher = false; RevealCard(); }

    void RevealCard()
    {
        //rotates card to reveal higher or lower
        higherBtn.interactable = false;
        lowerBtn.interactable = false;

        pendingCardObj.transform.DORotate(cameraInspectPoint.rotation.eulerAngles, 0.4f)
            .OnComplete(ProcessResult);
    }

    void ProcessResult()
    {
        //handles ties
        if (pendingCardData.value == activeCardData.value)
        {
            resultText.text = "It's a Tie! (Round Saved)";
            CycleCards();
            return;
        }

        bool isCorrect = false;
        if (isGuessingHigher && pendingCardData.value > activeCardData.value) isCorrect = true;
        else if (!isGuessingHigher && pendingCardData.value < activeCardData.value) isCorrect = true;
        
        //game continues if guessed correctly and ends if not
        if (isCorrect)
        {
            score++;
            scoreText.text = "Score: " + score;
            resultText.text = "Correct!";
            CycleCards(); 
        }
        else
        {
            GameOver();
        }
    }

    void GameOver()
    {
        resultText.text = "Game Over!";
        
        //show quit and play again buttons
        playAgainBtn.gameObject.SetActive(true); 
        quitBtn.gameObject.SetActive(true);
    }

    void CycleCards()
    {
        //moves active card to discard pile and guessed card to active slot
        discardedCardsList.Add(activeCardObj);

        float stackHeight = discardedCardsList.Count * 0.005f; 
        Vector3 targetPilePos = discardPilePoint.position + new Vector3(0, stackHeight, 0);

        activeCardObj.transform.DOJump(targetPilePos, 0.3f, 1, 0.5f).SetEase(Ease.OutQuad);
        activeCardObj.transform.DORotate(discardPilePoint.rotation.eulerAngles + new Vector3(0, Random.Range(-20, 20), 0), 0.5f);

        Sequence seq = DOTween.Sequence();
        seq.Append(pendingCardObj.transform.DOJump(activeCardPoint.position, 0.5f, 1, 0.5f).SetEase(Ease.OutQuad));
        seq.Join(pendingCardObj.transform.DORotate(activeCardPoint.rotation.eulerAngles, 0.45f));

        seq.OnComplete(() => {
            activeCardObj = pendingCardObj; 
            activeCardData = pendingCardData;
            pendingCardObj = null; 
            SpawnNextCardAtDeck();
        });
    }

    void ApplyTexture(GameObject cardObj, CardData data)
    {
        cardObj.transform.GetChild(0).GetComponent<Renderer>().material.mainTexture = data.cardImage.texture;
    }
}