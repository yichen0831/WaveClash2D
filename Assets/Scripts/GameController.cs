using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour
{
    struct ScoreStruct
    {
        public string Nickname { get; set; }
        public float AliveTime { get; set; }
    }

    // Singleton.
    public static GameController Instance { get; private set; }

    private Dictionary<uint, Fighter> fighterDict = new Dictionary<uint, Fighter>();
    private Dictionary<string, float> topScoreDict = new Dictionary<string, float>();
    private List<ScoreStruct> topScoreList = new List<ScoreStruct>();
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
            updateRankCountDown = 0.15f;

            // Update everyone's highest score.
            foreach (var fighter in fighterDict.Values)
            {
                if (topScoreDict.ContainsKey(fighter.nickname))
                {
                    float highest = topScoreDict[fighter.nickname];
                    if (fighter.AliveTime > highest)
                    {
                        topScoreDict[fighter.nickname] = fighter.AliveTime;
                    }
                }
                else
                {
                    topScoreDict.Add(fighter.nickname, fighter.AliveTime);
                }
            }

            topScoreList.Clear();
            foreach (var score in topScoreDict)
            {
                topScoreList.Add(new ScoreStruct { Nickname = score.Key, AliveTime = score.Value });
            }
            topScoreList.Sort((a, b) => { return (int)(-(a.AliveTime - b.AliveTime) * 10); });

            UpdateScore();
        }
    }

    [Server]
    private void UpdateScore()
    {
        var stringBuilder = new StringBuilder();

        // Find the top 5 scores.
        int total = 0;
        for (int i = 0; i < topScoreList.Count; i++)
        {
            stringBuilder.AppendFormat("{0}: {1:0.0}", topScoreList[i].Nickname, topScoreList[i].AliveTime).Append("\n");
            total++;
            if (total >= 5)
            {
                break;
            }
        }

        ShowScore(stringBuilder.ToString());

        RpcUpdateScore(stringBuilder.ToString());
    }

    [ClientRpc]
    private void RpcUpdateScore(string result)
    {
        if (isServer)
        {
            return;
        }

        ShowScore(result);
    }

    private void ShowScore(string score)
    {
        OperatingGui.Instance.scoreText.text = score;
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

    public void ResetScore()
    {
        if (!isServer)
        {
            return;
        }

        topScoreDict.Clear();
    }
}
