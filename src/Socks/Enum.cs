using System.ComponentModel;

namespace CodeCave.NetworkAgilityPack.Socks
{
    /// <summary>
    /// Socks protocol versions
    /// </summary>
    public enum SocksType
    {
        /// <summary>
        /// The Socks 4
        /// </summary>
        Socks4 = 4,

        /// <summary>
        /// The Socks 5
        /// </summary>
        Socks5 = 5
    }

    /// <summary>
    /// Socks 4 handshake statuses
    /// </summary>
    public enum SocksV4Status
    {
        /// <summary>
        /// Request granted
        /// </summary>
        [Description("Request granted")]
        RequestGranted = 0x5a,

        /// <summary>
        /// Request rejected or failed
        /// </summary>
        [Description("Request rejected or failed")]
        RequestRejected = 0x5b,

        /// <summary>
        /// Request failed because client is not running identd (or not reachable from the server)
        /// </summary>
        [Description("Request failed because client is not running identd (or not reachable from the server)")]
        RequestRejectedNoIdentd = 0x5c,

        /// <summary>
        /// Request failed because client's identd could not confirm the user ID string in the request
        /// </summary>
        [Description("Request failed because client's identd could not confirm the user ID string in the request")]
        RequestRejectedUnknownIdentd = 0x5d,
    }

    /// <summary>
    /// Socks 5 handshake statuses
    /// </summary>
    public enum SocksV5Status
    {
        /// <summary>
        /// Request granted
        /// </summary>
        [Description("Request granted")]
        RequestGranted = 0x00,

        /// <summary>
        /// General failure
        /// </summary>
        [Description("General failure")]
        GeneralFailure = 0x01,

        /// <summary>
        /// Connection not allowed by ruleset
        /// </summary>
        [Description("Connection not allowed by ruleset")]
        ConnectionNotAllowed = 0x02,

        /// <summary>
        /// Network unreachable
        /// </summary>
        [Description("Network unreachable")]
        NetworkUnreachable = 0x03,

        /// <summary>
        /// Host unreachable
        /// </summary>
        [Description("Host unreachable")]
        HostUnreachable = 0x04,

        /// <summary>
        /// Connection refused by destination host
        /// </summary>
        [Description("Connection refused by destination host")]
        ConnectionRefused = 0x05,

        /// <summary>
        /// TTL expired
        /// </summary>
        [Description("TTL expired")]
        TtlExpired = 0x06,

        /// <summary>
        /// Command not supported / protocol error
        /// </summary>
        [Description("Command not supported / protocol error")]
        ProtocolError = 0x07,

        /// <summary>
        /// Address type not supported
        /// </summary>
        [Description("Address type not supported")]
        AddressTypeNotSupported = 0x08,
    }
}
