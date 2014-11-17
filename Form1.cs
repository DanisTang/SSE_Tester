using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Intelligence;
using System.Runtime.InteropServices;

namespace WindowsFormsApplication1 {
    public partial class Form1 : Form {

        private UTF8Encoding encoding = new System.Text.UTF8Encoding();
        delegate void SetTextCallback(string text);
        delegate void SetBtnFlag(Boolean flag);

        private PushClient push;
        private String subTopic1 = "SSEOrder.Sv";
//        DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
        DateTime gtm = new DateTime(1970, 1, 1, 0, 0, 0, 0);        

        public Form1() {
            InitializeComponent();            
        }

        private void Form1_Load(object sender, EventArgs e) 
        {
            InitControlProperty();
            PB2010 pb2010 = new PB2010();
            Console.WriteLine(System.Runtime.InteropServices.Marshal.SizeOf(pb2010)); //to know struct length
        }

        private void InitControlProperty()
        {
            txtServerAddress.Text = "10.29.229.223";
            txtAccount.Text = "A233620030";
            txtSymbol.Text = "90000481";
            SetBtnStatus(false);

            cmbSide.Items.Add("Buy");
            cmbSide.Items.Add("Sell");
            cmbSide.SelectedIndex = 0;

            cmbOrdType.Items.Add("Limit");
            cmbOrdType.Items.Add("Market");
            cmbOrdType.SelectedIndex = 0;

            cmbTimeInForce.Items.Add("ROD");
            cmbTimeInForce.Items.Add("IOC");
            cmbTimeInForce.Items.Add("FOK");
            cmbTimeInForce.SelectedIndex = 0;

            cmbPositionEffect.Items.Add("Open");
            cmbPositionEffect.Items.Add("Close");
            cmbPositionEffect.Items.Add("Auto");
            cmbPositionEffect.SelectedIndex = 0;


        }

        private void OnStatus(object sender, MESSAGE_TYPE staus, byte[] msg) {
            string smsg=null;
            switch (staus) {
                case MESSAGE_TYPE.MT_CONNECT_READY:
                    push.Subscribe(push.UserTopic);
                    smsg = encoding.GetString(msg);
                    AddInfo("CONNECT_READY:" + String.Format("{0} UserNo:{1}", smsg, push.UserNo));
                    SetBtnStatus(true);                    
                    break;
                case MESSAGE_TYPE.MT_CONNECT_FAIL:
                    smsg = encoding.GetString(msg);
                    AddInfo("CONNECT_FAIL:" + String.Format("{0}", smsg));
                    break;
                case MESSAGE_TYPE.MT_DISCONNECTED:                    
                    smsg = encoding.GetString(msg);
                    AddInfo("DISCONNECTED:" + String.Format("{0}", smsg));
                    SetBtnStatus(false);
                    break;
                case MESSAGE_TYPE.MT_SUBSCRIBE:
                    smsg = encoding.GetString(msg);
                    AddInfo("SUBSCRIBE:" + String.Format("{0}", smsg));                    
                    break;
                case MESSAGE_TYPE.MT_UNSUBSCRIBE:
                    smsg = encoding.GetString(msg);
                    AddInfo("UNSUBSCRIBE:" + String.Format("{0}", smsg));
                    break;
                case MESSAGE_TYPE.MT_ACK_REQUESTID:
                    long RequestId = BitConverter.ToInt64(msg, 0);
                    AddInfo("Request Id BACK: " + RequestId);
                    break;
                case MESSAGE_TYPE.MT_RECOVER_DATA:
                    smsg = encoding.GetString(msg, 1, msg.Length - 1);
                    if (msg[0] == 0)
                        AddInfo(String.Format("Begin Recover Topic:{0}", smsg));
                    if (msg[0] == 1)
                        AddInfo(String.Format("End Recover Topic:{0}", smsg));
                    break;
                case MESSAGE_TYPE.MT_HEART_BEAT:
                    long UTC = ((PushClient)sender).ServerTime();
                    double totsec = (double)UTC / (double)1000000000;
                    DateTime st = (new DateTime(1970, 1, 1, 0, 0, 0)).AddHours(8).AddSeconds(totsec);
                    AddInfo(String.Format("{0:yyyy/MM/dd hh:mm:ss.fff}", st));
                    AddInfo(String.Format("{0:yyyy/MM/dd hh:mm:ss.fff}", DateTime.Now));
//                    TimeSpan toNow = new TimeSpan(UTC * 10);
//                    AddInfo(String.Format("{0:yyyy/MM/dd hh:mm:ss.fff}", gtm.Add(toNow)));
                    break;
            }
        }

