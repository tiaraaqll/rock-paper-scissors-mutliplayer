using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public Attack attackValue;
    public CardPlayer player;
    public Vector2 OriginalPositition;
    Vector2 originalScale;
    Color originalColor;
    bool isClickable = true;

    private void Start() 
    {
        OriginalPositition = this.transform.position;
        originalScale = this.transform.localScale;
        originalColor = GetComponent<Image>().color;

        //* memindahkan objeect scecara animasi
            // transform.DOMove(atkPosRef.position, 5).SetLoops(-1, LoopType.Yoyo);
            // var seq = DOTween.Sequence();
            // seq.Append(transform.DOMove(atkPosRef.position, 1));
            // seq.Append(transform.DOMove(startPositition, 1));
    }

    public void OnClick()
    {
         if (isClickable)
         player.SetChosenCard(this);
    }

    internal void Reset()
    {
        transform.position = OriginalPositition;
        transform.localScale = originalScale;
        GetComponent<Image>().color = originalColor;
    }

    public void setClickable(bool value)
    {
        isClickable = value;
    }

    //* Teori Twining
    // float timer = 0;
    // private void Update()
    // {
    //* Teori Twining
    // if (timer < 1)
    // {
    //     var newX = Linear(startPositition.x, attackPosRef.position.x, timer);
    //     var newY = Linear(startPositition.y, attackPosRef.position.y, timer);
    //     this.transform.position = new Vector2(newX, newY);
    //     timer += Time.deltaTime;
    // }
    // else
    // {
    //     timer = 0;
    // }
    // }
    //* Teori Twining
    // private float Linear(float start, float end, float time)
    // {
    //     return (end - start) * time * start;
    // }

}
