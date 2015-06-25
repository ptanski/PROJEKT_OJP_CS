using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Net.Sockets;
using System.ComponentModel;
using System.Net;

namespace Kalambury
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    
    public partial class MainWindow : Window
    {
        bool doDraw;
        bool canDraw;
        bool connected;
        byte id;
        int port = 61400;
        double pX, pY, cX, cY;
        SolidColorBrush userBrush;
        UdpClient sendDrawingHere;
        UdpClient udpClient;
        BackgroundWorker drawer;
        UdpClient udpClientt;
        class PPoint
        {
            public bool isHere;
            public int x;
            public int y;
            public PPoint() { isHere = false; x = -1; y = -1; }
        }
        PPoint[] clients;
        public MainWindow()
        {
            InitializeComponent();

            clients = new PPoint[256];
            for (int i = 0; i < clients.Length; ++i)
                clients[i] = new PPoint();

            this.doDraw = this.canDraw = this.connected = false;
            drawingBox.Background = new SolidColorBrush(Color.FromRgb(235, 235, 235));

            drawingBox.MouseEnter += drawingBox_MouseEnter;
            drawingBox.MouseLeave += drawingBox_MouseLeave;
            drawingBox.MouseMove += drawingBox_MouseMove;
            drawingBox.MouseLeftButtonDown += drawingBox_MouseLeftButtonDown;
            drawingBox.MouseLeftButtonUp += drawingBox_MouseLeftButtonUp;

            chooseColor.Click += chooseColor_Click;

            Random random = new Random();


            userBrush = new SolidColorBrush(Color.FromRgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));

            udpClient = new UdpClient(addressField.Text, 22222);
            chosenColor.Background = userBrush;

            disconnectButton.IsEnabled = false;

            connectButton.Click += connectButton_Click;
            disconnectButton.Click += disconnectButton_Click;

            drawer = new BackgroundWorker();

            drawer.DoWork += drawer_DoWork;

            drawer.ProgressChanged += drawer_ProgressChanged;

        }

        void drawer_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var recv = (byte[])e.UserState;
            Line line = new Line();

            var X = recv[4] * 255 + recv[5];
            var Y = recv[6] * 255 + recv[7];
            var R = recv[1];
            var G = recv[2];
            var B = recv[3];
            var recvId = recv[0];

            System.Console.WriteLine("RECVID={5}\tX={0}\tY={1}\tR={2}\tG={3}\tB={4}", X, Y, R, G, B, recvId);

            SolidColorBrush colorBrush = new SolidColorBrush();
            colorBrush.Color = System.Windows.Media.Color.FromRgb(R, G, B);
            line.X1 = clients[recvId].x;
            line.X2 = X;
            line.Y1 = clients[recvId].y;
            line.Y2 = Y;
            line.Stroke = colorBrush;
            drawingBox.Children.Add(line);
            clients[recvId].x = X;
            clients[recvId].y = Y;
        }

        void drawer_DoWork(object sender, DoWorkEventArgs e)
        {
            while( true )
            {
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var recv = udpClientt.Receive(ref remoteIpEndPoint);
                var X = recv[4] * 255 + recv[5];
                var Y = recv[6] * 255 + recv[7];
                var R = recv[1];
                var G = recv[2];
                var B = recv[3];
                var recvId = recv[0];
                
                if (X + Y + R + G + B == 0)
                {
                    clients[recvId].isHere = false;
                    continue;
                }
                if (clients[recvId].isHere == false)
                {
                    clients[recvId].isHere = true;
                    clients[recvId].x = X;
                    clients[recvId].y = Y;
                    continue;
                }

                drawer.ReportProgress(10, recv);
            }

        }

        void disconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connectButton.IsEnabled = true;
                disconnectButton.IsEnabled = false;
                var message = "disconnect";
                this.connected = false;
                udpClient.Send(Encoding.ASCII.GetBytes(message), message.Length);
                udpClientt.Close();
            }
            catch( Exception Ex )
            {
                System.Console.WriteLine(Ex.Message);
                connectButton.IsEnabled = false;
                disconnectButton.IsEnabled = true;
                this.connected = true;
            }
        }

        void connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connectButton.IsEnabled = false;
                disconnectButton.IsEnabled = true;
                var message = "connect";
                this.connected = true;
                udpClient.Connect(addressField.Text, 10101);
                udpClient.Send(Encoding.ASCII.GetBytes(message), message.Length);
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 10102);
                udpClientt = new UdpClient(10102);
                var recv = udpClientt.Receive(ref remoteIpEndPoint);
                if( recv[0] == 0 )
                {
                    id = recv[1];
                    port += id;
                }

                sendDrawingHere = new UdpClient(addressField.Text, port);
                sendDrawingHere.Connect(addressField.Text, port);
                drawer.WorkerReportsProgress = true;
                drawer.RunWorkerAsync();
            }
            catch( Exception Ex )
            {
                System.Console.WriteLine(Ex.Message);
                connectButton.IsEnabled = true;
                disconnectButton.IsEnabled = false;
                this.connected = false;
            }
            
        }

        void chooseBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog cDial = new ColorDialog();
            cDial.ShowDialog();
            ((SolidColorBrush)drawingBox.Background).Color = System.Windows.Media.Color.FromRgb(cDial.Color.R, cDial.Color.G, cDial.Color.B);
        }

        void chooseColor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog cDial = new ColorDialog();
            cDial.ShowDialog();
            userBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(cDial.Color.R, cDial.Color.G, cDial.Color.B));
            chosenColor.Background = userBrush;
        }

        void drawingBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.doDraw = false;
            byte[] message = new byte[] { 2 };
            sendDrawingHere.Send(message, 1);
        }

        void drawingBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.doDraw = true;
            if (!this.connected)
                return;
            cX = e.GetPosition(drawingBox).X;
            cY = e.GetPosition(drawingBox).Y;

            byte[] message = new byte[]{0, userBrush.Color.R, userBrush.Color.G, userBrush.Color.B};
            sendDrawingHere.Send(message, 4);

            message = new byte[] { 1, 
                cX > 255 ? (byte)1 : (byte)0,
                cX > 255 ? (byte)(cX-255) : (byte)cX, 
                cY > 255 ? (byte)1 : (byte)0,
                cY > 255 ? (byte)(cY-255) : (byte)cY};
            sendDrawingHere.Send(message, 5);
            
        }

        void drawingBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.doDraw && this.canDraw && this.connected)
            {
                pX = cX;
                pY = cY;
                cX = e.GetPosition(drawingBox).X;
                cY = e.GetPosition(drawingBox).Y;

                byte[] message = new byte[] { 1, 
                    cX > 255 ? (byte)1 : (byte)0,
                    cX > 255 ? (byte)(cX-255) : (byte)cX, 
                    cY > 255 ? (byte)1 : (byte)0,
                    cY > 255 ? (byte)(cY-255) : (byte)cY};
                sendDrawingHere.Send(message, 5);
            }
        }

        void drawingBox_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.canDraw = false;
            this.doDraw = false;
        }

        void drawingBox_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.canDraw = true;
        }

    }
}
