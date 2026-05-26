using System.Text.Json.Serialization;

namespace Cma.Services.Ftp.Contracts;


public record ConnectRequest : IFtpServiceConnectionInfo
{
    public string Password { get; set; }
    public string PrivateKey { get; set; }
    public string Passphrase { get; set; }
    public string HostName { get; }
    public string Username { get; }
    public int Port { get; }

    [JsonConstructor]
    public ConnectRequest(string HostName, string Username, int Port, string Password, string PrivateKey, string Passphrase)
    {
        this.HostName = HostName;
        this.Username = Username;
        this.Port = Port;
        this.Password = Password;
        this.PrivateKey = PrivateKey;
        this.Passphrase = Passphrase;
    }

    public ConnectRequest(IFtpServiceConnectionInfo connectionInfo) : this(
        connectionInfo.HostName, 
        connectionInfo.Username,
        connectionInfo.Port,
        connectionInfo.Password,
        connectionInfo.PrivateKey,
        connectionInfo.Passphrase
        )
    {
        
    }
}