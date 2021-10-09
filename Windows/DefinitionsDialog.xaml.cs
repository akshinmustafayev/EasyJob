using EasyJob.Serialization;
using System.Windows;

namespace EasyJob.Windows
{
    /// <summary>
    /// Interaction logic for DefinitionsDialog.xaml
    /// </summary>
    public partial class DefinitionsDialog : Window
    {
        #region Variables

        public string configPath = "";
        public Config config;

        string selectedFile = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DefinitionsDialog"/> class.
        /// </summary>
        public DefinitionsDialog()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods



        #endregion

        #region Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion
    }
}
