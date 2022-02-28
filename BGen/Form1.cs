using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using NBitcoin;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Net;
using System.IO;

namespace BGen
{
    public partial class Form1 : Form
    {
        HashSet<string> database = new HashSet<string>();
        bool isRun = false,
            test = false;
        Thread threadDom;
        Queue<int> graph_points;
        internal StreamWriter textFile;  //файл для текстовой таблицы
        int operCount = 1000;
        float chislitel = 1000000.0f;

        class Checker
        {
            interface ICheckResult
            {
                bool _Check(string _btcAddr);
            }

            public static void SetOnlineCheck()
            {
                GetCheckResult = new OnlineCheck();
            }

            public static bool checkBtcAddr(string _btcAddr)
            {
                return GetCheckResult._Check(_btcAddr);
            }

            static ICheckResult GetCheckResult = new OfflineCheck();

            class OnlineCheck : ICheckResult
            {
                static bool recurseMethod(string _btcAddr)
                {
                    bool continCycle = true;

                    while (continCycle)
                    {
                        try
                        {
                            using (var webClient = new WebClient())
                            {
                                var json = webClient.DownloadString("https://blockchain.info/q/getreceivedbyaddress/" + _btcAddr);

                                int bal = 0;
                                int.TryParse(json, out bal);
                                if (bal > 0) return true;
                            }
                            return false;
                        }
                        catch
                        {

                        }
                    }

                    return false;
                }

                bool ICheckResult._Check(string _btcAddr)
                {
                    return recurseMethod(_btcAddr);
                }
            }

            class OfflineCheck : ICheckResult
            {
                bool ICheckResult._Check(string _btcAddr)
                {
                    RandomGenerator test = new RandomGenerator();
                    if (test.GenerateStrKey().Substring(0, 3) == "6Fj") return true;

                    return false;
                }
            }


        }

        class RandomGenerator
        {
            static Random rndSym1 = new Random();
            static Random rndSym2 = new Random();
            static Random rndNum = new Random();
            static Random rndType = new Random();

            public string GenerateStrKey()
            {
                byte[] newPrivKey = new byte[32];

                for (int i = 0; i < newPrivKey.Length; i++)
                {
                    switch (rndType.Next(5))
                    {
                        case 0:
                        case 2:
                            newPrivKey[i] = (byte)rndSym1.Next(0x41, 0x5A);
                            break;
                        case 1:
                        case 3:
                            newPrivKey[i] = (byte)rndSym2.Next(0x61, 0x7A);
                            break;
                        case 4:
                            newPrivKey[i] = (byte)rndNum.Next(0x30, 0x39);
                            break;
                    }

                }

                return Encoding.GetEncoding(1251).GetString(newPrivKey);
            }

            public byte[] GenerateBytes()
            {
                byte[] result = new byte[32];
                rndNum.NextBytes(result);
                return result;
            }
        }

