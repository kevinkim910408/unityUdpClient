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
    public UdpClient udp; // 소켓

    // Start is called before the first frame update
    void Start()
    {
        //create new udp and bind the ip and port
        udp = new UdpClient(); // 소켓초기화

        udp.Connect("localhost", 12345); // 로컬에 접속

        //send msg to server - need to send bytes -- Encoding.ASCII.GetBytes -> converting
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1); //every one second, running heartbeat
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

    [Serializable] // Json string 을 각각의 클래스로 형변환 할때 필요함.반대로, Json으로 묶어서 서버에 줄때도 이게 있어야 Json 클래스로 만들 수 있음.
    public class AllClientsInfo
    {
        public IDColor[] player; // 서버랑 클라가 데이터를 주고받을때 변수이름, 서버에서 보내는 이름이 같아야함.
                                 // IDColor[] -  ID, Color가 들어있음.
    }

    [Serializable]
    public class IDColor // 서버로부터 ID, Color를 받아옴
    {
        public string id;
        [Serializable]
        public struct RGB
        {
            public float R;
            public float G;
            public float B;
        }
        public RGB color;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    public AllClientsInfo lastestInfo;

    public Player player;
    public GameObject capsule;
    Dictionary<string, GameObject> listOfPlayer = new Dictionary<string, GameObject>(); // string - ip, port gameObject - capsule

    bool needSpawn = false;
    bool needSpawn2 = false;

    // 클라가 서버로부터 정보를 받는곳
    void OnReceived(IAsyncResult result)
    { // result  = socket
        UdpClient socket = result.AsyncState as UdpClient; // 소켓 받은거 소켓으로 형변환

        IPEndPoint source = new IPEndPoint(0, 0); // 누가 보냈는지

        // EndReceive --> get data,  return byte[] // EndReceive 로 자료를 수신하고 다시 BeginReceive 호출하여 자료 수신 대기, 다은손님 못들어오게 끊음 일단 
        byte[] message = socket.EndReceive(result, ref source);  // 파라미터 - 서버의 정보

        //서버의 정보를 converting to string
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);  // FromJson: 서버에서 보낸 Json data를 우리가 알수있게 바꿈. <Message> 클래스로형변환
        try
        {
            // 멀티쓰레딩을 사용해서, 여기서는 유니티 함수를 사용 할 수없음.
            switch (latestMessage.cmd)
            { // latestMessage의 cmd가 무엇인지 
                case commands.NEW_CLIENT: // 기존의 클라들이 새로운 클라들의 정보를 받음
                    needSpawn2 = true;

                    player = JsonUtility.FromJson<Player>(returnData);
                    break;
                case commands.UPDATE:
                    break;
                case commands.INFO_FROM_SERVER: // 새로 접속한 애가 기존의 정보를 다 받음.
                    needSpawn = true; // 내가 기존의 클라들을 받아서 소환(나한테 보이는 클라를 생성)

                    lastestInfo = JsonUtility.FromJson<AllClientsInfo>(returnData); // returnData가 AllClientsInfo에 맞는 FromJson으로 변형됌,
                    for (int i = 0; i < lastestInfo.player.Length; ++i)
                    {
                        Debug.Log(lastestInfo.player[i].id);
                    }
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done: -->  // EndReceive 로 자료를 수신하고 다시 BeginReceive 호출하여 자료 수신 대기 
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    // 나에게 두성이와 현교를 보여줌
    void SpawnOldClients()
    {
        if(needSpawn == true)
        {
            needSpawn = false;
            for(int i = 0; i < lastestInfo.player.Length; ++i)
            {
                // Spawning
                GameObject gm = Instantiate(capsule, Vector3.zero, Quaternion.identity);

                //  Dictionary<string, GameObject> listOfPlayer에 player의 id = ip, port와 만들거 넣어줌
                listOfPlayer.Add(lastestInfo.player[i].id, gm);
            }
        }
    }

    // 두성이와 현교에게 나를 보여줌
    void SpawnPlayers()
    {
        if(needSpawn2 == true)
        {
            needSpawn2 = false;
            //gm 에 instantiating 할거 넣어줌
            GameObject gm = Instantiate(capsule, Vector3.zero, Quaternion.identity);

            //  Dictionary<string, GameObject> listOfPlayer에 player의 id = ip, port와 만들거 넣어줌
            listOfPlayer.Add(player.id, gm);
        }
    }

    void UpdatePlayers()
    {

    }

    void DestroyPlayers()
    {

    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
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