        private void OnReceiveData(object sender, long requestID, ushort dataType, byte[] body) {
            switch (dataType)
            {
                case 2001:                    
                    PB2001 pb2001 = new PB2001();
                    pb2001 = (PB2001)BytesToStruct(body, pb2001.GetType());
                    AddInfo(pb2001.toLog());                    
                    break;
                case 2002:
                    PB2002 pb2002 = new PB2002();
                    pb2002.toData(body);
                    AddInfo(pb2002.toLog());
                    break;
                case 2010:
                    PB2010 pb2010 = new PB2010();
                    pb2010.toData(body);
                    AddInfo(pb2010.toLog());
                    break;
                default:
                    string smsg = encoding.GetString(body);
                    AddInfo("Received Data Type=" + dataType.ToString() + "|Data" + smsg);
                    break;
            }
            push.Processed();
        }

        private static byte[] subArray(byte[] src, int startIndex, int Length)
        {
            byte [] dest = new byte[Length];
            Array.Copy(src, startIndex, dest, 0, Length);
            return dest;
        }
        
        private void SetBtnStatus(Boolean flag)
        {
            if (this.InvokeRequired)
            {
                SetBtnFlag d = new SetBtnFlag(SetBtnStatus);
                this.Invoke(d, new object[] { flag });
            }
            else
            {
                if (flag)
                {
                    this.btnConnect.Enabled = false;
                    this.btnDisconnect.Enabled = true;
                    this.btnSubscribe.Enabled = true;
                    this.btnUnsubscribe.Enabled = true;
                    this.btnPublish.Enabled = true;
                }
                else
                {
                    this.btnConnect.Enabled = true;
                    this.btnDisconnect.Enabled = false;
                    this.btnSubscribe.Enabled = false;
                    this.btnUnsubscribe.Enabled = false;
                    this.btnPublish.Enabled = false;
                }
            }
        }

        

        private void AddInfo (string msg) 
        {
            if (this.textBox1.InvokeRequired) 
            {
                SetTextCallback d = new SetTextCallback(AddInfo);
                this.Invoke(d, new object[] { msg });
            } else 
            {
                string fMsg = String.Format("[{0}] {1} {2}", DateTime.Now.ToString("hh:mm:ss:ffff"), msg, Environment.NewLine);
                try 
                {                    
                    textBox1.AppendText(fMsg);
                } catch { };
            }
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            push = new PushClient(txtServerAddress.Text.Trim(), 8000, DATA_MODE.ASYNC, "Danis_C#_Test");            
            push.OnStatus += this.OnStatus;
            push.OnRcvData += this.OnReceiveData;
            push.Connect();            
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            if (push != null)
                push.Disconnect();
            Application.Exit();
        }

        //1.首先宣告結構前要設定為對齊, 避免編譯器最佳化
        //2.因byte array預設不能在struct中初始化, 
        //  故要將struct設為unsafe, 並且在byte array前面加上修飾字fixed
        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]        
        public struct PB2001    {            
            public ushort userNo;            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private byte [] ip;            
            public char account_h;            
            public int account;            
            public long capital_account;            
            public int sub_account;
            public char exchange;
            public char market;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char [] symbol;
            public char side;
            public char modify_type;
            public short qty;
            public long price;
            public char priceFlag;
            public char timeInForce;
            public char positionEffect;
            public int order_no;
            public char front_office;

