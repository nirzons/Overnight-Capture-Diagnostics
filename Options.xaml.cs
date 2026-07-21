using System.ComponentModel.Composition;
using System.Windows;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics {
    [Export(typeof(ResourceDictionary))]
    public partial class Options : ResourceDictionary {
        public Options() {
            InitializeComponent();
        }
    }
}
