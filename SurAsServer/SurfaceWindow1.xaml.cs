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
        Thread threadWatch = null;//负责监听客户端请求的线程
        Socket socketWatch = null;//负责监听服务端的套接字

        //保存服务器端所有和客户端通信的套接字
        Dictionary<string, Socket> dict = new Dictionary<string, Socket>();
        //保存服务器端所有负责调用通信套接字的Receive方法的线程
        Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();
        //保存设备及对应控件
        Dictionary<TouchDevice, TouchDiagram> diagrams;
        //保存服务器端所有的设备TouchDevice
        Dictionary<string, TouchDevice> dictTouchDevices;
        
        //保存图片列表
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
            //创建负责监听的套接字，参数使用IP4寻址协议，使用流式连接，使用TCP协议传输数据
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress address = IPAddress.Parse(txtIP.Text.Trim());
            //创建包含IP和port的网络节点对象
            IPEndPoint endpoint = new IPEndPoint(address, int.Parse(txtPort.Text.Trim()));
            //将负责监听的套接字绑定到唯一的IP和端口上
            try
            {
                socketWatch.Bind(endpoint);
            }
            catch (SocketException ex)
            {
                ShowMsg("绑定IP时出现异常：" + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                ShowMsg("绑定IP时出现异常：" + ex.Message);
                return;
            }

            //设置监听队列的长度
            socketWatch.Listen(10);

            //创建负责监听的线程，并传入监听方法
            threadWatch = new Thread(WatchConnection);
            threadWatch.IsBackground = true;//设置为后台线程
            threadWatch.Start();//开启线程

            ShowMsg("服务端启动监听成功~");

        }

        /// <summary>
        /// 监听客户端请求的方法
        /// </summary>
        void WatchConnection()
        {
            //持续不断的监听客户端的新的连接请求
            while (true)
            {
                Socket socketConnection = null;
                try
                {
                    //开始监听请求，返回一个新的负责连接的套接字，负责和该客户端通信
                    //注意：Accept方法会阻断当前线程！
                    socketConnection = socketWatch.Accept();
                }
                catch (SocketException ex)
                {
                    ShowMsg("服务端连接时发生异常：" + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    ShowMsg("服务端连接时发生异常：" + ex.Message);
                    break;
                }

                //将每个新产生的套接字存起来，装到键值对Dict集合中，以客户端IP:端口作为key
                dict.Add(socketConnection.RemoteEndPoint.ToString(), socketConnection);

                //检测Shaking的TouchDevice
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

                //为每个服务端通信套接字创建一个单独的通信线程，负责调用通信套接字的Receive方法，监听客户端发来的数据
                //创建通信线程
                Thread threadCommunicate = new Thread(ReceiveMsg);
                threadCommunicate.IsBackground = true;
                threadCommunicate.Start(socketConnection);//有传入参数的线程

                dictThread.Add(socketConnection.RemoteEndPoint.ToString(), threadCommunicate);

                ShowMsg(string.Format("{0} 上线了. ", socketConnection.RemoteEndPoint.ToString()));
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
                //定义一个接收消息用的字节数组缓冲区（2M大小）
                byte[] arrMsgRev = new byte[1024 * 1024 * 2];
                //将接收到的数据存入arrMsgRev,并返回真正接收到数据的长度
                int length = -1;
                try
                {
                    length = socketClient.Receive(arrMsgRev);
                    //Console.WriteLine(length);
                }
                catch (SocketException ex)
                {
                    //ShowMsg("异常：" + ex.Message + ", RemoteEndPoint: " + socketClient.RemoteEndPoint.ToString());
                    //从通信套接字结合中删除被中断连接的通信套接字
                    dict.Remove(socketClient.RemoteEndPoint.ToString());
                    //从通信线程集合中删除被中断连接的通信线程对象
                    dictThread.Remove(socketClient.RemoteEndPoint.ToString());
                    break;
                }
                catch (Exception ex)
                {
                    ShowMsg("异常：" + ex.Message);
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
                if (tag.Equals("art") && (!isReceiving)) //开始传json
                {
                    writeStream = new System.IO.MemoryStream();
                    bWriter = new System.IO.BinaryWriter(writeStream);
                    isReceiving = true;
                }
                else if (tag.EndsWith("}")) //json串结束
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
            // 把 Stream 转换成 byte[]  
            ShowMsg(fileName);
            Stream stream = new MemoryStream(bytes);
            stream.Read(bytes, 0, bytes.Length);
            // 设置当前流的位置为流的开始  
            stream.Seek(0, SeekOrigin.Begin);

            // 把 byte[] 写入文件  
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