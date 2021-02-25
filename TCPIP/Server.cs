using System;
using System.Net;
using System.Net.Sockets;

namespace TCPIP
{
    public class Server : TcpIp
    {
        public IPAddress IP;
        public int PORT;
        static Socket handler;

        public Server(string IP, int PORT)
        {
            this.IP = IPAddress.Parse(IP);
            this.PORT = PORT;
        }

        public void ServerStart(ref Socket socket)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IP, PORT);

            // Create a TCP/IP socket.
            Socket listener = new Socket(IP.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                Console.WriteLine("클라이언트 접속 대기중...");
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                connectDone.WaitOne();

                socket = handler;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine("ServerStart");

            }
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            handler = listener.EndAccept(ar);

            // Signal the main thread to continue.  
            connectDone.Set();
            Console.WriteLine("클라이언트 접속");
        }
    }
}
