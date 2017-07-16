namespace Portsy
{
    using Portsy.PortQrySupport;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Ready = "Ready to go!";

        private List<Task> workingTasks = new List<Task>();
        private PortQry2 portQry2;

        public MainWindow()
        {
            this.portQry2 = new PortQry2();

            InitializeComponent();

            this.queryResultsListBox.ItemsSource = this.QueryResults;
            this.portTypeCombobox.ItemsSource = Enum.GetValues(typeof(ProtocolType)).Cast<ProtocolType>();
            this.portTypeCombobox.SelectedIndex = 0;
            this.progressLabel.Content = Ready;

            this.queryProgressBar.Minimum = 0;
            this.queryProgressBar.Maximum = 0;
            this.queryProgressBar.Value = 0;

            this.addressTextBox.Focus();
        }

        public ObservableCollection<QueryResult> QueryResults { get; set; } = new ObservableCollection<QueryResult>();

        /// <summary>
        /// Query specified ports.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void QueryButtonClick(object sender, RoutedEventArgs e)
        {
            var protocol = (ProtocolType) this.portTypeCombobox.SelectedValue;
            this.QueryPorts(this.addressTextBox.Text, protocol, this.portsTextBox.Text);
        }

        /// <summary>
        /// Take multiple ports separated by coma and process one by one.
        /// </summary>
        /// <param name="addresses">Address to check.</param>
        /// <param name="ports">Ports to check.</param>
        private void QueryPorts(string addresses, ProtocolType protocol, string ports)
        {
            var portsNumbers = ports.Split(',');
            var addressesNumbers = addresses.Split(',');

            // Extend progress bar.
            this.progressLabel.Content = "Working...";

            foreach (var singleAddress in addressesNumbers)
            {
                this.queryProgressBar.Maximum += portsNumbers.Length;

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
