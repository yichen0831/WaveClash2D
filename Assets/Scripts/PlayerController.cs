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
            string nickname = OperatingGui.Instance.GetNickname();
            int fighterIndex = OperatingGui.Instance.GetFighterIndex();
            CmdRegisterPlayer(nickname, fighterIndex);
        }
    }

    [Command]
    void CmdRegisterPlayer(string nickname, int fighterIndex)
    {
        GameController.Instance.RegisterPlayer(this, nickname, fighterIndex);
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
        if (controllingFighter == null)
        {
            return;
        }

        controllingFighter.SetVelocity(x, y);
    }

    [Command]
    void CmdJump()
    {
        if (controllingFighter == null)
        {
            return;
        }

        controllingFighter.Jump();
    }
}
