using System.Text;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Cma.Services.Ftp.Contracts;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;


namespace Cma.Services.Ftp;

public class FtpService : IFtpService
{
    private readonly ILogger<FtpService> _logger;
    private readonly ISftpClientFactory _sftpClientFactory;
    private ISftpClient? _sftpClient;

    public FtpService(ILogger<FtpService> logger, ISftpClientFactory sftpClientFactory)
    {
        _logger = logger;
        _sftpClientFactory = sftpClientFactory;
    }

    public bool IsConnected => _sftpClient is { IsConnected: true };

    public async Task<ListFilesResponse> ListFiles(ListFilesRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new ListFilesRequestValidator().ValidateAndThrowBadRequest(request);
        CheckIfConnected();

        var files =
            await _sftpClient!
                .ListDirectoryAsync(request.Path, CancellationToken.None)
                .Where(x => x.IsRegularFile)
                .ToListAsync();

        return new(files);
    }

    public Task<GetFileResponse> GetFile(GetFileRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new GetFileRequestValidator().ValidateAndThrowBadRequest(request);
        CheckIfConnected();

        using var memoryStream = new MemoryStream();
        try
        {
            _sftpClient!.DownloadFile(request.FullName, memoryStream);

            var fileBytes = memoryStream.ToArray(); 

            return Task.FromResult(new GetFileResponse(fileBytes));
        }
        catch (SftpPathNotFoundException)
        {
            _logger.LogWarningWithMetadata("File not found", new { request.FullName });
            return Task.FromResult(new GetFileResponse(null));
        }
    }

    public async Task DeleteFile(DeleteFileRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        CheckIfConnected();

        try
        {
            await _sftpClient!.DeleteFileAsync(request.FullName, CancellationToken.None);
        }
        catch (SftpPathNotFoundException)
        {
            _logger.LogWarningWithMetadata("File not found to delete", new { request.FullName });
        }
    }

    public Task DeleteFolder(DeleteFolderRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        CheckIfConnected();

        try
        {
            _sftpClient!.DeleteDirectory(request.Path);
        }
        catch (SftpPathNotFoundException)
        {
            _logger.LogWarningWithMetadata("Folder not found to delete", new { request.Path });
        }

        return Task.CompletedTask;
    }

    public Task UploadFile(UploadFileRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        CheckIfConnected();

        using var memoryStream = new MemoryStream(request.FileBytes);

        _sftpClient!.UploadFile(memoryStream, request.FullName);

        return Task.CompletedTask;
    }

    public Task CreateFolder(CreateFolderRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        CheckIfConnected();

        try
        {
            _sftpClient!.CreateDirectory(request.Path);
        }
        catch (SshException exception)
        {
            if (exception.Message == "Already exists.")
            {
                _logger.LogWarningWithMetadata("Cannot create folder. Folder already exists", new { request.Path });
            }
            else
            {
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public async Task Connect(ConnectRequest request)
    {
        new FtpServiceConnectionInfoValidator().ValidateAndThrowBadRequest(request);

        _logger.LogInformationWithMetadata(request.Clone(x =>
        {
            x.PrivateKey = "***";
            x.Passphrase = "***";
            x.Password = "***";
        }));

        var authenticationMethods = new List<AuthenticationMethod>();

        if (!string.IsNullOrWhiteSpace(request.PrivateKey))
        {
            var privateKey = FormatPrivateKey(request.PrivateKey);

            using var privateKeyStream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));

            if (!string.IsNullOrWhiteSpace(request.Passphrase))
            {
                try
                {
                    var privateKeyWithPassphraseMethod = new PrivateKeyAuthenticationMethod(
                        request.Username,
                        new PrivateKeyFile(privateKeyStream, request.Passphrase));

                    authenticationMethods.Add(privateKeyWithPassphraseMethod);
                }
                catch (Exception exception)
                {
                    throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(),
                        "Cannot parse private key with passphrase configuration.", innerException: exception);
                }
            }
            else
            {
                try
                {
                    var privateKeyMethod = new PrivateKeyAuthenticationMethod(
                        request.Username,
                        new PrivateKeyFile(privateKeyStream));

                    authenticationMethods.Add(privateKeyMethod);
                }
                catch (Exception exception)
                {
                    throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(),
                        "Cannot parse private key configuration.", innerException: exception);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            try
            {
                var passwordMethod = new PasswordAuthenticationMethod(request.Username, request.Password);

                authenticationMethods.Add(passwordMethod);
            }
            catch (Exception exception)
            {
                //NOTE: This scenario should not be possible.
                throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(),
                    "Cannot parse password configuration.",
                    innerException: exception);
            }
        }

        var connectionInfo = new ConnectionInfo(
            request.HostName,
            request.Port,
            request.Username,
            authenticationMethods.ToArray()
        );

        _sftpClient = _sftpClientFactory.Create(connectionInfo);

        await _sftpClient.ConnectAsync(CancellationToken.None);
        _logger.LogInformation(
            $"Connected to {_sftpClient.ConnectionInfo?.Username}@{_sftpClient.ConnectionInfo?.Host}:{_sftpClient.ConnectionInfo?.Port}");
    }

    public Task Disconnect()
    {
        _sftpClient?.Disconnect();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Format SSH private key
    /// </summary>
    /// <remarks>When using a private key as a single string in configuration or in key vault, the appropriate new lines are converted into spaces, which invalidates the private key. This method fixes the private key.</remarks>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public static string FormatPrivateKey(string privateKey)
    {
        string finalKey;
        try
        {
            var trimmedPrivateKey = privateKey.Replace("\r\n", "\n").Trim('\n');

            const string marker = "-----";
            var indexOfBeginStart = trimmedPrivateKey.IndexOf(marker, StringComparison.Ordinal);
            var indexOfBeginFinish = trimmedPrivateKey.IndexOf(marker, indexOfBeginStart + 1, StringComparison.Ordinal);
            var indexOfEndStart = trimmedPrivateKey.IndexOf(marker, indexOfBeginFinish + 1, StringComparison.Ordinal);
            var indexOfEndFinish = trimmedPrivateKey.IndexOf(marker, indexOfEndStart + 1, StringComparison.Ordinal);

            var begin = trimmedPrivateKey.Substring(indexOfBeginStart,
                indexOfBeginFinish - indexOfBeginStart + marker.Length);
            var end = trimmedPrivateKey.Substring(indexOfEndStart, indexOfEndFinish - indexOfEndStart + marker.Length);

            var internalKey = trimmedPrivateKey.Replace(begin, "").Replace(end, "");
            var modifiedKey = internalKey.Replace(" ", "\n");
            finalKey = begin + modifiedKey + end;
        }
        catch (Exception)
        {
            // Any error in parsing should return empty string.
            finalKey = "";
        }
        
        return finalKey;
    }

    private void CheckIfConnected()
    {
        if (_sftpClient == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(), "SftpClient is null");
        }

        if (!_sftpClient.IsConnected)
        {
            throw new BadRequestException(ErrorCode.NotConnected.ToSnakeCase(),
                "FTP Service is not connected. Please connect first.");
        }
    }
}