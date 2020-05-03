using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardBehaviour : MonoBehaviour
{
    public int Value;
    public GameObject CardText;

    public void Create(int value)
    {
        Value = value;
        CardText.GetComponent<TextMeshPro>().text = value.ToString();
    }
}
