using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : MonoBehaviour
{

    public List<GameObject> Cards;
    public GameObject VictoryText;

    public State CurrentState;
    public int FirstPickValue;
    public int SecondPickValue;

    public enum State
    {
        FirstCardChosen, SecondCardChosen, NoneChosen, GameWon
    }

    void Start()
    {
        CurrentState = State.NoneChosen;
    }

    void Update()
    {
        // Click on a card
        if (Input.GetMouseButtonDown(0))
        {
            var targetState = State.NoneChosen;
            switch (CurrentState)
            {
                case State.NoneChosen:
                    targetState = State.FirstCardChosen;
                    PickCard(targetState);
                    break;
                case State.FirstCardChosen:
                    targetState = State.SecondCardChosen;
                    PickCard(targetState);
                    break;
                case State.SecondCardChosen:
                    targetState = State.NoneChosen;
                    IsMatch();
                    CheckWin();
                    break;
                case State.GameWon:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    break;
            }
        }
    }

    void PickCard(State targetState)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100))
        {
            var cardObject = hit.transform.gameObject;
            switch (targetState)
            {
                case State.FirstCardChosen:
                    FirstPickValue = cardObject.GetComponent<CardBehaviour>().Value;
                    cardObject.transform.rotation *= Quaternion.Euler(0, 180f, 0);
                    CurrentState = targetState;
                    break;
                case State.SecondCardChosen:
                    SecondPickValue = cardObject.GetComponent<CardBehaviour>().Value;
                    cardObject.transform.rotation *= Quaternion.Euler(0, 180f, 0);
                    CurrentState = targetState;
                    break;
            }
        }
    }

    void IsMatch()
    {
        var isMatch = FirstPickValue == SecondPickValue;
        if (!isMatch)
        {
            // Reset all flipped cards into being facedown
            var cardsToFlip = new List<GameObject>();
            foreach (var card in Cards)
            {
                var rotation = card.transform.eulerAngles.y;
                if (rotation != 180)
                {
                    cardsToFlip.Add(card);
                }
            }

            if (cardsToFlip.Count != 0)
            {
                foreach (var card in cardsToFlip)
                {
                    card.transform.rotation *= Quaternion.Euler(0, 180f, 0);
                }
            }
            CurrentState = State.NoneChosen;
        }
        else
        {
            // remove chosen cards
            foreach (var card in Cards)
            {
                var value = card.GetComponent<CardBehaviour>().Value;
                if (value == FirstPickValue)
                {
                    Destroy(card);
                }
            }

            Cards.RemoveAll(c => c.GetComponent<CardBehaviour>().Value == FirstPickValue);

            CurrentState = State.NoneChosen;
        }

        FirstPickValue = 0;
        SecondPickValue = 0;
    }

    void CheckWin()
    {
        if (Cards.Count == 0)
        {
            VictoryText.SetActive(true);
            CurrentState = State.GameWon;
        }
    }
}