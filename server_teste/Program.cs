using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading.Tasks;

class ServidorCentral
{
    static async Task Main()
    {
        var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 12000);
        listener.Start();
        Console.WriteLine("[SERVIDOR CENTRAL] À escuta em 127.0.0.1:12000...");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new();

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (messageBuilder.ToString().Contains("<|EOM|>")) break;
            }

            string fullMessage = messageBuilder.ToString();
            fullMessage = fullMessage.Replace("<|EOM|>", "");

            string[] split = fullMessage.Split("<|SEP|>", 2);
            string fileName = split[0];
            string content = split[1];

            SaveDataToFile(fileName, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVIDOR CENTRAL] Erro: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    static void SaveDataToFile(string fileName, string content)
    {
        string path = Path.Combine("dados", fileName);
        Directory.CreateDirectory("dados");

        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;

        string header = "ID Wavy;Data;Hora;X;Y;Z";


        lines[0] = header;
        string[] dataLines = lines[1..];

        bool fileExists = File.Exists(path);
        if (!fileExists)
        {
            File.WriteAllText(path, header + Environment.NewLine);
        }

        File.AppendAllLines(path, dataLines);
        Console.WriteLine($"[SERVIDOR CENTRAL] Dados guardados em {path}");
    }
}