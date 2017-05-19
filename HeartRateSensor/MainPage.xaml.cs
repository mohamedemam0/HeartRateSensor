using System;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace HeartRateSensor
{
    public sealed partial class MainPage : Page
    {
        //Controller Name of the pi
        private const string SPI_CONTROLLER_NAME = "SPI0";
        //CS 0 of the raspberry pi:
        private const Int32 SPI_CHIP_SELECT_LINE = 0;
        private static SpiDevice SpiADC;
        //Channel 0 = 10000000 = 0x80: from the data sheet
        private static readonly byte[] MCP3008_CONFIG = { 0x01, 0x80 };
        private int adcValue;

        public static DispatcherTimer timer;

        public MainPage()
        {
            this.InitializeComponent();

            InitSPI();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            ReadADC();
        }
        
        private async void InitSPI()
        {
            try
            {
                // CS 0 -> Pin 24 in RPi
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                /* 1MHz clock rate */
                settings.ClockFrequency = 1000000;
                /* The ADC expects idle-low clock polarity so we use Mode0  */
                settings.Mode = SpiMode.Mode0;
                /* Controller Name in the Pi SPI0 or SPI1  */
                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                SpiADC = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void ReadADC()
        {
            /* Buffer to hold read data*/
            byte[] readBuffer = new byte[3];
            byte[] writeBuffer = new byte[3] { 0x00, 0x00, 0x00 };
            /* Setting up ADC Configuration: from the data sheet */
            writeBuffer[0] = MCP3008_CONFIG[0];
            writeBuffer[1] = MCP3008_CONFIG[1];
            /* Read data from the ADC */
            SpiADC.TransferFullDuplex(writeBuffer, readBuffer);
            /* Convert the returned bytes into a double value */
            adcValue = ConvertPulseToInt(readBuffer);
            /* Display it on the screen */
            PulseValueTxt.Text = adcValue.ToString();
        }

        public static int ConvertPulseToInt(byte[] data)
        {
            int result = 0;
            try
            {
                result = data[1] & 0x03;
                /* left-shift assignment. Shift the value of result left by 8 */
                result <<= 8;
                /* Add the 3rd byte of data to our result */
                result += data[2];
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return 0;
            }
        }
        

    }
}
