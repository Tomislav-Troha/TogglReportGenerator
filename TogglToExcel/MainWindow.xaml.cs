using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using TogglToExcel.ViewModel;

namespace TogglToExcel
{
    public partial class MainWindow : Window
    {
        readonly MainWindowViewModel vm;
        public MainWindow()
        {
            InitializeComponent();
            vm = new MainWindowViewModel();
            DataContext = vm;

            pwdApiToken.Password = vm.ApiToken;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.IsApiVisible) && !vm.IsApiVisible)
                {
                    pwdApiToken.Password = vm.ApiToken;
                }
            };
        }

        private void pwdApiToken_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
                vm.ApiToken = ((PasswordBox)sender).Password;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hl && hl.Inlines.FirstInline is Run run)
            {
                var uri = run.Text;
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
        }
    }
}