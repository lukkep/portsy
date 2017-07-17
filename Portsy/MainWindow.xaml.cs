namespace Portsy
{
    using Portsy.PortQrySupport;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Application name.
        /// </summary>
        private const string AppName = "Portsy";

        /// <summary>
        /// Ready text.
        /// </summary>
        private const string Ready = "Ready to go!";

        /// <summary>
        /// Ports range seprator.
        /// </summary>
        private const char RangeSeparator = '-';

        /// <summary>
        /// List of working tasks.
        /// </summary>
        private List<Task> workingTasks = new List<Task>();

        /// <summary>
        /// PortQry instance.
        /// </summary>
        private PortQry2 portQry2;

        public MainWindow()
        {
            this.portQry2 = new PortQry2();

            InitializeComponent();

            this.Title = $"{AppName} (v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor})";

            this.queryResultsListBox.ItemsSource = this.QueryResults;
            this.portTypeCombobox.ItemsSource = Enum.GetValues(typeof(ProtocolType)).Cast<ProtocolType>();
            this.portTypeCombobox.SelectedIndex = 0;
            this.progressLabel.Content = Ready;

            this.queryProgressBar.Minimum = 0;
            this.queryProgressBar.Maximum = 0;
            this.queryProgressBar.Value = 0;

            this.addressTextBox.Focus();
        }

        /// <summary>
        /// Query results list.
        /// </summary>
        public ObservableCollection<QueryResult> QueryResults { get; set; } = new ObservableCollection<QueryResult>();

        /// <summary>
        /// Query specified ports.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void QueryButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var protocol = (ProtocolType)this.portTypeCombobox.SelectedValue;
                this.QueryPorts(this.addressTextBox.Text, protocol, this.portsTextBox.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Error while parsing provided data. Verify input and try again.", "Data error.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Take multiple ports separated by coma and process one by one.
        /// </summary>
        /// <param name="addressesInput">Address to check.</param>
        /// <param name="portsInput">Ports to check.</param>
        private void QueryPorts(string addressesInput, ProtocolType protocol, string portsInput)
        {
            var portsNumbers = this.ParsePorts(portsInput);
            var addressesNumbers = addressesInput.Split(',');

            // Change progress text and extend progress bar.
            this.progressLabel.Content = "Working...";
            this.queryProgressBar.Maximum += portsNumbers.Count();

            foreach (var singleAddress in addressesNumbers)
            {
                foreach (var singlePort in portsNumbers)
                {
                    var task = this.RunQueryTask(singleAddress.Trim(), protocol, singlePort.Trim());
                    this.workingTasks.Add(task);
                }
            }

            // Reset app state when all tasks are finished.
            Task.WhenAll(this.workingTasks.ToArray()).ContinueWith((tasks) => this.AllQueryTasksCompleted());
        }

        /// <summary>
        /// Parse ports input.
        /// </summary>
        /// <param name="portsInput">Input as text.</param>
        /// <returns>List of ports.</returns>
        private List<string> ParsePorts(string portsInput)
        {
            var ports = new List<string>();
            var sepratedPortsText = portsInput.Split(',');

            foreach (var port in sepratedPortsText)
            {
                // If port text contains ports range.
                if (port.Contains(RangeSeparator))
                {
                    var portsRange = this.GetPortsRange(port);
                    ports.AddRange(portsRange);
                    continue;
                }

                ports.Add(port);
            }

            return ports;
        }

        /// <summary>
        /// Resolve ports range.
        /// </summary>
        /// <param name="portRangeInput">Ports range input text.</param>
        /// <returns>List of ports.</returns>
        private List<string> GetPortsRange(string portRangeInput)
        {
            var rangeValues = portRangeInput.Split(RangeSeparator);

            if (rangeValues.Length != 2)
            {
                throw new ArgumentException($"Could not parse [{portRangeInput}] value.");
            }

            // Ports range values.
            var lowerNumberSuccess = Int32.TryParse(rangeValues[0], out int lowerNumber);
            var higherNumberSuccess = Int32.TryParse(rangeValues[1], out int higherNumber);

            if (!lowerNumberSuccess || !higherNumberSuccess || higherNumber < lowerNumber)
            {
                throw new ArgumentException($"Could not interpret ports range [{lowerNumber}-{higherNumber}].");
            }

            // Get range numbers including highest number.
            var portsRange = Enumerable.Range(lowerNumber, higherNumber - lowerNumber + 1);
            this.queryProgressBar.Maximum += portsRange.Count();

            return portsRange.Select(n => n.ToString()).ToList();
        }

        /// <summary>
        /// Method fired when all queries are resolved.
        /// </summary>
        private void AllQueryTasksCompleted()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (this.workingTasks.All(t => t.IsCompleted))
                {
                    this.clearButton.IsEnabled = true;
                    this.workingTasks.Clear();
                    this.queryProgressBar.Value = 0;
                    this.queryProgressBar.Maximum = 0;
                    this.progressLabel.Content = Ready;
                }
            });
        }

        /// <summary>
        /// Run single query in separate task.
        /// </summary>
        /// <param name="address">Address to query.</param>
        /// <param name="protocol">Protocol to use.</param>
        /// <param name="port">Port to query.</param>
        /// <returns>Query task.</returns>
        private Task RunQueryTask(string address, ProtocolType protocol, string port)
        {
            var task = this.portQry2.Query(address, protocol, port);

            task.ContinueWith((queryTask) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    var queryStatus = queryTask.Result as QueryResult;
                    this.QueryResults.Add(queryStatus);
                    this.queryProgressBar.Value++;
                });
            });

            return task;
        }

        /// <summary>
        /// Copy all results.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void CopyAllButtonClick(object sender, RoutedEventArgs e)
        {
            this.SetResultsToClipboard(this.QueryResults.ToList());
        }

        /// <summary>
        /// Copy filtered results.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            if (!this.QueryResults.Any())
            {
                return;
            }

            this.SetResultsToClipboard(this.QueryResults.Where(r => r.Status == PortStatus.FILTERED).ToList(), false);
        }

        /// <summary>
        /// Clear results.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event parameters.</param>
        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            this.QueryResults.Clear();
        }

        /// <summary>
        /// Handle ENTER key press.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryOnEnterKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                this.QueryButtonClick(sender, e);
            }
        }

        /// <summary>
        /// Copy results on CTRL+C press.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Key events.</param>
        private void CopyOnCtrlCKeyDownHandler(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.C)
                {
                    if (this.queryResultsListBox.SelectedItems.Cast<QueryResult>().Any())
                    {
                        this.SetResultsToClipboard(this.queryResultsListBox.SelectedItems.Cast<QueryResult>().ToList());
                    }
                }
            }
        }

        /// <summary>
        /// Set clipboard content to specified results.
        /// </summary>
        /// <param name="results">Results to set.</param>
        /// <param name="includeStatus">Whether to inclued port status.</param>
        private void SetResultsToClipboard(List<QueryResult> results, bool includeStatus = true)
        {
            var copyBuilder = new StringBuilder();

            foreach (var item in results)
            {
                var queryStatus = (item as QueryResult);
                copyBuilder.Append($"{queryStatus.Protocol} - {queryStatus.Address}:{queryStatus.Port}");

                if (includeStatus)
                {
                    copyBuilder.Append($" - [{queryStatus.Status}]");
                }

                copyBuilder.AppendLine();
            }

            Clipboard.SetText(copyBuilder.ToString());
        }
    }
}
