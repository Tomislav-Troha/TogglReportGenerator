namespace TogglToExcel.Model
{
    public class TogglEntry
    {
        public record TogglRequest(string ApiToken, string WorkspaceId, string UserAgent, string Since, string Until);
        public record TogglEntryRecord(DateTime Date, string Project, string Developer, TimeSpan Duration, string Description);
    }
}
