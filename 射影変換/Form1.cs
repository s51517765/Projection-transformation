using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//OpenCvSharp4 //OpenCvSharp4.runtime.win //OpenCvSharp4.Windows //OpenCvSharp4.WpfExtensions
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace 射影変換
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int imgSizeX, imgSizeY;

        private void solve_squere_line(System.Drawing.Point px0, System.Drawing.Point px1, System.Drawing.Point px2, System.Drawing.Point px3, ref int[] x, ref int[] y)
        {
            double[] px_ = { px0.X, px1.X, px2.X, px3.X };
            double[] py_ = { px0.Y, px1.Y, px2.Y, px3.Y };

            System.Drawing.Point COG = new System.Drawing.Point(); //Center of Gravity
            COG.X = (px0.X + px1.X + px2.X + px3.X) / 4;
            COG.Y = (px0.Y + px1.Y + px2.Y + px3.Y) / 4;

            double min = 1.01;
            double max = 1000;
            double rate = min + max / 2;

            double[] px = new double[4];
            double[] py = new double[4];

            int k = 0;//何回BinarySerachしたか

            while (true) //点が画像の中なら
            {
                k += 1;
                for (int i = 0; i < 4; i++)
                {
                    px[i] = rate * (px_[i] - COG.X) + COG.X;
                    py[i] = rate * (py_[i] - COG.Y) + COG.Y;
                }

                //画像のサイズの中かどうか
                for (int i = 0; i < 4; i++)
                {
                    if (px[i] < 1 || imgSizeX - 1 < px[i] || py[i] < 1 || imgSizeY - 1 < py[i])
                    {//画像の外
                        max = rate;
                        rate = (rate + min) / 2;
                        break;
                    }
                    if (i == 3)
                    {
                        min = rate;
                        rate = (max + rate) / 2;
                    }
                }
                if (Math.Abs(max - min) < 0.00001) break; //MaxとMinが一致したら
            }
            for (int i = 0; i < 4; i++)
            {
                x[i] = (int)px[i];
                y[i] = (int)py[i];
            }
            //k=27
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //https://teratail.com/questions/256372
            this.Size = new System.Drawing.Size(1100, 900);

            System.Drawing.Point px0 = new System.Drawing.Point(108, 311);   //直したい台形の座標
            System.Drawing.Point px1 = new System.Drawing.Point(46, 643);
            System.Drawing.Point px2 = new System.Drawing.Point(414, 639);
            System.Drawing.Point px3 = new System.Drawing.Point(350, 310);

            Image img = System.Drawing.Image.FromFile("sample.jpg");

            imgSizeX = img.Width;
            imgSizeY = img.Height;
            int[] x = new int[4];
            int[] y = new int[4];
            solve_squere_line(px0, px1, px2, px3, ref x, ref y);
            px0.X = x[0];
            px0.Y = y[0];
            px1.X = x[1];
            px1.Y = y[1];
            px2.X = x[2];
            px2.Y = y[2];
            px3.X = x[3];
            px3.Y = y[3];


            // 元の画像に四角形を表示
            PictureBox p1 = new PictureBox();
            Bitmap bt1 = new Bitmap(img.Width, img.Height);
            Graphics gp1 = Graphics.FromImage(bt1);
            gp1.DrawImage(img, 0, 0, img.Width, img.Height);

            Pen skyBluePen = new Pen(Brushes.Red);
            skyBluePen.Width = 4.0F;

            gp1.DrawLine(skyBluePen, px0, px1);
            gp1.DrawLine(skyBluePen, px1, px2);
            gp1.DrawLine(skyBluePen, px2, px3);
            gp1.DrawLine(skyBluePen, px3, px0);

            p1.Image = bt1;
            p1.Location = new System.Drawing.Point(0, 0);
            p1.Size = new System.Drawing.Size(img.Width, img.Height);
            this.Controls.Add(p1);

            // 四角形で切り取って表示
            System.Drawing.Point[] p2pt = { px0, px1, px2, px3 };
            GraphicsPath p2path = new GraphicsPath();
            p2path.AddPolygon(p2pt);
            Region p2region = new Region(p2path);
            Bitmap p2btm = new Bitmap(468, 831);
            Graphics gp2 = Graphics.FromImage(p2btm);
            gp2.Clip = p2region;
            gp2.DrawImage(img, gp1.VisibleClipBounds);

            // p2の四角形を引き伸ばして表示する
            Mat src_img = BitmapConverter.ToMat((Bitmap)img);
            Mat dst_img = src_img;

            // 四角形の変換前と変換後の対応する頂点をそれぞれセットする
            Point2f[] src_pt = new Point2f[4];
            src_pt[0] = new Point2f(px0.X, px0.Y);
            src_pt[1] = new Point2f(px1.X, px1.Y);
            src_pt[2] = new Point2f(px2.X, px2.Y);
            src_pt[3] = new Point2f(px3.X, px3.Y);

            Point2f[] dst_pt = new Point2f[4];
            dst_pt[0] = new Point2f(0, 0);      //左上
            dst_pt[1] = new Point2f(0, 831);    //左下
            dst_pt[2] = new Point2f(468, 831);  //右下
            dst_pt[3] = new Point2f(468, 0);    //右上

            Mat map_matrix = Cv2.GetPerspectiveTransform(src_pt, dst_pt);

            // 指定された透視投影変換行列により，cvWarpPerspectiveを用いて画像を変換させる
            OpenCvSharp.Size mysize = new OpenCvSharp.Size(468, 831);
            InterpolationFlags OIFLiner = InterpolationFlags.Linear;
            BorderTypes OBTDefault = BorderTypes.Default;
            Cv2.WarpPerspective(src_img, dst_img, map_matrix, mysize, OIFLiner, OBTDefault);

            // 結果を表示する
            PictureBox p3 = new PictureBox();
            p3.Image = dst_img.ToBitmap();
            p3.Location = new System.Drawing.Point(500, 0);
            p3.Size = new System.Drawing.Size(468, 831);
            this.Controls.Add(p3);
        }
    }
}
