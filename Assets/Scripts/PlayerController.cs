using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public int selectedFighter;

    public Fighter controllingFighter;

    private bool gameStart;


    void Start()
    {
        if (isLocalPlayer)
        {
            CmdRegisterPlayer();
        }
    }

    [Command]
    void CmdRegisterPlayer()
    {
        GameController.Instance.RegisterPlayer(this);
    }

    [Server]
    public void RegisterDone()
    {
        RpcRegisterDone();
    }

    [ClientRpc]
    void RpcRegisterDone()
    {
        gameStart = true;
    }

    void Update()
    {
        if (!gameStart)
        {
            return;
        }

        if (!isLocalPlayer)
        {
            return;
        }

        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) + Mathf.Abs(v) > 0)
        {
            CmdSetVelocity(h * 6f, v * 3f);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdJump();
        }
    }

    [Command]
    void CmdSetVelocity(float x, float y)
    {
        controllingFighter.SetVelocity(x, y);
    }

    [Command]
    void CmdJump()
    {
        controllingFighter.Jump();
    }
}
