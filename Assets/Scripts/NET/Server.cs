using System;
using System.Net;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Server : MonoBehaviour
{
    #region Singleton implementation
    public static Server Instance { get; set; }
    public int connectedPlayers = 0;

    private void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
    }
    #endregion

    public NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    private bool isActive = false;
    private const float keepAliveTickRate = 20.0f;
    private float lastKeepAlive;

    public Action connectionDropped;


    public void Init(ushort port)
    {
        connectedPlayers = 0;

        Instance = this;

        driver = NetworkDriver.Create();
        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = port;

        if (driver.Bind(endpoint) != 0)
        {
            Debug.Log("Unable to bind on port" + endpoint.Port);
            return;
        }
        else
        {
            driver.Listen();
            Debug.Log("Currently listening on port" + endpoint.Port);
        }

        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
        isActive = true;
    }

    public void ShutDown()
    {
        if (isActive)
        {
            driver.Dispose();
            connections.Dispose();
            isActive = false;
        }
        Instance = null;
    }

    public void OnDestroy()
    {
        ShutDown();
    }

    public void Update()
    {
        if (!isActive) return;

        KeepAlive();
        driver.ScheduleUpdate().Complete();
        CleanupConnections();
        AcceptNewConnections();
        UpdateMessagePump();
    }

    private void KeepAlive()
    {
        if (Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            Broadcast(new NetKeepAlive());
        }
    }

    private void CleanupConnections()
    {
        for(int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
    }

    private void AcceptNewConnections()
    {
        NetworkConnection c;
        while ((c = driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(c);
            Debug.Log("Accepted a connection");

            NetWelcome nw = new NetWelcome();
            nw.AssignedTeam = connectedPlayers;
            SendToClient(c, nw);

            connectedPlayers++;

            if (connectedPlayers == 2)
            {
                Broadcast(new NetStartGame());
            }
        }
    }

    private void UpdateMessagePump()
    {
        DataStreamReader stream;

        if (!isActive) return;

        for (int i = 0; i < connections.Length; i++)
        {
            NetworkEvent.Type cmd;

            while (isActive && (cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    NetUtility.OnData(stream, connections[i], this);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[i] = default(NetworkConnection);

                    Broadcast(new NetPlayerLeft());
                    Debug.Log("¡iServer¡j¤w¼s¼½ PlayerLeft");

                    NetUtility.C_PLAYER_LEFT?.Invoke(new NetPlayerLeft());
                    if (!isActive) return;
                }
            }
        }
    }

    //server specific
    public void SendToClient(NetworkConnection connection, NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }

    public void Broadcast(NetMessage msg)
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if (connections[i].IsCreated)
            {
                //Debug.Log($"Sending {msg.Code} to : {connections[i].InternalId}");
                SendToClient(connections[i], msg);
            }
        }
    }

    private void OnApplicationQuit()
    {
        ShutDown();
    }
}