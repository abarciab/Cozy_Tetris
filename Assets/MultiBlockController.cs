using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class MultiBlockController : MonoBehaviour
{
    [SerializeField] List<BlockController> blocks = new List<BlockController>();
    [SerializeField] BlockController rotationPivot;
    public Color color;
    public Sprite sprite;
    [SerializeField] Sound blockStopSound;
    [SerializeField] bool rotatable = true;
    [SerializeField] Transform preview;
    [SerializeField] List<Vector2> previewYOffsets = new List<Vector2>();
    int currentRot;

    Vector3 startPos, endPos;
    bool readyToLand;

    public void ClearAllPosition()
    {
        foreach (var b in blocks) GameManager.i.ClearGridPos(b.gridPos, b);
    }

    public void SetPosition(Vector2 gridPos)
    {
        var offset = gridPos - rotationPivot.gridPos;
        foreach (var b in blocks) {
            var newPos = b.gridPos + offset;
            b.gridPos = newPos;
            b.TeleportToGridPosition();
        }
    }

    private void Update()
    {
        MoveDownIfPossible();

        Debug.DrawLine(startPos, endPos, Color.green);
    }

    public void SetColor(Color color)
    {
        this.color = color;
        foreach (var b in blocks) b.GetComponentInChildren<Renderer>().material.color = color;
    }

    public void DropDown()
    {
        float dist = GetAvaliableDistanceDown();
        foreach (var b in blocks) {
            b.gridPos += Vector2.down * dist;
            b.TeleportToGridPosition();
        }
        Land();
    }

    private void OnEnable()
    {
        preview.gameObject.SetActive(false);
    }

    void MoveDownIfPossible()
    {
        float dist = GetAvaliableDistanceDown();
        Vector2 previewGridPos = rotationPivot.gridPos + previewYOffsets[currentRot] + Vector2.down * dist;
        preview.gameObject.SetActive(true);
        preview.transform.position = GameManager.i.GridToWorldPos(previewGridPos);

        if (blocks[0].DistanceFromGridPos().y > 0.1f) return;
        if (dist == 1) readyToLand = true;
        else if (readyToLand) {
            Land();
            return;
        }
        foreach (var b in blocks) {
            b.gridPos += Vector2.down;
        }
    }

    float GetAvaliableDistanceDown()
    {
        var positions = GetPositions();

        for (int i = 0; i < positions.Count; i++) {
            var pos1 = positions[i];
            bool highest = false;
            foreach (var pos2 in positions) {
                if (pos2.x == pos1.x && pos1.y > pos2.y) highest = true;
            }
            if (highest) {
                positions.RemoveAt(i);
                i -= 1;
            }
        }

        float dist = -1;
        float minDist = Mathf.Infinity;
        Vector2 highestAvaliablePos = Vector2.one * -Mathf.Infinity;
        foreach (var pos in positions) {
            Vector2 freeSquare = GameManager.i.FindFreeSquareBelow(pos, blocks);

            float _dist = pos.y - freeSquare.y;
            if (minDist > _dist) minDist = _dist;

            if (freeSquare != pos && freeSquare.y > highestAvaliablePos.y) {
                highestAvaliablePos = freeSquare;
                dist = _dist;

                startPos = GameManager.i.GridToWorldPos(pos);
                endPos = GameManager.i.GridToWorldPos(highestAvaliablePos);
            }
        }
        return Mathf.Min(minDist, dist);
    }

    public void Move(Vector2 offset)
    {
        foreach (var b in blocks) b.gridPos += offset;
    }

    public List<Vector2> GetPositions()
    {
        var list = new List<Vector2>();
        foreach (var b in blocks) list.Add(b.gridPos);
        return list;
    }

    private void Start()
    {
        blockStopSound = Instantiate(blockStopSound);
        SetColor(color);
    }

    void Land()
    {
        Destroy(preview.gameObject);
        GameManager.i.InformLanded();
        Destroy(this);
        blockStopSound.Play();
        FindObjectOfType<CameraController>().Shake(0.05f, 1, 0.05f);
    }

    public List<BlockController> GetBlocks() { return blocks; }

    public List<Vector2> GetPotentialRotatedBlocks(bool clockwise)
    {
        var list = new List<Vector2>();
        foreach (var b in blocks) {
            var pos = b.GetPotentialRotation(rotationPivot.gridPos, clockwise);
            if (pos.x >= 0) list.Add(pos);
        }
        return list;
    }

    public void Rotate(bool clockwise)
    {
        if (!rotatable) return;

        currentRot += clockwise ? 1 : 3;
        currentRot %= 4;
        foreach (var b in blocks) {
            if (b.DistanceFromGridPos().y > 0.5f) {
                b.gridPos += Vector2.up;
                b.TeleportToGridPosition();
            }
        }
        foreach (var b in blocks) b.Rotate(rotationPivot.gridPos, clockwise);
        preview.Rotate(Vector3.forward, clockwise ? 90 : -90);
    }
}
