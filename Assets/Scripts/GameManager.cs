using MyBox;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using static UnityEditor.PlayerSettings;

public class GameManager : MonoBehaviour
{
    public static GameManager i;
    [SerializeField] GameObject pauseMenu;
    [SerializeField] Fade fade;
    [SerializeField] MusicPlayer music;

    [Header("Global variables")]
    [SerializeField] float normalSpeed;
    [SerializeField] float fastSpeed;
    [HideInInspector] public float speed;

    [Header("Sounds")]
    [SerializeField] Sound breakSound;
    [SerializeField] Sound spinSound, moveSound;

    [Header("Control")]
    public float horizontalMoveSpeed = 8; 
    [SerializeField] float rotateResetTime;
    [SerializeField] MultiBlockController currentBlock;
    MultiBlockController nextBlock, heldBlock;
    float rotateCooldown;

    [Header("Generation")]
    [SerializeField] List<GameObject> blockPrefabs = new List<GameObject>();
    [SerializeField] Transform spawnPos;
    [SerializeField] Gradient randomizeColorMod;
    [SerializeField] float randomizeLerp = 0.1f, holdMoveResetTime = 0.1f, holdMoveStartTime = 0.2f;
    float holdMoveLeftCooldown, holdMoveRightCooldown;

    [Header("UI")]
    [SerializeField] Image nextUpImg;
    [SerializeField] Image heldBlockImg;

    [Header("Effects")]
    [SerializeField] GameObject breakEffect;




    //NEW
    [Header("Grid")]
    [SerializeField] Vector2 gridDimensions;
    [SerializeField] Vector2 startPos;
    [SerializeField] Transform gridOffset;
    [SerializeField] float gridUnit = 1;
    BlockController[,] grid;
    [SerializeField] bool showGrid;
    public float blockDistThreshold = 0.01f;


    private void Update()
    {
        speed = Input.GetKey(KeyCode.S) ? fastSpeed : normalSpeed; 
        
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (Input.GetKeyDown(KeyCode.Space)) currentBlock.DropDown();
        if (Input.GetKeyDown(KeyCode.LeftShift)) SwitchHeldItem();

        if (Input.GetKey(KeyCode.A)) holdMoveLeftCooldown -= Time.deltaTime;
        if (Input.GetKeyUp(KeyCode.A)) holdMoveLeftCooldown = holdMoveResetTime;
        else if (Input.GetKey(KeyCode.D)) holdMoveRightCooldown -= Time.deltaTime;
        if (Input.GetKeyUp(KeyCode.D)) holdMoveRightCooldown = holdMoveResetTime;

        if (holdMoveLeftCooldown <= 0) Move(Direction.Left);
        if (holdMoveRightCooldown <= 0) Move(Direction.Right);

        if (Input.GetKeyDown(KeyCode.A)) {
            Move(Direction.Left);
            holdMoveLeftCooldown = holdMoveStartTime;
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            Move(Direction.Right);
            holdMoveRightCooldown = holdMoveStartTime;
        }

        rotateCooldown -= Time.deltaTime;
        if (rotateCooldown > 0) return;
        if (Input.GetKeyDown(KeyCode.E)) Rotate(false);
        if (Input.GetKeyDown(KeyCode.Q)) Rotate(true);
    }

    public Vector2 FindFreeSquareBelow(Vector2 gridPos, List<BlockController> blocks)
    {
        var selectedSquare = gridPos;

        for (int i = (int)gridPos.y; i >= 0; i--) {
            var pos = new Vector2(gridPos.x, i);
            var entryAtPos = Grid(pos);
            if (entryAtPos != null && !blocks.Contains(entryAtPos)) break;
            if ((entryAtPos == null || blocks.Contains(entryAtPos)) && pos.y < selectedSquare.y) selectedSquare = pos;
        }
        return selectedSquare;
    }

    public void ClearGridPos(Vector2 gridPos, BlockController controller)
    {
        if (Grid(gridPos) == controller) SetGridPos(gridPos, null);
    }

    public void SetGridPos(Vector2 pos, BlockController controller, bool checkLine = true)
    {
        if (InBounds(pos)) grid[(int)pos.x, (int)pos.y] = controller;
        if (checkLine) LineCheck();
    }
    bool InBounds(Vector2 pos)
    {
        return pos.x >= 0 && pos.x < gridDimensions.x && pos.y >= 0 && pos.y < gridDimensions.y;
    }

    public Vector3 GridToWorldPos (Vector2 gridPos)
    {
        return gridOffset.position + (new Vector3(gridPos.x, gridPos.y, 0) * gridUnit);
    }

    private void Start()
    {

        holdMoveLeftCooldown = holdMoveRightCooldown = holdMoveResetTime;

        grid = new BlockController[(int)gridDimensions.x, (int)gridDimensions.y];

        heldBlockImg.color = new Color(0, 0, 0, 0);
        spinSound = Instantiate(spinSound);
        moveSound = Instantiate(moveSound);
        breakSound = Instantiate(breakSound);
        fade.Hide();
        
        currentBlock = GenerateNewBlock();
    }

    BlockController Grid(Vector2 pos)
    {
        if (!InBounds(pos)) return null;
        return grid[(int)pos.x, (int)pos.y];
    }

