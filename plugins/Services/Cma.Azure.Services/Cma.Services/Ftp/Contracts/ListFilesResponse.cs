using Renci.SshNet.Sftp;

namespace Cma.Services.Ftp.Contracts;

public record ListFilesResponse(List<ISftpFile> Files);