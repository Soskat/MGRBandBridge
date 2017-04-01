
using Communication.Data;
using Communication.Packet;
using Communication.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

/// <summary>
/// Class that test all variants of messages used by BandBridge app.
/// </summary>
class BBTest {

    private string hostName;
    private int servicePort;
    private string choosenBandName = "Fake Band Name";
    private string[] connectedBands = null;

    /// <summary>
    /// Creates an instance of <see cref="BBTest"/>
    /// </summary>
    /// <param name="hostName"></param>
    /// <param name="servicePort"></param>
    public BBTest(string hostName, int servicePort){
        this.hostName = hostName;
        this.servicePort = servicePort;
    }

    private void Test__SHOW_ASK__Result_null(){
        Console.WriteLine(">> SHOW_ASK / Result == null ---------------------");
        Message msg = new Message(Command.SHOW_ASK, null);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.SHOW_ANS, "Wrong response Code - expected SHOW_ANS, but get: " + resp.Code);
            Debug.Assert((resp.Result == null) || (resp.Result.GetType() == typeof(string[])),
                         "Wrong response Result - expected null or string[], but get: " + resp.Result.GetType());
            
            if(resp.Result != null && resp.Result.GetType() == typeof(string[]) && ((string[])resp.Result).Length > 0){
                connectedBands = (string[])resp.Result;
                choosenBandName = connectedBands[0];
            }
        }
    }

    private void Test__SHOW_ASK__Result_not_null(){
        Console.WriteLine(">> SHOW_ASK / Result != null ---------------------");
        Message msg = new Message(Command.SHOW_ASK, 42);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.SHOW_ANS, "Wrong response Code - expected SHOW_ANS, but get: " + resp.Code);
            Debug.Assert((resp.Result == null) || (resp.Result.GetType() == typeof(string[])),
                         "Wrong response Result - expected null or string[], but get: " + resp.Result.GetType());
            
            if(resp.Result != null && resp.Result.GetType() == typeof(string[]) && ((string[])resp.Result).Length > 0){
                connectedBands = (string[])resp.Result;
                choosenBandName = connectedBands[0];
            }
        }
    }

    private void Test__GET_DATA_ASK__Result_null(){
        Console.WriteLine(">> GET_DATA_ASK / Result == null ---------------------");
        Message msg = new Message(Command.GET_DATA_ASK, null);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.CTR_MSG, "Wrong response Code - expected CTR_MSG, but get: " + resp.Code);
            Debug.Assert(resp.Result == null, "Wrong response Result - expected null, but get: " + resp.Result.GetType());
        }
    }

    private void Test__GET_DATA_ASK__Result_not_string(){
        Console.WriteLine(">> GET_DATA_ASK / typeof(Result) != typeof(string) ----");
        Message msg = new Message(Command.GET_DATA_ASK, 42);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.CTR_MSG, "Wrong response Code - expected CTR_MSG, but get: " + resp.Code);
            Debug.Assert(resp.Result == null, "Wrong response Result - expected null, but get: " + resp.Result.GetType());
        }
    }

    private void Test__GET_DATA_ASK__Result_string(){
        Console.WriteLine(">> GET_DATA_ASK / typeof(Result) == typeof(string) ----");
        Message msg = new Message(Command.GET_DATA_ASK, choosenBandName);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.GET_DATA_ANS, "Wrong response Code - expected GET_DATA_ANS, but get: " + resp.Code);
            Debug.Assert((resp.Result == null) || (resp.Result.GetType() == typeof(SensorData[])),
                         "Wrong response Result - expected null or typeof(SensorData[]), but get: " + resp.Result.GetType());
        }
    }

    private void Test__SHOW_ANS(){
        Console.WriteLine(">> SHOW_ANS / Result == null ----------------------");
        Message msg = new Message(Command.GET_DATA_ANS, null);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.CTR_MSG, "Wrong response Code - expected CTR_MSG, but get: " + resp.Code);
            Debug.Assert(resp.Result == null, "Wrong response Result - expected null, but get: " + resp.Result.GetType());
        }
    }

    private void Test__GET_DATA_ANS(){
        Console.WriteLine(">> GET_DATA_ANS / Result == null ----------------------");
        Message msg = new Message(Command.GET_DATA_ANS, null);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.CTR_MSG, "Wrong response Code - expected CTR_MSG, but get: " + resp.Code);
            Debug.Assert(resp.Result == null, "Wrong response Result - expected null, but get: " + resp.Result.GetType());
        }
    }

    private void Test__CTR_MSG(){
        Console.WriteLine(">> CTR_MSG / Result == null ---------------------------");
        Message msg = new Message(Command.CTR_MSG, null);
        Console.WriteLine("MSG = " + msg);
        Message resp = SocketClient.StartClient(hostName, servicePort, msg);
        Debug.Assert(resp != null, "Response is null!");
        if(resp != null) {
            Debug.Assert(resp.Code == Command.CTR_MSG, "Wrong response Code - expected CTR_MSG, but get: " + resp.Code);
            Debug.Assert(resp.Result == null, "Wrong response Result - expected null, but get: " + resp.Result.GetType());
        }
    }


    /// <summary>
    /// Starts tests.
    /// </summary>
    /// <param name="args"></param>
    public static void Main(String[] args) {
        BBTest test = new BBTest("ROGwolf", 2055);
        // BBTest test = new BBTest("DESKTOP-KPBRM2V", 2055);
        Thread t;
        // Begin tests:
        t = new Thread(new ThreadStart(test.Test__SHOW_ASK__Result_null));
        t.Start();  t.Join(); Thread.Sleep(1);
        Console.WriteLine();
        t = new Thread(new ThreadStart(test.Test__SHOW_ASK__Result_not_null));
        t.Start(); t.Join(); Thread.Sleep(1);
        Console.WriteLine();
        t = new Thread(new ThreadStart(test.Test__GET_DATA_ASK__Result_null));
        t.Start(); t.Join(); Thread.Sleep(1);
        Console.WriteLine();
        t = new Thread(new ThreadStart(test.Test__GET_DATA_ASK__Result_not_string));
        t.Start(); t.Join(); Thread.Sleep(1);
        Console.WriteLine();
        t = new Thread(new ThreadStart(test.Test__GET_DATA_ASK__Result_string));
        t.Start(); t.Join(); Thread.Sleep(1);
        Console.WriteLine();
        t = new Thread(new ThreadStart(test.Test__SHOW_ANS));
        t.Start(); t.Join(); Thread.Sleep(1);
        Console.WriteLine();
        t = new Thread(new ThreadStart(test.Test__GET_DATA_ANS));
        t.Start(); t.Join(); Thread.Sleep(1);
        Console.WriteLine();
        t = new Thread(new ThreadStart(test.Test__CTR_MSG));
        t.Start(); t.Join(); Thread.Sleep(1);
        Console.WriteLine();


        Console.WriteLine("\n\nPress any key to continue...");
        Console.ReadKey();
    }
}