    private void OnDrawGizmos()
    {
        if (!showGrid) return;
        for (int x = 0; x < gridDimensions.x; x++) {
            for (int y = 0; y < gridDimensions.y; y++) {
                if (Application.isPlaying) Gizmos.color = grid[x, y] != null ?  Color.red : Color.white;
                Gizmos.DrawWireCube(GridToWorldPos(new Vector2(x, y)), new Vector3(gridUnit, gridUnit, 0.01f));
            }
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(GridToWorldPos(startPos), new Vector3(gridUnit, gridUnit, 0.01f) * 1.1f);
    }

    MultiBlockController GenerateNewBlock()
    {
        var selected = nextBlock != null ? nextBlock : blockPrefabs[Random.Range(0, blockPrefabs.Count)].GetComponent<MultiBlockController>();
        var mb = selected.GetComponent<MultiBlockController>();
        var selectedColor = PickNewColor(mb);

        nextBlock = blockPrefabs[Random.Range(0, blockPrefabs.Count)].GetComponent<MultiBlockController>();
        nextUpImg.sprite = nextBlock.GetComponent<MultiBlockController>().sprite;

        mb = nextBlock.GetComponent<MultiBlockController>();
        var nextColor = PickNewColor(mb);
        nextUpImg.color = nextColor;

        var newBlock = Instantiate(selected).GetComponent<MultiBlockController>();
        newBlock.SetColor(selectedColor);
        newBlock.SetPosition(startPos);
        return newBlock;
    }

    Color PickNewColor(MultiBlockController mb)
    {
        Color color = mb.color;
        return Color.Lerp(color, randomizeColorMod.Evaluate(Random.Range(0f, 1)), randomizeLerp);
    }

    void Rotate(bool clockwise)
    {
        var positions = currentBlock.GetPotentialRotatedBlocks(clockwise);
        var blocks = currentBlock.GetBlocks();
        if (positions.Count != blocks.Count-1) return;
        foreach (var pos in positions) if (!InBounds(pos) || Grid(pos) != null) return;

        currentBlock.Rotate(clockwise);
        spinSound.Play();
        rotateCooldown = rotateResetTime;
    }


    void Move(Direction dir)
    {
        if (currentBlock == null) return;

        var offset = dir == Direction.Left ? Vector2.left : Vector2.right;
        var positions = currentBlock.GetPositions();
        var blocks = currentBlock.GetBlocks();
        foreach (var pos in positions) {
            var newPos = pos + offset;
            if (!InBounds(newPos) || (Grid(newPos) != null && !blocks.Contains(Grid(newPos))) ) return;
        }

        currentBlock.Move(offset);
        holdMoveLeftCooldown = holdMoveRightCooldown = holdMoveResetTime;
        moveSound.Play();
    }

    //OLD

    public void InformLanded()
    {
        currentBlock = GenerateNewBlock();
    }

    void Lose()
    {
        Destroy(currentBlock.gameObject);
        if (heldBlock) Destroy(heldBlock.gameObject);
        currentBlock = null;
    }

    void SwitchHeldItem()
    {
        var newBlock = heldBlock;
        if (heldBlock == null) {
            newBlock = GenerateNewBlock();
        }
        newBlock.gameObject.SetActive(true);
        newBlock.SetPosition(startPos);
        
        heldBlock = currentBlock;
        heldBlock.ClearAllPosition();
        heldBlock.gameObject.SetActive(false);
        heldBlockImg.sprite = heldBlock.GetComponent<MultiBlockController>().sprite;
        heldBlockImg.color = heldBlock.color;

        currentBlock = newBlock;
    }

    void LineCheck()
    {
        bool broken = false;
        for (int y = 0; y < gridDimensions.y; y++) { 
            int rowCount = 0;
            for (int x = 0; x < gridDimensions.x; x++) {
                Vector2 pos = new Vector2(x, y);
                if (Grid(pos) != null) rowCount += 1;
            }
            if (rowCount >= gridDimensions.x) {
                DeleteRow(y);

                FindObjectOfType<CameraController>().Shake(0.1f, 1, 0.1f);
                broken = true;
            }
        }
        if (broken) breakSound.Play();
    }

    void DeleteRow(int y)
    {
        print("Deleted row " + y);

        for (int x = 0; x < gridDimensions.x; x++) {
            var pos = new Vector2(x, y);
            var block = Grid(pos);

            var Color = block.GetComponentInChildren<Renderer>().material.color;
            Destroy(block.gameObject);
            var _breakEffect = Instantiate(breakEffect, block.transform.position, Quaternion.identity);
            _breakEffect.GetComponent<ParticleSystemRenderer>().material.color = Color;
        }

        for (int _y = y; _y + 1 < gridDimensions.y; _y++) {
            for (int x = 0; x < gridDimensions.x; x++) {
                var pos = new Vector2(x, _y);
                var block = Grid(pos);
                if (!block) continue;
                block.gridPos += Vector2.down;
            }
        }
    }

    void TogglePause()
    {
        if (Time.timeScale == 0) Resume();
        else Pause();
    }

    private void Awake()
    {
        i = this;
    }

   

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
        AudioManager.i.Resume();
    }

    public void Pause()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0;
        AudioManager.i.Pause();
    }

    [ButtonMethod]
    public void LoadMenu()
    {
        Resume();
        StartCoroutine(FadeThenLoadScene(0));
    }

    [ButtonMethod]
    public void EndGame()
    {
        Resume();
        StartCoroutine(FadeThenLoadScene(2));
    }

    IEnumerator FadeThenLoadScene(int num)
    {
        fade.Appear(); 
        music.FadeOutCurrent(fade.fadeTime);
        yield return new WaitForSeconds(fade.fadeTime + 0.5f);
        Destroy(AudioManager.i.gameObject);
        SceneManager.LoadScene(num);
    }

}
