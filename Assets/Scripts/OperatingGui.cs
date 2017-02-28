using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OperatingGui : MonoBehaviour
{
    public static OperatingGui Instance;

    private const int Init = 0;
    private const int Gaming = 1;

    private enum NetworkType
    {
        Client,
        Host,
        Server
    }

    public InputField nicknameInputField;
    public Dropdown fighterDropdown;
    // public Button clientButton;
    public InputField ipInputField;
    // public Button hostButton;
    // public Button serverButton;
    // public Button disconnectButton;

    public GameObject initPanel;
    public GameObject gamingPanel;

    public NetworkManager networkManager;

    private NetworkType networkType;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (this == Instance)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        nicknameInputField.text = string.Format("Player {0:000}", Random.Range(1, 1000));
        ipInputField.text = "localhost";
    }

    public void StartClient()
    {
        networkType = NetworkType.Client;
        string ip = ipInputField.text;
        if (ip.Length > 0)
        {
            networkManager.networkAddress = ip;
            networkManager.StartClient();
            SwitchPanel(Gaming);
        }
    }

    public void StartHost()
    {
        networkType = NetworkType.Host;
        networkManager.StartHost();
        SwitchPanel(Gaming);
    }

    public void StartServer()
    {
        networkType = NetworkType.Server;
        networkManager.StartServer();
        SwitchPanel(Gaming);
    }

    public void Disconnect()
    {
        switch (networkType)
        {
            case NetworkType.Client:
                networkManager.StopClient();
                break;
            case NetworkType.Host:
                networkManager.StopHost();
                break;
            case NetworkType.Server:
                networkManager.StopServer();
                break;
        }
        SwitchPanel(Init);
    }

    private void SwitchPanel(int mode)
    {
        switch (mode)
        {
            case Gaming:
                initPanel.SetActive(false);
                gamingPanel.SetActive(true);
                break;
            case Init:
            default:
                initPanel.SetActive(true);
                gamingPanel.SetActive(false);
                break;
        }
    }

    public string GetNickname()
    {
        return nicknameInputField.text;
    }

    public int GetFighterIndex()
    {
        return fighterDropdown.value;
    }

}
