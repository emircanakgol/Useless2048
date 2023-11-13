using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TileController : MonoBehaviour
{
    public static TileController instance;

    private Camera cam;
    public List<Tile> tiles;
    public List<int> tileIndexes;
    
    [Header("References")]
    [SerializeField]
    private Transform tileParent;

    [SerializeField]
    private GameObject tilePrefab;
    [SerializeField]
    private Image cooldownImage;
    [SerializeField]
    private Image bg;
    [SerializeField]
    private CanvasGroup gameOver;


    [Header("Materials")]
    public List<Material> tileMaterials;

    [Header("Tween Settings")]
    public float destroyTweenDuration;
    [Header("Prefabs")]
    [SerializeField]
    private AudioSource boopAudio;
    [SerializeField]
    private AudioSource collisionAudio;

    [Header("Settings")]
    [SerializeField]
    private float mousePixelThreshold;
    [SerializeField]
    private float dragSpeed;
    public float VelocityThreshold;
    private Vector3 mouseDownPos;
    public Coroutine waitNewTileCoroutine {get; private set;}
    public Coroutine addNewTileCoroutine {get; private set;}
    private bool canSwipe = true;
    private bool gameIsOver = false;

    private void Awake(){
        if(instance==null)
            instance = this;
        
        tiles = new();
        tileIndexes = new();
        cam = Camera.main;
    }

    private void Start(){
        bg.gameObject.SetActive(true);
        bg.DOColor(new Color(0,0,0,0), 1).OnComplete(() => bg.gameObject.SetActive(false));
        InitializeBoard();
    }

    private void Update(){
        if(gameIsOver){
            if(Input.GetMouseButtonDown(0)){
                bg.gameObject.SetActive(true);
                bg.DOColor(new Color(0,0,0,1), 1).OnComplete(() => SceneManager.LoadScene("Menu"));
            }
        }
        if(canSwipe){
            MouseUp();
            MouseDown();
        }
    }

    private void MouseDown(){
        if(Input.GetMouseButtonDown(0)){
            mouseDownPos = Input.mousePosition;
        }
    }

    private void MouseUp(){
        if(Input.GetMouseButtonUp(0)){
            Vector3 deltaMousePos = Input.mousePosition - mouseDownPos;

            //Check if over threshold.
            if(deltaMousePos.magnitude < mousePixelThreshold) return;
            
            RefreshTileList();
            if(Mathf.Abs(deltaMousePos.x) > Mathf.Abs(deltaMousePos.y)){
                //Dragged on X
                if(deltaMousePos.x > 0)
                    DragRight();
                else 
                    DragLeft();
            }
            else{
                //Dragged on Z (Screen coordinates' y is world coordinates' z)
                if(deltaMousePos.y > 0) 
                    DragUp();
                else 
                    DragDown();
            }
        }
    }

    private void DragLeft(){
        canSwipe = false;
        foreach(Tile tile in tiles){
            tile.GetComponent<Rigidbody>().AddForce(Vector3.left * dragSpeed, ForceMode.Impulse);
            tile.SwipeDirection = Vector3.left;
        }
        if(waitNewTileCoroutine != null)
            StopCoroutine(waitNewTileCoroutine);
        waitNewTileCoroutine = StartCoroutine(WaitForTiles());
    }

    private void DragRight(){
        canSwipe = false;
        foreach(Tile tile in tiles){
            tile.GetComponent<Rigidbody>().AddForce(Vector3.right * dragSpeed, ForceMode.Impulse);
            tile.SwipeDirection = Vector3.right;
        }
        if(waitNewTileCoroutine != null)
            StopCoroutine(waitNewTileCoroutine);
        waitNewTileCoroutine = StartCoroutine(WaitForTiles());
    }

    private void DragUp(){
        canSwipe = false;
        foreach(Tile tile in tiles){
            tile.GetComponent<Rigidbody>().AddForce(Vector3.forward * dragSpeed, ForceMode.Impulse);
            tile.SwipeDirection = Vector3.forward;
        }
        if(waitNewTileCoroutine != null)
            StopCoroutine(waitNewTileCoroutine);
        waitNewTileCoroutine = StartCoroutine(WaitForTiles());
    }

    private void DragDown(){
        canSwipe = false;
        foreach(Tile tile in tiles){
            tile.GetComponent<Rigidbody>().AddForce(Vector3.back * dragSpeed, ForceMode.Impulse);
            tile.SwipeDirection = Vector3.back;
        }
        if(waitNewTileCoroutine != null)
            StopCoroutine(waitNewTileCoroutine);
        waitNewTileCoroutine = StartCoroutine(WaitForTiles());
    }

    private void InitializeBoard(){
        GameObject tileGO = Instantiate(tilePrefab, tileParent);
        Tile tile = tileGO.GetComponent<Tile>();
        tile.collisionAudio = collisionAudio;
        int randIndex = UnityEngine.Random.Range(0,16);
        int x = randIndex % 4;
        int z = Mathf.FloorToInt((float)randIndex/4f);
        Vector3 spawnPos = new Vector3(x, 0, z);
        tile.transform.localPosition = spawnPos;
        tile.Index = randIndex;
        tile.Value = 1;
        tiles.Add(tile);
        for(int i = 0; i < UnityEngine.Random.Range(0,4); i++){
            int randomIndex = UnityEngine.Random.Range(0,16);

            bool sameIndex = false;
            foreach(Tile tile1 in tiles){
                if(tile1.Index == randomIndex){
                    tile1.Value++;
                    sameIndex = true;
                    continue;
                }
            }
            if(sameIndex)
                continue;
        
            int X = randomIndex % 4;
            int Z = Mathf.FloorToInt((float)randomIndex/4f);
            Vector3 spawnPosition = new(X, 0, Z);
            GameObject tlGO = Instantiate(tilePrefab, tileParent);
            Tile tl = tlGO.GetComponent<Tile>();
            tl.collisionAudio = collisionAudio;
            tl.transform.localPosition = spawnPosition;
            tl.Index = randomIndex;
            tl.Value = 1;
            tiles.Add(tl);
        }
        foreach(Tile tile2 in tiles){
            tileIndexes.Add(tile2.Index);
            tile2.RefreshAdjacentTiles();
        }
    }
    private void RefreshTileList(){
        tiles.Clear();
        foreach(Tile tl in FindObjectsOfType<Tile>()){
            tiles.Add(tl);
        }
    }
    private IEnumerator WaitForTiles(){
        DOVirtual.Float(1, 0, 1.7f, (x)=>cooldownImage.fillAmount=x);
        yield return new WaitForSeconds(1.2f);
        RefreshTileList();
        foreach(Tile tile in tiles){
            if(tile == null) continue;
            if(tile.IsWonky())
                tile.OnStopCallback(tile);            
        }
        if(IsPlayable()){
            if(addNewTileCoroutine != null)
                StopCoroutine(addNewTileCoroutine);
            addNewTileCoroutine = StartCoroutine(AddTiles());
        }
        else{
            if(addNewTileCoroutine != null)
                StopCoroutine(addNewTileCoroutine);
            GameOver();
        }
    }

    private IEnumerator AddTiles(){
        yield return new WaitForSeconds(0.5f);
        AddNewTiles();
        canSwipe = true;
    }
    public void AddNewTiles(){
        if(tiles.Count == 16) return;

        int maxCount = 3;
        if(16 - tiles.Count < maxCount)
            maxCount = 16 - tiles.Count;
        for(int i = 0; i < UnityEngine.Random.Range(1,maxCount); i++){
            int randIndex = UnityEngine.Random.Range(0,16);
            tileIndexes.Clear();
            foreach(Tile tile in tiles){
                tileIndexes.Add(tile.Index);
            }
            while(tileIndexes.Contains(randIndex)){
                randIndex = UnityEngine.Random.Range(0,16);
            }

            GameObject tlGO = Instantiate(tilePrefab, tileParent);
            Tile tl = tlGO.GetComponent<Tile>();
            tl.collisionAudio = collisionAudio;
            int x = randIndex % 4;
            int z = Mathf.FloorToInt((float)randIndex/4f);
            Vector3 spawnPos = new Vector3(x, 0, z);
            tl.transform.localPosition = spawnPos;
            tl.Index = randIndex;
            int randValue = UnityEngine.Random.Range(1,3);
            tl.Value = randValue;
            tiles.Add(tl);
            tl.RefreshAdjacentTiles();
        }
    }

    private bool IsPlayable(){
        if(tiles.Count == 16){
            foreach(Tile tile in tiles){
                if(tile.above != null && tile.above.Value == tile.Value)
                    return true;
                if(tile.below != null && tile.below.Value == tile.Value)
                    return true;
                if(tile.right != null && tile.right.Value == tile.Value)
                    return true;
                if(tile.left != null && tile.left.Value == tile.Value)
                    return true;
            }
            return false;
        }
        else
            return true;
    }

    public void PlayMergeAudios(){
        boopAudio.pitch = UnityEngine.Random.Range(0.75f, 1.25f); 
        boopAudio.Play();
    }

    private void GameOver(){
        canSwipe = false;
        gameOver.DOFade(1, 1);
        gameIsOver = true;
    }
}
