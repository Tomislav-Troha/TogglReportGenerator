using System.Windows;
using System.Windows.Controls;
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
    }
}