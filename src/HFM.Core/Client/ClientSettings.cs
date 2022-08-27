using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace HFM.Core.Client
{
    /// <summary>
    /// Client Types
    /// </summary>
    public enum ClientType
    {
        FahClient,
        [Obsolete("Do not use Legacy.")]
        Legacy
    }

    [DataContract(Namespace = "")]
    public class ClientSettings
    {
        public ClientSettings()
        {
            Port = DefaultPort;
        }

        public ClientIdentifier ClientIdentifier => new ClientIdentifier(Name, Server, Port, Guid);

        /// <summary>
        /// Gets or sets the client type.
        /// </summary>
        [DataMember(Order = 1)]
        public ClientType ClientType { get; set; }

        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        [DataMember(Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the client host name or IP address.
        /// </summary>
        [DataMember(Order = 3)]
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the client host port number.
        /// </summary>
        [DataMember(Order = 4)]
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the client host password.
        /// </summary>
        [DataMember(Order = 5)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the client unique identifier.
        /// </summary>
        [DataMember(Order = 6)]
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or set a value that determines if a client connection will be disabled.
        /// </summary>
        [DataMember(Order = 7)]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or set a value that determines if a client connection will be disabled.
        /// </summary>
        [DataMember(Order = 8, IsRequired = false)]
        public bool EnableSsh { get; set; }

        /// <summary>
        /// The stored SSH private key for connecting to a remote Linux server to obtain nvidia-smi statics
        /// <br/>REVISIT: This should be stored securely, just directly using string for now while working on proof-of-concept
        /// </summary>
        [DataMember(Order = 9, IsRequired = false)]
        public string SshUserName { get; set; } = String.Empty;

        /// <summary>
        /// The stored SSH private key for connecting to a remote Linux server to obtain nvidia-smi statics
        /// <br/>REVISIT: This should be stored securely, just directly using string for now while working on proof-of-concept
        /// </summary>
        [DataMember(Order = 10, IsRequired = false)]
        public int SshPort { get; set; } = 22;

        /// <summary>
        /// The stored SSH private key for connecting to a remote Linux server to obtain nvidia-smi statics
        /// <br/>REVISIT: This should be stored securely, just directly using string for now while working on proof-of-concept
        /// </summary>
        [DataMember(Order = 11, IsRequired = false)]
        public string SshPrivateKey { get; set; } = String.Empty;

        private const string FahClientLogFileName = "log.txt";

        public string ClientLogFileName => String.Format(CultureInfo.InvariantCulture, "{0}-{1}", Name, FahClientLogFileName);

        public const int NoPort = 0;
        /// <summary>
        /// The default Folding@Home client port.
        /// </summary>
        public const int DefaultPort = 36330;

        /// <summary>
        /// The default SSH port
        /// </summary>
        public const int DefaultSshPort = 22;

        private const string NameFirstCharPattern = "[a-zA-Z0-9\\+=\\-_\\$&^\\[\\]]";
        private const string NameMiddleCharsPattern = "[a-zA-Z0-9\\+=\\-_\\$&^\\[\\] \\.]";
        private const string NameLastCharPattern = "[a-zA-Z0-9\\+=\\-_\\$&^\\[\\]]";
        private const string LinuxUserNamePattern = @"^[a-z_]([a-z0-9_-]{0,31}|[a-z0-9_-]{0,30}\$)$";
        private const string SshRsaPemPrivateKeyPattern = @"-----BEGIN RSA PRIVATE KEY-----(\n|\r|\r\n)([0-9a-zA-Z\+\/=]{64}(\n|\r|\r\n))*([0-9a-zA-Z\+\/=]{1,63}(\n|\r|\r\n))?-----END RSA PRIVATE KEY-----";

        /// <summary>
        /// Validates the client settings name.
        /// </summary>
        public static bool ValidateName(string name)
        {
            if (name == null) return false;

            string pattern = String.Format(CultureInfo.InvariantCulture,
                "^{0}{1}+{2}$", NameFirstCharPattern, NameMiddleCharsPattern, NameLastCharPattern);
            return Regex.IsMatch(name, pattern, RegexOptions.Singleline);
        }

        /// <summary>
        /// Removes invalid characters from the client name string.
        /// </summary>
        public static string CleanName(string name)
        {
            if (name == null) return null;

            var first = new Regex(NameFirstCharPattern, RegexOptions.Singleline);
            var middle = new Regex(NameMiddleCharsPattern, RegexOptions.Singleline);
            var last = new Regex(NameLastCharPattern, RegexOptions.Singleline);

            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i == 0)
                {
                    if (first.IsMatch(c.ToString()))
                    {
                        sb.Append(c);
                    }
                }
                else if (i == name.Length - 1)
                {
                    if (last.IsMatch(c.ToString()))
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (middle.IsMatch(c.ToString()))
                    {
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks the input int to see if it is within the valid range for ports
        /// </summary>
        public static bool DoesPortLookValid(int port) => port is > 0 and < 65535;

        /// <summary>
        /// Checks the input string to see if it looks like a valid Linux user name
        /// <br/>NOTE: Only validates the format, not if it's an actual valid user on a system
        /// </summary>
        public static bool DoesLinuxUserNameLookValid(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return false;

            return Regex.IsMatch(name, LinuxUserNamePattern, RegexOptions.Singleline);
        }

        /// <summary>
        /// Checks the input string to see if it looks like a valid SSH RSA private key
        /// <br/>NOTE: Only validates the format, does not try to parse the file to verify it is a valid key
        /// </summary>
        public static bool DoesSshRsaPrivateKeyLookValid(string privateKey_PEM)
        {
            if (String.IsNullOrWhiteSpace(privateKey_PEM)) return false;

            return Regex.IsMatch(privateKey_PEM, SshRsaPemPrivateKeyPattern, RegexOptions.Multiline);
        }
    }
}
