using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosedXML.Excel;
using Newtonsoft.Json;
using static TogglToExcel.TogglEntry;

namespace TogglToExcel
{
    public partial class MainWindow : Window
    {
        private bool isApiVisible = false;
        private string? lastSavedPath;

        public MainWindow()
        {
            InitializeComponent();
            LoadUserSettings();
            InitDates();
        }

        private void InitDates()
        {
            DateTime today = DateTime.Today;
            int diffToMonday = (7 + (int)today.DayOfWeek - 1) % 7;
            DateTime monday = today.AddDays(-diffToMonday);
            DateTime friday = monday.AddDays(4);

            dpFrom.SelectedDate = monday;
            dpTo.SelectedDate = friday;
        }

        private void LoadUserSettings()
        {
            if (isApiVisible)
                txtApiTokenVisible.Text = Properties.Settings.Default.ApiToken;
            else
                pwdApiToken.Password = Properties.Settings.Default.ApiToken;

            txtWorkspaceId.Text = Properties.Settings.Default.WorkspaceId;
            txtEmail.Text = Properties.Settings.Default.Email;
        }

        private static void SaveUserSettings(string token, string workspaceId, string email)
        {
            Properties.Settings.Default.ApiToken = token;
            Properties.Settings.Default.WorkspaceId = workspaceId;
            Properties.Settings.Default.Email = email;
            Properties.Settings.Default.Save();
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "⏳ Exportam...";
            txtStatus.Foreground = Brushes.Green;

            string apiToken = isApiVisible ? txtApiTokenVisible.Text.Trim() : pwdApiToken.Password.Trim();
            string workspaceId = txtWorkspaceId.Text.Trim();
            string userAgent = txtEmail.Text.Trim();
            string since = dpFrom.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString();
            string until = dpTo.SelectedDate?.AddDays(1).ToString("yyyy-MM-dd") ?? DateTime.Now.ToString();

            if (string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(workspaceId)
                || string.IsNullOrWhiteSpace(userAgent) || since == null || until == null)
            {
                txtStatus.Text = "Unesi sve podatke (API token, Workspace ID, e-mail, datume).";
                return;
            }

            SaveUserSettings(apiToken, workspaceId, userAgent);

            try
            {
                var request = new TogglRequest(apiToken, workspaceId, userAgent, since, until);
                var entries = await FetchTogglEntries(request);
                ExportToExcel(entries);
                txtStatus.Text = $"Spremljeno na Desktop *klik";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Greška: {ex.Message}";
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

            List<TogglEntryRecord> entries = [];
            int currentPage = 1;

            while (true)
            {
                string json = await client.GetStringAsync(urlBase + currentPage);
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                if (result == null)
                {
                    txtStatus.Text = "Greška: Deserialization returned NULL.";
                    break;
                }

                if (result?.data == null || result?.data?.Count == 0)
                    break;

                foreach (var item in result?.data!)
                {
                    string project = item.project ?? "Bez projekta";
                    string description = item.description ?? "Bez opisa";
                    string startStr = item.start?.ToString() ?? "";
                    double durationMs = item.dur ?? 0;

                    if (DateTime.TryParse(startStr, out DateTime startDate))
                    {
                        TimeSpan duration = TimeSpan.FromMilliseconds(durationMs);
                        string developer = GetDeveloperNameFromEmail(request.UserAgent);
                        var newEntries = new TogglEntryRecord(startDate.Date, project, developer, duration, description);
                        entries.Add(newEntries);
                    }
                }

                if (result.data.Count < 50) break;
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


            string? selectedDateFrom = dpFrom.SelectedDate?.ToString("dd.MM.yyyy");
            string? selectedDateTo = dpTo.SelectedDate?.ToString("dd.MM.yyyy");

            string xlsxName = $"Toggl Track Summary Report {selectedDateFrom} {selectedDateTo} {GetDeveloperNameFromEmail(txtEmail.Text.Trim())}.xlsx";
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), xlsxName);
            lastSavedPath = path;
            wb.SaveAs(path);
        }

        private static string GetDeveloperNameFromEmail(string email)
        {
            try
            {
                var username = email.Split('@')[0];
                var parts = username.Split('.', '-', '_');

                if (parts.Length >= 2)
                {
                    string first = char.ToUpper(parts[0][0]) + parts[0][1..];
                    string last = char.ToUpper(parts[1][0]) + parts[1][1..];
                    return $"{first} {last}";
                }

                return username;
            }
            catch
            {
                return email;
            }
        }

        private void BtnToggleApi_Click(object sender, RoutedEventArgs e)
        {
            isApiVisible = !isApiVisible;

            if (isApiVisible)
            {
                txtApiTokenVisible.Text = pwdApiToken.Password;
                txtApiTokenVisible.Visibility = Visibility.Visible;
                pwdApiToken.Visibility = Visibility.Collapsed;
            }
            else
            {
                pwdApiToken.Password = txtApiTokenVisible.Text;
                pwdApiToken.Visibility = Visibility.Visible;
                txtApiTokenVisible.Visibility = Visibility.Collapsed;
            }
        }

        private void PwdApiToken_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isApiVisible)
                txtApiTokenVisible.Text = pwdApiToken.Password;
        }

        private void TxtApiTokenVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isApiVisible)
                pwdApiToken.Password = txtApiTokenVisible.Text;
        }

        private void txtStatus_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(lastSavedPath) && File.Exists(lastSavedPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{lastSavedPath}\"");
            }
        }
    }
}
