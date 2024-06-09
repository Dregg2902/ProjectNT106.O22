using Client;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace Client
{
    public partial class Form1 : Form
    {
        #region 
        Elephant[] elephant;
        Lion[] lion;
        Tiger[] tiger;
        Leopard[] leopard;
        Dog[] dog;
        Wolf[] wolf;
        Cat[] cat;
        Mouse[] mouse;

        public static Animal[] animals;
        #endregion

        PictureBox[] picForcast = new PictureBox[4];
        PictureBox[] pieces = new PictureBox[16];
        Bitmap[] b = new Bitmap[16];

        
        private int tableIndex;
        private int side;
        delegate void LabelDelegate(Label label, string str);
        delegate void ButtonDelegate(Button button, bool flag);
        LabelDelegate labelDelegate;
        ButtonDelegate buttonDelegate;
        private Service service;
       
        private bool order;

        public Form1(int TableIndex, int Side, StreamWriter sw)
        {
            InitializeComponent();

            //Cắt ảnh quân cờ
            Bitmap sourcePic = Client.Properties.Resources.pieces;
            for (int i = 0; i < 16; i += 2)
            {
                b[i] = new Bitmap(70, 70);
                b[i + 1] = new Bitmap(70, 70);
                Graphics g1 = Graphics.FromImage(b[i]);
                Graphics g2 = Graphics.FromImage(b[i + 1]);
                g1.DrawImage(sourcePic, new Rectangle(0, 0, 70, 70), new Rectangle(i / 2 * 70, 0, 70, 70), GraphicsUnit.Pixel);
                g2.DrawImage(sourcePic, new Rectangle(0, 0, 70, 70), new Rectangle(i / 2 * 70, 70, 70, 70), GraphicsUnit.Pixel);
                g1.Dispose();
                g2.Dispose();
            }

            Animal.setPiecePosition += onDefinePieces;
            Animal.pieceMove += onPieceMove;
            Animal.pieceBeEaten += onPieceBeEaten;

            #region 
            //Tạo định hướng bước đi
            for (int i = 0; i < picForcast.Length; i++)
            {
                picForcast[i] = new PictureBox();
                picForcast[i].Size = new Size(70, 70);
                picForcast[i].BackColor = Color.FromArgb(100, Color.Blue);
                picForcast[i].Visible = false;
                picForcast[i].Cursor = Cursors.Hand;
                picForcast[i].Click += picForcast_Click;
            }
            pic.Controls.AddRange(picForcast);
            #endregion

            #region 
            //Tạo quân cờ
            elephant = new Elephant[]{
                new Elephant(new Point(1,7),camp.red,0),
                new Elephant(new Point(7,3),camp.black,1)
            };
            lion = new Lion[]{
                new Lion(new Point(7,9),camp.red,2),
                new Lion(new Point(1,1),camp.black,3)
            };
            tiger = new Tiger[]{
                new Tiger(new Point(1,9),camp.red,4),
                new Tiger(new Point(7,1),camp.black,5)
            };
            leopard = new Leopard[]{
                new Leopard(new Point(5,7),camp.red,6),
                new Leopard(new Point(3,3),camp.black,7)
            };
            dog = new Dog[]{
                new Dog(new Point(6,8),camp.red,8),
                new Dog(new Point(2,2),camp.black,9)
            };
            wolf = new Wolf[]{
                new Wolf(new Point(3,7),camp.red,10),
                new Wolf(new Point(5,3),camp.black,11)
            };
            cat = new Cat[]{
                new Cat(new Point(2,8),camp.red,12),
                new Cat(new Point(6,2),camp.black,13)
            };
            mouse = new Mouse[]{
                new Mouse(new Point(7,7),camp.red,14),
                new Mouse(new Point(1,3),camp.black,15)
            };
            #endregion
            #region 
            //Mảng chứa quân cờ
            animals = new Animal[16]{
                elephant[0],
                elephant[1],
                lion[0],
                lion[1],
                tiger[0],
                tiger[1],
                leopard[0],
                leopard[1],
                dog[0],
                dog[1],
                wolf[0],
                wolf[1],
                cat[0],
                cat[1],
                mouse[0],
                mouse[1],
            };
            #endregion
            tableIndex = TableIndex;
            side = Side;
            order = false;
            labelDelegate = new LabelDelegate(SetLabel);
            buttonDelegate = new ButtonDelegate(SetButton);
            service = new Client.Service(listBox1, sw);
            for (int i = 0; i < 16; i ++)
            {
                pieces[i].Enabled = false;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            labelSide0.Text = "";
            labelSide1.Text = "";
            labelOrder.Text = "";
            label1.Text = "";
        }
        #region
        int pieceIndexTemp = 0;
        public void SetLabel(Label label, string str)
        {
            if (label.InvokeRequired)
            {
                this.Invoke(labelDelegate, label, str);
            }
            else
            {
                label.Text = str;
            }
        }
        private void SetButton(Button button, bool flag)
        {
            if (button.InvokeRequired)
            {
                this.Invoke(buttonDelegate, button, flag);
            }
            else
            {
                button.Enabled = flag;
            }
        }
        private void buttonStart_Click(object sender, EventArgs e)
        {
            service.SendToServer(string.Format("Start,{0},{1}", tableIndex.ToString(), side.ToString())); 
            this.button1.Enabled = false;
        }
        public void Restart(string str)
        {
            MessageBox.Show(str, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            service.AddItemToListBox(str);
            piecesRefresh();
            SetButton(button1, true);
        }

        public void ShowTalk(string talkMan, string str)
        {
            service.AddItemToListBox(string.Format("{0} says: {1}", talkMan, str));
        }
        //Display information
        public void ShowMessage(string str)
        {
            service.AddItemToListBox(str);
        }
        //exit button
        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        // send button
        private void buttonSend_Click(object sender, EventArgs e)
        {
            service.SendToServer(string.Format("Talk,{0},{1}", tableIndex, textBox1.Text));
            textBox1.Text = "";
        }
        public void SetTableSideText(string sideString, string labelSideString, string listBoxString)
        {
            string s = "Red";
            if (sideString == "0")
            {
                s = "Black";
            }
            //Determine whether you are black or red
            if (sideString == side.ToString())
            {
                SetLabel(labelSide1, s + labelSideString);
            }
            else
            {
                SetLabel(labelSide0, s + labelSideString);
            }
            service.AddItemToListBox(listBoxString);
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                service.SendToServer(string.Format("Talk,{0},{1}", tableIndex, textBox1.Text));
                textBox1.Text = "";
            }
        }
        public void CheckWin()
        {
            bool win = false;
            
            if (win == true)
            {
                service.SendToServer(string.Format("win,{0},{1}", tableIndex, side));
            }
        }
        public void Ready()
        {
            if (side == 1)
            {
                SetLabel(labelOrder, "Lượt Của Bạn");
                for (int h = 0; h < 15; h += 2)
                {
                    pieces[h].Enabled = true;
                }
                label1.Text = "BẠN CẦM QUÂN ĐỎ";
            }
            else
            {
                SetLabel(labelOrder, "Lượt Của Đối Thủ");
                label1.Text = "BẠN CẦM QUÂN ĐEN";
            }
        }
        public void ChangeOrder(int i)
        {
            //It's time for the opponent to move
            if (side == i)
            {
               
                SetLabel(labelOrder, "Lượt Của Đối Thủ");

            }
            else
            {
               
                SetLabel(labelOrder, "Lượt Của Bạn");

            }
        }
        private void onPieceMove(int index, Point position)
        {
            pieces[index].Location = format(position);
        }

        private void onPieceBeEaten(int index)
        {
            pieces[index].Visible = false;
        }

        private Point format(Point point)
        {
            point.X = point.X * 75 - 70;
            point.Y = point.Y * 75 - 70;
            return new Point(point.X, point.Y);
        }
        private Point unFormat(Point point)
        {
            point.X = (point.X + 70) / 75;
            point.Y = (point.Y + 70) / 75;
            return new Point(point.X, point.Y);
        }


        void onDefinePieces(Point position)
        {
            pieces[pieceIndexTemp] = new PictureBox();
            pieces[pieceIndexTemp].Size = new Size(70, 70);
            pieces[pieceIndexTemp].Tag = pieceIndexTemp; 
            pieces[pieceIndexTemp].Location = format(position);
            pieces[pieceIndexTemp].BackColor = Color.Transparent;
            pieces[pieceIndexTemp].BackgroundImage = b[pieceIndexTemp];
            pieces[pieceIndexTemp].Cursor = Cursors.Hand;
            pieces[pieceIndexTemp].Click += pieces_Click;
            pic.Controls.Add(pieces[pieceIndexTemp++]);
        }
        #endregion

        int pieceClick;
        void pieces_Click(object sender, EventArgs e)
        {
            int index = (int)(((PictureBox)sender).Tag);
            pieceClick = index;
            Point[] p = animals[index].Forcast();
            for (int i = 0; i < 4; i++)
            {
                picForcast[i].Location = format(p[i]);
                picForcast[i].Visible = true;
            }
        }
        void piecesRefresh()
        {
            for (int i = 0; i < 16; i++)
            {
                pieces[i].Visible = true;
                animals[i].MoveTo(animals[i].IniPosition);
                pieces[i].Enabled = false;
            }
            for (int i = 0; i < 4; i++)
            {
                picForcast[i].Visible = false;
            }
        }

        Point redBase = new Point(4, 9);
        Point blackBase = new Point(4, 1);

        void picForcast_Click(object sender, EventArgs e)
        {
            foreach (Animal a in animals)
            {
                if (unFormat(((PictureBox)sender).Location) == a.Position)
                {
                    a.BeEaten();
                    int b = a.Index;
                    service.SendToServer(string.Format("BeEaten,{0},{1},{2}",tableIndex,side,b));
                }
            }
            animals[pieceClick].MoveTo(unFormat(((PictureBox)sender).Location));

            for (int i = 0; i < 4; i++)
            {
                picForcast[i].Visible = false;
            }
            Point point = unFormat(((PictureBox)sender).Location);
            int x=point.X; int y=point.Y;
            string vitri = JsonConvert.SerializeObject(point);
            int m = side;
            int n = pieceClick;
            service.SendToServer(string.Format("ChessInfo,{0},{1},{2},{3},{4}", tableIndex, m, n, x,y));    
            if (animals[pieceClick].Camp == camp.red && animals[pieceClick].Position == blackBase)
            {
                MessageBox.Show("Đội Đỏ thắng");
                service.SendToServer(string.Format("Win,{0},{1}", tableIndex, side));
                piecesRefresh();
                for (int i = 0; i < 15; i+=2)
                {
                    pieces[i].Enabled = true;
                }
                SetLabel(labelOrder, "Lượt Của Bạn");
            }
            else if (animals[pieceClick].Camp == camp.black && animals[pieceClick].Position == redBase)
            {
                MessageBox.Show("Đội Đen thắng");
                service.SendToServer(string.Format("Win,{0},{1}", tableIndex, side));
                piecesRefresh();
                SetLabel(labelOrder, "Lượt Của Đối Thủ");
            }
        }
        public void ThongBaoWin(int nguoithang)
        {
            if (nguoithang == 0)
            {
                MessageBox.Show("Đội Đen thắng");
                piecesRefresh();
                if(side == 0) { SetLabel(labelOrder, "Lượt Của Đối Thủ"); }
                else { SetLabel(labelOrder, "Lượt Của Bạn"); }
            }
            else 
            {
                MessageBox.Show("Đội Đỏ thắng");
                piecesRefresh();
                SetLabel(labelOrder, "Lượt Của Bạn");
                if (side == 0) { SetLabel(labelOrder, "Lượt Của Đối Thủ"); }
                else { SetLabel(labelOrder, "Lượt Của Bạn"); }
            }
        }
        public void Reanimal(int luot,int quanco,int x, int y)
        {
            if(side != luot)
            {
                int newluot = (luot + 1) % 2;
                Point point = new Point(x, y);
                animals[quanco].MoveTo(point);
                for (int i = 0; i < 4; i++)
                {
                    picForcast[i].Visible = false;
                }
                if (newluot == 0 && side ==0)
                {
                    for (int i = 1; i < 16; i += 2)
                    {
                        pieces[i].Enabled = true;
                        pieces[i-1].Enabled = false;
                    }
                }
                else if(newluot == 1 && side ==1)
                {
                    for (int i = 1; i < 16; i += 2)
                    {
                        pieces[i-1].Enabled = true;
                        pieces[i].Enabled = false;
                    }
                }
                SetLabel(labelOrder, "Lượt Của Bạn");
            }
            else 
            {
                for (int i = 0; i < 16; i++)
                {
                    pieces[i].Enabled = false;
                }
                SetLabel(labelOrder, "Lượt Của Đối Thủ");
            }
            
           
        }
        public void Bian(int i)
        {
            foreach (Animal a in animals)
            {
                if(a.Index == i)
                {
                    a.BeEaten();
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            service.SendToServer(string.Format("GetUp,{0},{1}", tableIndex, side));
        }

       
    }
}
