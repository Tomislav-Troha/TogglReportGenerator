# TogglExcelExporter

A simple WPF application for exporting detailed Toggl Track time entries to Excel (`.xlsx`) format.

## Features

- Input for API token, Workspace ID, and email (user agent)
- Automatic saving of user settings
- Export detailed time entries (by day, project, description)
- Automatic date range preset from Monday to Friday
- App icon, fixed-size window, and centered on screen
- Click on status opens the saved Excel file location

## How to Use

1. Run the application  
2. Enter your API token, Workspace ID, and email  
3. Select the date range (or use the default one)  
4. Click **Export**  
5. The Excel file is saved to your Desktop and its folder opens automatically

## Note

The API must be deactivated before exporting active (currently running) timers.  
All data remains local and is never transmitted elsewhere.
