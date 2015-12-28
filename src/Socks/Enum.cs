using System.ComponentModel;

namespace CodeCave.NetworkAgilityPack.Socks
{
    public enum SocksType
    {
        Socks4 = 4,
        Socks5 = 5
    }

    public enum SocksV4Status
    {
        [Description("Request granted")]
        RequestGranted = 0x5a,

        [Description("Request rejected or failed")]
        RequestRejected = 0x5b,

        [Description("Request failed because client is not running identd (or not reachable from the server)")]
        RequestRejectedNoIdentd = 0x5c,

        [Description("Request failed because client's identd could not confirm the user ID string in the request")]
        RequestRejectedUnknownIdentd = 0x5d,
    }

    public enum SocksV5Status
    {
        [Description("Request granted")]
        RequestGranted = 0x00,

        [Description("General failure")]
        GeneralFailure = 0x01,

        [Description("Connection not allowed by ruleset")]
        ConnectionNotAllowed = 0x02,

        [Description("Network unreachable")]
        NetworkUnreachable = 0x03,

        [Description("Host unreachable")]
        HostUnreachable = 0x04,

        [Description("Connection refused by destination host")]
        ConnectionRefused = 0x05,

        [Description("TTL expired")]
        TtlExpired = 0x06,

        [Description("Command not supported / protocol error")]
        ProtocolError = 0x07,

        [Description("Address type not supported")]
        AddressTypeNotSupported = 0x08,
    }
}
