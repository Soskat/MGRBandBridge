using Communication.Data;
using Communication.Packet;
using FakeBand.Fakes;
using Microsoft.Band;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;

namespace BandBridge.ViewModels
{
    /// <summary>
    /// Class that implements server that provide communication between MS Band device and remote client.
    /// </summary>
    public class Server : NotificationBase
    {
        #region Fields
        /// <summary>
        /// Local host name.
        /// </summary>
        private HostName _LocalHost { get; set; }

        /// <summary>
        /// Service port number written in string.
        /// </summary>
        private string _ServicePort { get; set; }

        /// <summary>
        /// Server <see cref="StreamSocketListener"/> object.
        /// </summary>
        private StreamSocketListener _ServerSocketListener { get; set; }

        /// <summary>
        /// BandData buffers storage size.
        /// </summary>
        private int _StorageSize { get; set; }

        /// <summary>
        /// Fake Bands amount.
        /// </summary>
        private int _FakeBandsAmount { get; set; }

        /// <summary>
        /// Server debug info.
        /// </summary>
        private StringBuilder _DebugInfo;

        /// <summary>
        /// List of connected Band devices.
        /// </summary>
        private Dictionary<string, BandData> _ConnectedBands;

        /// <summary>
        /// List of connected Bands data.
        /// </summary>
        private ObservableCollection<BandData> _ConnectedBandsCollection;

        /// <summary>
        /// Is server working?
        /// </summary>
        private bool _IsServerWorking;
        
        /// <summary>
        /// Received message.
        /// </summary>
        private Message message;

        /// <summary>
        /// Message buffer size.
        /// </summary>
        private const int bufferSize = 256;

        /// <summary>
        /// Buffer for incoming data.
        /// </summary>
        private byte[] receiveBuffer;
        #endregion

        #region Properties
        /// <summary>
        /// Local host name.
        /// </summary>
        public HostName LocalHost
        {
            get { return _LocalHost; }
            set { SetProperty(_LocalHost, value, () => _LocalHost = value); }
        }

        /// <summary>
        /// Service port number written in string.
        /// </summary>
        public string ServicePort
        {
            get { return _ServicePort; }
            set { SetProperty(_ServicePort, value, () => _ServicePort = value); }
        }

        /// <summary>
        /// BandData buffers storage size.
        /// </summary>
        public int StorageSize
        {
            get { return _StorageSize; }
            set { SetProperty(_StorageSize, value, () => _StorageSize = value); }
        }

        /// <summary>
        /// Fake Bands amount.
        /// </summary>
        public int FakeBandsAmount
        {
            get { return _FakeBandsAmount; }
            set { if (value >= 0) SetProperty(_FakeBandsAmount, value, () => _FakeBandsAmount = value); }
        }

        /// <summary>
        /// List of connected Bands data.
        /// </summary>
        public ObservableCollection<BandData> ConnectedBandsCollection
        {
            get { return _ConnectedBandsCollection; }
            set { SetProperty(ref _ConnectedBandsCollection, value); }
        }

        /// <summary>
        /// Server debug info.
        /// </summary>
        public string DebugInfo
        {
            get { return _DebugInfo.ToString(); }
            set
            {
                if(value == "") SetProperty(_DebugInfo.ToString(), value, () => _DebugInfo.Clear());
                else SetProperty(_DebugInfo.ToString(), value, () => _DebugInfo.AppendLine(value));
            }
        }

