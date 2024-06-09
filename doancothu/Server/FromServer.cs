using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace communication
{
    public partial class FromServer : Form
    {
        //Maximum number of people allowed to enter
        private int maxUsers;
        //connected user
        List<User> userList = new List<User>();
        //Number of game tables
        private int maxTables;
        private GameTable[] gameTable;
        //Local IP
        IPAddress localAddress;
        //listen port
        private int port = 51888;
        private TcpListener myListener;
        private Service service;
        public FromServer()
        {
            InitializeComponent();
            service = new Service(listBox1);
        }
        // when loading the form
        private void FromServer_Load(object sender, EventArgs e)
        {
            listBox1.HorizontalScrollbar = true;
            // Gan localAddress dia chi ip 127.0.0.1
            localAddress = IPAddress.Parse("127.0.0.1");
            buttonStop.Enabled = false;
        }
        //"Start service" button
        private void buttonStart_Click(object sender, EventArgs e)
        {
            //Kiểm tra giá trị nhập vào
            if (int.TryParse(textBoxMaxTables.Text, out maxTables) == false
                || int.TryParse(textBoxMaxUsers.Text, out maxUsers) == false)
            {
                MessageBox.Show("Please enter a positive integer in the range!!!");
                return;
            }
            //Kiểm tra giới hạn cho số lượng người chơi và số bàn
            if (maxUsers < 1 || maxUsers > 300)
            {
                MessageBox.Show("The number of people allowed is 1-300!!!");
                return;
            }
            if (maxTables < 1 || maxTables > 100)
            {
                MessageBox.Show("The number of tables allowed is 1-100!!!");
                return;
            }
            //Vô hiệu hóa textBoxMaxTables và textBoxMaxUsers
            textBoxMaxTables.Enabled = false;
            textBoxMaxUsers.Enabled = false;
            //Create an array of game tables
            gameTable = new GameTable[maxTables];
            for (int i = 0; i < maxTables; i++)
            {
                gameTable[i] = new GameTable(listBox1);
            }
            //monitor
            myListener = new TcpListener(localAddress, port);
            myListener.Start();
            service.AddItem(string.Format("Start listening for client connections at {0}:{1}", localAddress, port));
            //Create a thread to listen for client connection requests
            //ThreadStart ts = new ThreadStart(ListenClientConnect);
            Thread myThread = new Thread(new ThreadStart(ListenClientConnect));
            myThread.Start();
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
        }
        //"Stop service" button
        private void buttonStop_Click(object sender, EventArgs e)
        {
            service.AddItem(string.Format("Number of currently connected users:{0}", userList.Count));
            service.AddItem(string.Format("Stop the service immediately, the user will exit in sequence"));
            for (int i = 0; i < userList.Count; i++)
            {
                userList[i].client.Close();
            }
            // exit the listener thread
            myListener.Stop();
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            textBoxMaxUsers.Enabled = true;
            textBoxMaxTables.Enabled = true;
        }
        //Nhận kết nối client
        private void ListenClientConnect()
        {
            while (true)
            {
                TcpClient newClient = null;
                try
                {
                    newClient = myListener.AcceptTcpClient();
                }
                catch
                {
                    break;
                }
                // create a thread for each client
                Thread threadReceive = new Thread(ReceiveData);
                User user = new User(newClient);
                threadReceive.Start(user);
                userList.Add(user);
                service.AddItem(string.Format("{0}Enter", newClient.Client.RemoteEndPoint));
                service.AddItem(string.Format("Number of currently connected users: {0}", userList.Count));
            }
        }
        //Nhận thông tin client
        private void ReceiveData(object obj)
        {
            User user = (User)obj;
            TcpClient client = user.client;
            //Whether to exit the receiving thread normally
            bool normalExit = false;
            //Used to control whether to exit the loop
            bool exitWhile = false;
            while (exitWhile == false)
            {
                string receiveString = null;
                try
                {
                    receiveString = user.sr.ReadLine();
                }
                catch
                {
                    service.AddItem("Failed to receive data:");
                }
                //If the TcpClient object is closed and the underlying socket is not closed, no exception is generated, but the read result is null
                if (receiveString == null)
                {
                    if (normalExit == false)
                    {
                        if (client.Connected == true)
                        {
                            service.AddItem(string.Format("lost contact with {0}, has terminated receiving the user information", client.Client.RemoteEndPoint));
                        }
                        RemoveClientfromPlayer(user);
                    }
                    break;
                }
                service.AddItem(string.Format("from {0}:{1}", user.userName, receiveString));
                string[] splitString = receiveString.Split(',');
                int tableIndex = -1; //table number
                int side = -1;//Seat number
                int anotherSide = -1; //The seat number of the other side
                string sendString = "";
                string command = splitString[0].ToLower();
                switch (command)
                {
                    //Login, format: Login, nickname
                    case "login":
                        if (userList.Count > maxUsers)
                        {
                            sendString = "Sorry";
                            service.SendToOne(user, sendString);
                            service.AddItem("The number of people is full, refuse" + splitString[1] + "Enter the game room");
                            exitWhile = true;
                        }
                        else
                        {
                            //Save the user's nickname to the user list
                            user.userName = string.Format("[{0}]", splitString[1]);
                            //Send the status of whether there are people at each table to the user
                            sendString = "Tables," + this.GetOnlineString();
                            service.SendToOne(user, sendString);

                        }
                        break;
                    //Exit, format: Logout
                    case "logout":
                        service.AddItem(string.Format("{0} exit the game room", user.userName));
                        normalExit = true;
                        exitWhile = true;
                        break;
                    //Sit down, format: SitDown, table number, seat number
                    case "sitdown":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        gameTable[tableIndex].gamePlayer[side].user = user;
                        gameTable[tableIndex].gamePlayer[side].someone = true;
                        service.AddItem(string.Format("{0} is seated at table {1}, seat {2}", user.userName,
                                                        tableIndex + 1, side + 1));
                        //Get the seat number of the other party
                        anotherSide = (side + 1) % 2;
                        // Determine if the other party is someone
                        if (gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            // Tell the user that the other party is seated
                            //Format: SitDown, seat number, username
                            sendString = string.Format("SitDown,{0},{1}", anotherSide,
                                            gameTable[tableIndex].gamePlayer[anotherSide].user.userName);
                            service.SendToOne(user, sendString);
                        }
                        //Tell both users that the user is seated
                        //Format: SitDown, seat number, username
                        sendString = string.Format("SitDown,{0},{1}", side, user.userName);
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        //Send the status of each table in the game room to all users
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    //Leave seat, format: GetUp, table number, seat number
                    case "getup":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        service.AddItem(string.Format("{0} leave seat and return to the game room", user.userName));
                        //Send the departure information to two users in the format: GetUp, seat number, user name
                        service.SendToBoth(gameTable[tableIndex], string.Format("GetUp,{0},{1}", side, user.userName));
                        gameTable[tableIndex].gamePlayer[side].someone = false;
                        gameTable[tableIndex].gamePlayer[side].started = false;
                        anotherSide = (side + 1) % 2;
                        if (gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            gameTable[tableIndex].gamePlayer[anotherSide].started = false;
                        }
                        //Send the status of each table in the game room to all users
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    //chat, format: Talk, username, conversation content
                    case "talk":
                        tableIndex = int.Parse(splitString[1]);
                        //special handling of commas
                        sendString = string.Format("Talk,{0},{1}", user.userName,
                                    receiveString.Substring(splitString[0].Length + splitString[1].Length + 2));
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        break;
                    //Prepare, format: Start, table number, seat number
                    case "start":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        gameTable[tableIndex].gamePlayer[side].started = true;
                        anotherSide =(side + 1) % 2;
                        if (side == 0)
                        {
                            sendString = "Message, Black is ready";
                        }
                        else if(side == 1)
                        { 
                            sendString = "Message, the red team is ready";
                        }
                        service.SendToOne(gameTable[tableIndex].gamePlayer[side].user, sendString);
                        service.SendToOne(gameTable[tableIndex].gamePlayer[anotherSide].user, sendString);
                        if (gameTable[tableIndex].gamePlayer[anotherSide].started == true)
                        {
                            sendString = "AllReady";
                            service.SendToOne(gameTable[tableIndex].gamePlayer[side].user, sendString);
                            service.SendToOne(gameTable[tableIndex].gamePlayer[anotherSide].user, sendString);
                        }
                        break;
                    //chess piece movement information, format: ChessInfo, table number, seat number, piece number, original x, original y, purpose x, purpose y
                    case "chessinfo":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        anotherSide = (side + 1) % 2;
                        sendString = string.Format("ChessInfo,{0},{1},{2},{3}", side, splitString[3], splitString[4], splitString[5]);
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        break;
                    //Victory, format: Win, table number, seat number
                    case "beeaten":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        anotherSide = (side + 1) % 2;
                        int a = int.Parse(splitString[3]);
                        sendString = string.Format("beeaten,{0}", a);
                        service.SendToOne(gameTable[tableIndex].gamePlayer[anotherSide].user, sendString);
                        break;
                    case "win":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        anotherSide = (side + 1) % 2;
                        gameTable[tableIndex].gamePlayer[side].started = false;
                        gameTable[tableIndex].gamePlayer[anotherSide].started = false;
                        sendString = string.Format("win,{0}",side);
                        service.SendToOne(gameTable[tableIndex].gamePlayer[anotherSide].user, sendString);

                        break;
                }
            }
            userList.Remove(user);
            client.Close();
            service.AddItem(string.Format("There is one exit, remaining connected users: {0}", userList.Count));
        }
        //Detect if the user is sitting on the game table, if so, remove it and terminate the table game
        private void RemoveClientfromPlayer(User user)
        {
            for (int i = 0; i < gameTable.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (gameTable[i].gamePlayer[j].user != null)
                    {
                        if (gameTable[i].gamePlayer[j].user == user)
                        {
                            StopPlayer(i, j);
                            return;
                        }
                    }
                }
            }
        }
        //dừng trò chơi ở bàn thứ i
        private void StopPlayer(int i, int j)
        {
            gameTable[i].gamePlayer[j].someone = false;
            gameTable[i].gamePlayer[j].started = false;
            int otherSide = (j + 1) % 2;
            if (gameTable[i].gamePlayer[otherSide].started == true)
            {
                gameTable[i].gamePlayer[otherSide].started = false;
                if (gameTable[i].gamePlayer[otherSide].user.client.Connected == true)
                {
                    service.SendToOne(gameTable[i].gamePlayer[otherSide].user,
                                        string.Format("Lost,{0},{1}",
                                        j, gameTable[i].gamePlayer[j].user.userName));
                }
            }
        }
        //Lấy chuỗi mỗi bàn có người hay không, 0 nghĩa là có ai đó, 1 nghĩa là không có ai
        private string GetOnlineString()
        {
            string str = "";
            for(int i = 0; i < gameTable.Length; i++)
            {
                for(int j = 0; j < 2; j++)
                {
                    str += gameTable[i].gamePlayer[j].someone == true ? "1" : "0";
                }
            }
            return str;
        }
        //Event fired before closing the form
        private void FromDDServer_FromClosing(object sender, FormClosingEventArgs e)
        {
            if(myListener != null)
            {
                buttonStop_Click(null, null);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
