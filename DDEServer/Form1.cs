using QuikDdeDataServer;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// В этом пространстве имен живет сервер DDE

namespace DDEServer
{
    public partial class Form1 : Form
    {
        public double StartCapital;

        private DdeInitializeHandler server;

        DataClasses1DataContext db = new DataClasses1DataContext(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

        Int64 portion = 0;

        public Form1()
        {
            StartCapital = 50000;
            InitializeComponent();

            // Создаем объект обработчика сервера DDE. 
            // Имя сервера указываем в меню Quik "Вывод через DDE сервер" - "DDE сервер"
            server = new DdeInitializeHandler("DDESample");

            // Инициализируем сервер DDE
            server.DdeInitialize();

            // Добавление раздела для регистрации в DDEML. С этого момента раздел может использоваться для 
            // экспорта информации из терминала Quik. Имя раздела формируется так: [Имя книги]Имя листа,
            // где "Имя книги" и "Имя листа" указывается в меню Quik "Вывод через DDE сервер" - 
            // "Рабочая книга", "Лист"
            server.AddTopic("candle_RIZ6");
            server.AddTopic("current");
            server.AddTopic("RIU30");
            //  server.AddTopic("[Book1]List2");
            //  server.AddTopic("[Book1]List3");

            // Удаление раздела из DDEML. С этого момента раздел не может использоваться для 
            // экспорта информации из терминала Quik.
            //server.RemoveTopic("[Book1]List3");

            // Устанавливаем приемник события OnExchangeData, которое происходит при полкчении новой 
            // порции данных от терминала Quik.
            DdeInitializeHandler.OnExchangeData += 
                new EventHandler<DataEventArgs>(DDEInitializeHandler_ExchangeData);

        }
        public bool CandleExistInBase(string ID)
        {



            return true;
        }
        // Приемник события OnExchangeData
        private void DDEInitializeHandler_ExchangeData(object sender, DataEventArgs e)
        {
            string ss = String.Empty;

            // Формируем определение таблицы биржевых данных
            ExchangeDataTable dataTable = e.exchangeDataTable;
            //ExchangeDataCell[,] dataCell = e.exchangeDataTable.ExchangeDataCells;
            portion++;

            //  string LastCandleBase  = (from c in db.Table where c.Tiker == dataTable.TopicName.ToString() orderby c.DateTime descending select c.Id).Max().ToString();

            int CandlesCount = 0;
            //label10.Text = LastCandleBase;

           // label8.Text = counter.ToString();

            Candles LastCandleFromQuik = new Candles();

            LastCandleFromQuik.ID = dataTable.ExchangeDataCells[1, 0].DataCell.ToString()+ dataTable.ExchangeDataCells[1, 1].DataCell.ToString() + dataTable.TopicName.ToString();

            label6.Text = LastCandleFromQuik.ID;

            if(checkBox3.Checked == false)
            CandlesCount = 1;

            System.Data.SqlClient.SqlConnection sqlConnection1 =
                                  new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
            cmd.CommandType = System.Data.CommandType.Text;
           
            cmd.Connection = sqlConnection1;

            sqlConnection1.Open();



            // Ну и выводим полученную от терминала Quik информацию
            for (int r = 0; r < dataTable.RowsLength; r++)
            {


                            if (dataTable.TopicName.ToString().Contains("candle") == true)
                            { 
                            string TempID = dataTable.ExchangeDataCells[r, 0].DataCell.ToString() + dataTable.ExchangeDataCells[r, 1].DataCell.ToString() + dataTable.TopicName.ToString().Replace("candle", "");
                            string TempTiker = dataTable.TopicName.ToString().Replace("candle","");
                            string TempTime = dataTable.ExchangeDataCells[r, 1].DataCell.ToString();
                            Single TempOpen = Convert.ToSingle(dataTable.ExchangeDataCells[r, 2].DataCell.ToString().Replace(".", ","));
                            Single TempClose = Convert.ToSingle(dataTable.ExchangeDataCells[r, 5].DataCell.ToString().Replace(".", ","));
                            Single TempHigh = Convert.ToSingle(dataTable.ExchangeDataCells[r, 3].DataCell.ToString().Replace(".", ","));
                            Single TempLow = Convert.ToSingle(dataTable.ExchangeDataCells[r, 4].DataCell.ToString().Replace(".", ","));



                            cmd.CommandText = "INSERT INTO Candles (ID, Ticker, CandleTime, CandleHigh, CandleLow, CandleOpen, CandleClose) SELECT '" + TempID + "','" + TempTiker + "','" + TempTime + "','" + TempHigh + "','" + TempLow + "','" + TempOpen + "','" + TempClose + "' WHERE NOT EXISTS (SELECT ID FROM Candles WHERE ID = '" + TempID + "');";
                            cmd.ExecuteNonQuery();

                            }

                if (dataTable.TopicName.ToString().Contains("current") == true)
                {

                    String TempChange, TempLast, TempOborot;
                    string TempTicker = dataTable.ExchangeDataCells[r, 0].DataCell.ToString();
                    TempChange = dataTable.ExchangeDataCells[r, 2].DataCell.ToString().Replace(",", ".");
                   TempLast = dataTable.ExchangeDataCells[r, 3].DataCell.ToString().Replace(",", ".");
                    TempOborot = dataTable.ExchangeDataCells[r, 4].DataCell.ToString().Replace(",", ".");
                    if (TempChange == "")
                        TempChange = "0.0";
                    if (TempLast == "")
                        TempLast = "0.0";

                    if (TempOborot == "")
                        TempOborot = "0.0";



                    cmd.CommandText = "INSERT INTO dbo.[Current] (Ticker, Change, Last_price, Oborot) SELECT '" + TempTicker + "'," + TempChange + "," + TempLast + "," + TempOborot + " WHERE NOT EXISTS (SELECT Ticker FROM dbo.[Current] WHERE Ticker = '" + TempTicker + "');";
                    cmd.CommandText = cmd.CommandText + "UPDATE dbo.[Current] SET Change = " + TempChange + ", Last_price = " + TempLast + ", Oborot = " + TempOborot + " where Ticker = '" + TempTicker + "' ;";

                    cmd.ExecuteNonQuery();

                }


            }

            sqlConnection1.Close();

            label5.Text = CandlesCount.ToString();

            label4.Text = portion.ToString();
        }

        private string Prosadka(string Tiker)
        {

            Single PreviosValue = 0;
            Single ProcentFall = 0;
            string lastvalue = "";
            int FallsCount = 0;
            int FallsMax = 0;
            Single ProcentFallMax = 0;
            bool LastWasFall = false;

            

            var Capital = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Capital).ToArray();
            var ID = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Id).ToArray();

