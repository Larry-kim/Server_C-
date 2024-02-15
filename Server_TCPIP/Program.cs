using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server_TCPIP
{
    internal class Program
    {
        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        //각 클라이언트마다 리스트에 추가

        TcpListener server = null;
        TcpClient clientSocket = null;
        static int counter = 0; // 사용자 수
        string date; // 날짜

        static void Main(string[] args)
        {
            // 쓰레드 생성
            Thread thread = new Thread(InitSocket);
            thread.IsBackground = true;
            thread.Start();
        }

        private void InitSocket()
        {
            server = new TcpListener(IPAddress.Any, 1000); // 서버 접속, IP, Port
            clientSocket = default(TcpClient); // 소켓 설정
            server.Start();
            Console.WriteLine(">> Server Started");

            while (true)
            {
                try
                {
                    counter++; // 클라이언트 수 증가
                    clientSocket = server.AcceptTcpClient();
                    Console.WriteLine(">> Accept connection from Client");

                    NetworkStream stream = clientSocket.GetStream();
                    byte[] buffer = new byte[1024]; // 버퍼
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string user_name = Encoding.UTF8.GetString(buffer, 0, bytes);
                    user_name = user_name.Substring(0, user_name.IndexOf("$")); // Client 사용자명

                    clientList.Add(clientSocket, user_name); // client 리스트에 추가
                    SendMessage(user_name + "님이 입장하셨습니다.", "", false);
                    // 모든 클라이언트에 메시지 전송

                    handleClient h_client = new handleClient(); // 클라이언트 추가
                    h_client.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new handleClient.DisconnectedHandler(handleClient_OnDisconnected);
                    h_client.startClient(clientSocket, clientList);
                }

                catch (SocketException se)
                {
                    Trace.WriteLine(string.Format("InitSocket - Exception : {0}", se.Message));
                    break;
                }

                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("InitSocket - Exception : {0}", ex.Message));
                    break;
                }
            }
            clientSocket.Close(); // client 소켓 닫기
            server.Stop(); // 서버 종료
        }

        void handleClient_OnDisconnected(TcpClient clientSocket) // 클라이언트 접속 해제 핸들러
        {
            if (clientList.ContainsKey(clientSocket))
            {
                clientList.Remove(clientSocket);
            }
        }

        private void OnReceived(string message, string user_name) // 클라이언트로부터 받은 데이터
        {
            if (message.Equals("leaveChat"))
            {
                string DisplayMessage = "leave user : " + user_name;
                Console.WriteLine(DisplayMessage);
                SendMessage("leaveChat", user_name, true);
            }
            else
            {
                string DisplayMessage = "From client : " + user_name + " : " + message;
                Console.WriteLine(DisplayMessage);
                SendMessage("leaveChat", user_name, true); // 모든 Client에게 전송
            }
        }

        public void SendMessage(string message, string user_name, bool flag)
        {
            foreach (var pair in clientList)
            {
                date = DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss"); // 현재 날짜 받기

                TcpClient client = pair.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    if (message.Equals("leaveChat"))
                        buffer = Encoding.UTF8.GetBytes(user_name + "님이 대화방을 나갔습니다.");
                    else
                        buffer = Encoding.UTF8.GetBytes("[ " + date + " ] " + user_name + " : " + message);
                }
                else
                {
                    buffer = Encoding.UTF8.GetBytes(message);
                }

                stream.Write(buffer, 0, buffer.Length); // 버퍼 쓰기
                stream.Flush();
            }
        }
    }
}
