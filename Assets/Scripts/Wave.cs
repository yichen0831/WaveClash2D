using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wave : MonoBehaviour
{
    public CircleCollider2D circleCollider2D;

    public Fighter caster;

    public float live;

    private float radius = 0.1f;

    void Start()
    {
        Destroy(gameObject, live);
    }

    void Update()
    {
        if (radius < 2.2f)
        {
            radius += 4f * Time.deltaTime;
            circleCollider2D.radius = radius;
        }
        else
        {
            circleCollider2D.radius = 0.01f;
        }
    }
}
