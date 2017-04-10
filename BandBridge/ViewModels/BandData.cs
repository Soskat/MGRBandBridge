using BandBridge.Data;
using Communication.Data;
using Microsoft.Band;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace BandBridge.ViewModels
{
    /// <summary>
    /// Represents MS Band data used in Unity game project.
    /// </summary>
    public class BandData : NotificationBase
    {
        #region Fields
        /// <summary>C
        /// onnected MS Band device.
        /// </summary>
        private IBandClient _BandClient;

        /// <summary>
        /// MS Band device name.
        /// </summary>
        private string _Name;

        /// <summary>
        /// Last Heart Rate sensor reading.
        /// </summary>
        private int _HrReading;

        /// <summary>
        /// Last GSR sensor reading.
        /// </summary>
        private int _GsrReading;

        /// <summary>
        /// Storage for Heart Rate sensor values.
        /// </summary>
        //private CircularBuffer _HrBuffer;

        /// <summary>
        /// Storage for GSR sensor values.
        /// </summary>
        //private CircularBuffer _GsrBuffer;
        #endregion

        #region Properties
        /// <summary>
        /// Connected MS Band device.
        /// </summary>
        public IBandClient BandClient
        {
            get { return _BandClient; }
            set { SetProperty(_BandClient, value, () => _BandClient = value); }
        }

        /// <summary>
        /// MS Band device name.
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(_Name, value, () => _Name = value); }
        }

        /// <summary>
        /// Last Heart Rate sensor reading.
        /// </summary>
        public int HrReading
        {
            get { return _HrReading; }
            set { SetProperty(_HrReading, value, () => _HrReading = value); }
        }

        /// <summary>
        /// Last GSR sensor reading.
        /// </summary>
        public int GsrReading
        {
            get { return _GsrReading; }
            set { SetProperty(_GsrReading, value, () => _GsrReading = value); }
        }

        ///// <summary>
        ///// Storage for Heart Rate sensor values.
        ///// </summary>
        //public CircularBuffer HrBuffer
        //{
        //    get { return _HrBuffer; }
        //    set { SetProperty(_HrBuffer, value, () => _HrBuffer = value); }
        //}

        ///// <summary>
        ///// Storage for GSR sensor values.
        ///// </summary>
        //public CircularBuffer GsrBuffer
        //{
        //    get { return _GsrBuffer; }
        //    set { SetProperty(_GsrBuffer, value, () => _GsrBuffer = value); }
        //}
        #endregion

        #region Delegates
        /// <summary>
        /// Indicates that new sensor reading has arrived.
        /// </summary>
        /// <remarks>
        /// <para>This event is invoked from within a call to <see cref="GetHeartRate"/> or <see cref="GetGsr"/>. Handlers for this event should not call in those methods.</para>
        /// </remarks>
        public Action<SensorData> NewSensorData { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="BandData"/>.
        /// </summary>
        /// <param name="bandClient"><see cref="IBandClient"/> object connected with Band device</param>
        /// <param name="bandName">Band device name</param>
        public BandData(IBandClient bandClient, string bandName)
        {
            BandClient = bandClient;
            Name = bandName;
            //HrBuffer = new CircularBuffer(storageSize);
            //GsrBuffer = new CircularBuffer(storageSize);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets Hear Rate sensor values from connected Band device.
        /// </summary>
        /// <returns></returns>
        public async Task GetHeartRate()
        {
            // check current user heart rate consent; if user hasn’t consented, request consent:
            if (BandClient.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await BandClient.SensorManager.HeartRate.RequestUserConsentAsync();
            }
            // hook up to the Heartrate sensor ReadingChanged event
            BandClient.SensorManager.HeartRate.ReadingChanged += async (sender, args) =>
            {
                // we've gotten new reading from sensor:
                //if (NewSensorData != null)
                //    NewSensorData(new SensorData(SensorCode.HR, args.SensorReading.HeartRate));   // ----------------------------------------------

                // update app GUI info:
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     HrReading = args.SensorReading.HeartRate;
                     //HrBuffer.Add(args.SensorReading.HeartRate);
                 });
            };
            // start the Heartrate sensor:
            try
            {
                await BandClient.SensorManager.HeartRate.StartReadingsAsync();
            }
            catch (BandException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Gets Galvenic Skin Response sensor values from connected Band device.
        /// </summary>
        /// <returns></returns>
        public async Task GetGsr()
        {
            // check current user gsr consent; if user hasn’t consented, request consent:
            if (BandClient.SensorManager.Gsr.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await BandClient.SensorManager.Gsr.RequestUserConsentAsync();
            }
            // hook up to the Gsr sensor ReadingChanged event:
            BandClient.SensorManager.Gsr.ReadingChanged += async (sender, args) =>
            {
                // we've gotten new reading from sensor:
                //if (NewSensorData != null)
                //    NewSensorData(new SensorData(SensorCode.GSR, args.SensorReading.Resistance)); // ------------------------------------------

                // update app GUI info:
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    GsrReading = args.SensorReading.Resistance;
                    //GsrBuffer.Add(args.SensorReading.Resistance);
                });
            };
            // start the Gsr sensor:
            try
            {
                await BandClient.SensorManager.Gsr.StartReadingsAsync();
            }
            catch (BandException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion
    }
}
