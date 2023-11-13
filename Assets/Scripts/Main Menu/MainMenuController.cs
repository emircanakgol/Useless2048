using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private RectTransform logoRT;
    [SerializeField]
    private RectTransform descRT;
    [SerializeField]
    private RectTransform hintRT;
    [SerializeField]
    private Image bg;

    void Start(){
        logoRT.DOScale(1.2f, 1).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutQuad);
        descRT.DOScale(1.2f, 1.2f).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutQuad);
        hintRT.DOScale(1.2f, 1.1f).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutQuad);
    }

    void Update(){
        if(Input.GetMouseButtonDown(0)){
            bg.DOColor(new Color(0,0,0,1), 1).OnComplete(()=>SceneManager.LoadScene("Main"));
        }
    }
}
