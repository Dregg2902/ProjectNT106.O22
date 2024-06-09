using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace Client
{
    public partial class Howtoplay : Form
    {
        string[] strings;
        public Howtoplay()
        {
            InitializeComponent();
            strings = new string[]
           {
                "Bàn cờ\r\nBàn game cờ thú được thiết kế dưới hình chữ nhật, với 7 hàng dọc, 9 hàng ngang, tạo thành tổng cộng 63 ô vuông độc lập. Cờ được phân chia thành hai nửa, tương ứng với lãnh thổ riêng và quy trình thiết lập quân cờ chi tiết như sau:\r\nHang: Một ô đặc biệt, gọi là hang, đặt chính giữa dòng hàng trung tâm trong lãnh thổ của mỗi người chơi. Đây là mục tiêu để giành được chiến thắng mà người chơi đối thủ nhắm đến trong mỗi trận chiến.\r\nBẫy: Có tổng cộng 3 bẫy cho mỗi với chơi với các vị trí lần lượt bao gồm phía trên và 2 bên của hang.\r\nVùng nước, sông: Bao gồm hai diện tích kích thước khoảng 2x3 tổng cộng 6 ô vuông, đặt ở giữa lãnh thổ của bàn cờ.",
                "Quân cờ\r\nKhi tìm hiểu luật chơi cờ thủ, đầu tiên bạn cần phải phân biệt được các quân cờ với nhau. Có tổng cộng 2 màu bao gồm đỏ và đen đại điện cho 2 bên đối thủ. Mỗi bên chơi sẽ sở hữu 8 quân cờ, mỗi quân sẽ đại diện cho một sinh linh với cấp bậc sức mạnh đặc trưng, ​​và hình ảnh sinh linh ấy sẽ được khắc họa trên bề mặt quân cờ. Dưới đây là các cấp độ sức mạnh của những sinh linh này:\r\nVoi: Bậc 8\r\nSư Tử: Bậc 7\r\nHổ: Bậc 6\r\nBáo: Bậc 5\r\nChó: Bậc 4\r\nSói: Bậc 3\r\nMèo: Bậc 2\r\nChuột: Bậc 1",
                "Cách di chuyển quân cờ thú\r\nLuật chơi cờ thú cho phép người chọn quân đỏ được đi trước để bắt đầu cuộc chiến. Mỗi nước đi chỉ có thể là bước tiến hoặc lùi 1 ô, sang phải hoặc sang trái 1 ô, mà không thể vượt qua ô. Bên cạnh đó, người chơi phải tuân theo các quy tắc đặc biệt sau đây:\r\n\r\nChuột là động vật duy nhất có thể vượt qua Sông.\r\nSư tử và Hổ có khả năng nhảy ngang hoặc dọc qua sông, ăn quân cờ cấp dưới nếu chúng đứng ở ô đích bên phía sông.\r\nBẫy có thể làm quân cờ mất sức mạnh và bị bắt nếu rơi vào ô “Bẫy” phía lãnh thổ đối thủ.\r\nViệc chuyển vào Hang của chính mình là điều cấm kỵ.\r\nCuộc chiến kết thúc khi một quân cờ được chiến thắng bằng cách vào được Hang đối thủ. Hãy chứng minh khả năng chiến thuật của bạn và trở thành người chơi giỏi nhất trong thế giới đầy rẫy này bằng cách vận dụng nhuần nhuyễn luật chơi và tùy cơ sử dụng các mẹo chơi cờ thú đặc biệt.",
                "Cách chơi cờ thú bắt quân, ăn quân\r\nTrong cuộc chiến của cách chơi cờ thú, quân cờ có thể ăn hoặc bắt quân cờ đối thủ theo luật cấp bật. Ví dụ, Báo (cấp 5) có thể ăn hoặc bắt báo (cấp 5), và cũng có thể bắt chó (cấp 4), nhưng chó (cấp 4) không thể bắt báo (cấp 5). Tuy nhiên, có những mẹo chơi cờ thú đặc biệt sau đây mà bạn có thể tham khảo:\r\n\r\nChuột, mặc dù có cấp bậc thấp nhất, nhưng lại có khả năng ăn hoặc bắt voi. Nguyên nhân là vì chuột \"chạy vào tai của Voi\", nhưng tuyệt đối không thể đối phó với bất kỳ quân cờ nào trên cạn khác.\r\nQuân Chuột dưới nước không thể bị tấn công bởi quân cờ trên cạn, bất chấp cấp bật cao hơn. Điều này chỉ có những quân chuột dưới nước mới có thể ăn nhau.\r\nNếu một quân cờ đi vào ô bẫy của đối thủ, nó sẽ bị giảm cấp xuống 0. Quân cờ bị mắc bẫy có thể bị ăn bởi bất kỳ kỳ quân cờ nào của đối thủ, không phụ thuộc vào thứ hạng. Hạng quân cờ sẽ được phục hồi khi thoát ra khỏi ô bẫy.",
                "Cách chơi cờ thú giành chiến thắng\r\nCách chơi cờ thú sẽ giúp bạn hiểu luật và có thể nhiều mẹo để chinh phục mục tiêu cuối cùng là hoàn thành chiến thắng bằng cách đem một quân cờ của bạn vào hang của đối thủ hoặc tiêu diệt toàn bộ quân cờ. Đó không chỉ là trận đấu cờ đầy thú vị mà còn là những hành trình trải nghiệm rèn luyện kỹ năng tư duy rất tốt cho bạn."
           };

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
           
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if(label1.Text == "1/5")
            {
                richTextBox1.Text = strings[1];
                label1.Text = "2/5";
            }
            else if (label1.Text == "2/5")
            {
                richTextBox1.Text = strings[2];
                label1.Text = "3/5";
            }
            else if (label1.Text == "3/5")
            {
                richTextBox1.Text = strings[3];
                label1.Text = "4/5";
            }
            else if (label1.Text == "4/5")
            {
                richTextBox1.Text = strings[4];
                label1.Text = "5/5";
            }
        }
        private void Howtoplay_Load(object sender, EventArgs e)
        {
            richTextBox1.Text = strings[0];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (label1.Text == "2/5")
            {
                richTextBox1.Text = strings[0];
                label1.Text = "1/5";
            }
            else if (label1.Text == "3/5")
            {
                richTextBox1.Text = strings[1];
                label1.Text = "2/5";
            }
            else if (label1.Text == "4/5")
            {
                richTextBox1.Text = strings[2];
                label1.Text = "3/5";
            }
            else if (label1.Text == "5/5")
            {
                richTextBox1.Text = strings[3];
                label1.Text = "4/5";
            }
        }
    }
}
