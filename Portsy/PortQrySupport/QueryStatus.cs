namespace Portsy.PortQrySupport
{
    using System.ComponentModel;
    using System.Windows.Media;

    /// <summary>
    /// Port query status information.
    /// </summary>
    public class QueryResult : INotifyPropertyChanged
    {
        /// <summary>
        /// Query address.
        /// </summary>
        private string address;

        /// <summary>
        /// Query port.
        /// </summary>
        private string port;

        /// <summary>
        /// Port type (i.e. TCP).
        /// </summary>
        private ProtocolType protocol;

        /// <summary>
        /// Query status.
        /// </summary>
        private PortStatus status;

        public QueryResult(string address, ProtocolType protocol, string port, PortStatus status)
        {
            this.address = address;
            this.protocol = protocol;
            this.port = port;
            this.status = status;
        }

        /// <summary>
        /// Query status.
        /// </summary>
        public PortStatus Status
        {
            get { return this.status; }

            set
            {
                if (value != this.status)
                {
                    this.status = value;
                    this.NotifyPropertyChanged("Status");
                }
            }
        }

        /// <summary>
        /// Query status color.
        /// </summary>
        public Brush StatusColor => this.GetColor();

        public string Address
        {
            get { return this.address; }

            set
            {
                if (value != this.address)
                {
                    this.address = value;
                    this.NotifyPropertyChanged("Address");
                }
            }
        }

        /// <summary>
        /// Query port number.
        /// </summary>
        public string Port
        {
            get
            {
                return this.port;
            }

            set
            {
                if (value != this.port)
                {
                    this.port = value;
                    this.NotifyPropertyChanged("Port");
                }
            }
        }

        /// <summary>
        /// Query port number.
        /// </summary>
        public ProtocolType Protocol
        {
            get
            {
                return this.protocol;
            }

            set
            {
                if (value != this.protocol)
                {
                    this.protocol = value;
                    this.NotifyPropertyChanged("PortType");
                }
            }
        }

        /// <summary>
        /// Property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Query string representation.
        /// </summary>
        /// <returns>Query as string value.</returns>
        public override string ToString()
        {
            return $"{this.protocol} - {this.Address}:{this.Port} - [{this.Status}]";
        }

        /// <summary>
        /// Get status color (brush) based on current query status.
        /// </summary>
        /// <returns>Solid color brush.</returns>
        public Brush GetColor()
        {
            switch (this.Status)
            {
                case PortStatus.LISTENING:
                    return new SolidColorBrush(Color.FromRgb(53, 131, 56));
                case PortStatus.NOT_LISTENING:
                    return new SolidColorBrush(Color.FromRgb(21, 116, 170));
                case PortStatus.FILTERED:
                    return new SolidColorBrush(Color.FromRgb(145, 48, 48));
                case PortStatus.ERROR:
                default:
                    return new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
        }

        /// <summary>
        /// NotifyPropertyChanged invoker.
        /// </summary>
        /// <param name="info">Property information.</param>
        private void NotifyPropertyChanged(string propertyInformation)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyInformation));
        }
    }
}
