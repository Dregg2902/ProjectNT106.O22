using Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class FormRoom : Form
    {
        private int maxPlayingTables;
        private CheckBox[,] checkBoxGameTables;
        private TcpClient client = null;
        private StreamWriter sw;
        private StreamReader sr;
        private Service service;
        private Form1 formPlaying;
        //Một biến boolean để chỉ định xem luồng nhận có thoát bình thường hay không.
        private bool normalExit = false;
        //Một biến boolean để xác định xem lệnh được nhận có phải từ máy chủ hay không.
        private bool isReceiveCommand = false;
        //Một biến số nguyên để lưu trữ số ghế trên bàn chơi mà người dùng đang ngồi. Giá trị -1 cho biết người dùng chưa ngồi, 0 là ghế màu đen và 1 là ghế màu đỏ.
        private int side = -1;
        public FormRoom()
        {
            InitializeComponent();
        }
        private void FormRoom_Load(object sender, EventArgs e)
        {
            // random số thứ tự người chơi
            Random r = new Random((int)DateTime.Now.Ticks);
            textBoxName.Text = "Player" + r.Next(1, 100);
            maxPlayingTables = 0;
            textBoxLocal.ReadOnly = true;
            textBoxServer.ReadOnly = true;
        }
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                //Một kết nối TCP client được tạo đến địa chỉ IP "127.0.0.1" và cổng "51888" 
                client = new TcpClient("127.0.0.1", 51888);
            }
            catch
            {
                MessageBox.Show("Failed to connect to the server", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            textBoxLocal.Text = client.Client.LocalEndPoint.ToString();
            textBoxServer.Text = client.Client.RemoteEndPoint.ToString();
            buttonConnect.Enabled = false;
            // Tạo stream để ghi và đọc(những thông tin mà server gửi tới và gửi đi những thông tin khác cho server)
            NetworkStream netStream = client.GetStream();
            sr = new StreamReader(netStream, System.Text.Encoding.UTF8);
            sw = new StreamWriter(netStream, System.Text.Encoding.UTF8);
            service = new Service(listBox1, sw);
            //Get the server table information
            //Format: Login, nickname
            // sau khi kết nối thành công,service gửi thông tin name của người chơi login vào game đến cho server
            service.SendToServer("Login," + textBoxName.Text.Trim());
            // tạo luồng để nhận data của server
            Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
            threadReceive.Start();
        }
        // process the received data
        private void ReceiveData()
        {
            bool exitWhile = false;
            while (exitWhile == false)
            {
                string receiveString = null;
                try
                {
                    receiveString = sr.ReadLine();
                }
                catch
                {
                    service.AddItemToListBox("Failed to receive data");
                }
                if (receiveString == null)
                {
                    if (normalExit == false)
                    {
                        MessageBox.Show("Lost contact with server, game cannot continue!");
                    }
                    if (side != 1)
                    {
                        ExitFormPlaying();
                    }
                    side = -1;
                    normalExit = true;
                    break;
                }
                service.AddItemToListBox("Received:" + receiveString);
                string[] splitString = receiveString.Split(',');
                string command = splitString[0].ToLower();
                switch (command)
                {
                    //The lobby is full
                    case "sorry":
                        MessageBox.Show("Successful connection, but the lobby is full");
                        exitWhile = true;
                        break;
                    // game table situation
                    //Format: Tables, string of whether there are people at each table
                    //1 means someone, 0 means nobody
                    case "tables":
                        string s = splitString[1];
                        //If maxPlayingTables is 0, it means checkBoxGameTables has not been created
                        if (maxPlayingTables == 0)
                        {
                            // count the number of tables
                            maxPlayingTables = s.Length / 2;
                            checkBoxGameTables = new CheckBox[maxPlayingTables, 2];
                            isReceiveCommand = true;
                            //Add the CheckBox object to the array
                            for (int i = 0; i < maxPlayingTables; i++)
                            {
                                AddCheckBoxToPanel(s, i);
                            }
                            isReceiveCommand = false;
                        }
                        else
                        {
                            isReceiveCommand = true;
                            for (int i = 0; i < maxPlayingTables; i++)
                            {
                                for (int j = 0; j < 2; j++)
                                {
                                    if (s[2 * i + j] == '0')
                                    {
                                        UpdateCheckBox(checkBoxGameTables[i, j], false);
                                    }
                                    else
                                    {
                                        UpdateCheckBox(checkBoxGameTables[i, j], true);
                                    }
                                }
                                isReceiveCommand = false;
                            }
                        }
                        break;
                    //Seat, format: SitDown, seat number, user name
                    case "sitdown":
                        formPlaying.SetTableSideText(splitString[1], splitString[2],
                                                string.Format("{0} enters", splitString[2]));
                        break;
                    // leave seat
                    case "getup":
                        if (side == int.Parse(splitString[1]))
                        {
                            side = -1;
                        }
                        else
                        {
                            formPlaying.SetTableSideText(splitString[1], "",
                                string.Format("{0} exit", splitString[2]));
                            formPlaying.Restart("The enemy escapes, our side wins");
                        }
                        break;
                    //The other party disconnects from the server
                    case "lost":
                        formPlaying.SetTableSideText(splitString[1], "",
                            string.Format("[{0}] lost contact with server", splitString[2]));
                        formPlaying.Restart("The opponent lost contact with the server and the game cannot continue");
                        break;
                    //chat
                    //Format: Talk, Speaker, Content
                    case "talk":
                        if (formPlaying != null)
                        {
                            formPlaying.ShowTalk(splitString[1],
                                receiveString.Substring(splitString[0].Length +
                                splitString[1].Length + 2));
                        }
                        break;
                    // information sent by the server
                    //Format: Message, information
                    case "message":
                        formPlaying.ShowMessage(splitString[1]);
                        break;
                    //Pawn change information, format: ChessInfo, side, piece number, original x, original y, purpose x, purpose y
                    case "chessinfo":
                        int luot = int.Parse(splitString[1]);
                        int h = int.Parse(splitString[2]);
                        int x = int.Parse(splitString[3]);
                        int y = int.Parse(splitString[4]);
                        formPlaying.Reanimal(luot,h,x,y);
                        break;
                    //Victory, format: Win, side
                    case "beeaten":
                        int bian = int.Parse(splitString[1]);
                        formPlaying.Bian(bian);
                        break;
                    case "win":
                        int winner = int.Parse(splitString[1]);
                        formPlaying.ThongBaoWin(winner);
                        break;
                    //both sides are ready
                    case "allready":
                        formPlaying.ShowMessage("Both sides are ready, the game starts!");
                        formPlaying.Ready();
                        break;
                    default: break;
                }
            }
            Application.Exit();
        }
        delegate void ExitFormPlayingDelegate();
        //exit the game
        private void ExitFormPlaying()
        {
            if (formPlaying.InvokeRequired == true)
            {
                ExitFormPlayingDelegate d = new ExitFormPlayingDelegate(ExitFormPlaying);
                this.Invoke(d);
            }
            else
            {
                formPlaying.Close();
            }
        }
        delegate void Paneldelegate(string s, int i);
        //Add a game table
        private void AddCheckBoxToPanel(string s, int i)
        {
            if (panel1.InvokeRequired == true)
            {
                Paneldelegate d = AddCheckBoxToPanel;
                this.Invoke(d, s, i);
            }
            else
            {
                Label label = new Label();
                label.Location = new Point(10, 15 + i * 30);
                label.Text = string.Format("Table {0}: ", i + 1);
                label.Width = 70;
                this.panel1.Controls.Add(label);
                CreateCheckBox(i, 1, s, "Red");
                CreateCheckBox(i, 0, s, "Black");
               
            }
        }
        delegate void CheckBoxDelegate(CheckBox checkbox, bool isChecked);
        //Modify the selection state
        private void UpdateCheckBox(CheckBox checkbox, bool isChecked)
        {
            if (checkbox.InvokeRequired == true)
            {
                CheckBoxDelegate d = UpdateCheckBox;
                this.Invoke(d, checkbox, isChecked);
            }
            else
            {
                if (side == -1)
                {
                    checkbox.Enabled = !isChecked;
                }
                else
                {
                    //Already seated, no other tables are allowed
                    checkbox.Enabled = false;
                }
                checkbox.Checked = isChecked;
            }
        }
        // Option to add game table seats
        private void CreateCheckBox(int i, int j, string s, string text)
        {
            int x = j == 0 ? 100 : 200;
            checkBoxGameTables[i, j] = new CheckBox();
            checkBoxGameTables[i, j].Name = string.Format("check{0:0000}{1:0000}", i, j);
            checkBoxGameTables[i, j].Width = 60;
            checkBoxGameTables[i, j].Location = new Point(x, 10 + i * 30);
            checkBoxGameTables[i, j].Text = text;
            checkBoxGameTables[i, j].TextAlign = ContentAlignment.MiddleLeft;
            if(s[2 * i + j] == '1')
            {
                //1 means someone
                checkBoxGameTables[i, j].Enabled = false;
                checkBoxGameTables[i, j].Checked = true;
            }
            else
            {
                //0 means no one
                checkBoxGameTables[i, j].Enabled = true;
                checkBoxGameTables[i, j].Checked = false;
            }
            this.panel1.Controls.Add(checkBoxGameTables[i, j]);
            checkBoxGameTables[i, j].CheckedChanged +=
                 new EventHandler(checkBox_CheckedChanged);
        }
        //Triggered when the Checked property of the CheckBox changes
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            //Whether to update the table status for the server
            if (isReceiveCommand == true)
            {
                return;
            }
            CheckBox checkbox = (CheckBox)sender;
            //If Checked is true, it means that the player sits on the jth table at the ith table
            if (checkbox.Checked == true)
            {
                int i = int.Parse(checkbox.Name.Substring(5, 4));
                int j = int.Parse(checkbox.Name.Substring(9, 4));
                side = j;
                //Format: SitDown, Nickname, Table Number, Seat Number
                service.SendToServer(string.Format("SitDown,{0},{1}", i, j));
                formPlaying = new Form1(i, j, sw);
                formPlaying.Show();
            }
        }
        //Event fired when the window is closed
        private void FormRoom_FromClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null)
            {
                // Do not allow the player to exit the entire program directly from the game table
                //Only allowed to return to the game room from the game table, and then exit from the game room
                if (side != -1)
                {
                    MessageBox.Show("Please stand up from the game table, return to the game room, and then exit");
                    e.Cancel = true;
                }
                else
                {
                    //When the server stops the service, normalExit is true, otherwise it is false
                    if (normalExit == false){
                        normalExit = true;
                        service.SendToServer("Logout");
                    }
                    client.Close();
                }
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Howtoplay howtoplay = new Howtoplay();
            howtoplay.ShowDialog();
        }
    }
}
