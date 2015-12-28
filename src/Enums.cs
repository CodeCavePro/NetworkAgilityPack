using System;
using System.IO;

namespace CodeCave.NetworkAgilityPack
{
    /// <summary>
    /// Proxy types
    /// </summary>
    public enum ProxyType
    {
        None = -1,
        System = 0,
        Http = 1,
        Socks4 = 4,
        Socks5 = 5
    }

    /// <summary>
    /// Type of HTTP requests
    /// </summary>
    public enum KnownHttpVerb
    {
        Get = 0,
        Post,
        Put,
        Delete,
        Head,
    }
}
