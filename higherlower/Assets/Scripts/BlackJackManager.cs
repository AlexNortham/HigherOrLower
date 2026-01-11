using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BlackjackManager : MonoBehaviour
{
    [Header("References")]
    public DeckManager deckManager;
    public GameObject cardPrefab;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI dealerScoreText;
    public TextMeshProUGUI resultText;
    
    [Header("Buttons")]
    public Button hitBtn;
    public Button standBtn;
    public Button playAgainBtn;
    public Button quitBtn;
    public Image fadePanel; 

    [Header("Anchors")]
    public Transform deckSpawnPoint;
    public Transform playerHandOrigin; 
    public Transform dealerHandOrigin; 
    public Transform discardPilePoint;


    private List<CardData> playerHandData = new List<CardData>();
    private List<CardData> dealerHandData = new List<CardData>();
 
    private List<GameObject> activeCardObjects = new List<GameObject>(); 
    private List<GameObject> deckPileList = new List<GameObject>();      
    private GameObject hiddenDealerCardObj; 

    private int playerScore = 0;
    private int dealerScore = 0;
    private bool isPlayerTurn = false;

    void Start()
    {
        //play again and quit buttons hidden initially
        playAgainBtn.gameObject.SetActive(false);
        quitBtn.gameObject.SetActive(false); 

        hitBtn.interactable = false;
        standBtn.interactable = false;

        //fade in
        fadePanel.gameObject.SetActive(true);
        fadePanel.color = Color.black;
        fadePanel.DOFade(0, 1.0f).OnComplete(() => fadePanel.raycastTarget = false);

        deckManager.InitialiseDeck();
        StartGame();
    }

    void StartGame()
    {
        //make sure no cards from previous games still stored
        foreach (GameObject c in activeCardObjects) Destroy(c);
        activeCardObjects.Clear();
        foreach (GameObject c in deckPileList) Destroy(c);
        deckPileList.Clear();

        playerHandData.Clear();
        dealerHandData.Clear();
        resultText.text = "";
        
        playAgainBtn.gameObject.SetActive(false);
        quitBtn.gameObject.SetActive(false);

        isPlayerTurn = false; 

        
        deckManager.InitialiseDeck(); 

        //create card pile
        for (int i = 0; i < 52; i++)
        {
            Vector3 stackPos = deckSpawnPoint.position + new Vector3(0, i * 0.005f, 0);
            GameObject newCard = Instantiate(cardPrefab, stackPos, deckSpawnPoint.rotation);
            newCard.transform.Rotate(180, 0, 0); 
            deckPileList.Add(newCard);
        }

        //deal cards
        Sequence dealSeq = DOTween.Sequence();
        dealSeq.AppendCallback(() => DealCardTo(true, true));  
        dealSeq.AppendInterval(0.5f);
        dealSeq.AppendCallback(() => DealCardTo(false, true)); 
        dealSeq.AppendInterval(0.5f);
        dealSeq.AppendCallback(() => DealCardTo(true, true));  
        dealSeq.AppendInterval(0.5f);
        dealSeq.AppendCallback(() => DealCardTo(false, false)); 

        dealSeq.OnComplete(() => {
            isPlayerTurn = true; 
            CalculateScores();   
            
            if (playerScore == 21) { GameOver("Blackjack! You Win!"); }
            else 
            {
                hitBtn.interactable = true;
                standBtn.interactable = true;
            }
        });
    }


    public void OnPlayAgainClicked()
    {
        //restart logic
        playAgainBtn.interactable = false;
        quitBtn.interactable = false;
        ReshuffleAndRestart();
    }

    public void OnQuitClicked()
    {
        playAgainBtn.interactable = false;
        quitBtn.interactable = false;
        //loads main menu scene
        fadePanel.raycastTarget = true;
        fadePanel.DOFade(1, 1.0f).OnComplete(() => {
            SceneManager.LoadScene(0);
        });
    }

    void ReshuffleAndRestart()
    {
        Sequence shuffleSeq = DOTween.Sequence();

        //moves card objects back to deck
        foreach (GameObject card in activeCardObjects)
        {
            shuffleSeq.Join(card.transform.DOJump(deckSpawnPoint.position, 0.5f, 1, 0.5f).SetEase(Ease.InCubic));
            Vector3 faceDownRot = deckSpawnPoint.rotation.eulerAngles + new Vector3(180, 0, 0);
            shuffleSeq.Join(card.transform.DORotate(faceDownRot, 0.45f));
        }

        //restarts game
        shuffleSeq.OnComplete(() => {
            playAgainBtn.interactable = true; 
            quitBtn.interactable = true;
            StartGame();
        });
    }

    public void OnHitClicked()
    {
        //deals player a card and checks if score is over 21
        hitBtn.interactable = false; 
        DealCardTo(true, true);
        
        DOVirtual.DelayedCall(0.6f, () => {
            CalculateScores();
            if (playerScore > 21) GameOver("Bust! You Lose.");
            else hitBtn.interactable = true;
        });
    }

    public void OnStandClicked()
    {
        //stops the players turn and begins the dealers turn
        isPlayerTurn = false; 
        hitBtn.interactable = false;
        standBtn.interactable = false;
        StartCoroutine(DealerTurnRoutine());
    }

    System.Collections.IEnumerator DealerTurnRoutine()
    {
        resultText.text = "Dealer's Turn...";
        RevealDealerCard();
        CalculateScores(); 
        
        yield return new WaitForSeconds(1.0f);

        //dealer logic - will hit if has less than 17 score
        while (dealerScore < 17)
        {
            DealCardTo(false, true); 
            yield return new WaitForSeconds(0.8f);
            CalculateScores();
        }

        //logic for comparing scores
        if (dealerScore > 21) GameOver("Dealer Busts! You Win!");
        else if (dealerScore > playerScore) GameOver("Dealer Wins!");
        else if (dealerScore < playerScore) GameOver("You Win!");
        else GameOver("Push (Tie).");
    }


    GameObject GetTopCardFromDeckPile()
    {
        if (deckPileList.Count == 0) return null;
        int lastIndex = deckPileList.Count - 1;
        GameObject card = deckPileList[lastIndex];
        deckPileList.RemoveAt(lastIndex);
        return card;
    }

    void DealCardTo(bool isPlayer, bool faceUp)
    {
        //deals to player or dealer based on bool isPlayer
        CardData cardData = deckManager.Draw();
        if (isPlayer) playerHandData.Add(cardData);
        else dealerHandData.Add(cardData);

        Transform origin = isPlayer ? playerHandOrigin : dealerHandOrigin;
        int handIndex = isPlayer ? playerHandData.Count - 1 : dealerHandData.Count - 1;
        
        Vector3 offset = new Vector3(handIndex * 0.15f, 0, 0); 

        //gets the card object
        GameObject newCard = GetTopCardFromDeckPile();
        if (newCard == null) 
            newCard = Instantiate(cardPrefab, deckSpawnPoint.position, deckSpawnPoint.rotation);

        if (faceUp) ApplyTexture(newCard, cardData);
        else hiddenDealerCardObj = newCard; 

        activeCardObjects.Add(newCard); 

        //handles card movement
        Sequence seq = DOTween.Sequence();
        seq.Append(newCard.transform.DOJump(origin.position + offset, 0.5f, 1, 0.5f));
        
        if (faceUp) seq.Join(newCard.transform.DORotate(origin.rotation.eulerAngles, 0.45f));
        else
        {
            Vector3 faceDownRot = origin.rotation.eulerAngles + new Vector3(180, 0, 0);
            seq.Join(newCard.transform.DORotate(faceDownRot, 0.45f));
        }
    }

    void RevealDealerCard()
    {
        if (hiddenDealerCardObj != null)
        {
            ApplyTexture(hiddenDealerCardObj, dealerHandData[1]);
            hiddenDealerCardObj.transform.DORotate(dealerHandOrigin.rotation.eulerAngles, 0.5f);
        }
    }

    void CalculateScores()
    {
        //handles what scores to show at what time
        playerScore = GetHandValue(playerHandData);
        dealerScore = GetHandValue(dealerHandData);

        playerScoreText.text = "Player: " + playerScore;
        
        if (isPlayerTurn) dealerScoreText.text = "Dealer: " + GetCardValue(dealerHandData[0]); 
        else dealerScoreText.text = "Dealer: " + dealerScore;
    }

    int GetHandValue(List<CardData> hand)
    {
        //handles ace logic
        int total = 0;
        int aces = 0;
        foreach (CardData card in hand)
        {
            int val = GetCardValue(card);
            if (val == 11) aces++;
            total += val;
        }
        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }
        return total;
    }

    int GetCardValue(CardData card)
    {
        //handles card scores - (face cards are 10, aces 11)
        if (card.value >= 10 && card.value <= 13) return 10; 
        if (card.value == 14) return 11; 
        return card.value; 
    }

    void GameOver(string message)
    {
        //disables game buttons and enables play again and quit buttons
        isPlayerTurn = false;
        hitBtn.interactable = false;
        standBtn.interactable = false;
        resultText.text = message;
        
        playAgainBtn.gameObject.SetActive(true);
        quitBtn.gameObject.SetActive(true);
    }

    void ApplyTexture(GameObject cardObj, CardData data)
    {
        cardObj.transform.GetChild(0).GetComponent<Renderer>().material.mainTexture = data.cardImage.texture;
    }
}