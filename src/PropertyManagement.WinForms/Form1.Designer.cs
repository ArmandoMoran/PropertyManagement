namespace PropertyManagement.WinForms;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(520, 520);
        this.Text = "Property Management - Report Generator";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // Year Label
        lblYear = new Label();
        lblYear.Text = "Report Year:";
        lblYear.Location = new Point(20, 20);
        lblYear.Size = new Size(100, 25);
        lblYear.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        // Year ComboBox
        cboYear = new ComboBox();
        cboYear.Location = new Point(130, 18);
        cboYear.Size = new Size(100, 25);
        cboYear.DropDownStyle = ComboBoxStyle.DropDownList;
        cboYear.Font = new Font("Segoe UI", 10F);

        // Properties Label
        lblProperties = new Label();
        lblProperties.Text = "Select Properties:";
        lblProperties.Location = new Point(20, 60);
        lblProperties.Size = new Size(200, 25);
        lblProperties.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        // Properties CheckedListBox
        clbProperties = new CheckedListBox();
        clbProperties.Location = new Point(20, 90);
        clbProperties.Size = new Size(470, 310);
        clbProperties.Font = new Font("Segoe UI", 9.5F);
        clbProperties.CheckOnClick = true;
        clbProperties.ItemCheck += clbProperties_ItemCheck;

        // Generate Button
        btnGenerate = new Button();
        btnGenerate.Text = "📊  Generate Excel Report";
        btnGenerate.Location = new Point(20, 415);
        btnGenerate.Size = new Size(470, 40);
        btnGenerate.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnGenerate.BackColor = Color.FromArgb(0, 120, 212);
        btnGenerate.ForeColor = Color.White;
        btnGenerate.FlatStyle = FlatStyle.Flat;
        btnGenerate.Cursor = Cursors.Hand;
        btnGenerate.Click += btnGenerate_Click;

        // Progress Bar
        progressBar = new ProgressBar();
        progressBar.Location = new Point(20, 465);
        progressBar.Size = new Size(370, 20);
        progressBar.Visible = false;

        // Status Label
        statusLabel = new Label();
        statusLabel.Text = "Ready";
        statusLabel.Location = new Point(20, 490);
        statusLabel.Size = new Size(470, 20);
        statusLabel.Font = new Font("Segoe UI", 8.5F);
        statusLabel.ForeColor = Color.DimGray;

        this.Controls.Add(lblYear);
        this.Controls.Add(cboYear);
        this.Controls.Add(lblProperties);
        this.Controls.Add(clbProperties);
        this.Controls.Add(btnGenerate);
        this.Controls.Add(progressBar);
        this.Controls.Add(statusLabel);
    }

    private Label lblYear;
    private ComboBox cboYear;
    private Label lblProperties;
    private CheckedListBox clbProperties;
    private Button btnGenerate;
    private ProgressBar progressBar;
    private Label statusLabel;

    #endregion
}
