using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public GameObject playerPrefab;
    private GameObject playerObject;

    public string myID;

    public Dictionary<string, GameObject> currentPlayers;
    public List<NetworkObjects.NetworkPlayer> latestGameState;

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);

        currentPlayers = new Dictionary<string, GameObject>();
        latestGameState = new List<NetworkObjects.NetworkPlayer>();
    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");


        //// Example to send a handshake message:
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = m_Connection.InternalId.ToString();
        SendToServer(JsonUtility.ToJson(m));


    }

    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                Debug.Log("Handshake message received!");

                myID = hsMsg.player.id;

                playerObject = Instantiate(playerPrefab);
                playerObject.gameObject.name = myID;

                currentPlayers.Add(myID, playerObject);

                StartCoroutine(SendPositionToServer(0.2f));

                break;

            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                //Debug.Log("Player update message received!");
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                //Debug.Log("Server update message received!");

                latestGameState = suMsg.players;

                //Debug.Log(recMsg);
                break;
            case Commands.DROPPED_CLIENT:
                DroppedClientMsg dcMsg = JsonUtility.FromJson<DroppedClientMsg>(recMsg);
                Destroy(currentPlayers[dcMsg.player.id]);
                currentPlayers.Remove(dcMsg.player.id);
                Debug.Log(dcMsg);
                break;
            default:
                Debug.Log("Unrecognized message received!");
                break;
        }
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);

    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");

        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }

        UpdatePlayers();
    }

    void UpdatePlayers() {
        if (latestGameState.Count > 0) {
            foreach (NetworkObjects.NetworkPlayer player in latestGameState) {
                if (player.id != myID) {
                    if (!currentPlayers.ContainsKey(player.id)) {
                        GameObject p = Instantiate(playerPrefab, player.cubPos, Quaternion.identity);
                        p.gameObject.name = player.id;
                        currentPlayers.Add(player.id, p);
                    }

                    currentPlayers[player.id].transform.position = player.cubPos;
                }
            }
            latestGameState = new List<NetworkObjects.NetworkPlayer>();
        }
    }

    IEnumerator SendPositionToServer(float waitTime) {
        while (true) {
            PlayerUpdateMsg playerUpdateMsg = new PlayerUpdateMsg();
            playerUpdateMsg.player.id = myID;
            playerUpdateMsg.player.cubPos = currentPlayers[myID].transform.position;

            SendToServer(JsonUtility.ToJson(playerUpdateMsg));

            yield return new WaitForSeconds(waitTime);
        }
    }
}