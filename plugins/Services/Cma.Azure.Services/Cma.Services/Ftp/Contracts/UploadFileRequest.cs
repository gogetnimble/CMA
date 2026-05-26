namespace Cma.Services.Ftp.Contracts;

public record UploadFileRequest(string FullName, byte[] FileBytes);