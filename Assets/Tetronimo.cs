using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public enum Direction { Right, Left, down}

public class Tetronimo : MonoBehaviour
{
    [HideInInspector] public bool moving = true;
    Rigidbody rb;
    List<Segment> segments = new List<Segment>();
    [SerializeField] bool rotatable;

    [Header("Animate")]
    [SerializeField] float rotateTime = 0.75f;
    [SerializeField] AnimationCurve rotateCurve;
    [HideInInspector] public bool rotating;

    public List<Vector3> Rotate(bool clockwise)
    {
        StopAllCoroutines();
        List<Vector3> newPositions = new List<Vector3>();
        if (!rotatable) return newPositions;

        float amount = clockwise ? -90 : 90;
        transform.rotation *= Quaternion.Euler(0, 0, amount);
        foreach (Segment s in segments) {
            s.transform.rotation = Quaternion.identity;
            newPositions.Add(s.transform.position);
        }
        return newPositions;
    }

    public void ConfirmRotate(bool clockwise)
    {
        if (!rotatable) return;
        Rotate(!clockwise);

        var target = transform.rotation * Quaternion.Euler(0, 0, clockwise ? -90 : 90);
        StartCoroutine(Rotate(target));
    }

    IEnumerator Rotate(Quaternion targetRotation)
    {
        rotating = true;
        var start = transform.rotation;
        float timePassed = 0;
        while (timePassed < rotateTime) {
            float progress = rotateCurve.Evaluate(timePassed / rotateTime);
            transform.rotation = Quaternion.Lerp(start, targetRotation, progress);

            timePassed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        foreach (Segment s in segments) s.transform.rotation = Quaternion.identity;
        transform.rotation = targetRotation;
        rotating = false;
    }

    public List<Vector3> GetPositions()
    {
        List<Vector3> Selected = new List<Vector3>();
        foreach (var s in segments) Selected.Add(s.transform.position);
        return Selected;
    }

    public List<Vector3> GetPositions(Direction dir)
    {
        List<Vector3> Selected = new List<Vector3>();
        float furthest = (dir == Direction.Left || dir == Direction.down) ? Mathf.Infinity : -Mathf.Infinity;

        for (int i = 0; i < segments.Count; i++) { 
            var s = segments[i];
            s.gameObject.name = i.ToString();
            float value = dir == Direction.down ? s.transform.position.y : s.transform.position.x;

            bool leftOrDownAddThreshold = (dir != Direction.Right && value <= furthest + 0.1f);
            bool rightAddThreshold = (dir == Direction.Right && value >= furthest - 0.1f);
            if (!leftOrDownAddThreshold && !rightAddThreshold) continue;
            Selected.Add(s.transform.position);

            bool leftOrDownClearThreshold = (dir != Direction.Right && value < furthest - 0.1f);
            bool rightClearThreshold = (dir == Direction.Right && value > furthest + 0.1f);
            if (!leftOrDownClearThreshold && !rightClearThreshold) continue;
            Selected.Clear();
            furthest = value; 
            Selected.Add(s.transform.position);
        }

        return Selected;
    }


    private void Update()
    {
        if (!moving) {
            foreach (var s in segments) {
                if (s.moving) {
                    s.rb.velocity = Vector3.down * GameManager.i.speed;
                }
            }
            return;
        }
        
        rb.velocity = Vector3.down * GameManager.i.speed;
        if (!rotating) foreach (var s in segments) if (s) s.transform.rotation = Quaternion.identity;
    }

    public void RemoveSegment(Segment segment)
    {
        segments.Remove(segment);
    }

    public void MoveDownOneUnit(float y)
    {
        if (moving) return;
        foreach (var s in segments) if (s.transform.position.y >= y) s.MoveDownOneUnit();
    }
    void ClampPosition()
    {
        var pos = transform.position;
        pos.y = Mathf.RoundToInt(pos.y) + 0.04f;
        transform.position = pos;
    }
}
