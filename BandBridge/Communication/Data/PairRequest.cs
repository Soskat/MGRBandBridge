namespace Communication.Data
{
    /// <summary>
    /// Contains all data required to paired remote client and connected Band.
    /// </summary>
    class PairRequest
    {
        #region Properties
        /// <summary>
        /// Number of client's open port listening for incoming Band data.
        /// </summary>
        public int OpenPort { get; set; }

        /// <summary>
        /// Name of the choosen Band.
        /// </summary>
        public string BandName { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of <see cref="PairRequest"/>
        /// </summary>
        /// <param name="openPort">client's open port number</param>
        /// <param name="bandName">client's choosen Band name</param>
        public PairRequest(int openPort, string bandName)
        {
            OpenPort = openPort;
            BandName = bandName;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Writes PairRequest object in form: '[OpenPort | BandName]'.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[{0} | {2}]", OpenPort.ToString(), BandName);
        }
        #endregion
    }
}
