namespace Portsy.PortQrySupport
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// PortQry v2 class.
    /// </summary>
    public class PortQry2
    {
        /// <summary>
        /// Indicator for resolving name.
        /// </summary>
        private const string NameResolvedIndicator = "Name resolved to ";

        /// <summary>
        /// Indicator for resolving address.
        /// </summary>
        private const string AddressResolvedIndicator = "IP address resolved to ";

        /// <summary>
        /// Indicator for resolving port.
        /// </summary>
        private const string PortIndicator = " port ";

        /// <summary>
        /// PortQry new line.
        /// </summary>
        private const string NewLineSeparator = "\r\n\r\n";

        /// <summary>
        /// PortQry file path.
        /// </summary>
        private string filePath;

        public PortQry2()
        {
            this.filePath = @"PortQrySupport\PortQryV2\PortQry.exe";
        }

        /// <summary>
        /// Query given port.
        /// </summary>
        /// <param name="address">Address to use.</param>
        /// <param name="protocol">Protocol to use.</param>
        /// <param name="port">Port to use.</param>
        /// <returns>Taks with query result.</returns>
        public Task<QueryResult> Query(string address, ProtocolType protocol, string port)
        {
            var queryTask = new Task<QueryResult>(() => {

                var startInfo = new ProcessStartInfo()
                {
                    FileName = this.filePath,
                    Arguments = $"-n {address} -e {port} -p {protocol}",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                // Start PortQry process.
                var portQryProcess = new Process();
                portQryProcess.StartInfo = startInfo;
                portQryProcess.Start();

                var output = portQryProcess.StandardOutput.ReadToEnd();

                return this.GetQueryStatus(address, protocol, port, output);
            });

            queryTask.Start();

            return queryTask;
        }

        /// <summary>
        /// Translate PortQry data to query result.
        /// </summary>
        /// <param name="address">Address value.</param>
        /// <param name="protocol">Protocol value.</param>
        /// <param name="port">Port number.</param>
        /// <param name="output">Process output.</param>
        /// <returns>Query result.</returns>
        private QueryResult GetQueryStatus(string address, ProtocolType protocol, string port, string output)
        {
            var resolvedIp = this.TryGetIpAddress(output);
            var resolvedName = this.TryGetName(output);
            var portType = this.TryGetPort(output);

            if (!string.IsNullOrEmpty(resolvedIp))
            {
                address = $"({address}) {resolvedIp}";
            }

            if (!string.IsNullOrEmpty(resolvedName))
            {
                address = $"({resolvedName}) {address}";
            }

            var status = this.GetStatus(output);

            // If port types does not match - set error.
            if (!string.IsNullOrEmpty(portType))
            {
                if (portType != protocol.ToString())
                {
                    status = PortStatus.ERROR;
                }
            }

            return new QueryResult(address, protocol, port, status);
        }

        /// <summary>
        /// Try to get port number from PortQry output.
        /// </summary>
        /// <param name="output">PortQry output.</param>
        /// <returns>Port number or empty string when not found.</returns>
        private string TryGetPort(string output)
        {
            if (!output.Contains(PortIndicator))
            {
                return string.Empty;
            }

            var index = output.IndexOf(PortIndicator);
            var portName = output.Remove(index);
            portName = portName.Substring(portName.LastIndexOf(NewLineSeparator)).Trim();

            return portName;
        }

        /// <summary>
        /// Try to get IP address from PortQry output.
        /// </summary>
        /// <param name="output">PortQry output.</param>
        /// <returns>IP address or empty string when not found.</returns>
        private string TryGetIpAddress(string output)
        {
            return this.GetOutputLineSubstring(output, NameResolvedIndicator);
        }

        /// <summary>
        /// Try to get resolved name from PortQry output.
        /// </summary>
        /// <param name="output">PortQry output.</param>
        /// <returns>Resolved name or empty string when not found.</returns>
        private string TryGetName(string output)
        {
            return this.GetOutputLineSubstring(output, AddressResolvedIndicator);
        }

        /// <summary>
        /// Get data from single line by indicator.
        /// </summary>
        /// <param name="output">PortQry output.</param>
        /// <param name="indicator">Specific indicator.</param>
        /// <returns>Specific substring.</returns>
        private string GetOutputLineSubstring(string output, string indicator)
        {
            if (!(output.Contains(indicator)))
            {
                return string.Empty;
            }

            var data = output.Substring(output.IndexOf(indicator) + indicator.Length);
            var length = data.IndexOf(NewLineSeparator);
            data = data.Substring(0, length);

            return data;
        }

        /// <summary>
        /// Get port status based on output.
        /// </summary>
        /// <param name="output">PortQry output.</param>
        /// <returns>Port status.</returns>
        private PortStatus GetStatus(string output)
        {
            if (output.Contains(": LISTENING"))
            {
                return PortStatus.LISTENING;
            }

            if (output.Contains(": FILTERED"))
            {
                return PortStatus.FILTERED;
            }

            if (output.Contains(": NOT LISTENING"))
            {
                return PortStatus.NOT_LISTENING;
            }

            return PortStatus.ERROR;
        }
    }
}
