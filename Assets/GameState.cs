using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using System.Linq;

public class GameState : MonoBehaviour
{

    public GameObject CardPrefabTemplate;
    public List<GameObject> Cards;
    public GameObject VictoryText;

    public GameObject FirstPickedCard;
    public GameObject SecondPickedCard;

    public Button RestartButton;
    public InputField PairInput;

    public int NumberOfPairs;
    private int _savedNumberOfPairs;

    public State CurrentState;
    public int FirstPickValue;
    public int SecondPickValue;

    public Vector3 CurrentCardPosition;
    public Quaternion CardQuaternion;

    public Quaternion CardFaceupRotation;
    public Quaternion CardFacedownRotation;
    public float CardRotateDuration;

    public enum State
    {
        FirstCardChosen, SecondCardChosen, NoneChosen, GameWon
    }

    void Start()
    {
        // fix this saving between reloads
        // maybe dont reload scene and just do a seperate BuildScene()
        if (_savedNumberOfPairs == 0)
        {
            _savedNumberOfPairs = 6;
        }

        NumberOfPairs = _savedNumberOfPairs;

        CurrentState = State.NoneChosen;
        CurrentCardPosition = new Vector3(0, 0, 0);
        CardQuaternion = new Quaternion(0, 180f, 0, 0);

        CardFaceupRotation = Quaternion.Euler(0, 0, 0);
        CardFacedownRotation = Quaternion.Euler(0, 180f, 0);
        CardRotateDuration = 0.3f;

        PairInput.onEndEdit.AddListener(delegate { HandlePairInput(PairInput.text); });
        RestartButton.onClick.AddListener(delegate { Restart(); });

        SetupCards();
        SetCameraPosition();
    }

    void HandlePairInput(string input)
    {
        Int32.TryParse(PairInput.text, out int pairs);
        if (pairs == 0)
        {
            pairs = 6;
        }

        NumberOfPairs = pairs;
        _savedNumberOfPairs = pairs;
    }

    void SetCameraPosition()
    {
        var targetBounds = new Bounds(transform.position, Vector3.one);
        foreach (var card in Cards)
        {
            targetBounds.Encapsulate(card.GetComponent<Renderer>().bounds);
        }

        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = targetBounds.size.x / targetBounds.size.y;

        if (screenRatio >= targetRatio)
        {
            Camera.main.orthographicSize = targetBounds.size.y / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            Camera.main.orthographicSize = targetBounds.size.y / 2 * differenceInSize;
        }

        Camera.main.orthographicSize += 1;
        Camera.main.transform.position = new Vector3(targetBounds.center.x, targetBounds.center.y, -1f);
    }

    void SetupCards()
    {
        // Create two cards for every pair and put them in a temporary array
        var cardValues = new List<int>();
        for (int i = 0; i < NumberOfPairs; i++)
        {
            var cardValue = i + 1;
            for (int y = 0; y < 2; y++)
            {
                cardValues.Add(cardValue);
            }
        }
        Shuffle(cardValues);


        var (columns, rows) = CalculateGrid();
        var firstPass = true;
        for (int i = 0; i < rows; i++)
        {
            for (int y = 0; y < columns; y++)
            {
                // if first pass, reset x value to start of row
                if (firstPass)
                {
                    CurrentCardPosition.x = 0f;
                    firstPass = false;
                }

                var cardValue = cardValues[0];
                cardValues.RemoveAt(0);

                var card = Instantiate(CardPrefabTemplate, CurrentCardPosition, CardQuaternion);
                card.SetActive(true);
                card.GetComponent<CardBehaviour>().Create(cardValue);
                Cards.Add(card);
                CurrentCardPosition.x += 1.1f;
            }
            CurrentCardPosition.y += 1.1f;
            firstPass = true;
        }
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
                    Restart();
                    break;
            }
        }
    }

    void PickCard(State targetState)
    {
        var cardObject = GetClickedCard();
        if (cardObject == null)
        {
            return;
        }

        // Don't allow the user to pick the same card twice
        if (GameObject.ReferenceEquals(cardObject, FirstPickedCard)
            || GameObject.ReferenceEquals(cardObject, SecondPickedCard))
        {
            return;
        }

        switch (targetState)
        {
            case State.FirstCardChosen:
                FirstPickValue = cardObject.GetComponent<CardBehaviour>().Value;
                FirstPickedCard = cardObject;
                StartCoroutine(RotateCard(CardFaceupRotation, cardObject));
                CurrentState = targetState;
                break;
            case State.SecondCardChosen:
                SecondPickValue = cardObject.GetComponent<CardBehaviour>().Value;
                SecondPickedCard = cardObject;
                StartCoroutine(RotateCard(CardFaceupRotation, cardObject));
                CurrentState = targetState;
                break;
        }
    }

    GameObject GetClickedCard()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100))
        {
            return hit.transform.gameObject;
        }
        else
        {
            return null;
        }
    }

    void FacedownCards()
    {
        // Reset all flipped cards into being facedown
        var cardsToFlip = new List<GameObject>();
        foreach (var card in Cards)
        {
            var rotation = card.transform.eulerAngles.y;
            if (rotation != 180f)
            {
                cardsToFlip.Add(card);
            }
        }

        if (cardsToFlip.Count != 0)
        {
            foreach (var card in cardsToFlip)
            {
                StartCoroutine(RotateCard(CardFacedownRotation, card));
                //card.transform.rotation *= Quaternion.Euler(0, -180f, 0);
            }
        }
    }

    void RemoveMatchingCards()
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
    }

    void IsMatch()
    {
        var isMatch = FirstPickValue == SecondPickValue;
        if (!isMatch)
        {
            FacedownCards();
            CurrentState = State.NoneChosen;
        }
        else
        {
            RemoveMatchingCards();
            CurrentState = State.NoneChosen;
        }

        FirstPickValue = 0;
        FirstPickedCard = null;
        SecondPickValue = 0;
        SecondPickedCard = null;
    }

    void CheckWin()
    {
        if (Cards.Count == 0)
        {
            VictoryText.SetActive(true);
            CurrentState = State.GameWon;
        }
    }

    void Shuffle<T>(IList<T> list)
    {
        var rng = new System.Random();
        var n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    (int, int) CalculateGrid()
    {
        var totalCards = NumberOfPairs * 2;
        var columns = (int)Math.Round(Math.Sqrt(totalCards));
        while (totalCards % columns > 0)
        {
            columns -= 1;
        }
        var rows = totalCards / columns;

        return (columns, rows);
    }

    IEnumerator RotateCard(Quaternion endRotation, GameObject card)
    {
        var startRotation = card.transform.rotation;
        for (float t = 0; t < CardRotateDuration; t += Time.deltaTime)
        {
            card.transform.rotation = Quaternion.Lerp(startRotation, endRotation, t / CardRotateDuration);
            yield return null;
        }
        card.transform.rotation = endRotation;
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}