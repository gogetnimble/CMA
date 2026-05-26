using Renci.SshNet;

namespace Cma.Services.Ftp;

public class SftpClientFactory : ISftpClientFactory
{
    public ISftpClient Create(ConnectionInfo connectionInfo)
    {
        var sftpClient = new SftpClient(connectionInfo);
        return sftpClient;
    }
}