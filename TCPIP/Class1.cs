using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCPIP
{
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public class TcpIp
    {
        public IPAddress SERVER_IP;
        public int SERVER_PORT;
        public IPAddress CLIENT_IP;
        public int CLIENT_PORT;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        private static String response = String.Empty;

        public TcpIp(string IP, int PORT)
        {
            SERVER_IP = IPAddress.Parse(IP);
            SERVER_PORT = PORT;
            serverStart();
        }

        public TcpIp(string SERVER_IP, int SERVER_PORT, string CLIENT_IP, int CLIENT_PORT)
        {
            this.SERVER_IP = IPAddress.Parse(SERVER_IP);
            this.SERVER_PORT = SERVER_PORT;
            this.CLIENT_IP = IPAddress.Parse(CLIENT_IP);
            this.CLIENT_PORT = CLIENT_PORT;
            connectServer();
        }

        void serverStart()
        {
            IPEndPoint localEndPoint = new IPEndPoint(SERVER_IP, SERVER_PORT);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(SERVER_IP.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                connectDone.Reset();
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            connectDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            StateObject state = new StateObject();
            state.workSocket = handler;

            Thread receiveThread = new Thread(() =>
            {
                while (true)
                {
                    receiveDone.Reset();
                    Receive(handler);
                    receiveDone.WaitOne();
                    Console.WriteLine("Response received : {0}", response);
                }
            });
            receiveThread.Start();

            Thread transferThread = new Thread(() =>
            {
                while (true)
                {
                    sendDone.Reset();
                    Send(handler, Console.ReadLine());
                    sendDone.WaitOne();
                }
            });
            transferThread.Start();
        }

        void connectServer()
        {
            try
            {
                Socket client = new Socket(SERVER_IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipe = new IPEndPoint(CLIENT_IP, CLIENT_PORT);

                client.BeginConnect(ipe, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
                Thread receiveThread = new Thread(() =>
                {
                    while (true)
                    {
                        receiveDone.Reset();
                        Receive(client);
                        receiveDone.WaitOne();
                        Console.WriteLine("Response received : {0}", response);
                    }
                });
                receiveThread.Start();

                Thread transferThread = new Thread(() =>
                {
                    while (true)
                    {
                        sendDone.Reset();
                        Send(client, Console.ReadLine());
                        sendDone.WaitOne();
                    }
                });
                transferThread.Start();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
            sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
