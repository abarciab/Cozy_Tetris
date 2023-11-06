using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventCoord : MonoBehaviour
{
    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
