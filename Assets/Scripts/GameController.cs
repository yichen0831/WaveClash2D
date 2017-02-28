using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour
{
    // Singleton.
    public static GameController Instance { get; private set; }

    private Dictionary<uint, Fighter> fighterDict = new Dictionary<uint, Fighter>();
    private ResourceManager resourceManager;

    private float updateRankCountDown;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (this != Instance)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        resourceManager = ResourceManager.Instance;
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        updateRankCountDown -= Time.deltaTime;
        if (updateRankCountDown <= 0)
        {
            updateRankCountDown = 0.5f;
            UpdateRank();
        }
    }

    [Server]
    private void UpdateRank()
    {
        // Update rank.
    }

    [ClientRpc]
    private void RpcUpdateRank(uint[] fighterIds)
    {

    }

    [Server]
    public void RegisterPlayer(PlayerController playerController, string nickname, int fighterIndex)
    {
        var fighter = Instantiate(resourceManager.fighterPrefab) as Fighter;
        fighter.transform.localPosition = new Vector3(0f, -1f, 0f);
        fighter.Setup(fighterIndex, playerController, nickname);
        NetworkServer.Spawn(fighter.gameObject);

        playerController.controllingFighter = fighter;
        playerController.RegisterDone();
    }

    [Server]
    public void RegisterFighter(Fighter fighter)
    {
        var id = fighter.netId.Value;
        fighterDict[id] = fighter;
    }

    [Server]
    public void RemoveFighter(Fighter fighter)
    {
        var id = fighter.netId.Value;
        if (fighterDict.ContainsKey(id))
        {
            fighterDict.Remove(id);
        }
    }
}
