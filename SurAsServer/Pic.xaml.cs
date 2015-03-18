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

namespace SurAsServer
{
    /// <summary>
    /// Pic.xaml 的交互逻辑
    /// </summary>
    public partial class Pic : UserControl
    {
        private Image image;
        private Vector vector;
        private Vector relativePos;
        public Pic()
        {
            InitializeComponent();
            image = new Image();
            image.TouchMove += new EventHandler<TouchEventArgs>(imageTouchDown);
            image.TouchDown += new EventHandler<TouchEventArgs>(imageTouchMove);
            
            canvas.Children.Add(image);
        }

        public Pic(String path)
        {
            InitializeComponent();
            image = new Image();
            canvas.Children.Add(image);
            image.Width = 100;
            image.Height = 100;
            //image.TouchDown += new EventHandler<TouchEventArgs>(imageTouchDown);
            image.TouchMove += new EventHandler<TouchEventArgs>(imageTouchMove);
            BitmapImage img = new BitmapImage(new Uri(path));
            image.Source = img;
            Console.WriteLine("here");
        }

        public void putPicAt(Point point)
        {
            Canvas.SetLeft(image, point.X);
            Canvas.SetTop(image, point.Y);
        }
        public void putPicAt(Rect bound)
        {
            Canvas.SetLeft(image, bound.Left-image.Width/2);
            Canvas.SetTop(image, bound.Top-image.Height/2);
        }
        private void imageTouchMove(object sender, TouchEventArgs e)
        {
            //Point topLeft = e.GetTouchPoint(this).Position;
            //topLeft.Offset(vector.X, vector.Y);
            updatePicPos(e.GetTouchPoint(this).Bounds);
            Console.WriteLine("TouchMove");
        }
        private void imageTouchDown(object sender, TouchEventArgs e)
        {
            TouchPoint tp = e.GetTouchPoint(this);
            vector = new Vector(tp.Bounds.Left - tp.Position.X, tp.Bounds.Top - tp.Position.Y);
            Console.WriteLine(vector.ToString());
            
            putPicAt(e.GetTouchPoint(this).Bounds);
            Console.WriteLine("TouchDown");
        }
        private void updatePicPos(Point point)
        {
            putPicAt(point);
        }
        private void updatePicPos(Rect bound)
        {
            putPicAt(bound);
        }

        public void setRelativePos(Vector vec)
        {
            relativePos.X = vec.X;
            relativePos.Y = vec.Y;
        }
        public Vector getRelativePos()
        {
            return relativePos;
        }

    }
}
