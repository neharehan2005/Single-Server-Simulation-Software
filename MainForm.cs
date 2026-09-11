using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Member1_DataGeneration
{
    /// <summary>
    /// Member 4's deliverable: the user-facing window. Wires up Member 1's
    /// data generation, Member 2's queue simulation, and Member 3's performance
    /// analysis behind an input form, a results grid, a chart visualizer, and a summary panel.
    /// Customers can either be randomly generated from the fitted historical
    /// distributions, or loaded directly from a user-supplied CSV file.
    /// </summary>
    public class MainForm : Form
    {
        private NumericUpDown customerCountInput = null!;
        private TextBox seedInput = null!;

        private RadioButton randomRadio = null!;
        private RadioButton csvRadio = null!;
        private Button browseCsvButton = null!;
        private Label csvFileLabel = null!;

        private Button runButton = null!;
        private DataGridView resultsGrid = null!;
        private TextBox summaryBox = null!;
        private Label statusLabel = null!;
        private Chart performanceChart = null!;

        private string? selectedCsvPath;

        private HistoricalDataAnalyzer? analyzer;

        // Series names, kept as constants so the build/update code never has to
        // repeat (and risk mistyping) a magic string.
        private const string WaitSeriesName = "Waiting Time";
        private const string SystemSeriesName = "Time In System";
        private const string QueueSeriesName = "Queue Length";
        private const string AvgWaitLineName = "Avg Waiting Time";
        private const string AvgSystemLineName = "Avg Time In System";

        public MainForm()
        {
            Text = "Single Server Queue Simulation";
            Width = 1150;
            Height = 850;
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
            LoadHistoricalData();
        }

        private void BuildLayout()
        {
            var inputPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight
            };

            inputPanel.Controls.Add(new Label
            {
                Text = "Customers to simulate:",
                AutoSize = true,
                Margin = new Padding(0, 10, 5, 0)
            });
            customerCountInput = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = 50, Width = 80 };
            inputPanel.Controls.Add(customerCountInput);

            randomRadio = new RadioButton
            {
                Text = "Random Generator",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(20, 8, 5, 0)
            };
            csvRadio = new RadioButton
            {
                Text = "Upload CSV",
                AutoSize = true,
                Margin = new Padding(10, 8, 5, 0)
            };
            browseCsvButton = new Button
            {
                Text = "Choose CSV",
                Width = 100,
                Height = 30,
                Enabled = false,
                Margin = new Padding(10, 3, 0, 0)
            };
            csvFileLabel = new Label
            {
                Text = "No CSV selected",
                AutoSize = true,
                Margin = new Padding(10, 10, 0, 0)
            };

            randomRadio.CheckedChanged += InputMethodChanged;
            csvRadio.CheckedChanged += InputMethodChanged;
            browseCsvButton.Click += BrowseCsvButton_Click;

            inputPanel.Controls.Add(randomRadio);
            inputPanel.Controls.Add(csvRadio);
            inputPanel.Controls.Add(browseCsvButton);
            inputPanel.Controls.Add(csvFileLabel);

            inputPanel.Controls.Add(new Label
            {
                Text = "Random seed (optional):",
                AutoSize = true,
                Margin = new Padding(25, 10, 5, 0)
            });
            seedInput = new TextBox { Width = 80 };
            inputPanel.Controls.Add(seedInput);

            runButton = new Button { Text = "Run Simulation", Width = 140, Height = 30, Margin = new Padding(25, 3, 0, 0) };
            runButton.Click += RunButton_Click;
            inputPanel.Controls.Add(runButton);

            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.WhiteSmoke
            };

            resultsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };

            summaryBox = new TextBox
            {
                Dock = DockStyle.Right,
                Width = 330,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font(FontFamily.GenericMonospace, 9.5f),
                Text = "Run a simulation to see the performance summary here."
            };

            // ---------------- Performance chart ----------------
            performanceChart = new Chart
            {
                Dock = DockStyle.Bottom,
                Height = 300,
                BackColor = Color.White
            };
            performanceChart.AntiAliasing = AntiAliasingStyles.All;
            performanceChart.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            performanceChart.Titles.Add(new Title
            {
                Text = "Per-Customer Performance Metrics",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 45)
            });

            var chartArea = new ChartArea("MainArea")
            {
                BackColor = Color.White
            };
            chartArea.AxisX.Title = "Customer ID";
            chartArea.AxisY.Title = "Minutes / Customers";
            chartArea.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            chartArea.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            chartArea.AxisX.LineColor = Color.DarkGray;
            chartArea.AxisY.LineColor = Color.DarkGray;

            // Let the user zoom into a busy chart (drag on the X axis) and pan
            // via the scrollbar once zoomed in — much easier to read with 500+
            // customers than a single squashed line.
            chartArea.AxisX.ScaleView.Zoomable = true;
            chartArea.AxisX.ScrollBar.Enabled = true;
            chartArea.AxisX.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
            chartArea.CursorX.IsUserEnabled = true;
            chartArea.CursorX.IsUserSelectionEnabled = true;

            performanceChart.ChartAreas.Add(chartArea);

            var legend = new Legend("Legend")
            {
                Docking = Docking.Top,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 8.5f)
            };
            performanceChart.Legends.Add(legend);

            var waitingTimeSeries = new Series(WaitSeriesName)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.Crimson,
                XValueMember = "CustomerId",
                YValueMembers = "WaitingTime",
                ToolTip = "Customer #VALX\nWaiting time: #VAL{F2} min"
            };

            var timeInSystemSeries = new Series(SystemSeriesName)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.RoyalBlue,
                XValueMember = "CustomerId",
                YValueMembers = "TimeInSystem",
                ToolTip = "Customer #VALX\nTime in system: #VAL{F2} min"
            };

            var queueLengthSeries = new Series(QueueSeriesName)
            {
                ChartType = SeriesChartType.StepLine,
                BorderWidth = 2,
                Color = Color.DarkOrange,
                XValueMember = "CustomerId",
                YValueMembers = "QueueLength",
                ToolTip = "Customer #VALX\nQueue length: #VAL customers"
            };

            // Dashed reference lines showing the averages, so a spike is easy to
            // judge against the mean at a glance rather than mentally averaging
            // the whole series. These get exactly two points (start/end of the
            // X range) whenever a simulation runs — see UpdateChart.
            var avgWaitLine = new Series(AvgWaitLineName)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 1,
                BorderDashStyle = ChartDashStyle.Dash,
                Color = Color.Crimson,
                IsVisibleInLegend = true,
                ToolTip = "Average waiting time: #VAL{F2} min"
            };
            var avgSystemLine = new Series(AvgSystemLineName)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 1,
                BorderDashStyle = ChartDashStyle.Dash,
                Color = Color.RoyalBlue,
                IsVisibleInLegend = true,
                ToolTip = "Average time in system: #VAL{F2} min"
            };

            performanceChart.Series.Add(waitingTimeSeries);
            performanceChart.Series.Add(timeInSystemSeries);
            performanceChart.Series.Add(queueLengthSeries);
            performanceChart.Series.Add(avgWaitLine);
            performanceChart.Series.Add(avgSystemLine);

            foreach (Series s in performanceChart.Series)
                s.ChartArea = "MainArea";

            // Docking order: Bottom controls, Right controls, then Fill controls last.
            Controls.Add(resultsGrid);
            Controls.Add(summaryBox);
            Controls.Add(performanceChart);
            Controls.Add(inputPanel);
            Controls.Add(statusLabel);

            InputMethodChanged(this, EventArgs.Empty);
        }

        private void InputMethodChanged(object? sender, EventArgs e)
        {
            bool useCsv = csvRadio.Checked;

            browseCsvButton.Enabled = useCsv;

            customerCountInput.Enabled = !useCsv;
            seedInput.Enabled = !useCsv;

            statusLabel.Text = useCsv
                ? "Choose a CSV file, then click Run Simulation."
                : "Ready to run using the random generator.";
        }

        private void BrowseCsvButton_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select Customer CSV File"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                selectedCsvPath = dialog.FileName;
                csvFileLabel.Text = Path.GetFileName(selectedCsvPath);
                statusLabel.Text = $"CSV selected: {Path.GetFileName(selectedCsvPath)}";
            }
        }

        private void LoadHistoricalData()
        {
            try
            {
                string csvPath = Path.Combine(AppContext.BaseDirectory, "SapphireData.csv");
                if (!File.Exists(csvPath))
                    csvPath = "SapphireData.csv";

                analyzer = new HistoricalDataAnalyzer();
                analyzer.LoadAndFit(csvPath);

                statusLabel.Text = $"Historical data loaded — mean inter-arrival {analyzer.MeanInterArrivalMinutes:F2} min, " +
                                    $"mean service {analyzer.MeanServiceMinutes:F2} min. Ready to run.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Failed to load SapphireData.csv — see error.";
                MessageBox.Show($"Could not load historical data:\n{ex.Message}", "Startup Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunButton_Click(object? sender, EventArgs e)
        {
            if (analyzer == null)
            {
                MessageBox.Show("Historical data isn't loaded — can't run a simulation.", "Not Ready",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool useCsv = csvRadio.Checked;

            if (useCsv && string.IsNullOrWhiteSpace(selectedCsvPath))
            {
                MessageBox.Show("Choose a CSV file first, or switch to Random Generator.", "No CSV Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int customerCount = (int)customerCountInput.Value;

            int? seed = null;
            if (!useCsv && !string.IsNullOrWhiteSpace(seedInput.Text))
            {
                if (!int.TryParse(seedInput.Text, out int parsedSeed))
                {
                    MessageBox.Show("Seed must be a whole number, or left blank.", "Invalid Seed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                seed = parsedSeed;
            }

            try
            {
                runButton.Enabled = false;
                statusLabel.Text = "Running simulation...";

                List<Customer> customers;
                string inputSourceLabel;

                if (useCsv)
                {
                    customers = LoadCustomersFromCsv(selectedCsvPath!);
                    inputSourceLabel = $"Uploaded CSV ({Path.GetFileName(selectedCsvPath)}, {customers.Count} customers)";
                }
                else
                {
                    var generator = new DataGenerator(analyzer, seed: seed);
                    customers = generator.Generate(customerCount);
                    inputSourceLabel = $"Random Generator ({generator.ServiceDistribution} service times)";
                }

                var simulation = new QueueServerSimulation();
                var results = simulation.Run(customers);

                var perfAnalyzer = new PerformanceAnalyzer();
                var metrics = perfAnalyzer.Analyze(results);

                resultsGrid.DataSource = null;
                resultsGrid.Columns.Clear();
                resultsGrid.DataSource = results;

                summaryBox.Text = BuildSummaryText(metrics, inputSourceLabel);

                UpdateChart(results, metrics);

                statusLabel.Text = $"Done — {results.Count} customers simulated ({inputSourceLabel}).";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Simulation failed — see error.";
                MessageBox.Show($"Something went wrong while running the simulation:\n{ex.Message}",
                    "Simulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                runButton.Enabled = true;
            }
        }

        /// <summary>
        /// Redraws the performance chart for a fresh set of results:
        /// - Per-customer waiting time, time-in-system, and queue length.
        /// - Dashed mean reference lines for waiting time and time-in-system.
        /// - Markers are only shown for smaller runs so large simulations
        ///   (hundreds/thousands of customers) don't turn into a solid smear.
        /// - The X axis interval is scaled to the data size so labels stay legible,
        ///   and zoom/pan (via the scrollbar or drag-select) is available for
        ///   digging into a specific range of customers.
        /// </summary>
        private void UpdateChart(List<SimulationResult> results, PerformanceMetrics metrics)
        {
            var waitSeries = performanceChart.Series[WaitSeriesName];
            var systemSeries = performanceChart.Series[SystemSeriesName];
            var queueSeries = performanceChart.Series[QueueSeriesName];
            var avgWaitLine = performanceChart.Series[AvgWaitLineName];
            var avgSystemLine = performanceChart.Series[AvgSystemLineName];

            waitSeries.Points.Clear();
            systemSeries.Points.Clear();
            queueSeries.Points.Clear();
            avgWaitLine.Points.Clear();
            avgSystemLine.Points.Clear();

            var ordered = results.OrderBy(r => r.CustomerId).ToList();

            bool showMarkers = ordered.Count <= 75;
            foreach (var s in new[] { waitSeries, systemSeries })
            {
                s.MarkerStyle = showMarkers ? MarkerStyle.Circle : MarkerStyle.None;
                s.MarkerSize = 5;
            }
            queueSeries.MarkerStyle = MarkerStyle.None;

            foreach (var item in ordered)
            {
                waitSeries.Points.AddXY(item.CustomerId, item.WaitingTime);
                systemSeries.Points.AddXY(item.CustomerId, item.TimeInSystem);
                queueSeries.Points.AddXY(item.CustomerId, item.QueueLength);
            }

            int minId = ordered.Min(r => r.CustomerId);
            int maxId = ordered.Max(r => r.CustomerId);
            avgWaitLine.Points.AddXY(minId, metrics.AverageWaitingTime);
            avgWaitLine.Points.AddXY(maxId, metrics.AverageWaitingTime);
            avgSystemLine.Points.AddXY(minId, metrics.AverageTimeInSystem);
            avgSystemLine.Points.AddXY(maxId, metrics.AverageTimeInSystem);

            var chartArea = performanceChart.ChartAreas["MainArea"];
            chartArea.AxisX.Interval = Math.Max(1, ordered.Count / 20);
            chartArea.AxisX.ScaleView.ZoomReset();
            chartArea.RecalculateAxesScale();

            performanceChart.Titles[0].Text =
                $"Per-Customer Performance Metrics  (L = {metrics.AverageNumberInSystem:F2}, Lq = {metrics.AverageNumberInQueue:F2})";
        }

        /// <summary>
        /// Loads customer records directly from a CSV file, as an alternative to the
        /// random generator. Expected columns (header row required, same shape as
        /// DataGenerator.ExportToCsv's output):
        ///   CustomerId,InterArrivalTime,ArrivalTime,ArrivalClockTime,ServiceTime
        /// InterArrivalTime/ArrivalTime/ServiceTime are in minutes; ArrivalClockTime
        /// is an optional "HH:mm" display string.
        /// </summary>
        private static List<Customer> LoadCustomersFromCsv(string csvPath)
        {
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Could not find CSV file at '{csvPath}'.");

            var lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
                throw new InvalidOperationException("CSV file has no data rows (expected a header row plus at least one customer).");

            var customers = new List<Customer>();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                var cols = line.Split(',');
                if (cols.Length < 5)
                    throw new InvalidOperationException(
                        $"Row {i + 1} has {cols.Length} column(s); expected 5: CustomerId,InterArrivalTime,ArrivalTime,ArrivalClockTime,ServiceTime.");

                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    throw new InvalidOperationException($"Row {i + 1}: '{cols[0]}' is not a valid CustomerId.");

                if (!double.TryParse(cols[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double interArrival))
                    throw new InvalidOperationException($"Row {i + 1}: '{cols[1]}' is not a valid InterArrivalTime.");

                if (!double.TryParse(cols[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double arrival))
                    throw new InvalidOperationException($"Row {i + 1}: '{cols[2]}' is not a valid ArrivalTime.");

                if (!double.TryParse(cols[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double service))
                    throw new InvalidOperationException($"Row {i + 1}: '{cols[4]}' is not a valid ServiceTime.");

                customers.Add(new Customer
                {
                    Id = id,
                    InterArrivalTime = interArrival,
                    ArrivalTime = arrival,
                    ArrivalClockTime = cols[3].Trim(),
                    ServiceTime = service
                });
            }

            if (customers.Count == 0)
                throw new InvalidOperationException("No valid customer rows found in the CSV.");

            return customers;
        }

        private static string BuildSummaryText(PerformanceMetrics m, string inputSourceLabel)
        {
            return
                "PERFORMANCE SUMMARY\r\n" +
                "====================\r\n" +
                $"Input source  : {inputSourceLabel}\r\n\r\n" +
                $"Total customers: {m.TotalCustomers}\r\n" +
                $"Total sim time: {m.TotalSimulationTime:F2}mins\r\n\r\n" +
                $"Arrival rate-λ: {m.ArrivalRate:F4} cust/min\r\n\r\n" +
                $"Avg waiting time-Wq: {m.AverageWaitingTime:F2}mins\r\n" +
                $"Max waiting time: {m.MaxWaitingTime:F2}mins\r\n\r\n" +
                $"Avg time in system-W: {m.AverageTimeInSystem:F2}mins\r\n" +
                $"Max time in system: {m.MaxTimeInSystem:F2}mins\r\n\r\n" +
                $"Avg num in system-L: {m.AverageNumberInSystem:F2} customers\r\n" +
                $"Avg num in queue-Lq: {m.AverageNumberInQueue:F2} customers\r\n\r\n" +
                $"Avg queue length : {m.AverageQueueLength:F2} customers\r\n" +
                $"Max queue length: {m.MaxQueueLength} customers\r\n\r\n" +
                $"Busy time: {m.TotalBusyTime:F2}mins\r\n" +
                $"Idle time: {m.TotalIdleTime:F2}mins\r\n" +
                $"Server utilization: {m.ServerUtilization:P2}\r\n\r\n" +
                $"Customers who waited: {m.NumberWhoWaited} / {m.TotalCustomers}\r\n" +
                $"P(waiting): {m.ProbabilityOfWaiting:P2}\r\n";
        }
    }
}
