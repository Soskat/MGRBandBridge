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
        private HostName localHost;

        /// <summary>
        /// Service port number written in string.
        /// </summary>
        private string servicePort;

        /// <summary>
        /// Server <see cref="StreamSocketListener"/> object.
        /// </summary>
        private StreamSocketListener serverSocketListener;
        
        /// <summary>
        /// Fake Bands amount.
        /// </summary>
        private int fakeBandsAmount = 6;
        
        /// <summary>
        /// Dictionary of connected Band devices.
        /// </summary>
        private Dictionary<string, BandData> connectedBands;

        /// <summary>
        /// List of connected Bands data.
        /// </summary>
        private ObservableCollection<BandData> connectedBandsCollection;
        
        /// <summary>
        /// Is server working?
        /// </summary>
        private bool isServerWorking;
        
        /// <summary>
        /// Band data buffer size.
        /// </summary>
        private int bandBufferSize = 16;

        /// <summary>
        /// Calibration data buffer size.
        /// </summary>
        private int calibrationBufferSize = 100;

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
            get { return localHost; }
            set { SetProperty(localHost, value, () => localHost = value); }
        }

        /// <summary>
        /// Service port number written in string.
        /// </summary>
        public string ServicePort
        {
            get { return servicePort; }
            set { SetProperty(servicePort, value, () => servicePort = value); }
        }

        /// <summary>
        /// Fake Bands amount.
        /// </summary>
        public int FakeBandsAmount
        {
            get { return fakeBandsAmount; }
            set { if (value >= 0) SetProperty(fakeBandsAmount, value, () => fakeBandsAmount = value); }
        }

        /// <summary>
        /// List of connected Bands data.
        /// </summary>
        public ObservableCollection<BandData> ConnectedBandsCollection
        {
            get { return connectedBandsCollection; }
            set { SetProperty(ref connectedBandsCollection, value); }
        }
        
        /// <summary>
        /// Is server working?
        /// </summary>
        public bool IsServerWorking
        {
            get { return isServerWorking; }
            set { SetProperty(isServerWorking, value, () => isServerWorking = value); }
        }
        
        /// <summary>
        /// Band data buffer size.
        /// </summary>
        public int BandBufferSize
        {
            get { return bandBufferSize; }
            set { SetProperty(bandBufferSize, value, () => bandBufferSize = value); }
        }

        /// <summary>
        /// Calibration data buffer size.
        /// </summary>
        public int CalibrationBufferSize
        {
            get { return calibrationBufferSize; }
            set { SetProperty(calibrationBufferSize, value, () => calibrationBufferSize = value); }
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
            //FakeBandsAmount = 6;
            IsServerWorking = false;
            // get host's IPv4 address:
            LocalHost = Array.Find(NetworkInformation.GetHostNames().ToArray(),
                                   a => a.IPInformation != null && a.Type == HostNameType.Ipv4);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Set-ups and starts the server.
        /// </summary>
        /// <returns></returns>
        public async Task StartServer()
        {
            try
            {
                // Create a StreamSocketListener to start listening for TCP connections:
                if (serverSocketListener == null) serverSocketListener = new StreamSocketListener();
                else return;

                // Hook up an event handler to call when connections are received:
                serverSocketListener.ConnectionReceived += SocketListener_ConnectionReceived;

                // Start listening for incoming TCP connections on the specified port:
                await serverSocketListener.BindServiceNameAsync(ServicePort);

                IsServerWorking = true;                
                Debug.WriteLine("Waiting for connections...");
            }
            catch (Exception e)
            {
                Debug.WriteLine(">> Exception in StartServer(): " + e.Message);
            }
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <returns></returns>
        public void StopServer()
        {
            try
            {
                // explicitly close the socketListener:
                if (serverSocketListener != null)
                {
                    serverSocketListener.Dispose();
                    serverSocketListener = null;
                    IsServerWorking = false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(">> Exception in StopServer(): " + e.Message);
            }
        }

        /// <summary>
        /// Gets MS Band devices connected to local computer.
        /// </summary>
        /// <returns></returns>
        public async Task GetMSBandDevices()
        {
            Debug.WriteLine(">> Get MS Band devices...");

            IBandClientManager clientManager = BandClientManager.Instance;
            IBandInfo[] pairedBands = await clientManager.GetBandsAsync();

            // make sure that connectedBands dictionary exists:
            if (connectedBands == null) connectedBands = new Dictionary<string, BandData>();
            else
            {
                // dispose all connected bands:
                foreach (var band in connectedBands)
                {
                    await band.Value.StopReadingSensorsData();
                    band.Value.BandClient.Dispose();
                }
                connectedBands.Clear();
            }

            // deal with all connected Band devices:
            if (pairedBands.Length > 0)
            {
                Dictionary<string, BandData> tempCB = new Dictionary<string, BandData>();

                // keep existing BandData from previously connected Band devices untouched:
                foreach (KeyValuePair<string, BandData> kvp in connectedBands)
                {
                    for (int i = 0; i < pairedBands.Length; i++)
                    {
                        if (pairedBands[i] != null && kvp.Key == pairedBands[i].Name)
                        {
                            tempCB.Add(kvp.Key, kvp.Value);
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
                            // add new Band to collection:
                            BandData bandData = new BandData(bandClient, pairedBands[i].Name, bandBufferSize, calibrationBufferSize);
                            tempCB.Add(pairedBands[i].Name, bandData);

                            await bandData.StartReadingSensorsData();
                        }
                    }
                }
                // update connectedBands dictionary:
                connectedBands.Clear();
                connectedBands = tempCB;
            }
            else
            {
                Debug.WriteLine(">> No Bands found");
            }

            // update ObservableCollection of connected Bands:
            SetupBandsListView();
        }

        /// <summary>
        /// Generates <see cref="FakeBandsAmount"/> number of Fake Bands.
        /// </summary>
        /// <returns></returns>
        public async Task GetFakeBands()
        {
            Debug.WriteLine(">> Get Fake Bands...");

            List<IBandInfo> fakeBands = new List<IBandInfo>();
            for (int i = 1; i <= FakeBandsAmount; i++)
            {
                fakeBands.Add(new FakeBandInfo(BandConnectionType.Bluetooth, "Fake Band " + i.ToString()));
            }
            FakeBandClientManager.Configure(new FakeBandClientManagerOptions { Bands = fakeBands });

            IBandClientManager clientManager = FakeBandClientManager.Instance;
            IBandInfo[] pairedBands = await clientManager.GetBandsAsync();

            // clear connectedBands dictionary:
            if (connectedBands == null) connectedBands = new Dictionary<string, BandData>();
            else
            {
                // dispose all connected bands:
                foreach (var band in connectedBands)
                {
                    await band.Value.StopReadingSensorsData();
                    band.Value.BandClient.Dispose();
                }
                connectedBands.Clear();
            }

            // connect new fake Bands:
            foreach (IBandInfo band in pairedBands)
            {
                var bandClient = await clientManager.ConnectAsync(band);
                if (bandClient != null)
                {
                    // add new Band to collection:
                    BandData bandData = new BandData(bandClient, band.Name, bandBufferSize, calibrationBufferSize);
                    connectedBands.Add(band.Name, bandData);

                    await bandData.StartReadingSensorsData();
                }
            }

            // update ObservableCollection of connected Bands:
            SetupBandsListView();
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Server behaviour on receiving a connection with remote client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void SocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                //Debug.WriteLine("Connection received from " + args.Socket.Information.RemoteAddress);
                receiveBuffer = new byte[bufferSize];

                PacketProtocol packetizer = new PacketProtocol(2048);
                packetizer.MessageArrived += receivedMessage =>
                {
                    Debug.Write(":: Received bytes: " + receivedMessage.Length + "  => ");
                    if (receivedMessage.Length > 0)
                    {
                        Debug.WriteLine("deserialize message");
                        message = Message.Deserialize(receivedMessage);
                        Debug.WriteLine("Received: " + message);
                    }
                };

                //Read data from the remote client.
                Stream inStream = args.Socket.InputStream.AsStreamForRead();
                while (!packetizer.AllBytesReceived)
                {
                    await inStream.ReadAsync(receiveBuffer, 0, bufferSize);
                    packetizer.DataReceived(receiveBuffer);
                }
                Debug.WriteLine("---------------------------------------------------------");
                Debug.WriteLine("__Received: " + message);

                // Prepare response:
                Message response = await PrepareResponseToClient(message);
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
        /// Sets up currently connected Band devices list.
        /// </summary>
        private void SetupBandsListView()
        {
            // make surte that ConnectedBandsCollection exists:
            if (ConnectedBandsCollection == null)
                ConnectedBandsCollection = new ObservableCollection<BandData>();
            else
                ConnectedBandsCollection.Clear();

            // update ObservableCollection:
            foreach (BandData bandData in connectedBands.Values)
            {
                ConnectedBandsCollection.Add(bandData);
            }
        }

        /// <summary>
        /// Prepares response to given message.
        /// </summary>
        /// <param name="message">Received message</param>
        /// <returns>Response to send</returns>
        private async Task<Message> PrepareResponseToClient(Message message)
        {
            switch (message.Code)
            {
                // send the list of all connected Bands:
                case MessageCode.SHOW_LIST_ASK:
                    if (connectedBands != null)
                        return new Message(MessageCode.SHOW_LIST_ANS, connectedBands.Keys.ToArray());
                    else
                        return new Message(MessageCode.SHOW_LIST_ANS, null);

                // send current sensors data from specific Band device:
                case MessageCode.GET_DATA_ASK:
                    if (connectedBands != null && message.Result != null && message.Result.GetType() == typeof(string))
                    {
                        if (connectedBands.ContainsKey((string)message.Result))
                        {
                            // get current sensors data and send them back to remote client:
                            SensorData hrData = new SensorData(SensorCode.HR, connectedBands[(string)message.Result].HrBuffer.GetAverage());
                            SensorData gsrData = new SensorData(SensorCode.HR, connectedBands[(string)message.Result].GsrBuffer.GetAverage());
                            return new Message(MessageCode.GET_DATA_ANS, new SensorData[] { hrData, gsrData });
                        }
                        else
                            return new Message(MessageCode.GET_DATA_ANS, null);
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                // callibrate sensors data to get control average values:
                case MessageCode.CALIB_ASK:
                    if (connectedBands != null && message.Result != null && message.Result.GetType() == typeof(string))
                    {
                        if (connectedBands.ContainsKey((string)message.Result))
                        {
                            // get current sensors data and send them back to remote client:
                            var data = await connectedBands[(string)message.Result].CalibrateSensorsData();
                            return new Message(MessageCode.CALIB_ANS, data);
                        }
                        else
                            return new Message(MessageCode.CALIB_ANS, null);
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                // wrong message code:
                default:
                    return new Message(MessageCode.CTR_MSG, null);
            }
        }
        #endregion
    }
}