            for (int i = 1; i < Capital.Count(); i++)
            {
                if (Convert.ToSingle(Capital.GetValue(i - 1)) > 0)
                {
                    PreviosValue = Convert.ToSingle(Capital.GetValue(i - 1));

                }
                if (Convert.ToSingle(Capital.GetValue(i)) > PreviosValue)
                {
                    ProcentFall = 0;
                    FallsCount = 0;
                }

                    if (Convert.ToSingle(Capital.GetValue(i)) < PreviosValue && Convert.ToSingle(Capital.GetValue(i)) >0)
                {
                    ProcentFall = ProcentFall+(PreviosValue - Convert.ToSingle(Capital.GetValue(i))) * 100 / PreviosValue;
                    LastWasFall = true;
                    FallsCount++;

                }
                    else
                {
                    if (ProcentFallMax < ProcentFall)
                    {
                        ProcentFallMax = ProcentFall;
                        lastvalue = ID.GetValue(i).ToString();
                        FallsMax = FallsCount;
                    }
                }


               
            }

           // MessageBox.Show(lastvalue.ToString());
            return (ProcentFallMax.ToString() + " по количеству " + FallsMax.ToString());
        }

        private void TestSignals2(string Tiker)
        {
            string sdelka = "";
            int SignalsCount = 0;
            int LossCount = 0;
            int ProfitCount = 0;
            int PaperCount = 0;
            int StepCount = 0;
            int StepIncrement = 0;
            double OpenPrice = 0;
            double LastPrice = 100000;
            double Deposit = 35000;

            double StopLoss = 0;
            double TakeProfit = 0;
            double Step = 0;
            bool inStockDown = false;
            bool inStockUp = false;


            var ID = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Id).ToArray();
            var H = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.High).ToArray();
            var L = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Low).ToArray();
            var O = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Open).ToArray();
            var BPU = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.BPU).ToArray();

            for (int i = 2; i < ID.Count() - 2; i++)
            {
                // if(i== ID.Count()-1)
                // MessageBox.Show(ID.GetValue(i).ToString());

               

                if (/*Convert.ToString(BPU.GetValue(i)).TrimEnd() == "up" && */StepIncrement <= 10000)
                {

                    
                    i = i + 2;
                    

                    inStockUp = true;
                    OpenPrice = Convert.ToDouble(O.GetValue(i));
                   
                    TakeProfit = Convert.ToDouble(O.GetValue(i)) * (1 + Convert.ToDouble(textBox2.Text) / 100);
                    PaperCount = 100;// ( Convert.ToInt32(Deposit / OpenPrice) - 1)*7;
                    Step = (TakeProfit - OpenPrice) / Convert.ToInt32(textBox4.Text); // Дельту профита разделили на кол-во частей
                    StepCount = Convert.ToInt32(PaperCount / Convert.ToInt32(textBox4.Text)); // кол-во бумаг разделили на кол-во степов - 1

                   if (OpenPrice > LastPrice)
                    { inStockUp = false; }

                   

                    if (inStockUp == true)
                    {
                        LastPrice = OpenPrice-1;

                        StepIncrement = StepIncrement+100;

                        sdelka = Convert.ToString(ID.GetValue(i)) + " Покупка. Цена " + OpenPrice.ToString() + " Стоп: " + StopLoss.ToString() + " Профит: " + TakeProfit.ToString() + " Бумаг всего: " + StepIncrement.ToString();
                        //    listBox3.Items.Add(sdelka);
                        SignalsCount = SignalsCount + 1;

                        System.Data.SqlClient.SqlConnection sqlConnection1 =
                                    new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                        System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "UPDATE [Table] SET Trade = 'Buy' WHERE id='" + ID.GetValue(i - 1).ToString() + "';" + "UPDATE [Table] SET TradeComment = N'" + sdelka + "' WHERE id='" + ID.GetValue(i - 1).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i - 1).ToString() + "';";

                        cmd.Connection = sqlConnection1;

                        sqlConnection1.Open();
                        cmd.ExecuteNonQuery();
                        sqlConnection1.Close();
                    }
                }

             
                if (StepIncrement > 0 && Convert.ToDouble(H.GetValue(i)) > TakeProfit)
                {
                 

                    StepIncrement = StepIncrement - 100;
                    ProfitCount = ProfitCount + 1;
                    Deposit = Deposit + (TakeProfit - OpenPrice) * PaperCount - TakeProfit  * PaperCount*0.1/100;
                    inStockUp = false;
                    //   listBox3.Items.Add("Выход по профиту. " + "Депозит: " + Deposit.ToString() + "Остаток бумаг:" + PaperCount.ToString());

                    System.Data.SqlClient.SqlConnection sqlConnection1 =
                                        new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "UPDATE [Table] SET Trade = 'Profit' WHERE id='" + ID.GetValue(i).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i).ToString() + "';";

                    cmd.Connection = sqlConnection1;

                    sqlConnection1.Open();
                    cmd.ExecuteNonQuery();
                    sqlConnection1.Close();

                }

                               
               

            }



            label16.Text = SignalsCount.ToString() + "  " + Deposit.ToString();
            /*listBox2.Items.Add("Выиграных: " + ProfitCount.ToString());
            listBox2.Items.Add("Проиграных: " + LossCount.ToString());
            listBox2.Items.Add("Депоз*/
        }
        private void TestSignals(string Tiker)
        {
            string sdelka = "";
            int SignalsCount = 0;
            int LossCount = 0;
            int ProfitCount = 0;
            int PaperCount = 0;
            int StepCount = 0;
            int StepIncrement = 0;
            double OpenPrice = 0;
            double Deposit = 35000;
           
            double StopLoss = 0;
            double TakeProfit = 0;
            double Step = 0;
            bool inStockDown = false;
            bool inStockUp = false;

            
            var ID = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Id).ToArray();
            var H = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.High).ToArray();
            var L = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Low).ToArray();
            var O = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.Open).ToArray();
            var BPU = (from c in db.Table where c.Tiker == Tiker orderby c.Id ascending select c.BPU).ToArray();

            for (int i = 2; i < ID.Count()-2; i++)
            {
               // if(i== ID.Count()-1)
             // MessageBox.Show(ID.GetValue(i).ToString());

                if (Convert.ToString(BPU.GetValue(i)).TrimEnd() == "down" && inStockDown == false && inStockUp == false)
                {

                    StopLoss = Convert.ToDouble(H.GetValue(i)) * (1 + Convert.ToDouble(textBox1.Text) / 100);
                    i = i + 2;

                    inStockDown = true;
                    OpenPrice = Convert.ToDouble(O.GetValue(i));
               /*  if (Convert.ToDouble((StopLoss - OpenPrice) * 100 / StopLoss) > Convert.ToDouble(textBox3.Text))
                    { inStockDown = false; }*/
                    TakeProfit = Convert.ToDouble(O.GetValue(i)) * (1 - Convert.ToDouble(textBox2.Text) / 100);
                    PaperCount = 1;//(Convert.ToInt32(Deposit / OpenPrice) - 1)*7;
                    Step = (OpenPrice - TakeProfit) / Convert.ToInt32(textBox4.Text); // Дельту профита разделили на кол-во частей
                    StepCount = Convert.ToInt32(PaperCount / Convert.ToInt32(textBox4.Text)); // кол-во бумаг разделили на кол-во степов - 1
                    if (StepCount * Convert.ToInt32(textBox4.Text) > PaperCount)
                        StepCount = StepCount - 1;
                    StepIncrement = 1;
                    if (inStockDown == true)
                    {
                        sdelka = Convert.ToString(ID.GetValue(i)) + " Продажа. Цена " + OpenPrice.ToString() + " Стоп: " + StopLoss.ToString() + " Профит: " + TakeProfit.ToString() + " Бумаг всего: " + PaperCount.ToString() + " Степ: " + StepCount.ToString(); ;
                        SignalsCount = SignalsCount + 1;
                        // listBox3.Items.Add(sdelka);

                        System.Data.SqlClient.SqlConnection sqlConnection1 =
                                        new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                        System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "UPDATE [Table] SET Trade = 'Sell' WHERE id='" + ID.GetValue(i-1).ToString() + "';" + "UPDATE [Table] SET TradeComment = N'" + sdelka + "' WHERE id='" + ID.GetValue(i-1).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i-1).ToString() + "';";
                      

                        cmd.Connection = sqlConnection1;

                        sqlConnection1.Open();
                        cmd.ExecuteNonQuery();
                        sqlConnection1.Close();

                    }

                }

                if (Convert.ToString(BPU.GetValue(i)).TrimEnd() == "up" && inStockDown == false && inStockUp == false)
                {

                    StopLoss = Convert.ToDouble(L.GetValue(i)) * (1 - Convert.ToDouble(textBox1.Text) / 100);
                    i = i + 2;


                    inStockUp = true;
                    OpenPrice = Convert.ToDouble(O.GetValue(i));
                   if (Convert.ToDouble((OpenPrice - StopLoss) * 100 / OpenPrice) > Convert.ToDouble(textBox3.Text))
                    { inStockUp = false; }
                    TakeProfit = Convert.ToDouble(O.GetValue(i)) * (1 + Convert.ToDouble(textBox2.Text) / 100);
                    PaperCount = 1;// ( Convert.ToInt32(Deposit / OpenPrice) - 1)*7;
                    Step = (TakeProfit - OpenPrice) / Convert.ToInt32(textBox4.Text); // Дельту профита разделили на кол-во частей
                    StepCount = Convert.ToInt32(PaperCount / Convert.ToInt32(textBox4.Text)); // кол-во бумаг разделили на кол-во степов - 1
                    if (StepCount * Convert.ToInt32(textBox4.Text) > PaperCount)
                        StepCount = StepCount - 1;
                    StepIncrement = 1;

                    if (inStockUp == true)
                    {
                        sdelka = Convert.ToString(ID.GetValue(i)) + " Покупка. Цена " + OpenPrice.ToString() + " Стоп: " + StopLoss.ToString() + " Профит: " + TakeProfit.ToString() + " Бумаг всего: " + PaperCount.ToString() + " Степ: " + StepCount.ToString();
                    //    listBox3.Items.Add(sdelka);
                        SignalsCount = SignalsCount + 1;

                        System.Data.SqlClient.SqlConnection sqlConnection1 =
                                    new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                        System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "UPDATE [Table] SET Trade = 'Buy' WHERE id='" + ID.GetValue(i-1).ToString() + "';" + "UPDATE [Table] SET TradeComment = N'" + sdelka + "' WHERE id='" + ID.GetValue(i-1).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i-1).ToString() + "';";
                       
                        cmd.Connection = sqlConnection1;

                        sqlConnection1.Open();
                        cmd.ExecuteNonQuery();
                        sqlConnection1.Close();
                    }
                }

                if (inStockUp == true && StepIncrement < Convert.ToInt32(textBox4.Text) && checkBox2.Checked == true && Convert.ToDouble(H.GetValue(i)) > (OpenPrice + Step * StepIncrement))
                {
                    Deposit = Deposit + ((OpenPrice + Step * StepIncrement) - OpenPrice) * StepCount - OpenPrice * StepCount * 0.00046 - (OpenPrice + Step * StepIncrement) * StepCount * 0.00046;

                    PaperCount = PaperCount - StepCount;
                    StepIncrement++;
                    //   listBox3.Items.Add("Частичный выход по профиту. " + "Депозит: " + Deposit.ToString() + " Остаток бумаг1: " + PaperCount.ToString());


                }

                if (inStockUp == true && Convert.ToDouble(H.GetValue(i)) > TakeProfit)
                {

                    ProfitCount = ProfitCount + 1;
                    Deposit = Deposit + (TakeProfit - OpenPrice) * PaperCount - PaperCount * 6;
                    inStockUp = false;
                    //   listBox3.Items.Add("Выход по профиту. " + "Депозит: " + Deposit.ToString() + "Остаток бумаг:" + PaperCount.ToString());

                    System.Data.SqlClient.SqlConnection sqlConnection1 =
                                        new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "UPDATE [Table] SET Trade = 'Profit' WHERE id='" + ID.GetValue(i).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i).ToString() + "';";

                    cmd.Connection = sqlConnection1;

                    sqlConnection1.Open();
                    cmd.ExecuteNonQuery();
                    sqlConnection1.Close();

                }

                if (inStockDown == true && StepIncrement < Convert.ToInt32(textBox4.Text) && checkBox2.Checked == true && Convert.ToDouble(L.GetValue(i)) < (OpenPrice - Step * StepIncrement))
                {

                    Deposit = Deposit + (OpenPrice - (OpenPrice - Step * StepIncrement)) * StepCount - OpenPrice * StepCount * 0.00046 - (OpenPrice - Step * StepIncrement) * StepCount * 0.00046;
                    PaperCount = PaperCount - StepCount;
                    StepIncrement++;
                   // listBox3.Items.Add("Частичный выход по профиту. " + "Депозит: " + Deposit.ToString() + " Остаток бумаг:" + PaperCount.ToString());
                }

                if (inStockDown == true && Convert.ToDouble(L.GetValue(i)) < TakeProfit)
                {

                    ProfitCount = ProfitCount + 1;
                    Deposit = Deposit + (OpenPrice - TakeProfit) * PaperCount - PaperCount * 6;
                    inStockDown = false;
                    // listBox3.Items.Add("Выход по профиту. " + "Депозит:" + Deposit.ToString() + " Остаток бумаг:" + PaperCount.ToString());
                    System.Data.SqlClient.SqlConnection sqlConnection1 =
                                                        new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "UPDATE [Table] SET Trade = 'Profit' WHERE id='" + ID.GetValue(i).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i).ToString() + "';";

                    cmd.Connection = sqlConnection1;

                    sqlConnection1.Open();
                    cmd.ExecuteNonQuery();
                    sqlConnection1.Close();


                }
                if (inStockUp == true && Convert.ToDouble(L.GetValue(i)) < StopLoss)
                {
                    if (ID.GetValue(i).ToString().Substring(8, 6) == "100000")
                        StopLoss = Convert.ToDouble(L.GetValue(i));

                    LossCount = LossCount + 1;
                    Deposit = Deposit - (OpenPrice - StopLoss) * PaperCount - PaperCount * 6;
                    inStockUp = false;
                    //  listBox3.Items.Add("Выход по стоплосс. " + "Депозит:" + Deposit.ToString() + " Остаток бумаг:" + PaperCount.ToString());

                    System.Data.SqlClient.SqlConnection sqlConnection1 =
                                        new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "UPDATE [Table] SET Trade = 'StopLoss' WHERE id='" + ID.GetValue(i).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i).ToString() + "';";

                    cmd.Connection = sqlConnection1;

                    sqlConnection1.Open();
                    cmd.ExecuteNonQuery();
                    sqlConnection1.Close();


                }


                if (inStockDown == true && Convert.ToDouble(H.GetValue(i)) > StopLoss)
                {

                    if (ID.GetValue(i).ToString().Substring(8, 6) == "100000")
                        StopLoss = Convert.ToDouble(H.GetValue(i));

                    LossCount = LossCount + 1;
                    Deposit = Deposit - (StopLoss - OpenPrice) * PaperCount - PaperCount * 6;
                    inStockDown = false;
                    //   listBox3.Items.Add("Выход по стоплосс. " + "Депозит:" + Deposit.ToString() + " Остаток бумаг:" + PaperCount.ToString());

                    System.Data.SqlClient.SqlConnection sqlConnection1 =
                                        new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "UPDATE [Table] SET Trade = 'StopLoss' WHERE id='" + ID.GetValue(i).ToString() + "';" + "UPDATE [Table] SET Capital = " + Deposit.ToString().Replace(",", ".") + " WHERE id='" + ID.GetValue(i).ToString() + "';";

                    cmd.Connection = sqlConnection1;

                    sqlConnection1.Open();
                    cmd.ExecuteNonQuery();
                    sqlConnection1.Close();
                }


            }



            label16.Text =SignalsCount.ToString() +"  " + Deposit.ToString();
            /*listBox2.Items.Add("Выиграных: " + ProfitCount.ToString());
            listBox2.Items.Add("Проиграных: " + LossCount.ToString());
            listBox2.Items.Add("Депоз*/
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            // Отключаем приемник события OnExchangeData
            DdeInitializeHandler.OnExchangeData -= 
                new EventHandler<DataEventArgs>(DDEInitializeHandler_ExchangeData);

            // Деинициализируем DDE сервер и закрываем используемые внутренние ресурсы
            server.DdeUninitialize();

           
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
           
        }

        private void Signals(string Tiker)
        {
            int vse, bpu_count, bsu_count;
            double OpenPrice = 0;
            string Signal = "";
            bool inTrade = true;
            double StopLoss = 0;
            double TakeProfit = 0;
            var Trade = (from c in db.Table where c.Tiker == Tiker orderby c.Id descending select c.Trade).ToArray();
            var ID = (from c in db.Table where c.Tiker == Tiker orderby c.Id descending select c.Id).ToArray();
            var H = (from c in db.Table where c.Tiker == Tiker orderby c.Id descending select c.High).ToArray();
            var C = (from c in db.Table where c.Tiker == Tiker orderby c.DateTime descending select c.Close).ToArray();
            var L = (from c in db.Table where c.Tiker == Tiker orderby c.Id descending select c.Low).ToArray();
            var MailSent = (from c in db.Table where c.Tiker == Tiker orderby c.Id descending select c.MailSent).ToArray();
            // var BSU = (from c in db.Table where c.Tiker == Tiker orderby c.DateTime descending select c.BSU).ToArray();
            //var BPU = (from c in db.Table where c.Tiker == Tiker orderby c.DateTime descending select c.BPU).ToArray();

            vse = ID.Count() - 1;

            if (checkBox1.Checked == true)
            {
                bpu_count = ID.Count() - 1;
            }
            else
            {
                bpu_count = 4;

            }

            for (int k = 0; k < bpu_count; k++)
            {
                if (Trade.GetValue(k).ToString().TrimEnd() != "")
                {
                    if (Trade.GetValue(k).ToString().TrimEnd() != "Sell" && Trade.GetValue(k).ToString().TrimEnd() != "Buy" )
                    {

                        inTrade = false;

                        label20.Text = "Ждем сигнал. Последний " + Trade.GetValue(k).ToString().TrimEnd();

                        k = bpu_count;

                        

                    }
                    else
                    {
                        inTrade = true;
                        
                        label20.Text = "В сделке " + Trade.GetValue(k).ToString().TrimEnd();
                        k = bpu_count;
                    }

                }
            }

                for (int k = 0; k < bpu_count; k++)
            {

          
                if (Convert.ToDouble(H.GetValue(k)) == Convert.ToDouble(H.GetValue(k + 1)))
                {
                    for (int p = k + 2; p < vse; p++)
                    {

                        if ((p - k) > 120)
                            p = vse;

                        if (Convert.ToDouble(H.GetValue(k)) == Convert.ToDouble(H.GetValue(p)))
                        {
                          if (k < 50 && MailSent.GetValue(k).ToString().TrimEnd() != "1" )
                            {
                                Signal = "down";
                                                               
                                StopLoss = Convert.ToDouble(H.GetValue(k)) * (1 + Convert.ToDouble(textBox1.Text) / 100);

                                OpenPrice = Convert.ToDouble(C.GetValue(k));

                                TakeProfit = Convert.ToDouble(C.GetValue(k)) * (1 - Convert.ToDouble(textBox2.Text) / 100);

                            
                            }

                                                    
                            System.Data.SqlClient.SqlConnection sqlConnection1 =
                         new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "UPDATE [Table] SET BPU = 'down' WHERE id='" + ID.GetValue(k + 1).ToString() + "';" + "UPDATE [Table] SET MailSent = '1' WHERE id='" + ID.GetValue(k).ToString() + "';";

                            cmd.Connection = sqlConnection1;

                            sqlConnection1.Open();
                            cmd.ExecuteNonQuery();
                            sqlConnection1.Close();
                            
                            break;

                        }

                    }

                }

                if (Convert.ToDouble(L.GetValue(k)) == Convert.ToDouble(L.GetValue(k + 1)))
                {
                    for (int p = k + 2; p < vse; p++)
                    {

                        if ((p - k) > 120)
                            p = vse;

                        if (Convert.ToDouble(L.GetValue(k)) == Convert.ToDouble(L.GetValue(p)) )
                        {
                             if (k < 50 && MailSent.GetValue(k).ToString().TrimEnd() != "1" )
                             {
                               
                                Signal = "up";

                                StopLoss = Convert.ToDouble(L.GetValue(k)) * (1 - Convert.ToDouble(textBox1.Text) / 100);
                               
                                OpenPrice = Convert.ToDouble(C.GetValue(k));
                              
                                TakeProfit = Convert.ToDouble(C.GetValue(k)) * (1 + Convert.ToDouble(textBox2.Text) / 100);


                            }
                         
                            System.Data.SqlClient.SqlConnection sqlConnection1 =
                        new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

                            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "UPDATE [Table] SET BPU = 'up' WHERE id='" + ID.GetValue(k+1).ToString() + "';" + "UPDATE [Table] SET MailSent = '1' WHERE id='" + ID.GetValue(k).ToString() + "';";

                            cmd.Connection = sqlConnection1;

                            sqlConnection1.Open();
                            cmd.ExecuteNonQuery();
                            sqlConnection1.Close();


                            break;

                        }

                    }

                }

            }

           ////
           if (Signal != "")
            {
                string TRANS = String.Format("{0:1ddHHmmss}", DateTime.ParseExact(Convert.ToString(DateTime.Now), "dd.MM.yyyy H:mm:ss", null));
                string SLTRANS = String.Format("{0:2HHddmmss}", DateTime.ParseExact(Convert.ToString(DateTime.Now), "dd.MM.yyyy H:mm:ss", null));
                string s = "";

                StopLoss = Math.Round(StopLoss / 10) * 10;
                TakeProfit =  Math.Round(TakeProfit / 10) * 10;


                if (Signal =="down")
                {

                    if (checkBox4.Checked == false  && inTrade == false )
                    {
                          SendOrder("RIU6", "S", "1", OpenPrice, TRANS);

                         SendSLOrder("RIU6", "B", "1", StopLoss, TakeProfit, SLTRANS);

                    }

                        s = Convert.ToString(DateTime.Now) + " " + " Сигнал к продаже: " + OpenPrice.ToString() + " Стоп: " + (Math.Round(StopLoss / 10) * 10).ToString() + " Тэйк: " + (Math.Round(TakeProfit / 10) * 10).ToString();

                }
                if (Signal == "up")
                {
                   if  (checkBox4.Checked == false  && inTrade == false )
                    {

                         SendOrder("RIU6", "B", "1", OpenPrice, TRANS);

                         SendSLOrder("RIU6", "S", "1", StopLoss, TakeProfit, SLTRANS);
                    }

                    s = Convert.ToString(DateTime.Now) + " " + " Сигнал к покупке: " + OpenPrice.ToString() + " Стоп: " + (Math.Round(StopLoss/10)*10).ToString() + " Тэйк: " + (Math.Round(TakeProfit/10)*10).ToString();

                }
                
             
                
                SmtpClient Smtp = new SmtpClient("smtp.mail.ru", 25);

                Smtp.EnableSsl = true;
                Smtp.UseDefaultCredentials = false;
                Smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                Smtp.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                Smtp.Credentials = new NetworkCredential("rvkstudent@mail.ru", "Sapromat1");

                MailMessage Message = new MailMessage("rvkstudent@mail.ru", "rvkstudent@mail.ru", "Сигнал ", s);


                Smtp.Send(Message);//отправка
                
            }
           
            
             
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label18.Text = Prosadka("RIU30");

            System.Data.SqlClient.SqlConnection sqlConnection1 =
                                       new System.Data.SqlClient.SqlConnection(@"Data Source=ROMANNB-ПК;Initial Catalog=C:\CANDLEBASE\DATABASE1.MDF;Integrated Security=True");

            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "update [Table] set Capital = '';update[Table] set Capital = '';UPDATE[Table] SET Trade = '';UPDATE[Table] SET BPU = '';UPDATE[Table] SET BSU = ''; UPDATE[Table] SET TradeComment = '';";
            
            
            cmd.Connection = sqlConnection1;

            sqlConnection1.Open();
            cmd.ExecuteNonQuery();
            sqlConnection1.Close();
        }

        private string SendOrder(string seccode, string BuySell, string lots, double Price, string TransID)
        {
            string Format = "";
            string ClassCode = "";

            if (seccode == "RIU6")
            {
                Format = "#####";
                ClassCode = "SPBFUT";
            }
           
            using (StreamWriter sw1 = new StreamWriter("D:\\import\\TRANS.tro"))
            {
                sw1.WriteLine("");
                sw1.Close();
            }

            using (StreamWriter sw = new StreamWriter("D:\\import\\trans.tri"))
            {
                sw.WriteLine("ACCOUNT=4100PEO; CLIENT_CODE=42658; TYPE=L; TRANS_ID={4}; CLASSCODE={5}; SECCODE={0}; ACTION=NEW_ORDER; OPERATION={1}; PRICE={2}; QUANTITY={3};", seccode, BuySell, Price.ToString(Format), lots, TransID, ClassCode);
                sw.Close();
            }

            Thread.Sleep(2000);

            FileInfo TRO = new FileInfo("D:\\import\\TRANS.tro");

            string OrderNumber = "";

            if (TRO.Exists == true && TRO.LastWriteTime < DateTime.Now)
            {
                using (StreamReader streamReader = new StreamReader("D:\\import\\TRANS.tro", Encoding.Default))
                {

                    string str = "";


                    while (!streamReader.EndOfStream)
                    {
                        str += streamReader.ReadLine();
                        if (str != "")
                        {
                            if (str.Split(new Char[] { '=', ' ', ';' })[1] == TransID)
                            {
                                for (int i = 1; i < str.Split(new Char[] { '=', ' ', ';' }).Length; i++)
                                {
                                    if (str.Split(new Char[] { '=', ' ', ';' })[i] == "ORDER_NUMBER")
                                    {
                                        OrderNumber = str.Split(new Char[] { '=', ' ', ';' })[i + 1];
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            return OrderNumber;

        }
        private string SendSLOrder(string seccode, string BuySell, string lots, double StopLoss, double TakeProfit, string TransID)
        {
            /// определение формата цены
            /// 
            string Format = "";
            string ClassCode = "";

            if (seccode == "RIU6")
            {
                Format = "#####";
                ClassCode = "SPBFUT";
            }

            using (StreamWriter sw1 = new StreamWriter("D:\\import\\TRANS.tro"))
            {
                sw1.WriteLine("");
                sw1.Close();
            }

            using (StreamWriter sw = new StreamWriter("D:\\import\\trans.tri"))
            {
                sw.WriteLine("ACTION=NEW_STOP_ORDER; STOP_ORDER_KIND=TAKE_PROFIT_AND_STOP_LIMIT_ORDER; ACCOUNT=4100PEO; TRANS_ID={4}; CLASSCODE={5}; SECCODE={0}; OPERATION={1}; QUANTITY={3}; CLIENT_CODE=42658; STOPPRICE={6}; MARKET_STOP_LIMIT=YES;MARKET_TAKE_PROFIT=YES; STOPPRICE2={2}; KILL_IF_LINKED_ORDER_PARTLY_FILLED=NO", seccode, BuySell, StopLoss.ToString(Format), lots, TransID, ClassCode, TakeProfit.ToString(Format));

                sw.Close();
            }

            Thread.Sleep(2000);

            FileInfo TRO = new FileInfo("D:\\import\\TRANS.tro");

            string OrderNumber = "";

            if (TRO.Exists == true && TRO.LastWriteTime < DateTime.Now)
            {
                using (StreamReader streamReader = new StreamReader("D:\\import\\TRANS.tro", Encoding.Default))
                {

                    string str = "";

                    while (!streamReader.EndOfStream)
                    {
                        str += streamReader.ReadLine();
                        if (str != "")
                        {
                            if (str.Split(new Char[] { '=', ' ', ';' })[1] == TransID)
                            {
                                for (int i = 1; i < str.Split(new Char[] { '=', ' ', ';' }).Length; i++)
                                {
                                    if (str.Split(new Char[] { '=', ' ', ';' })[i] == "ORDER_NUMBER")
                                    {
                                        OrderNumber = str.Split(new Char[] { '=', ' ', ';' })[i + 1];
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            return OrderNumber;

        }
        private string KillSLOrder(string STOP_ORDER_KEY, string seccode, string TransID)
        {
            string Format = "";
            string ClassCode = "";

            if (seccode == "SBER")
            {
                Format = "#.##";
                ClassCode = "EQBR";
            }
            if (seccode == "GAZP")
            {
                Format = "#.##";
                ClassCode = "EQNE";
            }
            if (seccode == "VTBR")
            {
                Format = "#.#####";
                ClassCode = "EQBR";
            }


            using (StreamWriter sw1 = new StreamWriter("D:\\import\\TRANS.tro"))
            {
                sw1.WriteLine("");
                sw1.Close();
            }

            using (StreamWriter sw = new StreamWriter("D:\\import\\trans.tri"))
            {


                sw.WriteLine("ACCOUNT=L01-00000F00; CLIENT_CODE=42658; TYPE=L; TRANS_ID={1}; CLASSCODE={3}; ACTION=KILL_STOP_ORDER; STOP_ORDER_KEY={2}; SECCODE={0};", seccode, TransID, STOP_ORDER_KEY, ClassCode);

                sw.Close();
            }

            Thread.Sleep(2000);

            FileInfo TRO = new FileInfo("D:\\import\\TRANS.tro");

            string OrderNumber = "";

            if (TRO.Exists == true && TRO.LastWriteTime < DateTime.Now)
            {
                using (StreamReader streamReader = new StreamReader("D:\\import\\TRANS.tro", Encoding.Default))
                {



                    string str = "";


                    while (!streamReader.EndOfStream)
                    {
                        str += streamReader.ReadLine();
                        if (str != "")
                        {
                            if (str.Split(new Char[] { '=', ' ', ';' })[1] == TransID)
                            {
                                for (int i = 1; i < str.Split(new Char[] { '=', ' ', ';' }).Length; i++)
                                {
                                    if (str.Split(new Char[] { '=', ' ', ';' })[i] == "ORDER_NUMBER")
                                    {
                                        OrderNumber = str.Split(new Char[] { '=', ' ', ';' })[i + 1];
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

           
            return OrderNumber;

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
    }
