﻿using BandBridge.Data;
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
        private HostName _LocalHost;

        /// <summary>
        /// Service port number written in string.
        /// </summary>
        private string _ServicePort;

        /// <summary>
        /// Server <see cref="StreamSocketListener"/> object.
        /// </summary>
        private StreamSocketListener _ServerSocketListener;

        ///// <summary>
        ///// BandData buffers storage size.
        ///// </summary>
        //private int _StorageSize;

        /// <summary>
        /// Fake Bands amount.
        /// </summary>
        private int _FakeBandsAmount;
        
        /// <summary>
        /// Dictionary of connected Band devices.
        /// </summary>
        private Dictionary<string, BandData> _ConnectedBands;

        /// <summary>
        /// List of connected Bands data.
        /// </summary>
        private ObservableCollection<BandData> _ConnectedBandsCollection;

        /// <summary>
        /// Dictionary of client-Band pairs.
        /// </summary>
        private Dictionary<string, ClientInfo> clientBandPairs;

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
            FakeBandsAmount = 6;
            IsServerWorking = false;
            // get host's IPv4 address:
            LocalHost = Array.Find(NetworkInformation.GetHostNames().ToArray(),
                                   a => a.IPInformation != null && a.Type == HostNameType.Ipv4);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Set ups and starts the server.
        /// </summary>
        /// <returns>Asynchronous operation</returns>
        public async Task StartServer()
        {
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
        /// <returns>Asynchronous operation</returns>
        public void StopServer()
        {
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
                Debug.WriteLine(">> Server closed...");
            }
            catch (Exception e)
            {
                Debug.WriteLine(">> Exception in StopServer(): " + e.Message);
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
                        Debug.WriteLine("Received: " + message);
                    }
                    //else Debug.WriteLine("keepalive message");
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
        /// Sends a message to specified remote host.
        /// </summary>
        /// <param name="clientInfo">Remote host's info</param>
        /// <param name="message">Message to send</param>
        private async void SendDataToPairedClient(string sender, ClientInfo clientInfo, Message message)
        {
            try
            {
                // make sure if clientInfo exists:
                if (clientInfo == null)
                    return;

                // Create the StreamSocket and establish a connection to remote server:
                StreamSocket socket = new StreamSocket();

                // Prepare data for sending:
                byte[] byteData = PacketProtocol.WrapMessage(Message.Serialize(message));

                // Connect to remote host:
                await socket.ConnectAsync(new HostName(clientInfo.ClientAddress), clientInfo.Port.ToString());

                // Write data to the remote server.
                Stream outStream = socket.OutputStream.AsStreamForWrite();
                await outStream.WriteAsync(byteData, 0, byteData.Length);
                await outStream.FlushAsync();

                Debug.WriteLine(string.Format("Send {0} to {1}:{2}", message, clientInfo.ClientAddress, clientInfo.Port));
            }
            catch (Exception ex)
            {
                // lost connection to remote host:
                Debug.WriteLine(ex.Message);
                //Debug.WriteLine(ex.ToString());
                Debug.WriteLine("BukaBEFORE: " + sender + ":" + clientBandPairs[sender]);
                lock (clientBandPairs)
                {
                    clientBandPairs[sender] = null;
                }
                Debug.WriteLine("BukaAFTER: " + sender + ":" + clientBandPairs[sender]);
                await _ConnectedBands[sender].StopReadingSensorsData();
            }
        }



        /// <summary>
        /// Gets MS Band devices connected to local computer.
        /// </summary>
        public async Task GetMSBandDevices()
        {
            Debug.WriteLine(">> Get MS Band devices...");

            IBandClientManager clientManager = BandClientManager.Instance;
            IBandInfo[] pairedBands = await clientManager.GetBandsAsync();

            // make sure that _ConnectedBands dictionary exists:
            if (_ConnectedBands == null) _ConnectedBands = new Dictionary<string, BandData>();
            else
            {
                // dispose all connected bands:
                foreach (var band in _ConnectedBands)
                {
                    await band.Value.StopReadingSensorsData();
                    band.Value.BandClient.Dispose();
                }
                _ConnectedBands.Clear();
            }

            // deal with all connected Band devices:
            if (pairedBands.Length > 0)
            {
                Dictionary<string, BandData> tempCB = new Dictionary<string, BandData>();

                // keep existing BandData from previously connected Band devices untouched:
                foreach (KeyValuePair<string, BandData> kvp in _ConnectedBands)
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
                            BandData bandData = new BandData(bandClient, pairedBands[i].Name);
                            tempCB.Add(pairedBands[i].Name, bandData);


                            //// starts reading sensor data:
                            //await bandData.StartReadingSensorsData();
                        }
                    }
                }
                // update _ConnectedBands dictionary:
                _ConnectedBands.Clear();
                _ConnectedBands = tempCB;
            }
            else
            {
                Debug.WriteLine(">> No Bands found");
            }

            // update ObservableCollection of connected Bands:
            SetupBandsListView();
            // update client-Bands pairs list:
            UpdateClientBandPairsList();
        }

        /// <summary>
        /// Generates <see cref="FakeBandsAmount"/> number of Fake Bands.
        /// </summary>
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

            // clear _ConnectedBands dictionary:
            if (_ConnectedBands == null) _ConnectedBands = new Dictionary<string, BandData>();
            else
            {
                // dispose all connected bands:
                foreach (var band in _ConnectedBands)
                {
                    await band.Value.StopReadingSensorsData();
                    band.Value.BandClient.Dispose();
                }
                _ConnectedBands.Clear();
            }

            // connect new fake Bands:
            foreach (IBandInfo band in pairedBands)
            {
                var bandClient = await clientManager.ConnectAsync(band);
                if (bandClient != null)
                {
                    // add new Band to collection:
                    BandData bandData = new BandData(bandClient, band.Name);
                    _ConnectedBands.Add(band.Name, bandData);


                    //// starts reading sensor data:
                    //await bandData.StartReadingSensorsData();
                }
            }

            // update ObservableCollection of connected Bands:
            SetupBandsListView();
            // update client-Bands pairs list:
            UpdateClientBandPairsList();

            // TEST------ TEST------ TEST
            //Test();
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
            foreach (BandData bandData in _ConnectedBands.Values)
            {
                ConnectedBandsCollection.Add(bandData);
            }
        }

        /// <summary>
        /// Updates dictionary that contains client-Band pairs.
        /// </summary>
        private void UpdateClientBandPairsList()
        {
            // make sure that clientBandPairs dictionary exists:
            if (clientBandPairs == null)
                clientBandPairs = new Dictionary<string, ClientInfo>();

            Dictionary<string, ClientInfo> temp = new Dictionary<string, ClientInfo>();

            // check if there are already some Bands which are paired with some remote clients:
            foreach (var kvp in _ConnectedBands)
            {
                if (clientBandPairs.ContainsKey(kvp.Key))
                {
                    temp.Add(kvp.Key, clientBandPairs[kvp.Key]);
                }
                else
                {
                    temp.Add(kvp.Key, null);
                    // set up actions on new reading arrived:
                    kvp.Value.NewSensorData += newReading =>
                    {
                        Message msg = new Message(MessageCode.BAND_DATA, newReading);
                        Debug.WriteLine(kvp.Key + " : " + temp[kvp.Key] + newReading);
                        SendDataToPairedClient(kvp.Key, temp[kvp.Key], msg);
                    };
                }
            }

            // update client-Bands pairs list:
            clientBandPairs = temp;
        }
        

        private async Task<Message> PrepareResponseToClient(Message message)
        {
            switch (message.Code)
            {
                // send the list of all connected Bands:
                case MessageCode.SHOW_LIST_ASK:
                    if (_ConnectedBands != null)
                        return new Message(MessageCode.SHOW_LIST_ANS, _ConnectedBands.Keys.ToArray());
                    else
                        return new Message(MessageCode.SHOW_LIST_ANS, null);

                // pair with specified Band request from connected client:
                case MessageCode.PAIR_BAND_ASK:
                    if (message.Result != null && message.Result.GetType() == typeof(PairRequest))
                    {
                        if (clientBandPairs.ContainsKey(((PairRequest)message.Result).BandName))
                        {
                            try
                            {
                                // update pair info about remote host:
                                ClientInfo clientInfo = new ClientInfo(
                                                                       ((PairRequest)message.Result).ClientAddress,
                                                                       ((PairRequest)message.Result).OpenPort
                                                                      );
                                clientBandPairs[((PairRequest)message.Result).BandName] = clientInfo;

                                // start reading from sensors:
                                await _ConnectedBands[((PairRequest)message.Result).BandName].StartReadingSensorsData();

                                return new Message(MessageCode.PAIR_BAND_ANS, true);

                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                return new Message(MessageCode.CTR_MSG, null);
                            }
                        }
                        return new Message(MessageCode.PAIR_BAND_ANS, false);
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                // free paired Band request from connected client:
                case MessageCode.FREE_BAND_ASK:
                    if (message.Result != null && message.Result.GetType() == typeof(string))
                    {
                        try
                        {
                            if (clientBandPairs.ContainsKey((string)message.Result))
                            {
                                // reset pair info about remote host:
                                clientBandPairs[(string)message.Result] = null;

                                // stop reading from sensors:
                                await _ConnectedBands[(string)message.Result].StopReadingSensorsData();
                                Debug.WriteLine((string)message.Result + ": stopped reading sensors");
                            }
                            return new Message(MessageCode.FREE_BAND_ANS, null);

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            return new Message(MessageCode.CTR_MSG, null);
                        }
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                // wrong message code:
                default:
                    return new Message(MessageCode.CTR_MSG, null);
            }
        }

        #endregion



        private void Test()
        {
            if (clientBandPairs != null && clientBandPairs.Count > 0)
            {
                clientBandPairs["Fake Band 1"] = new ClientInfo("ROGwolf", 2066);
                _ConnectedBands["Fake Band 1"].StartReadingSensorsData();
            }
        }
    }
}