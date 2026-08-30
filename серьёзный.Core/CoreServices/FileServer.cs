using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace серьёзный.Core.CoreServices
{
    public class FileServer
    {
        private readonly TcpListener listener;

        public FileServer()
        {
            listener =
                new TcpListener(
                    IPAddress.Any,
                    CoreConstants.NetworkConstants.FilePort);
        }

        public void Start()
        {
            listener.Start();

            Task.Run(ListenLoop);
        }

        private async Task ListenLoop()
        {
            while (true)
            {
                var client =
                    await listener.AcceptTcpClientAsync();

                _ = Task.Run(() => Handle(client));
            }
        }

        private async Task Handle(
            TcpClient client)
        {
            try
            {
                using var stream =
                    client.GetStream();

                using var reader =
                    new BinaryReader(stream);

                using var writer =
                    new BinaryWriter(stream);

                var json =
                    reader.ReadString();

                var header =
                    JsonSerializer.Deserialize<FileTransferHeader>(json);

                if (header == null)
                    return;

                var path =
                    reader.ReadString();

                if (!File.Exists(path))
                {
                    writer.Write(false);
                    return;
                }

                writer.Write(true);

                using var file =
                    File.OpenRead(path);

                var buffer =
                    new byte[64 * 1024];

                int read;

                while ((read =
                    await file.ReadAsync(buffer)) > 0)
                {
                    await stream.WriteAsync(
                        buffer.AsMemory(0, read));
                }
            }
            catch
            {
            }
        }
    }
}