            public void setSymbol(String Symbol)
            {
                symbol = Symbol.PadLeft(20, ' ').ToCharArray();                
            }

            public string getSymbol()
            {
                return new string(symbol).Trim();                
            }

            public string getAccount()
            {
                return account_h + account.ToString();
            }

            public string toLog()
            {                
                string log = "-->|DT=2001|userNo=" + this.userNo + "|account_h=" + this.account_h + "|account=" + this.account + 
                             "|capital_account=" + this.capital_account + "|sub_account=" + this.sub_account + "|exchange=" + this.exchange + 
                             "|market=" + this.market + "|symbol=" + this.getSymbol() + "|side=" + this.side + "|modify_type=" + this.modify_type + 
                             "|qty=" + this.qty + "|price=" + this.price + "|priceFlag=" + this.priceFlag + "|timeInForce=" + this.timeInForce + 
                             "|positionEffect" + this.positionEffect + "|order_no" + this.order_no + "|front_office" + this.front_office;
                return log;
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PB2002
        {
            public long requestID;
            public int orderNo;
            public char frontOffice;
            public short errorCode;

            public void toData(byte[] body)
            {
                requestID = BitConverter.ToInt64(body, 0);
                orderNo = BitConverter.ToInt32(subArray(body, 8, 4), 0);
                frontOffice = Convert.ToChar(body[12]);
                errorCode = (Int16)BitConverter.ToInt16(subArray(body, 13, 2), 0);
            }

            public string toLog()
            {
                string logData = "<--|DT=2002|RequestID=" + requestID.ToString() + "|OrderNo=" + orderNo.ToString() +
                                 "|FrontOffice=" + frontOffice + "|ErrorCode=" + errorCode.ToString();
                return logData;
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PB2010
        {
            public int reportSeqno;            
            public char frontOffice;
            public char functionCode;
            public char account_h;
            public int account;
            public long capital_account;            
            public int sub_account;
            public char exchange;
            public char market;
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            //private char [] symbol;
            private string symbol;
            public char side;
            public short qty;
            public long price;
            public char priceFlag;
            public char timeInForce;
            public char positionEffect;
            public int reportTime;
            public int orderNo;
            public short code;

            public void toData(byte[] body)
            {
                reportSeqno = BitConverter.ToInt32(body, 0);
                frontOffice = Convert.ToChar(body[4]);
                functionCode = Convert.ToChar(body[5]);
                account_h = Convert.ToChar(body[6]);
                account = BitConverter.ToInt32(subArray(body, 7, 4), 0);
                capital_account = BitConverter.ToInt64(subArray(body, 11, 8), 0);
                sub_account = BitConverter.ToInt32(subArray(body, 19, 4), 0);
                exchange = Convert.ToChar(body[23]);
                market = Convert.ToChar(body[24]);
                symbol = Encoding.UTF8.GetString(subArray(body, 25, 20));
                side = Convert.ToChar(body[45]);
                qty = BitConverter.ToInt16(subArray(body, 46, 2), 0);
                price = BitConverter.ToInt64(subArray(body, 48, 8), 0);
                priceFlag = Convert.ToChar(body[56]);
                timeInForce = Convert.ToChar(body[57]);
                positionEffect = Convert.ToChar(body[58]);
                reportTime = BitConverter.ToInt32(subArray(body, 59, 4), 0);
                orderNo = BitConverter.ToInt32(subArray(body, 63, 4), 0);
                code = BitConverter.ToInt16(subArray(body, 67, 2), 0);                
            }

            public string toLog()
            {
                string logData = "<--|DT=2010|ReportSeqno=" + reportSeqno + "|frontOffice=" + frontOffice +
                                 "|functionCode=" + functionCode + "|account_h=" + account_h +
                                 "|account=" + account + "|Capital_account" + capital_account +
                                 "|sub_account=" + sub_account + "|exchange=" + exchange +
                                 "|market=" + market + "|symbol=" + symbol.Trim() +"|side=" + side +
                                 "|qty=" + qty + "|price=" + price + "|priceFlag=" + priceFlag +
                                 "|timeInForce=" + timeInForce + "|positionEffect=" + positionEffect +
                                 "|reportTime=" + reportTime + "|orderNo=" + orderNo + "|code=" + code;
                return logData;
            }

        }

        public static byte[] StructToBytes(object structObj)
        {
            int size = Marshal.SizeOf(structObj);                   //得到结构体的大小 
            byte[] bytes = new byte[size];                          //创建byte数组 
            IntPtr structPtr = Marshal.AllocHGlobal(size);          //分配结构体大小的内存空间 
            Marshal.StructureToPtr(structObj, structPtr, false);    //将结构体拷到分配好的内存空间 
            Marshal.Copy(structPtr, bytes, 0, size);                //从内存空间拷到byte数组
            Marshal.FreeHGlobal(structPtr);                         //释放内存空间 
            return bytes;                                           //返回byte数组             
        }

        public static object BytesToStruct(byte[] bytes, Type type)
        {
            int size = Marshal.SizeOf(type);                        //得到结构体的大小
            if (size > bytes.Length)                                //byte数组长度小于结构体的大小
                return null;                                        //返回空
            IntPtr structPtr = Marshal.AllocHGlobal(size);          //分配结构体大小的内存空间
            Marshal.Copy(bytes, 0, structPtr, size);                //将byte数组拷到分配好的内存空间
            object obj = Marshal.PtrToStructure(structPtr, type);   //将内存空间转换为目标结构体
            Marshal.FreeHGlobal(structPtr);                         //释放内存空间
            return obj;                                             //返回结构体
        }

        public static byte[] ConvertIntToByteArray(Int16 I16)
        {
            return BitConverter.GetBytes(I16);
        }

        public static byte[] ConvertIntToByteArray(Int32 I32)
        {
            return BitConverter.GetBytes(I32);
        }

        public static byte[] ConvertIntToByteArray(Int64 I64)
        {
            return BitConverter.GetBytes(I64);
        }

        //public static byte[] ConvertIntToByteArray(int I)
        //{
        //    return BitConverter.GetBytes(I);
        //}

        public static double ConvertByteArrayToDouble(byte[] b)
        {
            double result;
            switch (b.Length)
            {
                case 2:
                    result = BitConverter.ToInt16(b, 0);
                    break;
                case 4:
                    result = BitConverter.ToInt32(b, 0);
                    break;
                case 8:
                    result = BitConverter.ToInt16(b, 0);
                    break;
                default:
                    result = 0;
                    break;
            }
            return result;
        }


        private void btnPublish_Click(object sender, EventArgs e) {
            if (push.ConnectionStatus != CONNECTION_STATUS.CS_CONNECTREADY)
            {
                AddInfo("Connection is not ready");
                return;
            }

            PB2001 pb2001 = new PB2001();
            pb2001.userNo = push.UserNo;
            pb2001.account_h = txtAccount.Text.ToString()[0];
            pb2001.account = Convert.ToInt32(txtAccount.Text.Substring(1, 9));
            pb2001.exchange = '0';
            pb2001.market = 'O';            
            pb2001.setSymbol(txtSymbol.Text.Trim());
            pb2001.side = (cmbSide.Text.Substring(0, 1).ToUpper() == "B") ? 'B' : 'S';
            pb2001.modify_type = 'N';
            pb2001.qty = Convert.ToInt16(txtQty.Text);
            pb2001.price = Convert.ToInt64(txtPrice.Text);
            pb2001.priceFlag = (cmbOrdType.Text.Substring(0, 1).ToUpper() == "L") ? '2' : '1';
            switch (cmbTimeInForce.Text.Trim().ToUpper())
            {
                case "ROD":
                    pb2001.timeInForce = 'R';
                    break;
                case "IOC":
                    pb2001.timeInForce = 'I';
                    break;
                case "FOK":
                    pb2001.timeInForce = 'F';
                    break;
            }
            
            pb2001.positionEffect = (cmbPositionEffect.Text.Substring(0,1).ToUpper() == "O") ? 'O' : 'C';
            pb2001.front_office = '0';

            byte[] pb = StructToBytes(pb2001);

            push.Subscribe(pb2001.getAccount());
            //PB2001
            //byte[] pb = new byte[65];

            //ushort userNo = push.UserNo;
            //byte[] UserNo = ConvertIntToByteArray(userNo);
            
            //char account_h = 'A';
            //byte Account_h = Convert.ToByte(account_h);

            //int account = 233620030;
            //byte[] Account = ConvertIntToByteArray(account);

            //char exchange = '0';
            //byte Exchange = Convert.ToByte(exchange);

            //char market = 'O';
            //byte Market = Convert.ToByte(market);

            //string symbol = "90000461";
            //char[] Symbol = new char[20]; ;            
            //Array.Copy(symbol.ToCharArray(), Symbol, symbol.Length);
            //byte[] bSymbol = Encoding.Default.GetBytes(Symbol);

            //char side = 'B';
            //byte Side = Convert.ToByte(side);

            //char modify = 'N';
            //byte Modify = Convert.ToByte(modify);

            //ushort qty = 1;
            //byte[] Qty = ConvertIntToByteArray(qty);

            //long price = 500;
            //byte[] Price = ConvertIntToByteArray(price);

            //char priceFlag = '2';
            //byte PriceFlag = Convert.ToByte(priceFlag);

            //char timeInForce = 'R';
            //byte TimeInForce = Convert.ToByte(timeInForce);

            //char positionEffect = 'O';
            //byte PositionEffect = Convert.ToByte(positionEffect);

            //char front_office = '0';
            //byte Front_office = Convert.ToByte(front_office);

            //Array.Copy(UserNo, pb, UserNo.Length);
            //pb[6] = Account_h;            
            //Array.Copy(Account, 0, pb, 7, Account.Length);
            //pb[23] = Exchange;
            //pb[24] = Market;
            //Array.Copy(bSymbol, 0, pb, 25, bSymbol.Length);
            //pb[45] = Side;
            //pb[46] = Modify;
            //Array.Copy(Qty, 0, pb, 47, Qty.Length);
            //Array.Copy(Price, 0, pb, 49, Price.Length);
            //pb[57] = PriceFlag;
            //pb[58] = TimeInForce;
            //pb[59] = PositionEffect;
            //pb[64] = Front_office;

            //var s = System.Runtime.InteropServices.Marshal.SizeOf(pb); //to know struct length
            //Console.WriteLine(ConvertByteArrayToDouble(bUserNo));

            //long RequestId = push.Publish("SSEOrder.Sv", 2001, pb);
            long RequestId = push.Publish(subTopic1, 2001, pb, false, true);
            //AddInfo("Request Id SEND: " + RequestId);
            AddInfo(pb2001.toLog());
            //}

        }

        private void btnSubscribe_Click(object sender, EventArgs e) {
            if (txtTopic.Text.Trim() == "")
            {
                MessageBox.Show("Topic is empty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            push.Subscribe(txtTopic.Text.Trim());
        }

        private void btnUnsubscribe_Click(object sender, EventArgs e) {
            push.Unsubscribe(subTopic1);
        }

        private void btnDisconnect_Click(object sender, EventArgs e) {
            if (push != null)
                push.Disconnect();
        }
    }
}
