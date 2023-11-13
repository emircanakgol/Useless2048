using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public static int count = 0;
    public int ID {get; private set;}

    public Action<Tile> OnStopCallback;
    public Action<Tile,Vector3> OnMergeDestroyCallback;
    public Action<Tile> OnMergeAddCallback;
    public Action<Tile> OnValueChangedCallback;
    private int _index = -1;
    public int Index {
        get{
            return _index;
        }
        set{
            _index = value;
            SetPosition(_index);
        }
    }

    private int _value = -1;
    public int Value {
        get{
            return _value;
        }
        set{
            if(_value != value){
                _value = value;
                SetValue(_value);
                OnValueChangedCallback(this);
            }
        }
    }
    
    private bool _isEmpty;
    public bool IsEmpty {
        get{
            return _isEmpty;
        }
        set{
            if(_isEmpty != value){
                _isEmpty = value;
                if(_isEmpty){
                    SetValue(0);
                }
            }
        }
    }
    public Tile above;
    public Tile below;
    public Tile right;
    public Tile left;

    public bool Ready;

    public Vector3 SwipeDirection;

    private bool firstStart = false;

    [SerializeField]
    private TextMeshPro numberTMP;
    public AudioSource collisionAudio;

    private TileController tileController;
    private Rigidbody rb;

    private float destroyTweenDuration;


    private void Awake(){
        tileController = TileController.instance;
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable(){
        OnStopCallback += SetPosition;
        OnMergeDestroyCallback += OnMergeDestroy;
        OnValueChangedCallback += OnValueChanged;
        OnMergeAddCallback += OnValueChanged;
    }

    private void OnDisable(){
        OnStopCallback -= SetPosition;
        OnMergeDestroyCallback -= OnMergeDestroy;
        OnValueChangedCallback -= OnValueChanged;
        OnMergeAddCallback -= OnValueChanged;
    }

    void Start(){
        StartCoroutine(WaitForFirstIndex());
        destroyTweenDuration = tileController.destroyTweenDuration;
        AnimateOnStart();
        ID = count;
        count++;
    }
    private void AnimateOnStart(){
        Renderer renderer = this.GetComponentInChildren<Renderer>();
        Material tileMat = renderer.material;
        tileMat.color = new Color(tileMat.color.r, tileMat.color.g, tileMat.color.b, 0);
        Transform tileTr = renderer.transform;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(tileTr.DOScale(Vector3.one,destroyTweenDuration*2));
        sequence.Join(tileMat.DOFade(1,destroyTweenDuration));
        sequence.Append(tileTr.DOScale(new Vector3(0.8f, 0.8f, 1),destroyTweenDuration));
    }

    private IEnumerator WaitForFirstIndex(){
        yield return new WaitForSeconds(0.5f);
        firstStart = true;
    }

    private void Update(){
        if(!firstStart) return;
    }
    
    public bool IsWonky(){
        if(transform.position.x % 1 != 0)
            return true;
        if(transform.position.z % 1 != 0)
            return true;
        return false;
    }
    private Vector3 CalculatePosition(int index){
        int x = index % 4;
        int z = Mathf.FloorToInt((float)index/4f);

        return new Vector3(x, 0, z);
    }

    private void SetPosition(Tile tile){
        rb.velocity = Vector3.zero;
        int roundedXPos = Mathf.RoundToInt(transform.localPosition.x);
        int roundedZPos = Mathf.RoundToInt(transform.localPosition.z);
        Index = roundedZPos * 4 + roundedXPos;
        RefreshAdjacentTiles();
        CheckPosition();
        Invoke(nameof(CheckIfOnTop), 0.2f);
    }

    private void SetPosition(int index){
        gameObject.name = "Tile_" + index.ToString();
        rb.velocity = Vector3.zero;
        transform.DOLocalMove(CalculatePosition(index), 0.3f).SetEase(Ease.OutSine);
    }

    private void SetValue(int value){
        if(value == 0){
            numberTMP.enabled = false;
        }
        else{
            numberTMP.enabled = true;
        }
        
        numberTMP.text = Mathf.RoundToInt(Mathf.Pow(2, (float)value)).ToString();
        if(value >= tileController.tileMaterials.Count){
            GetComponentInChildren<Renderer>().material = tileController.tileMaterials[tileController.tileMaterials.Count - 1];
        }
        else{
            GetComponentInChildren<Renderer>().material = tileController.tileMaterials[value];
        }
    }

    private void OnCollisionEnter(Collision collision){
        collisionAudio.pitch = UnityEngine.Random.Range(0.75f, 1.25f);
        collisionAudio.Play();
        if(collision.gameObject.CompareTag("Tile")){
            Tile tile = collision.gameObject.GetComponent<Tile>();
            if(tile.Value != Value) return;

            if(SwipeDirection == Vector3.left){
                if(tile.transform.position.x < transform.position.x)
                    MergeTiles(this, tile, Vector3.left);
                else
                    MergeTiles(tile, this, Vector3.left);
            }
            if(SwipeDirection == Vector3.right){
                if(tile.transform.position.x > transform.position.x)
                    MergeTiles(this, tile, Vector3.right);
                else
                    MergeTiles(tile, this, Vector3.right);
            }
            if(SwipeDirection == Vector3.forward){
                if(tile.transform.position.z > transform.position.z)
                    MergeTiles(this, tile, Vector3.forward);
                else
                    MergeTiles(tile, this, Vector3.forward);
            }
            if(SwipeDirection == Vector3.back){
                if(tile.transform.position.z < transform.position.z)
                    MergeTiles(this, tile, Vector3.back);
                else
                    MergeTiles(tile, this, Vector3.back);
            }
        }
    }

    private void MergeTiles(Tile from, Tile to, Vector3 direction){
        if(from == null || to == null) return;
        foreach(Collider collider in from.GetComponentsInChildren<Collider>()){
            collider.enabled = false;
        }
        from.Value = 0;
        to.Value++;
        ScoreController.instance.Score += to.Value;
        tileController.PlayMergeAudios();
        from.OnMergeDestroyCallback(from, direction);
        to.OnMergeAddCallback(to);
    }

    public void RefreshAdjacentTiles(){
        above = below = right = left = null;
        above = GetTileAbove();
        if(above!=null)
            above.below = this;

        below = GetTileBelow();
        if(below!=null)
            below.above = this;

        right = GetTileRight();
        if(right!=null)
            right.left = this;
        
        left = GetTileLeft();
        if(left!=null)
            left.right = this;
    }

    public Tile GetTileAbove(){
        foreach(Tile tile in tileController.tiles){
            if(tile.Index == Index + 4)
                return tile;
        }
        return null;
    }

    public Tile GetTileBelow(){
        foreach(Tile tile in tileController.tiles){
            if(tile.Index == Index - 4)
                return tile;
        }
        return null;
    }

    public Tile GetTileLeft(){
        if(Index%4==0)
            return null;
        foreach(Tile tile in tileController.tiles){
            if(tile.Index == Index - 1)
                return tile;
        }
        return null;
    }

    public Tile GetTileRight(){
        if(Index%4==3)
            return null;
        foreach(Tile tile in tileController.tiles){
            if(tile.Index == Index + 1)
                return tile;
        }
        return null;
    }

    private void CheckIfOnTop(){
        Dictionary<Tile, int> tilesToMove = new();
        List<Tile> tilesToDestroy = new();
        foreach(Tile tile in tileController.tiles){
            if(tile.ID != ID && tile.Index == Index){
                if(Index + 1 < 16 && !tileController.tileIndexes.Contains(Index + 1))
                    tilesToMove.Add(tile,Index + 1);
                else if(Index - 1 > 16 && !tileController.tileIndexes.Contains(Index - 1))
                    tilesToMove.Add(tile,Index - 1);
                else if(Index + 4 < 16 && !tileController.tileIndexes.Contains(Index + 4))
                    tilesToMove.Add(tile,Index + 4);
                else if(Index - 4 < 16 && !tileController.tileIndexes.Contains(Index - 4))
                    tilesToMove.Add(tile,Index - 4);
                else{
                    if(tile.ID > ID)
                        tilesToDestroy.Add(tile);
                }
            }
        }
        if(tilesToDestroy.Count == 0 && tilesToMove.Count == 0)
            return;
        foreach(KeyValuePair<Tile,int> keyValuePair in tilesToMove){
            keyValuePair.Key.Index = keyValuePair.Value;
        }
        foreach(Tile tile1 in tilesToDestroy){
            StartCoroutine(WaitAndDestroy(tile1.gameObject));
        }
        tileController.tileIndexes.Clear();
        foreach(Tile tile in tileController.tiles){
            tileController.tileIndexes.Add(tile.Index);
        }
    }

    private void CheckPosition(){
        if(transform.localPosition.x < -0.9f || transform.localPosition.x > 3.9f || transform.localPosition.z < -0.9f || transform.localPosition.z > 3.9f){
            StartCoroutine(WaitAndDestroy(gameObject));
        }
    }

    private void OnMergeDestroy(Tile tile, Vector3 direction){
        if(tile==null) return;
        Material tileMat = tile.GetComponentInChildren<Renderer>().material;
        Transform tileTr = tile.transform;
        tileController.tiles.Remove(tile);
        Sequence sequence = DOTween.Sequence(tile.gameObject);

        sequence.Append(tileMat.DOColor(new Color(2,2,2,1), destroyTweenDuration));
        if(direction == Vector3.left){
            sequence.Join(tileTr.DOScaleX(0, destroyTweenDuration));
            sequence.Join(tileTr.DOLocalMoveX(tileTr.localPosition.x - 0.5f, destroyTweenDuration));
        }
        if(direction == Vector3.right){
            sequence.Join(tileTr.DOScaleX(0, destroyTweenDuration));
            sequence.Join(tileTr.DOLocalMoveX(tileTr.localPosition.x + 0.5f, destroyTweenDuration));
        }
        if(direction == Vector3.forward){
            sequence.Join(tileTr.DOScaleZ(0, destroyTweenDuration));
            sequence.Join(tileTr.DOLocalMoveZ(tileTr.localPosition.z + 0.5f, destroyTweenDuration));
        }
        if(direction == Vector3.back){
            sequence.Join(tileTr.DOScaleZ(0, destroyTweenDuration));
            sequence.Join(tileTr.DOLocalMoveZ(tileTr.localPosition.z - 0.5f, destroyTweenDuration));
        }
        sequence.OnComplete(()=>StartCoroutine(WaitAndDestroy(tile.gameObject)));
    }

    private IEnumerator WaitAndDestroy(GameObject go){
        yield return new WaitForSeconds(0.1f);
        Destroy(go);
    }

    private void OnValueChanged(Tile tile){
        if(tile==null) return;
        Renderer renderer = tile.GetComponentInChildren<Renderer>();
        Material tileMat = renderer.material;
        Color startColor = tileMat.color;
        Transform tileTr = renderer.transform;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(tileMat.DOColor(startColor + new Color(0.5f, 0.5f, 0.5f, 0), destroyTweenDuration));
        sequence.Join(tileTr.DOScale(Vector3.one, destroyTweenDuration));
        sequence.Append(tileMat.DOColor(startColor, destroyTweenDuration));
        sequence.Join(tileTr.DOScale(new Vector3(0.8f,0.8f,1), destroyTweenDuration));
    }

    public void RegisterOnSleepCallback(Action<Tile> action){
        OnStopCallback += action;
    }

    public void UnregisterOnSleepCallback(Action<Tile> action){
        OnStopCallback -= action;
    }
}
