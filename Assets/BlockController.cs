using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BlockController : MonoBehaviour
{
    [SerializeField] Vector2 _gridPos;
    public Vector2 gridPos { get { return _gridPos; } set { SetGridPos(value); } }
    MultiBlockController mbController;
    bool doneMoving;

    private void Start()
    {
        mbController = GetComponentInParent<MultiBlockController>();    
    }

    public void SetGridPosDontLineCheck(Vector2 newPos)
    {
        SetGridPos(newPos);
    }

    public Vector2 DistanceFromGridPos()
    {
        var targetPos = GameManager.i.GridToWorldPos(_gridPos);
        Vector2 dist = new Vector2(Mathf.Abs(targetPos.x - transform.position.x), Mathf.Abs(targetPos.y - transform.position.y));
        return dist;
    }

    void SetGridPos(Vector2 newPos)
    {
        if (newPos == _gridPos) return;
        GameManager.i.ClearGridPos(_gridPos, this);
        _gridPos = newPos;
        doneMoving = false;
    }

    void FinishMoving()
    {
        GameManager.i.SetGridPos(_gridPos, this, mbController == null);
    }

    private void Update()
    {
        Vector3 targetPos = GameManager.i.GridToWorldPos(_gridPos);
        float dist = Vector3.Distance(transform.position, targetPos);
        if (dist > GameManager.i.blockDistThreshold) Move(targetPos);
        else {
            transform.position = targetPos;
            if (!doneMoving) FinishMoving();
        }
    }

    void Move(Vector3 targetPos)
    {
        var dir = (targetPos - transform.position).normalized;

        var vertSpeed = GameManager.i.speed * (mbController == null ? 2 : 1);
        Vector3 speed = new Vector3(GameManager.i.horizontalMoveSpeed, vertSpeed, 0);
        if (Mathf.Abs(dir.x) > 0.2f) speed.y /= 3;
        dir = new Vector3(dir.x * speed.x, dir.y * speed.y, 0);

        transform.position += dir * Time.deltaTime;
        Debug.DrawLine(transform.position, targetPos, Color.magenta);
    }

    public Vector2 GetPotentialRotation(Vector2 originGridPos, bool clockwise)
    {
        if (originGridPos == _gridPos) return Vector2.one * -1;

        var currentGridPos = _gridPos;
        var newGridPos = _gridPos;
        var relativePos = originGridPos - currentGridPos;
        if (clockwise) newGridPos = originGridPos + new Vector2(relativePos.y, -relativePos.x);
        if (!clockwise) newGridPos = originGridPos + new Vector2(-relativePos.y, relativePos.x);

        return newGridPos;
    }


    public void Rotate(Vector2 originGridPos, bool clockwise)
    {
        var newPos = GetPotentialRotation(originGridPos, clockwise);
        if (newPos.x >= 0) SetGridPos(newPos);
        TeleportToGridPosition();
    }

    public void TeleportToGridPosition()
    {
        transform.position = GameManager.i.GridToWorldPos(_gridPos);
    }

    /*
    0 1 0 0
    0 1 0 0
    0 1 0 1

    0 0 0 0
    1 1 1 0
    0 0 0 1

    0 0 0
    0 0 0
    0 1 0           pos = origin + [0, -1]

    0 0 0           case: clockwise
    1 0 0           pos = origin + [-1, 0] 
    0 0 0
    
    0 0 0           case !clockwise
    0 0 1           pos = origin + [1, 0]
    0 0 0    
    */
}
