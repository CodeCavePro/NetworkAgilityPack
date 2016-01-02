namespace CodeCave.NetworkAgilityPack
{
    /// <summary>
    /// Proxy types
    /// </summary>
    public enum ProxyType
    {
        /// <summary>
        /// Forcibly bypass proxy
        /// </summary>
        None = -1,

        /// <summary>
        /// Try to use system-wide proxy
        /// </summary>
        System = 0,

        /// <summary>
        /// Regular HTTP proxy
        /// </summary>
        Http = 1,

        /// <summary>
        /// SOCKS4 proxy
        /// </summary>
        Socks4 = 4,

        /// <summary>
        /// SOCKS5 proxy
        /// </summary>
        Socks5 = 5
    }

    /// <summary>
    /// Type of HTTP requests
    /// </summary>
    public enum KnownHttpVerb
    {
        /// <summary>
        /// Means retrieve whatever information (in the form of an entity) is identified by the Request-URI
        /// </summary>
        Get = 0,

        /// <summary>
        /// Is used to request that the origin server accept the entity enclosed in the request
        /// </summary>
        Post,

        /// <summary>
        /// Requests that the enclosed entity be stored under the supplied Request-URI
        /// </summary>
        Put,

        /// <summary>
        /// Requests that the origin server delete the resource identified by the Request-URI
        /// </summary>
        Delete,

        /// <summary>
        /// Is identical to GET except that the server MUST NOT return a message-body in the response
        /// </summary>
        Head,
    }
}
