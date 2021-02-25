using System;
using System.Net;
using System.Net.Sockets;

namespace TCPIP
{
    public class Client : TcpIp
    {
        public IPAddress IP;
        public int PORT;

        public Client(string IP, int PORT)
        {
            this.IP = IPAddress.Parse(IP);
            this.PORT = PORT;
        }

        public void ConnectServer(ref Socket client)
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IP, PORT);
                client = new Socket(ipe.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                Console.WriteLine("서버 접속 대기중...");
                client.BeginConnect(ipe, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine("ConnectServer");
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);
                Console.WriteLine("서버 접속");

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                // Signal that the connection has been made.  
                connectDone.Set();
                Console.WriteLine("ConnectCallback");
            }
        }
    }
}
