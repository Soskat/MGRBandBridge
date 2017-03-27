using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

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
            return string.Format("Message: [{0}][{1}] -> {2}", Code, Result, Result.ToString());
        }
        #endregion
    }

}
