using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp; // Socket

    // Start is called before the first frame update
    void Start()
    {
        //create new udp and bind the ip and port
        udp = new UdpClient(); // Initailing socket

        udp.Connect("3.20.240.191", 12345); // AWS - SERVER _ Junho Kim
        //udp.Connect("localhost", 12345); //  LOCAL

        //send msg to server - need to send bytes -- Encoding.ASCII.GetBytes -> converting
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1); //every one second, running heartbeat
        InvokeRepeating("SendPosition", 0.3f, 0.3f); // call send position function every 0.3 seconds
    }

    void OnDestroy()
    {
        udp.Dispose();
    }


    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        INFO_FROM_SERVER,
        GET_ID_FROM_SERVER,
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
    }

    [Serializable]
    public class Player
    {
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;
    }


    [Serializable]
    public class GameState
    {
        public Player[] players;

    }
    // Json string 을 각각의 클래스로 형변환 할때 필요함.반대로, Json으로 묶어서 서버에 줄때도 이게 있어야 Json 클래스로 만들 수 있음.
    // -> to convert json string to class, I need [Serializable]. In the opposite way, also need this
    [Serializable] 
    public class AllClientsInfo
    {
        // 서버랑 클라가 데이터를 주고받을때 변수이름, 서버에서 보내는 이름이 같아야함.
        // -> all the variables name should be same with server!!!!!
        public IDColorPos[] players;  
    }

    [Serializable]
    public class IDColorPos // get id, color, position from server
    {   
        //***************************************** very important!!!!!!!!!!!!!!!!!!!!!!! *********************************
        //dictionary key and variable name should be same!!!!!!!!!!!!!!!!!!!!!!! --> even upper and lower!!!!!!!!!
        public string id;
        [Serializable]
        public struct RGB
        {
            public float R;
            public float G;
            public float B;
        }
        public RGB color;

        [Serializable]
        public struct pos
        {
            public float x;
            public float y;
            public float z;
        }
        public pos position;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    public AllClientsInfo lastestInfo;
    public AllClientsInfo allClientsInfo;
    public IDColorPos idFromServer;


    public Player player;
    public GameObject capsule;
    public GameObject me;
    // string - ip, port gameObject - capsule --> change this to cube at the last
    Dictionary<string, GameObject> listOfPlayer = new Dictionary<string, GameObject>(); 

    bool needSpawn = false;
    bool needSpawn2 = false;

    // 클라가 서버로부터 정보를 받는곳
    // clients get information from server
    void OnReceived(IAsyncResult result) // result  = socket
    { 
       
        UdpClient socket = result.AsyncState as UdpClient; // result convert to udpclient's socket

        IPEndPoint source = new IPEndPoint(0, 0); // sent by whom? or send to where? idk

        // EndReceive 로 자료를 수신하고 다시 BeginReceive 호출하여 자료 수신 대기, 다은손님 못들어오게 끊음 일단 
        // EndReceive --> get data,  return byte[] --> send 

        // 파라미터 - 서버의 정보
        // -> parameter = information of server
        byte[] message = socket.EndReceive(result, ref source);

        // convert server information to string
        string returnData = Encoding.ASCII.GetString(message);
        //Debug.Log("Got this: " + returnData);

        // FromJson: 서버에서 보낸 Json data를 우리가 알수있게 바꿈. <Message> 클래스로형변환
        // -> FromJson: convert Json data from server --> to Message class
        latestMessage = JsonUtility.FromJson<Message>(returnData);  
        try
        {
            // 멀티쓰레딩을 사용해서, 여기서는 유니티 함수를 사용 할 수없음.
            // -> because of multi threading, I cannot use unity method here.
            switch (latestMessage.cmd)
            { 
                // latestMessage의 cmd -> enum 
                case commands.NEW_CLIENT: // old clients get new client's info
                    needSpawn2 = true;
                    player = JsonUtility.FromJson<Player>(returnData);
                    break;

                case commands.UPDATE:
                    allClientsInfo = JsonUtility.FromJson<AllClientsInfo>(returnData); // contains old clients' color id and position

                    for (int i = 0; i < allClientsInfo.players.Length; ++i)
                    {
                        if (allClientsInfo.players[i].id == idFromServer.id)
                            Debug.Log(allClientsInfo.players[i].position.x + " " + allClientsInfo.players[i].position.y +
                                " " + allClientsInfo.players[i].position.z);
                    }
                    break;

                case commands.INFO_FROM_SERVER: // 새로 접속한 애가 기존의 정보를 다 받음.
                    needSpawn = true; // 내가 기존의 클라들을 받아서 소환(나한테 보이는 클라를 생성)
                    lastestInfo = JsonUtility.FromJson<AllClientsInfo>(returnData); // returnData가 AllClientsInfo에 맞는 FromJson으로 변형됌,
                    for (int i = 0; i < lastestInfo.players.Length; ++i)
                    {
                        //Debug.Log(lastestInfo.players[i].id);
                    }
                    break;

                case commands.GET_ID_FROM_SERVER: // get ip, port from the server
                    idFromServer = JsonUtility.FromJson<IDColorPos>(returnData);
                    break;

                default:
                    //Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            //Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done: -->  // EndReceive 로 자료를 수신하고 다시 BeginReceive 호출하여 자료 수신 대기 
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    // show me old clients
    void SpawnOldClients()
    {
        if(needSpawn == true)
        {
            needSpawn = false;
            for(int i = 0; i < lastestInfo.players.Length; ++i)
            {
                // Spawning
                GameObject gm = Instantiate(capsule, Vector3.zero, Quaternion.identity);

                //  Dictionary<string, GameObject> listOfPlayer에 player의 id = ip, port와 만들거 넣어줌
                listOfPlayer.Add(lastestInfo.players[i].id, gm);
            }
        }
    }

    // old clients can see me 
    void SpawnPlayers()
    {
        if(needSpawn2 == true)
        {
            needSpawn2 = false;
            GameObject gm = Instantiate(capsule, Vector3.zero, Quaternion.identity);

            //  Dictionary<string, GameObject> listOfPlayer에 player의 id = ip, port와 만들거 넣어줌
            listOfPlayer.Add(player.id, gm);
        }
    }

    void UpdatePlayers()
    {
        for(int i = 0; i < allClientsInfo.players.Length; ++i)
        {
            if (listOfPlayer.ContainsKey(allClientsInfo.players[i].id))
            {
                listOfPlayer[allClientsInfo.players[i].id].transform.GetComponent<Renderer>().material.color 
                    = new Color(allClientsInfo.players[i].color.R, allClientsInfo.players[i].color.G, allClientsInfo.players[i].color.B);

                listOfPlayer[allClientsInfo.players[i].id].transform.position
                    = new Vector3(allClientsInfo.players[i].position.x, allClientsInfo.players[i].position.y, allClientsInfo.players[i].position.z);
            }
            if(allClientsInfo.players[i].id == idFromServer.id)
            {
                me.GetComponent<Renderer>().material.color
                    = new Color(allClientsInfo.players[i].color.R, allClientsInfo.players[i].color.G, allClientsInfo.players[i].color.B);
            }
        }
    }

    void DestroyPlayers()
    {

    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void SendPosition()
    {
        string pos = "position " + me.transform.position.x.ToString() + " " + me.transform.position.y.ToString() + " "
             + me.transform.position.z.ToString() + " ";

        Byte[] sendBytes = Encoding.ASCII.GetBytes(pos);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnOldClients();
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}

