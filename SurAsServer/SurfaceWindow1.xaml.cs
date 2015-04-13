using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SurAsServer
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        Thread threadWatch = null;//��������ͻ���������߳�
        Socket socketWatch = null;//�����������˵��׽���

        //��������������кͿͻ���ͨ�ŵ��׽���
        Dictionary<string, Socket> dict = new Dictionary<string, Socket>();
        //��������������и������ͨ���׽��ֵ�Receive�������߳�
        Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();
        //�����豸����Ӧ�ؼ�
        Dictionary<TouchDevice, TouchDiagram> diagrams;
        //��������������е��豸TouchDevice
        Dictionary<string, TouchDevice> dictTouchDevices;
        
        //����ͼƬ�б�
        public Dictionary<ScatterViewItem, string> photoList;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            diagrams = new Dictionary<TouchDevice, TouchDiagram>();
            dictTouchDevices = new Dictionary<string, TouchDevice>();
            photoList = new Dictionary<ScatterViewItem, string>();
            InitializeComponent();

            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();
            
        }


        /// <summary>
        /// Occurs when the window is about to close. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Remove handlers for window availability events
            RemoveWindowAvailabilityHandlers();
        }

        /// <summary>
        /// Adds handlers for window availability events.
        /// </summary>
        private void AddWindowAvailabilityHandlers()
        {
            // Subscribe to surface window availability events
            ApplicationServices.WindowInteractive += OnWindowInteractive;
            ApplicationServices.WindowNoninteractive += OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable += OnWindowUnavailable;
        }

        /// <summary>
        /// Removes handlers for window availability events.
        /// </summary>
        private void RemoveWindowAvailabilityHandlers()
        {
            // Unsubscribe from surface window availability events
            ApplicationServices.WindowInteractive -= OnWindowInteractive;
            ApplicationServices.WindowNoninteractive -= OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable -= OnWindowUnavailable;
        }

        /// <summary>
        /// This is called when the user can interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowInteractive(object sender, EventArgs e)
        {
            //TODO: enable audio, animations here
        }

        /// <summary>
        /// This is called when the user can see but not interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowNoninteractive(object sender, EventArgs e)
        {
            //TODO: Disable audio here if it is enabled

            //TODO: optionally enable animations here
        }

        /// <summary>
        /// This is called when the application's window is not visible or interactive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowUnavailable(object sender, EventArgs e)
        {
            //TODO: disable audio, animations here
        }

        private void OnTouchDown(object sender, TouchEventArgs e)
        {
            Console.WriteLine("TouchDown");
            if (!e.TouchDevice.GetIsFingerRecognized())
            {
                addTouchDiagram(e.TouchDevice);
                updateTouchDiagram(e.TouchDevice);
            }
        }

        private void OnTouchMove(object sender, TouchEventArgs e)
        {
            //Console.WriteLine(e.TouchDevice.GetTouchPoint(this).Position.X);
            updateTouchDiagram(e.TouchDevice);
        }

        //private void OnTouchLeave(object sender, TouchEventArgs e)
        //{
        //    removeTouchDiagram(e.TouchDevice);
        //    removeConnection(e.TouchDevice);
        //}

        private void OnLostTouchCapture(object sender, TouchEventArgs e)
        {
            removeTouchDiagram(e.TouchDevice);
            removeConnection(e.TouchDevice);
        }

        private void addTouchDiagram(TouchDevice touchDevice)
        {
            //if two touch Device is too closed, don't addTouchDiagram
            if (isClose(touchDevice)) return;
            TouchDiagram touchDiagram = new TouchDiagram(this);
            touchDiagram.position = touchDevice.GetPosition(this);
            diagrams.Add(touchDevice, touchDiagram);
            DiagramContainerGrid.Children.Add(touchDiagram);
        }
        private void updateTouchDiagram(TouchDevice touchDevice)
        {
            TouchDiagram diagram;
            if (diagrams.TryGetValue(touchDevice, out diagram))
            {
                diagram.Update(DiagramContainerGrid,
                               touchDevice);
                diagram.position = touchDevice.GetPosition(this);
                //position = new Thickness(touchDevice.GetCenterPosition(this).X, touchDevice.GetCenterPosition(this).Y, 0, 0);
            }

        }
        private void removeTouchDiagram(TouchDevice touchDevice)
        {
            TouchDiagram diagram;
            if (diagrams.TryGetValue(touchDevice, out diagram))
            {
                diagrams.Remove(touchDevice);
                DiagramContainerGrid.Children.Remove(diagram);
            }
        }

        private void removeConnection(TouchDevice touchDevice)
        {
            foreach (KeyValuePair<string, TouchDevice> device in dictTouchDevices)
            {
                if (device.Value == touchDevice)
                {
                    string ip = device.Key;
                    Thread thread;
                    if (dictThread.TryGetValue(ip, out thread))
                    {
                        thread.Abort();
                        dictThread.Remove(ip);
                    }
                    Socket socket;
                    if (dict.TryGetValue(ip, out socket))
                    {
                        socket.Disconnect(false);
                        dict.Remove(ip);
                    }
                    dictTouchDevices.Remove(ip);
                    break;

                    
                    

                }

            }

        }

        private bool isClose(TouchDevice touchDevice)
        {
            System.Windows.Point touchPoint = touchDevice.GetCenterPosition(this);
            foreach (KeyValuePair<TouchDevice, TouchDiagram> diagram in diagrams)
            {
                System.Windows.Point historyPoint = diagram.Key.GetCenterPosition(this);
                if (getDistance(touchPoint, historyPoint) < 62500) return true;
            }
            return false;
        }
        private double getDistance(System.Windows.Point point1, System.Windows.Point point2)
        {
            return (Math.Pow((point1.X - point2.X), 2) + Math.Pow((point1.Y - point2.Y), 2));
        }








        private void btnBeginListen_Click(object sender, RoutedEventArgs e)
        {
            //��������������׽��֣�����ʹ��IP4ѰַЭ�飬ʹ����ʽ���ӣ�ʹ��TCPЭ�鴫������
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress address = IPAddress.Parse(txtIP.Text.Trim());
            //��������IP��port������ڵ����
            IPEndPoint endpoint = new IPEndPoint(address, int.Parse(txtPort.Text.Trim()));
            //������������׽��ְ󶨵�Ψһ��IP�Ͷ˿���
            try
            {
                socketWatch.Bind(endpoint);
            }
            catch (SocketException ex)
            {
                ShowMsg("��IPʱ�����쳣��" + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                ShowMsg("��IPʱ�����쳣��" + ex.Message);
                return;
            }

            //���ü������еĳ���
            socketWatch.Listen(10);

            //��������������̣߳��������������
            threadWatch = new Thread(WatchConnection);
            threadWatch.IsBackground = true;//����Ϊ��̨�߳�
            threadWatch.Start();//�����߳�

            ShowMsg("��������������ɹ�~");

        }

        /// <summary>
        /// �����ͻ�������ķ���
        /// </summary>
        void WatchConnection()
        {
            //�������ϵļ����ͻ��˵��µ���������
            while (true)
            {
                Socket socketConnection = null;
                try
                {
                    //��ʼ�������󣬷���һ���µĸ������ӵ��׽��֣�����͸ÿͻ���ͨ��
                    //ע�⣺Accept��������ϵ�ǰ�̣߳�
                    socketConnection = socketWatch.Accept();
                }
                catch (SocketException ex)
                {
                    ShowMsg("���������ʱ�����쳣��" + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    ShowMsg("���������ʱ�����쳣��" + ex.Message);
                    break;
                }

                //��ÿ���²������׽��ִ�������װ����ֵ��Dict�����У��Կͻ���IP:�˿���Ϊkey
                dict.Add(socketConnection.RemoteEndPoint.ToString(), socketConnection);

                //���Shaking��TouchDevice
                foreach (KeyValuePair<TouchDevice, TouchDiagram> diagram in diagrams)
                {
                    //if (diagram.Value.shaking)
                    if (true)
                    {
                        ShowMsg("Shaked");
                        dictTouchDevices.Add(socketConnection.RemoteEndPoint.ToString(), diagram.Key);
                        diagram.Value.iKnowYouAreShaking();
                        ShowMsg(diagram.Key.GetPosition(this).ToString());
                        break;
                    } 
                }

                //Ϊÿ�������ͨ���׽��ִ���һ��������ͨ���̣߳��������ͨ���׽��ֵ�Receive�����������ͻ��˷���������
                //����ͨ���߳�
                Thread threadCommunicate = new Thread(ReceiveMsg);
                threadCommunicate.IsBackground = true;
                threadCommunicate.Start(socketConnection);//�д���������߳�

                dictThread.Add(socketConnection.RemoteEndPoint.ToString(), threadCommunicate);

                ShowMsg(string.Format("{0} ������. ", socketConnection.RemoteEndPoint.ToString()));
            }
        }
        void ShowMsg(string msg)
        {
            txtMsg.Dispatcher.Invoke(new UpdateTextCallback(this.updateText),
                msg);
        }
        public delegate void UpdateTextCallback(string message);
        private void updateText(string message)
        {
            txtMsg.AppendText(message + "\r\n");
        }

        void ReceiveMsg(object socketClientPara)
        {
            System.IO.MemoryStream writeStream = null;
            System.IO.BinaryWriter bWriter = null;
            Socket socketClient = socketClientPara as Socket;
            Boolean isReceiving = false;

            while (true)
            {
                //����һ��������Ϣ�õ��ֽ����黺������2M��С��
                byte[] arrMsgRev = new byte[1024 * 1024 * 2];
                //�����յ������ݴ���arrMsgRev,�������������յ����ݵĳ���
                int length = -1;
                try
                {
                    length = socketClient.Receive(arrMsgRev);
                    //Console.WriteLine(length);
                }
                catch (SocketException ex)
                {
                    //ShowMsg("�쳣��" + ex.Message + ", RemoteEndPoint: " + socketClient.RemoteEndPoint.ToString());
                    //��ͨ���׽��ֽ����ɾ�����ж����ӵ�ͨ���׽���
                    dict.Remove(socketClient.RemoteEndPoint.ToString());
                    //��ͨ���̼߳�����ɾ�����ж����ӵ�ͨ���̶߳���
                    dictThread.Remove(socketClient.RemoteEndPoint.ToString());
                    break;
                }
                catch (Exception ex)
                {
                    ShowMsg("�쳣��" + ex.Message);
                    break;
                }
                if (bWriter != null)
                {
                    bWriter.Write(arrMsgRev, 0, length);
                }
                
                
                string strMsgReceive = Encoding.UTF8.GetString(arrMsgRev, 0, length);
                string tag = "";
                if (strMsgReceive.Length >= 3)
                {
                    tag = strMsgReceive.Substring(strMsgReceive.Length - 3);
                }
                if (tag.Equals("art") && (!isReceiving)) //��ʼ��json
                {
                    writeStream = new System.IO.MemoryStream();
                    bWriter = new System.IO.BinaryWriter(writeStream);
                    isReceiving = true;
                }
                else if (tag.EndsWith("}")) //json������
                {
                    bWriter.Flush();
                    String json = System.Text.Encoding.UTF8.GetString(writeStream.ToArray());
                    //Console.WriteLine(json);
                    parseJson(json, socketClient);
                    bWriter.Close();
                    bWriter = null;
                    writeStream.Close();
                    writeStream = null;
                    isReceiving = false;
                }
               

            }
        }
        void parseJson(String json, Socket source)
        {
            //JsonReader reader = new JsonTextReader(new StringReader(json));

            //while (reader.Read())
            //{
            //    Console.WriteLine(reader.Value);
            //}

            JObject jo = JObject.Parse(json);

            String picString = jo["content"].ToString();
            String ip = source.RemoteEndPoint.ToString();
            ip = ip.Substring(0, ip.LastIndexOf(":"));
            //Console.WriteLine(picString);
            byte[] picByte = Convert.FromBase64String(picString);
            BitmapImage bmp = null;

            try
            {
                string dt = "C:\\temp\\" + ip;
                ShowMsg(dt);
                if (!Directory.Exists(dt))
                    Directory.CreateDirectory(dt);
                dt = dt + "\\" +jo["filename"];
                BytesToFile(picByte, dt);
                bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(picByte);
                bmp.EndInit();
                //showPic(dt);
                showPicInDiagram(source, dt);
            }
            catch(Exception e)
            {
                bmp = null;
                ShowMsg(e.Message);
            } 
        }
        public void BytesToFile(byte[] bytes, string fileName)
        {
            // �� Stream ת���� byte[]  
            ShowMsg(fileName);
            Stream stream = new MemoryStream(bytes);
            stream.Read(bytes, 0, bytes.Length);
            // ���õ�ǰ����λ��Ϊ���Ŀ�ʼ  
            stream.Seek(0, SeekOrigin.Begin);

            // �� byte[] д���ļ�  
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(bytes);
            bw.Close();
            fs.Close();
        }
        
        private void showPicInDiagram(Socket source,String path)
        {
            ShowMsg("begin to find diagram");
            String ip = source.RemoteEndPoint.ToString();
            TouchDevice touchDevice;
            if (dictTouchDevices.TryGetValue(ip, out touchDevice))
            {
                TouchDiagram touchDiagram;
                if (diagrams.TryGetValue(touchDevice, out touchDiagram))
                {
                    ShowMsg("Find you touchdiagram");
                    touchDiagram.showPic(path);
                }
                else
                {
                    ShowMsg("Could not find touchDiagram");
                }
            }
            else
            {
                ShowMsg("Could not find touchDevice");
            }
        }



        public delegate void ShowPicHandler(String path);
        ShowPicHandler setPic;
        private void showPic(String path)
        {
            //if (System.Threading.Thread.CurrentThread != image1.Dispatcher.Thread)
            //{
            //    if (setPic == null)
            //    {
            //        setPic = new ShowPicHandler(showPic);
            //    }
            //    image1.Dispatcher.BeginInvoke(setPic, DispatcherPriority.Normal, new Object[]{path});
            //}
            //else
            //{

            //    //image1 = new Image();
            //    //BitmapImage img = getBitmap(bytes);
            //    String name = path;
            //    //File.Copy("c:\\temp\\tmp.jpg", name, true);
            //    BitmapImage img = new BitmapImage(new Uri(name));
            //    image1.Source = img;


            //    double coff = getMax(image1.Width / img.Width, image1.Height / img.Height);
            //    image1.Width = img.Width * coff;
            //    image1.Height = img.Height * coff;

            //}


        }
        private double getMax(double a, double b)
        {
            if (a > b) return a;
            return b;
        }



        public void findTouchDiagram(System.Windows.Point center, string picPath)
        {
            foreach (KeyValuePair<TouchDevice, TouchDiagram> diagram in diagrams)
            {
                if ((getDistance(diagram.Value.position, center) < Math.Pow(diagram.Value.radius, 2)))
                {
                    // the image is in the diagram
                    sendPic(diagram.Key, picPath);
                    break;
                }
               
            }
        }

        public void sendPic(TouchDevice des, string picPath)
        {
            Socket socket = getSocketByTouchDevice(des);
            try
            {
                socket.SendFile(picPath);
                byte[] end = new byte[3];
                end[0] = 0x0D;
                end[1] = 0x0A;
                end[2] = 0x0A;
                socket.Send(end);
                ShowMsg(picPath + "is sent to " + socket.RemoteEndPoint.ToString());
            }
            catch
            {

                ShowMsg("Something wrong with socket");
            }
        }

        private Socket getSocketByTouchDevice(TouchDevice touchDevice)
        {
            Console.WriteLine(dictTouchDevices.Keys.Count + "touch device(s)");
            foreach(KeyValuePair<string,TouchDevice> touch in dictTouchDevices)
            {
                if (touch.Value.Equals(touchDevice))
                {
                    Console.WriteLine(touch.Key);
                    Socket socket;
                    if (dict.TryGetValue(touch.Key, out socket))
                    {
                        return socket;
                    }
                    
                }
            }
            return null;
        }



        //public delegate void ShowPicHandler(BitmapImage img);
        //ShowPicHandler setPic;

        //void showPic(BitmapImage img)
        //{
        //    if (System.Threading.Thread.CurrentThread != image1.Dispatcher.Thread)
        //    {
        //        Console.WriteLine("judge");
        //        if (setPic == null)
        //        {
        //            setPic = new ShowPicHandler(showPic);
        //        }
        //        image1.Dispatcher.BeginInvoke(setPic, DispatcherPriority.Normal, new Object[]{img});

        //    }
        //    else
        //    {
        //        image1.Dispatcher.Invoke((Action)(() =>
        //            {
        //                image1.Source = img;
        //            }));
               
        //    }


        //    //image1.Dispatcher.Invoke(new updatePicCallBack(this.updatePic),img);
        //    //Dispatcher.Invoke((Action)(() =>
        //    //{
        //    //   // your code here.
        //    //    image1.Source = img;
        //    //}));
        //}

        ////public delegate void updatePicCallBack(BitmapImage img);

        ////private void updatePic(BitmapImage img)
        ////{
        ////    //image1.Source = img;
        ////    txtMsg.AppendText("there is a picture \r\n");
        ////    //System.Windows.Controls.Image showImage = new System.Windows.Controls.Image();
        ////    //showImage.Source = img;
        ////}

       
    }
}