using Renci.SshNet;

namespace Cma.Services.Ftp;

public interface ISftpClientFactory
{
    ISftpClient Create(ConnectionInfo connectionInfo);
}