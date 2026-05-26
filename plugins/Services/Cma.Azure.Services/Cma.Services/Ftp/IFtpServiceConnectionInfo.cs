namespace Cma.Services.Ftp;

public interface IFtpServiceConnectionInfo
{
    string HostName { get; }
    string Username { get; }
    int Port { get; }
    string Password { get; set; }
    string PrivateKey { get; set; }
    string Passphrase { get; set; }
}