        /// <summary>
        /// Is server working?
        /// </summary>
        public bool IsServerWorking
        {
            get { return _IsServerWorking; }
            set { SetProperty(_IsServerWorking, value, () => _IsServerWorking = value); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="Server"/>.
        /// </summary>
        /// <param name="servicePort">Number of port used for service</param>
        public Server(string servicePort)
        {
            ServicePort = servicePort;
            StorageSize = 30;
            FakeBandsAmount = 6;
            IsServerWorking = false;
            _DebugInfo = new StringBuilder();
            foreach (HostName localHostName in NetworkInformation.GetHostNames())
            {
                if (localHostName.IPInformation != null && localHostName.Type == HostNameType.Ipv4)
                {
                    LocalHost = localHostName;
                    break;
                }
            }
            receiveBuffer = new byte[bufferSize];
        }
        #endregion

        #region Methods
        /// <summary>
        /// Set ups and starts the server.
        /// </summary>
        /// <returns>Asynchronous operation</returns>
        public async Task StartServer()
        {
            DebugInfo = ">> Try to start the server...";
            Debug.WriteLine(">> Try to start the server...");

            try
            {
                // Create a StreamSocketListener to start listening for TCP connections:
                if (_ServerSocketListener == null) _ServerSocketListener = new StreamSocketListener();
                else return;

                // Hook up an event handler to call when connections are received:
                _ServerSocketListener.ConnectionReceived += SocketListener_ConnectionReceived;

                // Start listening for incoming TCP connections on the specified port:
                await _ServerSocketListener.BindServiceNameAsync(ServicePort);

                IsServerWorking = true;
                
                DebugInfo = "Waiting for connections...";
                Debug.WriteLine("Waiting for connections...");
            }
            catch (Exception e)
            {
                DebugInfo = ">> Exception in StartServer(): " + e.Message;
                Debug.WriteLine(">> Exception in StartServer(): " + e.Message);
            }
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <returns>Asynchronous operation</returns>
        public void StopServer()
        {
            DebugInfo = ">> Try to stop the server...";
            Debug.WriteLine(">> Try to stop the server...");

            try
            {
                // explicitly close the socketListener:
                if (_ServerSocketListener != null)
                {
                    //await socketListener.CancelIOAsync();
                    _ServerSocketListener.Dispose();
                    _ServerSocketListener = null;

                    IsServerWorking = false;
                }
                //DebugInfo = ">> Closing the server...";
                Debug.WriteLine(">> Closing the server...");
            }
            catch (Exception e)
            {
                //DebugInfo = ">> Exception in StartServer(): " + e.Message;
                Debug.WriteLine(">> Exception in StartServer(): " + e.Message);
            }
        }

        /// <summary>
        /// Server behaviour on receiving a connection with remote client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void SocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                //DebugInfo = "Connection received from " + args.Socket.Information.RemoteAddress;
                Debug.WriteLine("Connection received from " + args.Socket.Information.RemoteAddress);


                PacketProtocol packetizer = new PacketProtocol(2048);
                packetizer.MessageArrived += receivedMessage =>
                {
                    Debug.Write(":: Received bytes: " + receivedMessage.Length + "  => ");
                    if (receivedMessage.Length > 0)
                    {
                        Debug.WriteLine("deserialize message");
                        message = Message.Deserialize(receivedMessage);
                    }
                    else Debug.WriteLine("keepalive message");
                };

                //Read data from the remote client.
                Stream inStream = args.Socket.InputStream.AsStreamForRead();
                await inStream.ReadAsync(receiveBuffer, 0, bufferSize);
                packetizer.DataReceived(receiveBuffer);
                Debug.WriteLine("__Received: " + message);

                // Prepare response:
                Message response = PrepareResponseToClient(message);
                byte[] byteData = PacketProtocol.WrapMessage(Message.Serialize(response));

                //Send the response to the remote client.
                Stream outStream = args.Socket.OutputStream.AsStreamForWrite();
                await outStream.WriteAsync(byteData, 0, byteData.Length);
                await outStream.FlushAsync();
                Debug.WriteLine("__Sent back: " + response);
                Debug.WriteLine("---------------------------------------------------------");

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Gets MS Band devices connected to local computer.
        /// </summary>
        public async Task GetMSBandDevices()
        {
            //DebugInfo = ">> Get MS Band devices...";
            Debug.WriteLine(">> Get MS Band devices...");

            IBandClientManager clientManager = BandClientManager.Instance;
            IBandInfo[] pairedBands = await clientManager.GetBandsAsync();

            if (_ConnectedBands == null) _ConnectedBands = new Dictionary<string, BandData>();

            if (pairedBands.Length > 0)
            {
                Dictionary<string, BandData> tempDic = new Dictionary<string, BandData>();

                // keep existing BandData from previously connected Band devices untouched:
                foreach (KeyValuePair<string, BandData> kvp in _ConnectedBands)
                {
                    for (int i = 0; i < pairedBands.Length; i++)
                    {
                        if (pairedBands[i] != null &&
                           kvp.Key == pairedBands[i].Name)
                        {
                            tempDic.Add(kvp.Key, kvp.Value);
                            pairedBands[i] = null;
                            break;
                        }
                    }
                }
                // add new BandData from recently connected Band devices:
                for (int i = 0; i < pairedBands.Length; i++)
                {
                    if (pairedBands[i] != null)
                    {
                        var bandClient = await clientManager.ConnectAsync(pairedBands[i]);
                        if (bandClient != null)
                        {
                            BandData bandData = new BandData(bandClient, pairedBands[i].Name, StorageSize);
                            tempDic.Add(pairedBands[i].Name, bandData);
                            await bandData.GetHeartRate();
                            await bandData.GetGsr();
                        }
                    }
                }
                // update ConnectedBands dictionary:
                _ConnectedBands.Clear();
                _ConnectedBands = tempDic;
            }
            else
            {
                //DebugInfo = ">> No Bands found";
                Debug.WriteLine(">> No Bands found");
            }

            // update ObservableCollection of connected Bands:
            SetupBandsListView();
        }

        /// <summary>
        /// Generates <see cref="FakeBandsAmount"/> number of Fake Bands.
        /// </summary>
        public async Task GetFakeBands()
        {
            //DebugInfo = ">> Get Fake Bands...";
            Debug.WriteLine(">> Get Fake Bands...");

            List<IBandInfo> fakeBands = new List<IBandInfo>();
            for (int i = 1; i <= FakeBandsAmount; i++)
            {
                fakeBands.Add(new FakeBandInfo(BandConnectionType.Bluetooth, "Fake Band " + i.ToString()));
            }
            FakeBandClientManager.Configure(new FakeBandClientManagerOptions { Bands = fakeBands });

            IBandClientManager clientManager = FakeBandClientManager.Instance;
            IBandInfo[] pairedBands = await clientManager.GetBandsAsync();

            if (_ConnectedBands == null) _ConnectedBands = new Dictionary<string, BandData>();
            else
            {
                // dispose all connected bands:
                foreach (var band in _ConnectedBands)
                {
                    band.Value.BandClient.Dispose();
                }
                _ConnectedBands.Clear();
            }

            foreach (IBandInfo band in pairedBands)
            {
                var bandClient = await clientManager.ConnectAsync(band);
                if (bandClient != null)
                {
                    BandData bandData = new BandData(bandClient, band.Name, StorageSize);
                    _ConnectedBands.Add(band.Name, bandData);
                    await bandData.GetHeartRate();
                    await bandData.GetGsr();
                }
            }

            // update ObservableCollection of connected Bands:
            SetupBandsListView();
        }
        
        /// <summary>
        /// Sets up currently connected Band devices list.
        /// </summary>
        private void SetupBandsListView()
        {
            if(ConnectedBandsCollection == null) ConnectedBandsCollection = new ObservableCollection<BandData>();
            else ConnectedBandsCollection.Clear();
            // update ObservableCollection:
            foreach (BandData bandData in _ConnectedBands.Values)
            {
                ConnectedBandsCollection.Add(bandData);
            }
        }

        /// <summary>
        /// Clears all debug info.
        /// </summary>
        public void ClearDebugInfo()
        {
            DebugInfo = "";
        }
        #endregion
        
        
        private Message PrepareResponseToClient(Message message)
        {
            switch (message.Code)
            {
                // send the list of all connected Bands:
                case Command.SHOW_ASK:
                    if (_ConnectedBands != null)
                        return new Message(Command.SHOW_ANS, _ConnectedBands.Keys.ToArray());
                    else
                        return new Message(Command.SHOW_ANS, null);

                // send data from specified connected Band:
                case Command.GET_DATA_ASK:
                    if (message.Result != null)
                    {
                        if (message.Result.GetType() == typeof(string))
                        {
                            if (_ConnectedBands != null && _ConnectedBands.ContainsKey((string)message.Result))
                            {
                                // prepare data:
                                int hrAvg = _ConnectedBands[(string)message.Result].HrBuffer.GetAverage();
                                int gsrAvg = _ConnectedBands[(string)message.Result].GsrBuffer.GetAverage();
                                return new Message(Command.GET_DATA_ANS,
                                                   new SensorData[] {
                                                       new SensorData(SensorCode.HR, hrAvg),
                                                       new SensorData(SensorCode.GSR, gsrAvg)
                                                   });
                            }
                            else return new Message(Command.GET_DATA_ANS, null);
                        }
                    }
                    return new Message(Command.CTR_MSG, null);
                    
                // wrong message code:
                default:
                    return new Message(Command.CTR_MSG, null);
            }
        }
    
    }
}