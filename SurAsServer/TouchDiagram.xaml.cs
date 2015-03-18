using System;
using System.Collections.Generic;
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

namespace SurAsServer
{
    /// <summary>
    /// TouchDiagram.xaml 的交互逻辑
    /// </summary>
    public partial class TouchDiagram : UserControl
    {
        private Ellipse circle;
        private int radius = 250;
        private SolidColorBrush color = Brushes.Black;
        private TouchPoint lastPoint;
        private int numOfToRight = 0; //摇晃时到右则极值次数 
        private bool increasing;
        public bool shaking;
        private bool colorToChange = false;

        private SurfaceButton button;
        public Point position;
        private Dictionary<String,Pic> imageMap;

        public TouchDiagram()
        {
            InitializeComponent();
            imageMap = new Dictionary<string, Pic>();
            lastPoint = null;
        }

        private void shaked()
        {
            shaking = true;
            
        }

        public void iKnowYouAreShaking()
        {
            color = Brushes.BlueViolet;
            colorToChange = true;
            shaking = false;
            numOfToRight = 0;
        }

        public void Update(Grid parentGrid, TouchDevice touchDevice)
        {
            shakeChecker(touchDevice);
            updateCircle(touchDevice);
            updateButton(touchDevice);
            updatePic(touchDevice);
        }

        //检测摇晃动作
        private void shakeChecker(TouchDevice touchDevice)
        {
            //Console.Write("check shake");
            if (lastPoint == null) return;

            //到右极值点时，统计值+1
            if (touchDevice.GetTouchPoint(this).Position.X - lastPoint.Position.X > 0) increasing = true;
            if (increasing && touchDevice.GetTouchPoint(this).Position.X - lastPoint.Position.X < -2)
            {
                numOfToRight++;
                increasing = false;
                Console.Write("***************" + numOfToRight);
            }

            if (numOfToRight == 3) shaked();

        }

        private void updateCircle(TouchDevice touchDevice)
        {
            UIElement relativeTo = this;

            if (colorToChange)
            {
                circle.Stroke = color;
                colorToChange = false;
            }

            if (circle == null)
            {
                circle = new Ellipse();
                circle.Width = 2 * radius;
                circle.Height = 2 * radius;
                circle.Stroke = color;
                canvas.Children.Add(circle);
                Canvas.SetLeft(circle, touchDevice.GetTouchPoint(this).Position.X - radius);
                Canvas.SetTop(circle, touchDevice.GetTouchPoint(this).Position.Y - radius);
            }
            else
            {
                Canvas.SetLeft(circle, touchDevice.GetTouchPoint(this).Position.X - radius);
                Canvas.SetTop(circle, touchDevice.GetTouchPoint(this).Position.Y - radius);
            }
            lastPoint = touchDevice.GetTouchPoint(this);
            //Console.WriteLine(lastPoint.Position.X);
        }
        private void updateButton(TouchDevice touchDevice)
        {
            UIElement relativeTo = this;
            if (button == null)
            {
                button = new SurfaceButton();
                button.Height = 23;
                button.Width = 75;
                button.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                button.Content = "Share";
                canvas.Children.Add(button);
            }
            Canvas.SetLeft(button, touchDevice.GetTouchPoint(this).Position.X + radius - 20);
            Canvas.SetTop(button, touchDevice.GetTouchPoint(this).Position.Y - button.Height / 2);
        }
        private void updatePic(TouchDevice touchDevice)
        {
            //if (image == null)
            //{
            //    image = new Image();
            //    image.Height = 50;
            //    image.Width = 50;
            //    canvas.Children.Add(image);
            //}
            //Canvas.SetLeft(image, touchDevice.GetTouchPoint(this).Position.X + 5);
            //Canvas.SetTop(image, touchDevice.GetTouchPoint(this).Position.Y);
            foreach (KeyValuePair<String, Pic> image in imageMap)
            {
                Pic pic = image.Value;
                Vector pos = pic.getRelativePos();

                pic.putPicAt(new System.Windows.Point(position.X + pos.X, position.Y + pos.Y));
            }
        }


        public delegate void ShowPicHandler(String path);
        ShowPicHandler setPic;
        public void showPic(String path)
        {
            //Image image = new Image();
            //image.Height = 50;
            //image.Width = 50;
            //canvas.Children.Add(image);
            //Canvas.SetLeft(image,position.X + 5);
            //Canvas.SetTop(image, posstion.Y);
            
            
            //if (System.Threading.Thread.CurrentThread != image.Dispatcher.Thread)
            //{
                
            //    if (setPic == null)
            //    {
            //        setPic = new ShowPicHandler(showPic);
            //    }
            //    image.Dispatcher.BeginInvoke(setPic, DispatcherPriority.Normal, new Object[] { path });
            //}
            //else
            //{

            //    BitmapImage img = new BitmapImage(new Uri(path));
            //    image.Source = img;


            //    double coff = getMax(image.Width / img.Width, image.Height / img.Height);
            //    image.Width = img.Width * coff;
            //    image.Height = img.Height * coff;

            //}

            if (System.Threading.Thread.CurrentThread != canvas.Dispatcher.Thread)
            {
                if (setPic == null)
                {
                    setPic = new ShowPicHandler(showPic);
                }
                //canvas.Dispatcher.BeginInvoke(setPic, DispatcherPriority.Normal, new Object[] { path });
                scatterView.Dispatcher.BeginInvoke(setPic, DispatcherPriority.Normal, new Object[] { path });
                
            }
            else
            {
                Image image = new Image();
                BitmapImage img = new BitmapImage(new Uri(path));

                double minLength = 50.0;
                double coff = getMax(minLength / img.Width, minLength / img.Height);
                image.Width = img.Width * coff;
                image.Height = img.Height * coff;
                image.Source = img;

                Vector pos = randomPos(); 
                ScatterViewItem item = new ScatterViewItem();
                item.Content = image;
                item.Center = new System.Windows.Point(position.X + pos.X, position.Y + pos.Y);

                
                        
                
                scatterView.Items.Add(item);



                //Pic pic = new Pic(path);
                //Vector pos = randomPos();

                //pic.putPicAt(new System.Windows.Point(position.X + pos.X, position.Y + pos.Y));
                //pic.setRelativePos(pos);
                //canvas.Children.Add(pic);
                //imageMap.Add(path, pic);
            }


        }
        private double getMax(double a, double b)
        {
            if (a > b) return a;
            return b;
        }

        //generate a pos in the circle
        private Vector randomPos()
        {
            Random ran = new Random();
            Vector pos = new Vector();
            double r = ran.Next(radius);
            double alpha = ran.NextDouble()*2*3.14;
            pos.X = r * Math.Cos(alpha);
            pos.Y = r * Math.Sin(alpha);
            return pos;
        }

    }
}
