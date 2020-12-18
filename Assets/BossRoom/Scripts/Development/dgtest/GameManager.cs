using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameObject m_playerSpawn;

    private List<GameObject> m_players = new List<GameObject>(10);

    public GameObject PlayerPrefab;

    // Start is called before the first frame update
    void Start()
    {
        m_playerSpawn = GameObject.Find("SpawnPoint");

        GameObject main_player = Instantiate(PlayerPrefab);
        main_player.transform.position = m_playerSpawn.transform.position;
        main_player.GetComponent<NetworkedObject>().Spawn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
