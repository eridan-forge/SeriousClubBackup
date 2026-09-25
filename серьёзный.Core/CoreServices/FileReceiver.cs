using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using серьёзный.Core.CoreEvents;

namespace серьёзный.Core.CoreServices
{
    public static class FileReceiver
    {
        public static async Task<string?> Download(
            string host,
            FileTransferHeader header,
            string remotePath,
            string saveFolder)
        {
            try
            {
                Directory.CreateDirectory(saveFolder);

                using var client =
                    new TcpClient();

                await client.ConnectAsync(
                    host,
                    CoreConstants.NetworkConstants.FilePort);

                using var stream =
                    client.GetStream();

                using var reader =
                    new BinaryReader(stream);

                using var writer =
                    new BinaryWriter(stream);

                writer.Write(
                    JsonSerializer.Serialize(header));

                writer.Write(remotePath);

                if (!reader.ReadBoolean())
                    return null;

                var destination =
                    Path.Combine(
                        saveFolder,
                        header.FileName);

                using var file =
                    File.Create(destination);

                var buffer =
                    new byte[64 * 1024];

                long received = 0;

                while (received < header.Size)
                {
                    var count =
                        await stream.ReadAsync(buffer);

                    if (count <= 0)
                        break;

                    await file.WriteAsync(
                        buffer.AsMemory(0, count));

                    received += count;

                    TransferProgressEvent.RaiseProgress(
                        header.JobId,
                        received * 100d / header.Size);
                }

                TransferProgressEvent.RaiseFinished(
                    header.JobId);

                return destination;
            }
            catch
            {
                TransferProgressEvent.RaiseFailed(
                    header.JobId);

                return null;
            }
        }
    }
}