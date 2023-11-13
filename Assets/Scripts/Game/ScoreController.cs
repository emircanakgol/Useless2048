using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public static ScoreController instance;

    

    private Action OnScoreChangedCallback;

    private int _score;
    public int Score {
        get{
            return _score;
        }
        set{
            if(_score != value){
                _score = value;
                OnScoreChangedCallback();
            }
        }
    }

    [SerializeField]
    private TextMeshProUGUI scoreTMP;
    [SerializeField]
    private RectTransform logoTransform;

    private void Awake(){
        if(instance == null)
            instance = this;
    }

    private void OnEnable(){
        RegisterOnScoreChangedCallback(OnScoreChangedUI);
    }

    private void OnDisable(){
        UnregisterOnScoreChangedCallback(OnScoreChangedUI);
    }

    private void OnScoreChangedUI(){
        DOTween.Kill(scoreTMP.rectTransform);
        DOTween.Kill(logoTransform);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(scoreTMP.rectTransform.DOScale(1.2f, 0.3f).OnComplete(()=>scoreTMP.text = Score.ToString()));
        sequence.Join(scoreTMP.DOColor(Color.white, 0.3f));
        sequence.Join(logoTransform.DOScale(1.1f, 0.3f).SetDelay(0.3f));
        sequence.Append(scoreTMP.rectTransform.DOScale(1, 0.3f));
        sequence.Join(scoreTMP.DOColor(new Color(1, 175f/255, 78f/255, 1), 0.3f));
        sequence.Join(logoTransform.DOScale(1, 0.3f).SetDelay(0.3f));
    }

    public void RegisterOnScoreChangedCallback(Action action){
        OnScoreChangedCallback += action;
    }
    public void UnregisterOnScoreChangedCallback(Action action){
        OnScoreChangedCallback -= action;
    }
}
