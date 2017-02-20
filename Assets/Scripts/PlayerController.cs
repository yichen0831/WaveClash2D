using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public int selectedFighter;

    public Fighter controllingFighter;


    void Start()
    {
        GameController.Instance.RegisterPlayer(this);
    }

    void Update()
    {
        if (controllingFighter == null)
        {
            return;
        }

        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) + Mathf.Abs(v) > 0)
        {
            controllingFighter.SetVelocity(h * 6f, v * 3f);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            controllingFighter.Jump();
        }
    }
}
