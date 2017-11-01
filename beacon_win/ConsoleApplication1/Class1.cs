public class winBeacon
{
    private BluetoothLEAdvertisementWatcher watcher;
    static void Main()
    {
        watcher = new BluetoothLEAdvertisementWatcher();
        watcher.Received += OnAdvertisementReceived;//ここのイベント内で取得する
        watcher.Start();
        Console.WriteLine("終了するには何かキーを押してください．．．");
        Console.ReadKey();
        watcher.Stop();
    }

    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        //イベントの値から取得
        DateTimeOffset timestamp = eventArgs.Timestamp;
        BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType;
        string rssi = eventArgs.RawSignalStrengthInDBm;
        string name = eventArgs.Advertisement.LocalName;

        Console.WriteLine(string.Format("timestamp:{0}", timestamp.ToString("HH\\:mm\\:ss\\.fff")));
        Console.WriteLine(string.Format("rssi:{0}", rssi.ToString("D")));
    }
}
