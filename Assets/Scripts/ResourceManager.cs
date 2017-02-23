using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{

    public static ResourceManager Instance;

    public Arena arena;
    public Fighter fighterPrefab;
    public GameObject[] bodyPrefabs;
    public Wave wavePrefab;
    public Dictionary<string, GameObject> bodyDict = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (this != Instance)
        {
            Destroy(gameObject);
        }

        foreach (var body in bodyPrefabs)
        {
            var name = body.name.Substring(0, body.name.IndexOf("Body"));
            bodyDict.Add(name, body);
        }
    }
}
