using System.Collections.Generic;
using UnityEngine;

public class DeckManager: MonoBehaviour{
    public List<Sprite> allCardSprites;
    public List<CardData> currentDeck = new List<CardData>();
    public void InitialiseDeck(){
        //create all cards
        currentDeck.Clear();
        string[] suits = { "Clubs", "Diamonds", "Hearts", "Spades"};
        int spriteIndex = 0;
        foreach (string suit in suits){
            for(int val = 2; val <= 14; val++){
                CardData newCard = new CardData();
                newCard.value = val;
                newCard.cardName = val + " of " + suit;

                if(spriteIndex < allCardSprites.Count){
                    newCard.cardImage = allCardSprites[spriteIndex];
                    spriteIndex++;
                }

                currentDeck.Add(newCard);
            }
        }
        Shuffle();
    }

    public void Shuffle(){
        //shuffle cards
        for(int i = 0; i < currentDeck.Count; i++){
            CardData temp = currentDeck[i];
            int ran = Random.Range(0, currentDeck.Count);
            currentDeck[i] = currentDeck[ran];
            currentDeck[ran] = temp;
        }
    }

    public CardData Draw(){
        //draws top card
        if(currentDeck.Count == 0){
            return null;
        }

        CardData drawnCard = currentDeck[0];
        currentDeck.RemoveAt(0);
        return drawnCard;
    }
}
