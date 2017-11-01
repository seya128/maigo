using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using System.Net;
using System.IO;

public class winBeacon
{
    private static BluetoothLEAdvertisementWatcher watcher;
    static void Main()
    {
        watcher = new BluetoothLEAdvertisementWatcher();
        watcher.Received += OnAdvertisementReceived;//ここのイベント内で取得する
        watcher.Start();
        Console.WriteLine("終了するには何かキーを押してください．．．");
        Console.ReadKey();
        watcher.Stop();
    }

    private static void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        //プロパティ値から取得
        DateTimeOffset timestamp = eventArgs.Timestamp;
        BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType;

        iBeacon bcon = new iBeacon(eventArgs);

        //UUIDからハイフンを削除
        String uuidn;
        char[] removeChars = new char[] { '-' };
        uuidn = removeChars.Aggregate(
            bcon.UUID, (s, c) => s.Replace(c.ToString(), ""));

        string retBeaconData;
        retBeaconData = "{";
        retBeaconData += string.Format("uuid:'{0}',", bcon.UUID);//"00000000-0000-0000-0000-000000000000"
        retBeaconData += string.Format("major:{0},", bcon.Major.ToString("D"));
        retBeaconData += string.Format("minor:{0},", bcon.Minor.ToString("D"));
        retBeaconData += string.Format("measuredPower:{0},", bcon.MeasuredPower.ToString("D"));
        retBeaconData += string.Format("rssi:{0},", bcon.Rssi.ToString("D"));
        retBeaconData += string.Format("accuracy:{0},", bcon.Accuracy.ToString("F6"));
        retBeaconData += string.Format("distance:{0},", bcon.Distance.ToString("F6"));
        retBeaconData += string.Format("proximity:'{0}'", bcon.Proximity);
        retBeaconData += "}";
        if (bcon.UUID.Length != 0)
        {
            if (uuidn == "B9407F30F5F8466EAFF925556B57FE6E" && (bcon.Major==  8446 || bcon.Major == 58842 || bcon.Major == 2427 || bcon.Major == 61343))
            {
                //Console.WriteLine(string.Format("timestamp:{0}", timestamp.ToString("HH\\:mm\\:ss\\.fff")));
                //Console.WriteLine(retBeaconData);
                Console.WriteLine(bcon.Major.ToString("D") +" "+ bcon.Distance.ToString("F6"));
                // Create a request for the URL.
                //http://lost-child.eu-gb.mybluemix.net/watchdog?client_id=1234&major=123&minor=12&location_id=2&distance=0.23
                String sURL = "http://lost-child.eu-gb.mybluemix.net/watchdog?client_id=";
                sURL += uuidn.ToLower();
                sURL += "&major=";
                sURL += bcon.Major.ToString("D");
                sURL += "&minor=";
                sURL += bcon.Minor.ToString("D");
                sURL += "&location_id=1&distance=";
                sURL += bcon.Distance.ToString("F6");
                //Console.WriteLine(sURL);
                WebRequest request = WebRequest.Create(sURL);
                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Display the content.
                //Console.WriteLine(responseFromServer);
                // Clean up the streams and the response.
                reader.Close();
                response.Close();
            }
        }
    }
}

public class iBeacon
{

    private const int MinimumLengthInBytes = 25;//最小の長さ
    private const int AdjustedLengthInBytes = -2;//CompanyID分の2桁ずれている為読み取り位置補正

