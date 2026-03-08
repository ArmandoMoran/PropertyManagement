using System.Net.Http.Json;
using System.Text.Json;
using PropertyManagement.Application.DTOs;

namespace PropertyManagement.WinForms;

public partial class Form1 : Form
{
    private readonly HttpClient _httpClient;
    private List<PropertyListItemDto> _properties = new();
    private const string BaseUrl = "http://localhost:5100";

    public Form1()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        Load += Form1_Load;
    }

    private async void Form1_Load(object? sender, EventArgs e)
    {
        await LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            statusLabel.Text = "Loading data...";

            // Load available years
            var years = await _httpClient.GetFromJsonAsync<List<int>>("api/properties/years");
            if (years != null)
            {
                cboYear.Items.Clear();
                foreach (var year in years)
                    cboYear.Items.Add(year);
                if (cboYear.Items.Count > 0)
                    cboYear.SelectedIndex = 0;
            }

            // Load properties
            _properties = await _httpClient.GetFromJsonAsync<List<PropertyListItemDto>>("api/properties") ?? new();

            clbProperties.Items.Clear();
            clbProperties.Items.Add("(All Properties)", true);
            foreach (var prop in _properties)
                clbProperties.Items.Add(prop.ShortName, false);

            statusLabel.Text = $"Loaded {_properties.Count} properties. Ready.";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Error loading data. Is the API running?";
            MessageBox.Show($"Could not connect to API at {BaseUrl}.\n\nMake sure to run the API first.\n\n{ex.Message}",
                "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void clbProperties_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // If "(All Properties)" is being checked, uncheck all others
        if (e.Index == 0 && e.NewValue == CheckState.Checked)
        {
            BeginInvoke(() =>
            {
                for (int i = 1; i < clbProperties.Items.Count; i++)
                    clbProperties.SetItemChecked(i, false);
            });
        }
        // If any specific property is checked, uncheck "(All Properties)"
        else if (e.Index > 0 && e.NewValue == CheckState.Checked)
        {
            BeginInvoke(() => clbProperties.SetItemChecked(0, false));
        }
    }

    private async void btnGenerate_Click(object? sender, EventArgs e)
    {
        if (cboYear.SelectedItem == null)
        {
            MessageBox.Show("Please select a year.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int year = (int)cboYear.SelectedItem;
        string propertyFilter = GetPropertyFilter();

        try
        {
            btnGenerate.Enabled = false;
            statusLabel.Text = $"Generating report for {year}...";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            string url = $"api/reports/{year}/excel";
            if (!string.IsNullOrEmpty(propertyFilter))
                url += $"?property={Uri.EscapeDataString(propertyFilter)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();

            // Save file dialog
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                FileName = $"PropertyReport_{year}.xlsx",
                Title = "Save Property Report"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                await File.WriteAllBytesAsync(sfd.FileName, bytes);
                statusLabel.Text = $"Report saved to {sfd.FileName}";
                MessageBox.Show($"Report generated successfully!\n\nSaved to: {sfd.FileName}",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                statusLabel.Text = "Report generation cancelled.";
            }
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Error generating report.";
            MessageBox.Show($"Error generating report:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnGenerate.Enabled = true;
            progressBar.Visible = false;
        }
    }

    private string GetPropertyFilter()
    {
        if (clbProperties.GetItemChecked(0)) // "All Properties" is checked
            return "";

        var selectedProperties = new List<string>();
        for (int i = 1; i < clbProperties.Items.Count; i++)
        {
            if (clbProperties.GetItemChecked(i))
                selectedProperties.Add(clbProperties.Items[i].ToString()!);
        }

        return string.Join(",", selectedProperties);
    }
}