namespace Communication.Client
{
    // State object for receiving data from remote device.
    public class StateObject {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
    }


    public class SocketClient {
        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);
        // other stuff:
        private static PacketProtocol packetizer = null;
        private static Message receivedResponse;


        public static Message StartClient(string hostName, int port, Message message) {
            // Connect to a remote device.

            try {

                // Establish the remote endpoint for the socket
                IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
                IPAddress ipAddress = Array.Find(ipHostInfo.AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipAddress == null){
                    Console.WriteLine("Cannot find host with IP address: " + ipAddress.ToString());
                    return null;
                }
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    
                // Connect to the remote endpoint.
                // client.BeginConnect( remoteEP, new AsyncCallback(ConnectCallback), client);
                // connectDone.WaitOne();

                var result = client.BeginConnect(remoteEP, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(3000);
                if (success)
                {
                    client.EndConnect(result);
                    Console.WriteLine("Socket connected to " + client.RemoteEndPoint.ToString());
                }
                else
                {
                    Console.WriteLine("Could not connect to " + client.RemoteEndPoint.ToString());
                    client.Close();
                    return null;
                }

                packetizer = new PacketProtocol(2048);

                // Send test data to the remote device.
                Send(client, message);
                sendDone.WaitOne();
                Console.WriteLine("Sent to server: " + message);

                // Console.WriteLine("Prepare for receiving...");
                packetizer.MessageArrived += receivedMsg => 
                {
                    // Console.Write(":: received bytes: " + receivedMsg.Length + " => ");
                    if (receivedMsg.Length > 0)
                    {
                        // Console.WriteLine("deserialize message");
                        receivedResponse = Message.Deserialize(receivedMsg);
                        // Console.WriteLine("::Received: " + receivedResponse);
                    }
                    // else Console.WriteLine("keepalive message");
                };
                // Receive the response from the remote device.
                Receive(client);
                receiveDone.WaitOne();
                
                Console.WriteLine("Received from server: " + receivedResponse);

                // Release the socket.
                client.Shutdown(SocketShutdown.Both);
                client.Close();

                Console.WriteLine("Shotdown the connection");
                return receivedResponse;

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }


        // private static void ConnectCallback(IAsyncResult ar) {
        //     try {
        //         // Retrieve the socket from the state object.
        //         Socket client = (Socket) ar.AsyncState;

        //         // Complete the connection.
        //         client.EndConnect(ar);
        //         // Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

        //         // Signal that the connection has been made.
        //         connectDone.Set();

        //     } catch (Exception e) {
        //         Console.WriteLine(e.ToString());
        //     }
        // }


        private static void Send(Socket client, Message data) {
            // convert message into byte array and wrap it for network transport:
            var serializedMsg = Message.Serialize(data);
            // Console.WriteLine("serialized msg length: " + serializedMsg.Length);

            byte[] byteData = PacketProtocol.WrapMessage(serializedMsg);
            // Console.WriteLine("wrapped serialized msg length: " + byteData.Length);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
        }


        private static void SendCallback(IAsyncResult ar) {
            try {
                // Retrieve the socket from the state object.
                Socket client = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                // Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }


        private static void Receive(Socket client) {
            try {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }


        private static void ReceiveCallback( IAsyncResult ar ) {
            try {
                // Retrieve the state object and the client socket from the asynchronous state object.
                StateObject state = (StateObject) ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                // Console.WriteLine("Recv_2 - bytesRead: " + bytesRead);

                if (bytesRead > 0) {
                    packetizer.DataReceived(state.buffer);
                }
                
                receiveDone.Set();
                // Console.WriteLine("Recv is done");

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}


namespace Communication.Packet
{
    // Original source: http://blog.stephencleary.com/2009/04/sample-code-length-prefix-message.html
     /// <summary>
     /// Maintains the necessary buffers for applying a length-prefix message framing protocol over a stream.
     /// </summary>
     /// <remarks>
     /// <para>Create one instance of this class for each incoming stream, and assign a handler to <see cref="MessageArrived"/>. As bytes arrive at the stream, pass them to <see cref="DataReceived"/>, which will invoke <see cref="MessageArrived"/> as necessary.</para>
     /// <para>If <see cref="DataReceived"/> raises <see cref="System.Net.ProtocolViolationException"/>, then the stream data should be considered invalid. After that point, no methods should be called on that <see cref="PacketProtocol"/> instance.</para>
     /// <para>This class uses a 4-byte signed integer length prefix, which allows for message sizes up to 2 GB. Keepalive messages are supported as messages with a length prefix of 0 and no message data.</para>
     /// <para>This is EXAMPLE CODE! It is not particularly efficient; in particular, if this class is rewritten so that a particular interface is used (e.g., Socket's IAsyncResult methods), some buffer copies become unnecessary and may be removed.</para>
     /// </remarks>
    public class PacketProtocol
    {
        /// <summary>
        /// Wraps a message. The wrapped message is ready to send to a stream.
        /// </summary>
        /// <remarks>
        /// <para>Generates a length prefix for the message and returns the combined length prefix and message.</para>
        /// </remarks>
        /// <param name="message">The message to send.</param>
        public static byte[] WrapMessage(byte[] message)
        {
            // Get the length prefix for the message
            byte[] lengthPrefix = BitConverter.GetBytes(message.Length);

            // Concatenate the length prefix and the message
            byte[] ret = new byte[lengthPrefix.Length + message.Length];
            lengthPrefix.CopyTo(ret, 0);
            message.CopyTo(ret, lengthPrefix.Length);

            return ret;
        }

        /// <summary>
        /// Wraps a keepalive (0-length) message. The wrapped message is ready to send to a stream.
        /// </summary>
        public static byte[] WrapKeepaliveMessage()
        {
            return BitConverter.GetBytes((int)0);
        }

        /// <summary>
        /// Initializes a new <see cref="PacketProtocol"/>, limiting message sizes to the given maximum size.
        /// </summary>
        /// <param name="maxMessageSize">The maximum message size supported by this protocol. This may be less than or equal to zero to indicate no maximum message size.</param>
        public PacketProtocol(int maxMessageSize)
        {
            // We allocate the buffer for receiving message lengths immediately
            this.lengthBuffer = new byte[sizeof(int)];
            this.maxMessageSize = maxMessageSize;
        }

        /// <summary>
        /// The buffer for the length prefix; this is always 4 bytes long.
        /// </summary>
        private byte[] lengthBuffer;

        /// <summary>
        /// The buffer for the data; this is null if we are receiving the length prefix buffer.
        /// </summary>
        private byte[] dataBuffer;

        /// <summary>
        /// The number of bytes already read into the buffer (the length buffer if <see cref="dataBuffer"/> is null, otherwise the data buffer).
        /// </summary>
        private int bytesReceived;

        /// <summary>
        /// The maximum size of messages allowed.
        /// </summary>
        private int maxMessageSize;

        /// <summary>
        /// Indicates the completion of a message read from the stream.
        /// </summary>
        /// <remarks>
        /// <para>This may be called with an empty message, indicating that the other end had sent a keepalive message. This will never be called with a null message.</para>
        /// <para>This event is invoked from within a call to <see cref="DataReceived"/>. Handlers for this event should not call <see cref="DataReceived"/>.</para>
        /// </remarks>
        public Action<byte[]> MessageArrived { get; set; }

        /// <summary>
        /// Notifies the <see cref="PacketProtocol"/> instance that incoming data has been received from the stream. This method will invoke <see cref="MessageArrived"/> as necessary.
        /// </summary>
        /// <remarks>
        /// <para>This method may invoke <see cref="MessageArrived"/> zero or more times.</para>
        /// <para>Zero-length receives are ignored. Many streams use a 0-length read to indicate the end of a stream, but <see cref="PacketProtocol"/> takes no action in this case.</para>
        /// </remarks>
        /// <param name="data">The data received from the stream. Cannot be null.</param>
        /// <exception cref="System.Net.ProtocolViolationException">If the data received is not a properly-formed message.</exception>
        public void DataReceived(byte[] data)
        {
            // Process the incoming data in chunks, as the ReadCompleted requests it

            // Logically, we are satisfying read requests with the received data, instead of processing the
            //  incoming buffer looking for messages.

            int i = 0;
            while (i != data.Length)
            {
                // Determine how many bytes we want to transfer to the buffer and transfer them
                int bytesAvailable = data.Length - i;
                if (this.dataBuffer != null)
                {
                    // We're reading into the data buffer
                    int bytesRequested = this.dataBuffer.Length - this.bytesReceived;

                    // Copy the incoming bytes into the buffer
                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, this.dataBuffer, this.bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    // Notify "read completion"
                    this.ReadCompleted(bytesTransferred);
                }
                else
                {
                    // We're reading into the length prefix buffer
                    int bytesRequested = this.lengthBuffer.Length - this.bytesReceived;

                    // Copy the incoming bytes into the buffer
                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, this.lengthBuffer, this.bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    // Notify "read completion"
                    this.ReadCompleted(bytesTransferred);
                }
            }
        }

        /// <summary>
        /// Called when a read completes. Parses the received data and calls <see cref="MessageArrived"/> if necessary.
        /// </summary>
        /// <param name="count">The number of bytes read.</param>
        /// <exception cref="System.Net.ProtocolViolationException">If the data received is not a properly-formed message.</exception>
        private void ReadCompleted(int count)
        {
            // Get the number of bytes read into the buffer
            this.bytesReceived += count;

            if (this.dataBuffer == null)
            {
                // We're currently receiving the length buffer

                if (this.bytesReceived != sizeof(int))
                {
                    // We haven't gotten all the length buffer yet: just wait for more data to arrive
                }
                else
                {
                    // We've gotten the length buffer
                    int length = BitConverter.ToInt32(this.lengthBuffer, 0);

                    // Sanity check for length < 0
                    if (length < 0)
                        throw new System.Net.ProtocolViolationException("Message length is less than zero");

                    // Another sanity check is needed here for very large packets, to prevent denial-of-service attacks
                    if (this.maxMessageSize > 0 && length > this.maxMessageSize)
                        throw new System.Net.ProtocolViolationException("Message length " + length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " is larger than maximum message size " + this.maxMessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    // Zero-length packets are allowed as keepalives
                    if (length == 0)
                    {
                        this.bytesReceived = 0;
                        // if (this.MessageArrived != null)
                        //     this.MessageArrived(new byte[0]);
                    }
                    else
                    {
                        // Create the data buffer and start reading into it
                        this.dataBuffer = new byte[length];
                        this.bytesReceived = 0;
                    }
                }
            }
            else
            {
                if (this.bytesReceived != this.dataBuffer.Length)
                {
                    // We haven't gotten all the data buffer yet: just wait for more data to arrive
                }
                else
                {
                    // We've gotten an entire packet
                    if (this.MessageArrived != null)
                        this.MessageArrived(this.dataBuffer);

                    // Start reading the length buffer again
                    this.dataBuffer = null;
                    this.bytesReceived = 0;
                }
            }
        }
    }
}







namespace Communication.Data
{
    /// <summary>
    /// Message commands type codes.
    /// </summary>
    public enum Command : byte { CTR_MSG, SHOW_ASK, SHOW_ANS, GET_DATA_ASK, GET_DATA_ANS }

    /// <summary>
    /// Class that represents a message structure used in communication with BandBridge server.
    /// </summary>
    [DataContract]
    public class Message
    {
        #region Properties
        /// <summary>
        /// Message command type code.
        /// </summary>
        [DataMember]
        public Command Code { get; set; }
        
        /// <summary>
        /// Message result object.
        /// </summary>
        [DataMember]
        public object Result { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="Message"/>.
        /// </summary>
        /// <param name="code">Message command type code</param>
        /// <param name="result">Message result object</param>
        public Message(Command code, object result)
        {
            Code = code;
            Result = result;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Serializes <see cref="Message"/> object to a byte array.
        /// </summary>
        /// <param name="message">Message to serialize</param>
        /// <returns>Serialized message</returns>
        public static byte[] Serialize(Message message)
        {
            byte[] data = null;
            using (MemoryStream stream = new MemoryStream())
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Message), new Type[] { typeof(SensorData) });
                serializer.WriteObject(writer, message);
                writer.Flush();
                data = stream.ToArray();
            }
            return data;
        }

        /// <summary>
        /// Deserializes byte array as <see cref="Message"/> object.
        /// </summary>
        /// <param name="data">Array of bytes</param>
        /// <returns>Deserialized message</returns>
        public static Message Deserialize(byte[] data)
        {
            Message response = null;
            using (MemoryStream stream = new MemoryStream(data))
            using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                DataContractSerializer deserializer = new DataContractSerializer(typeof(Message), new Type[] { typeof(SensorData) });
                response = (Message)deserializer.ReadObject(reader);
            }
            return response;
        }

        /// <summary>
        /// Writes Message object in form: 'Message: [Code][Result] -> Result.ToString()'.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Result != null)
                return string.Format("Message: [{0}][{1}] -> {2}", Code, Result, Result.ToString());
            else
                return string.Format("Message: [{0}][{1}]", Code, Result);
        }
        #endregion
    }


    /// <summary>
    /// Microsoft Band sensor type code.
    /// </summary>
    public enum SensorCode : byte { HR, GSR }

    /// <summary>
    /// Class that encapsulates specific value of Microsoft Band sensor reading.
    /// </summary>
    [DataContract]
    public class SensorData
    {
        #region Properties
        /// <summary>
        /// Microsoft Band sensor type code.
        /// </summary>
        [DataMember]
        public SensorCode Code { get; set; }

        /// <summary>
        /// Value of sensor reading.
        /// </summary>
        [DataMember]
        public int Data { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="SensorData"/>.
        /// </summary>
        /// <param name="code">Microsoft Band sensor type code</param>
        /// <param name="data">Value of sensor reading</param>
        public SensorData(SensorCode code, int data)
        {
            Code = code;
            Data = data;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Writes SensorData object in form: '[Code][Data]'.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[{0}][{1}]", Code, Data);
        }
        #endregion
    }
}