    //プロパティ
    public string Name { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public BluetoothLEAdvertisementType AdvertisementType { get; set; }

    public int ManufacturerId { get; set; }
    public int Major { get; set; }
    public int Minor { get; set; }
    public string UUID { get; set; }
    public short Rssi { get; set; }
    public short MeasuredPower { get; set; }
    public double ManufacturerReserved { get; set; }
    //public double Distance { get; set; }

    //精度（accuracy）
    public double Accuracy
    {
        get { return calcAccuracy(MeasuredPower, Rssi); }
    }
    public double Distance
    {
        get { return calcDistance(MeasuredPower, Rssi); }
    }
    //近接度（Proximity）：近接（immidiate）、1m以内（near）、1m以遠（far）、不明（Unknown）
    public string Proximity
    {
        get
        {

            string _Proximity = "Unknown";

            //Rssi未取得ならUnknown
            if (Rssi == 0) { return _Proximity; }

            //rssi値からProximityを判別
            if (Rssi > -40)
            {
                _Proximity = "immidiate";//近接
            }
            else if (Rssi > -59)
            {
                _Proximity = "near";//1m以内
            }
            else
            {
                _Proximity = "far";//1m以遠
            }
            return _Proximity;
        }
    }

    //コンストラクタ
    public iBeacon()
    {
        ManufacturerId = -1;
        Major = -1;
        Minor = -1;
        Rssi = 0;
        UUID = "";
        MeasuredPower = -1;
        ManufacturerReserved = -1.0;
        //Distance = -1;
    }

    //コンストラクタ２
    public iBeacon(BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {

        //出力されているbyteデータから各値を抽出する
        var manufacturerSections = eventArgs.Advertisement.ManufacturerData;
        Timestamp = eventArgs.Timestamp;
        AdvertisementType = eventArgs.AdvertisementType;

        if (manufacturerSections.Count > 0)
        {
            var manufacturerData = manufacturerSections[0];
            var data = new byte[manufacturerData.Data.Length];

            iBeacon bcon = new iBeacon();

            using (var reader = DataReader.FromBuffer(manufacturerData.Data))
            {
                reader.ReadBytes(data);
            }

            //長さをチェック
            if (data == null || data.Length < MinimumLengthInBytes + AdjustedLengthInBytes)
            {
                return;
            }

            //イベントから取得
            Rssi = eventArgs.RawSignalStrengthInDBm;
            Name = eventArgs.Advertisement.LocalName;
            ManufacturerId = manufacturerData.CompanyId;

            //バイトデータから抽出
            //公式での出力値（Windowsでは2byteずれているので補正が必要）
            // Byte(s)  WinByte(s) Name
            // --------------------------
            // 0-1      none       Manufacturer ID (16-bit unsigned integer, big endian)
            // 2-3      0-1        Beacon code (two 8-bit unsigned integers, but can be considered as one 16-bit unsigned integer in little endian)
            // 4-19     2-17       ID1 (UUID)
            // 20-21    18-19      ID2 (16-bit unsigned integer, big endian)
            // 22-23    20-21      ID3 (16-bit unsigned integer, big endian)
            // 24       22         Measured Power (signed 8-bit integer)
            // 25       23         Reserved for use by the manufacturer to implement special features (optional)

            //BigEndianの値を取得
            UUID = BitConverter.ToString(data, 4 + AdjustedLengthInBytes, 16); // Bytes 2-17
            MeasuredPower = Convert.ToSByte(BitConverter.ToString(data, 24 + AdjustedLengthInBytes, 1), 16); // Byte 22

            //もし追加データがあればここで取得
            if (data.Length >= MinimumLengthInBytes + AdjustedLengthInBytes + 1)
            {
                ManufacturerReserved = data[25 + AdjustedLengthInBytes]; // Byte 23
            }

            //.NET FramewarkのEndianはCPUに依存するらしい
            if (BitConverter.IsLittleEndian)
            {
                //LittleEndianの値を取得
                byte[] revData;

                revData = new byte[] { data[20 + AdjustedLengthInBytes], data[21 + AdjustedLengthInBytes] };// Bytes 18-19
                Array.Reverse(revData);
                Major = BitConverter.ToUInt16(revData, 0);

                revData = new byte[] { data[22 + AdjustedLengthInBytes], data[23 + AdjustedLengthInBytes] };// Bytes 20-21
                Array.Reverse(revData);
                Minor = BitConverter.ToUInt16(revData, 0);
            }
            else
            {
                //BigEndianの値を取得
                Major = BitConverter.ToUInt16(data, 20 + AdjustedLengthInBytes); // Bytes 18-19
                Minor = BitConverter.ToUInt16(data, 22 + AdjustedLengthInBytes); // Bytes 20-21
            }
        }
        else
        {
            new iBeacon();
        }
    }

    //精度を計算する
    protected static double calcAccuracy(short measuredPower, short rssi)
    {
        if (rssi == 0)
        {
            return -1.0; //nodata return -1.
        }

        double ratio = rssi * 1.0 / measuredPower;
        if (ratio < 1.0)
        {
            return Math.Pow(ratio, 10);
        }
        else
        {
            double accuracy = (0.89976) * Math.Pow(ratio, 7.7095) + 0.111;
            return accuracy;
        }
    }
    protected static double calcDistance(short measuredPower, short rssi)
    {
        if (rssi == 0)
        {
            return -1.0; //nodata return -1.
        }

        //double d = 10^(((double)measuredPower -(double)rssi) / 20.0);
        double d = Math.Pow(10, ((double)measuredPower - (double)rssi) / 20.0);
        if (d < 0) { d = 0; }
        //Console.WriteLine(d);
        return d;
    }
    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        //プロパティ値から取得
        DateTimeOffset timestamp = eventArgs.Timestamp;
        BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType;

        iBeacon bcon = new iBeacon(eventArgs);

        string retBeaconData;
        retBeaconData = "{";
        retBeaconData += string.Format("uuid:'{0}',", bcon.UUID);//"00000000-0000-0000-0000-000000000000"
        retBeaconData += string.Format("major:{0},", bcon.Major.ToString("D"));
        retBeaconData += string.Format("minor:{0},", bcon.Minor.ToString("D"));
        retBeaconData += string.Format("measuredPower:{0},", bcon.MeasuredPower.ToString("D"));
        retBeaconData += string.Format("rssi:{0},", bcon.Rssi.ToString("D"));
        retBeaconData += string.Format("accuracy:{0},", bcon.Accuracy.ToString("F6"));
        retBeaconData += string.Format("distance:{0},", bcon.Distance.ToString("F6"));
        retBeaconData += string.Format("proximity:'{0}'", bcon.Proximity);
        retBeaconData += "}";

        //Console.WriteLine(string.Format("timestamp:{0}", timestamp.ToString("HH\\:mm\\:ss\\.fff")));

    }
}