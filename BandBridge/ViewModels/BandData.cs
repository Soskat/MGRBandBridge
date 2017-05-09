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
        /// <summary>
        /// Connected MS Band device.
        /// </summary>
        private IBandClient bandClient;

        /// <summary>
        /// MS Band device name.
        /// </summary>
        private string name;

        /// <summary>
        /// Last Heart Rate sensor reading.
        /// </summary>
        private int hrReading;

        /// <summary>
        /// Last GSR sensor reading.
        /// </summary>
        private int gsrReading;

        /// <summary>
        /// Storage for Heart Rate sensor values.
        /// </summary>
        private CircularBuffer hrBuffer;

        /// <summary>
        /// Storage for GSR sensor values.
        /// </summary>
        private CircularBuffer gsrBuffer;
        #endregion

        #region Properties
        /// <summary>
        /// Connected MS Band device.
        /// </summary>
        public IBandClient BandClient
        {
            get { return bandClient; }
            set { SetProperty(bandClient, value, () => bandClient = value); }
        }

        /// <summary>
        /// MS Band device name.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(name, value, () => name = value); }
        }

        /// <summary>
        /// Last Heart Rate sensor reading.
        /// </summary>
        public int HrReading
        {
            get { return hrReading; }
            set { SetProperty(hrReading, value, () => hrReading = value); }
        }

        /// <summary>
        /// Last GSR sensor reading.
        /// </summary>
        public int GsrReading
        {
            get { return gsrReading; }
            set { SetProperty(gsrReading, value, () => gsrReading = value); }
        }

        /// <summary>
        /// Storage for Heart Rate sensor values.
        /// </summary>
        public CircularBuffer HrBuffer
        {
            get { return hrBuffer; }
            set { SetProperty(hrBuffer, value, () => hrBuffer = value); }
        }

        /// <summary>
        /// Storage for GSR sensor values.
        /// </summary>
        public CircularBuffer GsrBuffer
        {
            get { return gsrBuffer; }
            set { SetProperty(gsrBuffer, value, () => gsrBuffer = value); }
        }
        #endregion
        
        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="BandData"/>.
        /// </summary>
        /// <param name="bandClient"><see cref="IBandClient"/> object connected with Band device</param>
        /// <param name="bandName">Band device name</param>
        /// <param name="storageSize">HR and GSR storage size</param>
        public BandData(IBandClient bandClient, string bandName, int storageSize)
        {
            BandClient = bandClient;
            Name = bandName;
            HrBuffer = new CircularBuffer(storageSize);
            GsrBuffer = new CircularBuffer(storageSize);
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Gets Hear Rate sensor values from connected Band device.
        /// </summary>
        /// <returns></returns>
        private async Task StartHrReading()
        {
            // check current user heart rate consent; if user hasn’t consented, request consent:
            if (BandClient.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await BandClient.SensorManager.HeartRate.RequestUserConsentAsync();
            }
            // hook up to the Heartrate sensor ReadingChanged event
            BandClient.SensorManager.HeartRate.ReadingChanged += async (sender, args) =>
            {
                // update app GUI info:
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     HrReading = args.SensorReading.HeartRate;
                     HrBuffer.Add(args.SensorReading.HeartRate);
                 });
            };
            // start the Heartrate sensor:
            try
            {
                await BandClient.SensorManager.HeartRate.StartReadingsAsync();
                Debug.WriteLine(name + ": Started HR reading");
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
        private async Task StartGsrReading()
        {
            // check current user gsr consent; if user hasn’t consented, request consent:
            if (BandClient.SensorManager.Gsr.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await BandClient.SensorManager.Gsr.RequestUserConsentAsync();
            }
            // hook up to the Gsr sensor ReadingChanged event:
            BandClient.SensorManager.Gsr.ReadingChanged += async (sender, args) =>
            {
                // update app GUI info:
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    GsrReading = args.SensorReading.Resistance;
                    GsrBuffer.Add(args.SensorReading.Resistance);
                });
            };
            // start the GSR sensor:
            try
            {
                await BandClient.SensorManager.Gsr.StartReadingsAsync();
                Debug.WriteLine(name + ": Started GSR reading");
            }
            catch (BandException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stops reading from connected Band's Heart Rate sensor.
        /// </summary>
        /// <returns></returns>
        private async Task StopHrReading()
        {
            // stop the HR sensor:
            try
            {
                await BandClient.SensorManager.HeartRate.StopReadingsAsync();
                Debug.WriteLine(name + ": Stopped HR reading");
            }
            catch (BandException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stops reading from connected Band's Galvanic Skin Response sensor.
        /// </summary>
        /// <returns></returns>
        private async Task StopGsrReading()
        {
            // stop the GSR sensor:
            try
            {
                await BandClient.SensorManager.Gsr.StopReadingsAsync();
                Debug.WriteLine(name + ": Stopped GSR reading");
            }
            catch (BandException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Starts reading data from connected Band's sensors.
        /// </summary>
        /// <returns></returns>
        public async Task StartReadingSensorsData()
        {
            await StartHrReading();
            await StartGsrReading();
            Debug.WriteLine("__Started reading data__");
        }

        /// <summary>
        /// Stops reading data from connected Band's sensors.
        /// </summary>
        /// <returns></returns>
        public async Task StopReadingSensorsData()
        {
            await StopHrReading();
            await StopGsrReading();
            Debug.WriteLine("__Stopped reading data__");
        }
        #endregion
    }
}
