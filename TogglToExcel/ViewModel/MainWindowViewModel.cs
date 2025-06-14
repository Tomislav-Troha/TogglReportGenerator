using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using ClosedXML.Excel;
using Newtonsoft.Json;
using TogglToExcel.Commands;
using static TogglToExcel.Model.TogglEntry;

namespace TogglToExcel.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string? lastSavedPath;

        #region Constructor
        public MainWindowViewModel()
        {
            LoadUserSettings();
            InitDates();

            ExportCommand = new RelayCommand(async _ => await ExportAsync(), _ => !IsProcessing);
            ToggleApiCommand = new RelayCommand(_ => ToggleApiVisibility());
            OpenFolderCommand = new RelayCommand(_ => OpenFolder(), _ => File.Exists(lastSavedPath ?? string.Empty));

            StatusText = "Ready";
            StatusBrush = Brushes.Black;
        }
        #endregion

        #region Properties
        private bool _isApiVisible;
        public bool IsApiVisible
        {
            get => _isApiVisible;
            set { if (_isApiVisible != value) { _isApiVisible = value; OnPropertyChanged(nameof(IsApiVisible)); } }
        }

        private string _apiToken = string.Empty;
        public string ApiToken
        {
            get => _apiToken;
            set { if (_apiToken != value) { _apiToken = value; OnPropertyChanged(nameof(ApiToken)); } }
        }

        private string _workspaceId = string.Empty;
        public string WorkspaceId
        {
            get => _workspaceId;
            set { if (_workspaceId != value) { _workspaceId = value; OnPropertyChanged(nameof(WorkspaceId)); } }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { if (_email != value) { _email = value; OnPropertyChanged(nameof(Email)); } }
        }

        private DateTime _since;
        public DateTime Since
        {
            get => _since;
            set { if (_since != value) { _since = value; OnPropertyChanged(nameof(Since)); } }
        }

        private DateTime _until;
        public DateTime Until
        {
            get => _until;
            set { if (_until != value) { _until = value; OnPropertyChanged(nameof(Until)); } }
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set { if (_statusText != value) { _statusText = value; OnPropertyChanged(nameof(StatusText)); } }
        }

        private Brush _statusBrush = Brushes.Black;
        public Brush StatusBrush
        {
            get => _statusBrush;
            set { if (_statusBrush != value) { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); } }
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged(nameof(IsProcessing));
                    ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
                }
            }
        }
        #endregion

        #region Commands
        public ICommand ExportCommand { get; }
        public ICommand ToggleApiCommand { get; }
        public ICommand OpenFolderCommand { get; }
        #endregion

        #region Private Methods
        private void SaveUserSettings()
        {
            Properties.Settings.Default.ApiToken = ApiToken;
            Properties.Settings.Default.WorkspaceId = WorkspaceId;
            Properties.Settings.Default.Email = Email;
            Properties.Settings.Default.Save();
        }

        private void InitDates()
        {
            DateTime today = DateTime.Today;
            int diffToMonday = (7 + (int)today.DayOfWeek - 1) % 7;
            DateTime monday = today.AddDays(-diffToMonday);
            Since = monday;
            Until = monday.AddDays(4);
        }

        private async Task ExportAsync()
        {
            IsProcessing = true;
            StatusText = "⏳ Exportam...";
            StatusBrush = Brushes.Green;

            if (string.IsNullOrWhiteSpace(ApiToken) ||
                string.IsNullOrWhiteSpace(WorkspaceId) ||
                string.IsNullOrWhiteSpace(Email))
            {
                StatusText = "Unesi sve podatke (API token, Workspace ID, e-mail, datume).";
                StatusBrush = Brushes.Red;
                IsProcessing = false;
                return;
            }

            SaveUserSettings();

            try
            {
                var request = new TogglRequest(ApiToken, WorkspaceId, Email, Since.ToString("yyyy-MM-dd"), Until.ToString("yyyy-MM-dd"));
                var entries = await FetchTogglEntries(request);
                ExportToExcel(entries);
                StatusText = "Spremljeno na Desktop *klik";
                StatusBrush = Brushes.Green;
                ((RelayCommand)OpenFolderCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusText = $"Greška: {ex.Message}";
                StatusBrush = Brushes.Red;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ToggleApiVisibility()
        {
            IsApiVisible = !IsApiVisible;
        }

        private void OpenFolder()
        {
            if (!string.IsNullOrEmpty(lastSavedPath) && File.Exists(lastSavedPath))
            {
                Process.Start("explorer.exe", $"/select,\"{lastSavedPath}\"");
            }
        }

        private async Task<List<TogglEntryRecord>> FetchTogglEntries(TogglRequest request)
        {
            var client = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{request.ApiToken}:api_token"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            string urlBase = $"https://api.track.toggl.com/reports/api/v2/details" +
                             $"?workspace_id={request.WorkspaceId}&since={request.Since}&until={request.Until}&user_agent={request.UserAgent}&page=";

            var entries = new List<TogglEntryRecord>();
            int currentPage = 1;

            while (true)
            {
                string json = await client.GetStringAsync(urlBase + currentPage);
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                if (result == null || result?.data == null || result?.data?.Count == 0)
                {
                    StatusText = $"Greška: Deserialization returned NULL.";
                    return entries;
                }

                foreach (var item in result?.data!)
                {
                    string project = item.project ?? "Bez projekta";
                    string description = item.description ?? "Bez opisa";
                    string startStr = item.start?.ToString() ?? string.Empty;
                    double durationMs = item.dur ?? 0;

                    if (DateTime.TryParse(startStr, out DateTime startDate))
                    {
                        TimeSpan duration = TimeSpan.FromMilliseconds(durationMs);
                        string developer = GetDeveloperNameFromEmail(request.UserAgent);
                        entries.Add(new TogglEntryRecord(startDate.Date, project, developer, duration, description));
                    }
                }

                if (result.data.Count < 50)
                    break;
                currentPage++;
            }

            return entries;
        }

        private void ExportToExcel(List<TogglEntryRecord> entries)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Izvještaj");
            // Header
            ws.Cell(1, 1).Value = "Datum";
            ws.Cell(1, 2).Value = "Projekt";
            ws.Cell(1, 3).Value = "Opis";
            ws.Cell(1, 4).Value = "Developer";
            ws.Cell(1, 5).Value = "Trajanje";

            int row = 2;
            foreach (var e in entries)
            {
                ws.Cell(row, 1).Value = e.Date.ToString("dd.MM.yyyy");
                ws.Cell(row, 2).Value = e.Project;
                ws.Cell(row, 3).Value = e.Description;
                ws.Cell(row, 4).Value = e.Developer;
                ws.Cell(row, 5).Value = e.Duration.ToString(@"hh\:mm\:ss");
                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
            ws.Row(1).Style.Font.Bold = true;

            string selectedFrom = Since.ToString("dd.MM.yyyy");
            string selectedTo = Until.ToString("dd.MM.yyyy");
            string devName = GetDeveloperNameFromEmail(Email);
            string fileName = $"Toggl Track Summary Report {selectedFrom} {selectedTo} {devName}.xlsx";
            lastSavedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            wb.SaveAs(lastSavedPath);
        }

        private static string GetDeveloperNameFromEmail(string email)
        {
            try
            {
                var username = email.Split('@')[0];
                var parts = username.Split('.', '-', '_');
                if (parts.Length >= 2)
                {
                    string first = char.ToUpper(parts[0][0]) + parts[0].Substring(1);
                    string last = char.ToUpper(parts[1][0]) + parts[1].Substring(1);
                    return $"{first} {last}";
                }
                return username;
            }
            catch
            {
                return email;
            }
        }

        private void LoadUserSettings()
        {
            ApiToken = Properties.Settings.Default.ApiToken;
            WorkspaceId = Properties.Settings.Default.WorkspaceId;
            Email = Properties.Settings.Default.Email;
        }
        #endregion
        
        #region Events 
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}