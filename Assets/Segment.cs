using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Segment : MonoBehaviour
{
    Tetronimo tet;
    public bool moving;
    public Rigidbody rb;
    RigidbodyConstraints startingConstraints;
    Vector3 collisionPoint;
    GameObject other;
    bool lineCleared;
    float vertDistMoved;
    float startYPos;

    //public Vector2 gridPos;

    public void MoveDownOneUnit()
    {
        vertDistMoved = 0;
        moving = true;
        lineCleared = true;
        startYPos = transform.position.y;
    }

    private void Start()
    {
        startingConstraints = rb.constraints;
        tet = GetComponentInParent<Tetronimo>();    
    }

    public void ClampPosition()
    {
        var pos = transform.position;
        pos.y = Mathf.RoundToInt(pos.y) + 0.04f;
        transform.position = pos;
    }

    private void OnDestroy()
    {
        tet.RemoveSegment(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = other == null ? Color.red : Color.green;
        if (collisionPoint != Vector3.zero) Gizmos.DrawLine(transform.position, collisionPoint);
        Gizmos.color = lineCleared ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
