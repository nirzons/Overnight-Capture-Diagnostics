using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics {

    [Export(typeof(IPluginManifest))]
    public class OvernightCaptureDiagnostics : PluginBase, INotifyPropertyChanged {

        [ImportingConstructor]
        public OvernightCaptureDiagnostics(IProfileService profileService) {
        }

        public override Task Teardown() {
            return base.Teardown();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
