namespace Communication.Data
{
    /// <summary>
    /// Message commands type codes.
    /// </summary>
    public enum MessageCode : byte
    {
        SHOW_LIST_ASK, SHOW_LIST_ANS,   // for synchronizing connected Bands list
        GET_DATA_ASK, GET_DATA_ANS,     // request for current sensors data from specific Band device
        CTR_MSG,                        // control message
        //PAIR_BAND_ASK, PAIR_BAND_ANS,   // for pairing remote client with specified Band
        //BAND_DATA,                      // for sending Band data package
        //FREE_BAND_ASK, FREE_BAND_ANS    // for freeing paired Band
    }
}
