using BandBridge.Data;
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
        private HostName _LocalHost;

        /// <summary>
        /// Service port number written in string.
        /// </summary>
        private string _ServicePort;

        /// <summary>
        /// Server <see cref="StreamSocketListener"/> object.
        /// </summary>
        private StreamSocketListener _ServerSocketListener;

        /// <summary>
        /// BandData buffers storage size.
        /// </summary>
        private int _StorageSize;

        /// <summary>
        /// Fake Bands amount.
        /// </summary>
        private int _FakeBandsAmount;

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


        private Dictionary<string, ClientInfo> clientBandPairs;


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
            //receiveBuffer = new byte[bufferSize];
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

                receiveBuffer = new byte[bufferSize];

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
                while (!packetizer.AllBytesReceived)
                {
                    await inStream.ReadAsync(receiveBuffer, 0, bufferSize);
                    packetizer.DataReceived(receiveBuffer);
                }
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



        private async void SendDataToPairedClient(ClientInfo clientInfo, Message message)
        {
            try
            {
                if (clientInfo == null)
                    return;

                // Create the StreamSocket and establish a connection to remote server:
                StreamSocket socket = new StreamSocket();

                // Prepare data for sending:
                byte[] byteData = PacketProtocol.WrapMessage(Message.Serialize(message));

                // Connect to remote host:
                await socket.ConnectAsync(clientInfo.ClientAddress, clientInfo.Port.ToString());

                // Write data to the remote server.
                Stream outStream = socket.OutputStream.AsStreamForWrite();
                await outStream.WriteAsync(byteData, 0, byteData.Length);
                await outStream.FlushAsync();

                Debug.WriteLine(string.Format("Send {0} to {1}:{2}", message, clientInfo.ClientAddress, clientInfo.Port));
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

            // check if _ConnectedBands and clientBandPairs dictionaries exist:
            if (_ConnectedBands == null) _ConnectedBands = new Dictionary<string, BandData>();
            if (clientBandPairs == null) clientBandPairs = new Dictionary<string, ClientInfo>();

            // deal with all connected Band devices:
            if (pairedBands.Length > 0)
            {
                Dictionary<string, BandData> tempCB = new Dictionary<string, BandData>();
                Dictionary<string, ClientInfo> tempCBP = new Dictionary<string, ClientInfo>();

                // keep existing BandData from previously connected Band devices untouched:
                foreach (KeyValuePair<string, BandData> kvp in _ConnectedBands)
                {
                    for (int i = 0; i < pairedBands.Length; i++)
                    {
                        if (pairedBands[i] != null && kvp.Key == pairedBands[i].Name)
                        {
                            tempCB.Add(kvp.Key, kvp.Value);
                            tempCBP.Add(kvp.Key, clientBandPairs[kvp.Key]);
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
                            tempCB.Add(pairedBands[i].Name, bandData);

                            // add Band to client-Band pairs temporaty dictionary:
                            tempCBP.Add(bandData.Name, null);
                            bandData.NewSensorData += newReading =>
                            {
                                Message msg = new Message(MessageCode.BAND_DATA, newReading);
                                SendDataToPairedClient(tempCBP[bandData.Name], msg);
                            };

                            // starts reading sensor data:
                            await bandData.GetHeartRate();
                            await bandData.GetGsr();
                        }
                    }
                }
                // update _ConnectedBands dictionary:
                _ConnectedBands.Clear();
                _ConnectedBands = tempCB;
                // update clientBandPairs dictionary:
                clientBandPairs.Clear();
                clientBandPairs = tempCBP;
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

            // clear _ConnectedBands dictionary:
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
            // clear clientBandPairs dictionary:
            if (clientBandPairs == null) clientBandPairs = new Dictionary<string, ClientInfo>();
            else clientBandPairs.Clear();

            Dictionary<string, ClientInfo> tempCBP = new Dictionary<string, ClientInfo>();
            // connect new fake Bands:
            foreach (IBandInfo band in pairedBands)
            {
                var bandClient = await clientManager.ConnectAsync(band);
                if (bandClient != null)
                {
                    BandData bandData = new BandData(bandClient, band.Name, StorageSize);
                    _ConnectedBands.Add(band.Name, bandData);

                    // add Band to client-Band pairs temporaty dictionary:
                    tempCBP.Add(band.Name, null);
                    bandData.NewSensorData += newReading =>
                    {
                        Message msg = new Message(MessageCode.BAND_DATA, newReading);
                        SendDataToPairedClient(tempCBP[band.Name], msg);
                    };

                    // starts reading sensor data:
                    await bandData.GetHeartRate();
                    await bandData.GetGsr();
                }
            }
            // update client-Bands pairs list:
            clientBandPairs = tempCBP;

            // update ObservableCollection of connected Bands:
            SetupBandsListView();
        }
        
        /// <summary>
        /// Sets up currently connected Band devices list.
        /// </summary>
        private void SetupBandsListView()
        {
            if (ConnectedBandsCollection == null)
                ConnectedBandsCollection = new ObservableCollection<BandData>();
            else
                ConnectedBandsCollection.Clear();
            // update ObservableCollection:
            foreach (BandData bandData in _ConnectedBands.Values)
            {
                ConnectedBandsCollection.Add(bandData);
            }
        }

        /// <summary>
        /// Updates list that contains client-Band pairs.
        /// </summary>
        private void UpdateClientBandPairsList()
        {
            if (clientBandPairs == null)
                clientBandPairs = new Dictionary<string, ClientInfo>();

            Dictionary<string, ClientInfo> temp = new Dictionary<string, ClientInfo>();

            // check if there are already some Bands which are paired with some remote clients:
            foreach(var key in _ConnectedBands.Keys.ToList())
            {
                if (clientBandPairs.ContainsKey(key))
                    temp.Add(key, clientBandPairs[key]);
                else
                    temp.Add(key, null);
            }

            // update client-Bands pairs list:
            clientBandPairs = temp;
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
                case MessageCode.SHOW_ASK:
                    if (_ConnectedBands != null)
                        return new Message(MessageCode.SHOW_ANS, _ConnectedBands.Keys.ToArray());
                    else
                        return new Message(MessageCode.SHOW_ANS, null);

                // pair with specified Band request from connected client:
                case MessageCode.PAIR_ASK:
                    if(message.Result != null && message.Result.GetType() == typeof(PairRequest))
                    {
                        if (clientBandPairs.ContainsKey(((PairRequest)message.Result).BandName))
                        {
                            ClientInfo clientInfo = new ClientInfo(
                                                                   ((PairRequest)message.Result).ClientAddress,
                                                                   ((PairRequest)message.Result).OpenPort
                                                                  );
                            clientBandPairs.Add(((PairRequest)message.Result).BandName, clientInfo);
                            return new Message(MessageCode.PAIR_ANS, true);
                        }
                        return new Message(MessageCode.PAIR_ANS, false);
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                // free paired Band request from connected client:
                case MessageCode.FREE_ASK:
                    if(message.Result != null && message.Result.GetType() == typeof(string))
                    {
                        if (clientBandPairs.ContainsKey((string)message.Result))
                        {
                            clientBandPairs[(string)message.Result].ClientAddress = null;
                        }
                        return new Message(MessageCode.FREE_ANS, null);
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                //// send data from specified connected Band:
                //case MessageCode.GET_DATA_ASK:
                //    if (message.Result != null)
                //    {
                //        if (message.Result.GetType() == typeof(string))
                //        {
                //            if (_ConnectedBands != null && _ConnectedBands.ContainsKey((string)message.Result))
                //            {
                //                // prepare data:
                //                int hrAvg = _ConnectedBands[(string)message.Result].HrBuffer.GetAverage();
                //                int gsrAvg = _ConnectedBands[(string)message.Result].GsrBuffer.GetAverage();
                //                return new Message(MessageCode.GET_DATA_ANS,
                //                                   new SensorData[] {
                //                                       new SensorData(SensorCode.HR, hrAvg),
                //                                       new SensorData(SensorCode.GSR, gsrAvg)
                //                                   });
                //            }
                //            else return new Message(MessageCode.GET_DATA_ANS, null);
                //        }
                //    }
                //    return new Message(MessageCode.CTR_MSG, null);


                // wrong message code:
                default:
                    return new Message(MessageCode.CTR_MSG, null);
            }
        }
    
    }
}