using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class Tile : MonoBehaviour
    {
        public static int Count = 0;
        public int ID {get; private set;}

        public Action<Tile> OnStopCallback;
        public event Action<Tile,Vector3> OnMergeDestroyCallback;
        public event Action<Tile> OnMergeAddCallback;
        public event Action<Tile> OnValueChangedCallback;
        private int _index = -1;
        public int Index {
            get => _index;
            set{
                _index = value;
                SetPosition(_index);
            }
        }

        private int _value = -1;
        public int Value {
            get => _value;
            set {
                if (_value == value) return;
                _value = value;
                SetValue(_value);
                OnValueChangedCallback?.Invoke(this);
            }
        }
    
        private bool _isEmpty;
        public bool IsEmpty {
            get => _isEmpty;
            set {
                if (_isEmpty == value) return;
                _isEmpty = value;
                if(_isEmpty){
                    SetValue(0);
                }
            }
        }
        public Tile above;
        public Tile below;
        public Tile right;
        public Tile left;

        public bool ready;

        public Vector3 swipeDirection;

        private bool _firstStart = false;

        [SerializeField]
        private TextMeshPro numberTMP;
        public AudioSource collisionAudio;

        private TileController _tileController;
        private Rigidbody _rb;

        private float _destroyTweenDuration;


        private void Awake(){
            _tileController = TileController.instance;
            _rb = GetComponent<Rigidbody>();
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

        private void Start(){
            StartCoroutine(WaitForFirstIndex());
            _destroyTweenDuration = _tileController.destroyTweenDuration;
            AnimateOnStart();
            ID = Count;
            Count++;
        }
        private void AnimateOnStart(){
            var rend = this.GetComponentInChildren<Renderer>();
            var tileMat = rend.material;
            tileMat.color = new Color(tileMat.color.r, tileMat.color.g, tileMat.color.b, 0);
            var tileTr = rend.transform;
            var sequence = DOTween.Sequence();
            sequence.Append(tileTr.DOScale(Vector3.one,_destroyTweenDuration*2));
            sequence.Join(tileMat.DOFade(1,_destroyTweenDuration));
            sequence.Append(tileTr.DOScale(new Vector3(0.8f, 0.8f, 1),_destroyTweenDuration));
        }

        private IEnumerator WaitForFirstIndex(){
            yield return new WaitForSeconds(0.5f);
            _firstStart = true;
        }

        private void Update(){
            if(!_firstStart) return;
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
            _rb.velocity = Vector3.zero;
            var localPosition = transform.localPosition;
            var roundedXPos = Mathf.RoundToInt(localPosition.x);
            var roundedZPos = Mathf.RoundToInt(localPosition.z);
            Index = roundedZPos * 4 + roundedXPos;
            RefreshAdjacentTiles();
            CheckPosition();
            Invoke(nameof(CheckIfOnTop), 0.2f);
        }

        private void SetPosition(int index){
            gameObject.name = "Tile_" + index.ToString();
            _rb.velocity = Vector3.zero;
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
            if(value >= _tileController.tileMaterials.Count){
                GetComponentInChildren<Renderer>().material = 
                    _tileController.tileMaterials[_tileController.tileMaterials.Count - 1];
            }
            else{
                GetComponentInChildren<Renderer>().material = _tileController.tileMaterials[value];
            }
        }

        private void OnCollisionEnter(Collision collision){
            collisionAudio.pitch = UnityEngine.Random.Range(0.75f, 1.25f);
            collisionAudio.Play();
            if (!collision.gameObject.CompareTag("Tile")) return;
            var tile = collision.gameObject.GetComponent<Tile>();
            if(tile.Value != Value) return;

            if(swipeDirection == Vector3.left){
                if(tile.transform.position.x < transform.position.x)
                    MergeTiles(this, tile, Vector3.left);
                else
                    MergeTiles(tile, this, Vector3.left);
            }
            if(swipeDirection == Vector3.right){
                if(tile.transform.position.x > transform.position.x)
                    MergeTiles(this, tile, Vector3.right);
                else
                    MergeTiles(tile, this, Vector3.right);
            }
            if(swipeDirection == Vector3.forward){
                if(tile.transform.position.z > transform.position.z)
                    MergeTiles(this, tile, Vector3.forward);
                else
                    MergeTiles(tile, this, Vector3.forward);
            }
            if(swipeDirection == Vector3.back){
                if(tile.transform.position.z < transform.position.z)
                    MergeTiles(this, tile, Vector3.back);
                else
                    MergeTiles(tile, this, Vector3.back);
            }
        }

        private void MergeTiles(Tile from, Tile to, Vector3 direction){
            if(from == null || to == null) return;
            foreach(Collider collider in from.GetComponentsInChildren<Collider>()){
                collider.enabled = false;
            }
            from.Value = 0;
            to.Value++;
            ScoreController.Instance.Score += to.Value;
            _tileController.PlayMergeAudios();
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
            foreach(Tile tile in _tileController.tiles){
                if(tile.Index == Index + 4)
                    return tile;
            }
            return null;
        }

        public Tile GetTileBelow(){
            foreach(Tile tile in _tileController.tiles){
                if(tile.Index == Index - 4)
                    return tile;
            }
            return null;
        }

        public Tile GetTileLeft(){
            if(Index%4==0)
                return null;
            foreach(Tile tile in _tileController.tiles){
                if(tile.Index == Index - 1)
                    return tile;
            }
            return null;
        }

        public Tile GetTileRight(){
            if(Index%4==3)
                return null;
            foreach(Tile tile in _tileController.tiles){
                if(tile.Index == Index + 1)
                    return tile;
            }
            return null;
        }

        private void CheckIfOnTop(){
            Dictionary<Tile, int> tilesToMove = new();
            List<Tile> tilesToDestroy = new();
            foreach(Tile tile in _tileController.tiles) {
                if (tile.ID == ID || tile.Index != Index) continue;
                if(Index + 1 < 16 && !_tileController.tileIndexes.Contains(Index + 1))
                    tilesToMove.Add(tile,Index + 1);
                else if(Index - 1 > 16 && !_tileController.tileIndexes.Contains(Index - 1))
                    tilesToMove.Add(tile,Index - 1);
                else if(Index + 4 < 16 && !_tileController.tileIndexes.Contains(Index + 4))
                    tilesToMove.Add(tile,Index + 4);
                else if(Index - 4 < 16 && !_tileController.tileIndexes.Contains(Index - 4))
                    tilesToMove.Add(tile,Index - 4);
                else{
                    if(tile.ID > ID)
                        tilesToDestroy.Add(tile);
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
            _tileController.tileIndexes.Clear();
            foreach(Tile tile in _tileController.tiles){
                _tileController.tileIndexes.Add(tile.Index);
            }
        }

        private void CheckPosition(){
            if(transform.localPosition.x < -0.9f || transform.localPosition.x > 3.9f || transform.localPosition.z < -0.9f || transform.localPosition.z > 3.9f){
                StartCoroutine(WaitAndDestroy(gameObject));
            }
        }

        private void OnMergeDestroy(Tile tile, Vector3 direction){
            if(tile==null) return;
            var tileMat = tile.GetComponentInChildren<Renderer>().material;
            var tileTr = tile.transform;
            _tileController.tiles.Remove(tile);
            var sequence = DOTween.Sequence(tile.gameObject);

            sequence.Append(tileMat.DOColor(new Color(2,2,2,1), _destroyTweenDuration));
            if(direction == Vector3.left){
                sequence.Join(tileTr.DOScaleX(0, _destroyTweenDuration));
                sequence.Join(tileTr.DOLocalMoveX(tileTr.localPosition.x - 0.5f, _destroyTweenDuration));
            }
            if(direction == Vector3.right){
                sequence.Join(tileTr.DOScaleX(0, _destroyTweenDuration));
                sequence.Join(tileTr.DOLocalMoveX(tileTr.localPosition.x + 0.5f, _destroyTweenDuration));
            }
            if(direction == Vector3.forward){
                sequence.Join(tileTr.DOScaleZ(0, _destroyTweenDuration));
                sequence.Join(tileTr.DOLocalMoveZ(tileTr.localPosition.z + 0.5f, _destroyTweenDuration));
            }
            if(direction == Vector3.back){
                sequence.Join(tileTr.DOScaleZ(0, _destroyTweenDuration));
                sequence.Join(tileTr.DOLocalMoveZ(tileTr.localPosition.z - 0.5f, _destroyTweenDuration));
            }
            sequence.OnComplete(()=>StartCoroutine(WaitAndDestroy(tile.gameObject)));
        }

        private static IEnumerator WaitAndDestroy(GameObject go){
            yield return new WaitForSeconds(0.1f);
            Destroy(go);
        }

        private void OnValueChanged(Tile tile){
            if(tile==null) return;
            Renderer rend = tile.GetComponentInChildren<Renderer>();
            Material tileMat = rend.material;
            Color startColor = tileMat.color;
            Transform tileTr = rend.transform;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(tileMat.DOColor(startColor + new Color(0.5f, 0.5f, 0.5f, 0), _destroyTweenDuration));
            sequence.Join(tileTr.DOScale(Vector3.one, _destroyTweenDuration));
            sequence.Append(tileMat.DOColor(startColor, _destroyTweenDuration));
            sequence.Join(tileTr.DOScale(new Vector3(0.8f,0.8f,1), _destroyTweenDuration));
        }
    }
}
