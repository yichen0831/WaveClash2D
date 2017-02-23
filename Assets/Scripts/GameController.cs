using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour
{
    // Singleton.
    public static GameController Instance { get; private set; }

    private ResourceManager resourceManager;

    private int selectedFighter;

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

    void Start()
    {
        resourceManager = ResourceManager.Instance;
        selectedFighter = 0;
    }

    void Update()
    {

    }

    [Server]
    public void RegisterPlayer(PlayerController playerController)
    {
        //selectedFighter = playerController.selectedFighter;
        //if (playerController.selectedFighter < 0 || playerController.selectedFighter >= bodyPrefabs.Length)
        //{
        //    Debug.LogError("Fighter selection error: selected " + playerController.selectedFighter);
        //    selectedFighter = 0;
        //}

        var fighter = Instantiate(resourceManager.fighterPrefab) as Fighter;
        fighter.transform.localPosition = new Vector3(0f, -1f, 0f);
        fighter.Setup(selectedFighter, playerController);
        NetworkServer.Spawn(fighter.gameObject);

        playerController.controllingFighter = fighter;
        playerController.RegisterDone();

        selectedFighter = (selectedFighter + 1) % resourceManager.bodyPrefabs.Length;
    }
}
