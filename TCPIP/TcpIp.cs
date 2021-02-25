using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCPIP
{
    public static class receiveData
    {

        public const int BufferSize = 1024;

        public static int dataSize = 0;

        public static byte[] buffer = new byte[BufferSize];
    }

    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public string sb = String.Empty;
    }

    public class TcpIp
    {

        // ManualResetEvent instances signal completion.  
        public static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        public static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        public static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        public static String response = String.Empty;

        public static void Send(Socket client, byte[] data)
        {
            // Begin sending the data to the remote device.  
            client.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        public static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                // Signal that all bytes have been sent.  
                sendDone.Set();
                Console.WriteLine("send callback");
            }
        }

        public static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject
                {
                    workSocket = client
                };

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine("receive");
            }
        }
        public static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                state.buffer.CopyTo(receiveData.buffer, 0);
                receiveData.dataSize = bytesRead;

                // There might be more data, so store the data received so far.
                state.sb = Encoding.Default.GetString(state.buffer, 0, bytesRead);
                Console.WriteLine("Response received : {0}", state.sb);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                receiveDone.Set();
                Console.WriteLine("receive callback");
            }
        }
    }
}