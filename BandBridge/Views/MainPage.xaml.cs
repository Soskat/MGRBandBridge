using BandBridge.ViewModels;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BandBridge.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// <see cref="Server"/> object.
        /// </summary>
        public Server ServerVM { get; set; }

        /// <summary>
        /// Server service port number written as string.
        /// </summary>
        private string serivicePort = "2055";

        public MainPage()
        {
            this.InitializeComponent();
            ServerVM = new Server(serivicePort);
        }
    }
}