        public Form1()
        {
            InitializeComponent();
            threadDom = new Thread(new ThreadStart(AntiMiner));
            threadDom.Start();
            CheckForIllegalCrossThreadCalls = false;
            graph_points = new Queue<int>();

            //TEST CONNECT
            try
            {
                using (WebClient client = new WebClient())
                {
                    string htmlCode = client.DownloadString("https://www.blockchain.com/btc/address/1ByxGHcfdTWH3FgKYmwWGCCh6tETZkJKB");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Not connected to blockchain.com",
                    "Warning", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }


        }

        void AntiMiner()
        {
            System.Diagnostics.PerformanceCounter cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");

            RandomGenerator test = new RandomGenerator();
            int iterator = 0;
            var t1 = Environment.TickCount;

            while (true)
            {
                if (!isRun) continue;
                iterator++;

                var priv256bits = test.GenerateBytes();

                //var privKeyStr = Encoders.Hex.EncodeData(priv256bits);  //"L4dj5Vn59JeTVU12cgsMm5q8NBWPRx5TT8WjMhmriMT4ocdmezNe";

                //label1.Text = privKeyStr.ToUpper();
                //label2.Text = privKeyStr.Length.ToString();

                Key privateKey = new Key(priv256bits);  //Key.Parse("L4dj5Vn59JeTVU12cgsMm5q8NBWPRx5TT8WjMhmriMT4ocdmezNe", Network.Main);
                string testPrivKey = privateKey.GetWif(Network.Main).ToString();

                PubKey pubKey = privateKey.PubKey;

                //label3.Text = pubKey.Decompress().ToString();
                //label4.Text = label3.Text.Length.ToString();

                BitcoinAddress btcAddr = pubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);
                string testBtcAddr = btcAddr.ToString();
                //label5.Text = btcAddr.ToString();
                //label6.Text = label5.Text.Length.ToString(); 

                bool condition = Checker.checkBtcAddr(testBtcAddr);
                if (condition)
                {
                    label11.Text = "Result " + testBtcAddr;
                    database.Add(testPrivKey);

                    if (textFile != null) textFile.WriteLine(testPrivKey);
                    else
                    {
                        textFile = new StreamWriter(Application.StartupPath + "\\result.txt"); //дополнительно текстовый файл
                        textFile.WriteLine(testPrivKey + "\t" + testBtcAddr);
                        textFile.Flush();
                    }

                }

                if (iterator == operCount)
                {
                    //Application.DoEvents();

                    label1.Text = testPrivKey;
                    label3.Text = pubKey.ToString(); //.Decompress()
                    label5.Text = testBtcAddr;

                    var t2 = Environment.TickCount - t1;
                    var operPerSec = chislitel / t2;
                    if (operPerSec < 10)
                    {
                        operCount = 1;
                        chislitel = 1000.0f;
                    }
                    else if (operPerSec < 100)
                    {
                        operCount = 10;
                        chislitel = 10000.0f;
                    }
                    else if (operPerSec < 400)
                    {
                        operCount = 100;
                        chislitel = 100000.0f;
                    }
                    else
                    {
                        operCount = 1000;
                        chislitel = 1000000.0f;
                    }

                    label10.Text = operCount < 100 ? operPerSec.ToString("#0.0") : operPerSec.ToString("#0.");
                    label4.Text = cpuCounter.NextValue().ToString("#0.0") + " %";

                    graph_points.Enqueue((int)float.Parse(label10.Text));
                    pictureBox1.Refresh();
                    if (graph_points.Count >= pictureBox1.Width / 3) graph_points.Dequeue();

                    iterator = 0;
                    t1 = Environment.TickCount;
                }
            }

            //var publicKeyHash = pubKey.Hash;
            //label6.Text = publicKeyHash.GetAddress(Network.Main).ToString();

            //label1.Text = Encoders.Base58Check.EncodeData(priv256bits);

            //ExtKey extPubKey = new ExtKey();

            //database.Add(label1.Text);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            isRun = !isRun;
            if (isRun)
            {
                button1.Text = "Stop";
            }
            else
            {
                button1.Text = "Start";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isRun = false;
            if (textFile != null) textFile.Close();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            threadDom.Abort();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen_tan = new Pen(Color.Tan, 1);
            Pen pen_black = new Pen(Color.Black, 1);
            SolidBrush brush_black = new SolidBrush(Color.Black);
            Font Arial8Font = new Font("Arial", 8);

            g.Clear(Color.White);
            for (var y = 0; y <= pictureBox1.Height; y += 40)
                g.DrawLine(pen_tan, 0, y, pictureBox1.Width, y);
            for (var x = 0; x <= pictureBox1.Width; x += 40)
                g.DrawLine(pen_tan, x, 0, x, pictureBox1.Height);


            if (graph_points.Count != 0)
            {
                float maxValue = graph_points.Max(),
                    x = 0;
                int zhirnota = operCount > 100 ? 30 : 15;
                g.DrawString(maxValue.ToString("###0."), Arial8Font, brush_black, pictureBox1.Width - zhirnota, 1);
                g.DrawString("0", Arial8Font, brush_black, pictureBox1.Width - 14, pictureBox1.Height - 15);

                if (maxValue == 0) maxValue = 1;
                maxValue *= 1.05f;
                foreach (var _point in graph_points)
                {
                    int y = (int)(pictureBox1.Height - _point * (pictureBox1.Height / maxValue));
                    if (y == pictureBox1.Height) y -= 2;
                    g.DrawRectangle(pen_black, x, y, 1, 1);
                    x += 2;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                if (args[1] == "-test") test = true;

                operCount = 1000;
                chislitel = 1000000.0f;
            }
            else
            {
                Checker.SetOnlineCheck();
                button2.Visible = false;
                textBox1.Visible = false;

                operCount = 10;
                chislitel = 10000.0f;
            }
        }

        int sposob1()
        {
            int t1 = Environment.TickCount;

            try
            {
                using (var webClient = new WebClient())
                {
                    var json = webClient.DownloadString("https://blockchain.info/q/getreceivedbyaddress/" + textBox1.Text);

                    int bal = 0;
                    int.TryParse(json, out bal);
                    button2.Text = bal.ToString();

                }
            }
            catch (Exception ex)
            {
                button2.Text = ex.ToString().Substring(0, 30);
                textFile = new StreamWriter(Application.StartupPath + "\\result.txt"); //дополнительно текстовый файл
                textFile.WriteLine(ex.ToString());
                textFile.Flush();
            }
            return Environment.TickCount - t1;
        }

        int sposob2()
        {
            int t1 = Environment.TickCount;
            int sdelok = 0;
            using (WebClient client = new WebClient())
            {
                string htmlCode = client.DownloadString("https://www.blockchain.com/btc/address/" + textBox1.Text);
                int idx = htmlCode.IndexOf("<span class=\"sc-1ryi78w-0 cILyoi sc-16b9dsl-1 ZwupP sc-1n72lkw-0 ebXUGH\" opacity=\"1\">Transactions</span>");
                button2.Text = htmlCode.Substring(idx + 229, 3);
                button2.Text = button2.Text.Trim('<', '/');
                int.TryParse(button2.Text, out sdelok);
                button2.Text = sdelok.ToString();
            }
            return Environment.TickCount - t1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (test) this.Text = sposob2().ToString();
            else this.Text = sposob1().ToString();
        }
    }

}
