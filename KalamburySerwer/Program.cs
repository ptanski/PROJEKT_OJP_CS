using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace KalamburySerwer
{
    class Client
    {
        public byte id;
        public byte R, G, B;
        public byte X1, X2;
        public byte Y1, Y2;
        public bool unset;
    }
    class Program
    {
        static int startingPort = 61400;
        static byte clientId = 0;
        static UdpClient udpClient;
        static BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>();
        static void manageClient(byte id )
        {
            id -= 1;
            UdpClient udpClientt = new UdpClient(startingPort + id);
            Client client = new Client();
            client.id = id;
            while( true )
            {
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, startingPort + id);
                byte[] buffer = udpClientt.Receive(ref remoteIpEndPoint);
                if (buffer[0] == 0) // info o kolorze
                {
                    System.Console.WriteLine("ID = {3} R = {0} G = {1} B = {2}", buffer[1], buffer[2], buffer[3], id);
                    client.R = buffer[1];
                    client.G = buffer[2];
                    client.B = buffer[3];
                }
                else if (buffer[0] == 1) // info o pozycji
                {
                    int X = buffer[1] * 255 + buffer[2];
                    int Y = buffer[3] * 255 + buffer[4];
                    System.Console.WriteLine("ID = {2} X = {0} Y = {1}", X, Y, id);
                    byte[] data = { client.id, client.R, client.G, client.B, buffer[1], buffer[2], buffer[3], buffer[4] };
                    queue.Add(data);
                }
                else if (buffer[0] == 2) // rozlaczenie
                {
                    byte[] data = { client.id, 0, 0, 0, 0, 0, 0, 0 };
                    queue.Add(data);
                }
            }
        }

        static void sendPoints()
        {
            UdpClient udpClientt = new UdpClient();
            while(true)
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, 10102);
                byte[] data = queue.Take();
                udpClientt.Send( data, data.Length, ip );
            }
        }

        static void Main(string[] args)
        {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            udpClient = new UdpClient(10101);
            byte[] buffer;
            Thread tread = new Thread(() => sendPoints());
            tread.Start();
            while( true )
            {
                buffer = udpClient.Receive(ref remoteIpEndPoint);
                
                {
                    foreach (var el in buffer)
                        System.Console.Write((char)el);
                    if( buffer[0] == 'c' )
                    {
                        UdpClient udpClientt = new UdpClient();
                        udpClientt.Connect(remoteIpEndPoint.Address, 10102);
                        byte[] msg = { 0, clientId++ };
                        Thread.Sleep(100);
                        udpClientt.Send( msg, 2);
                        udpClientt.Close();
                        Thread tr = new Thread(() => manageClient(clientId));
                        tr.Start();
                    }
                }
                    

                System.Console.WriteLine();
            }
        }
    }
}
