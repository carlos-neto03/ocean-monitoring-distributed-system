using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Linq;

class Agregador
{
    static async Task Main()
    {
        string ipAgregador = "127.0.0.1";
        int port = 11000;
        var ipEndPoint = new IPEndPoint(IPAddress.Parse(ipAgregador), port);

        using Socket listener = new(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        listener.Bind(ipEndPoint);
        listener.Listen(10);

        Console.WriteLine($"[AGREGADOR] Aguardando conexões em {ipAgregador}:{port}...");

        _ = Task.Run(() => ShowMenu());

        while (true)
        {
            Socket handler = await listener.AcceptAsync();
            _ = Task.Run(() => HandleWavy(handler));
            
        }
        
    }

    static async Task HandleWavy(Socket wavySocket)
    {
        try
        {
            var buffer = new byte[1024];
            StringBuilder fullMessage = new();
            bool isInitDone = false;
            string currentWavyId = "UNKNOWN";

            while (true)
            {
                int received = await wavySocket.ReceiveAsync(buffer, SocketFlags.None);
                if (received == 0)
                {
                    Console.WriteLine("[AGREGADOR] A WAVY fechou a conexão.");
                    break;
                }

                string chunk = Encoding.UTF8.GetString(buffer, 0, received);
                fullMessage.Append(chunk);

                while (fullMessage.ToString().Contains("<|EOM|>"))
                {
                    string allData = fullMessage.ToString();
                    int eomIndex = allData.IndexOf("<|EOM|>") + 7;
                    string message = allData.Substring(0, eomIndex - 7);
                    fullMessage.Remove(0, eomIndex);

                    if (!isInitDone && message.StartsWith("INIT"))
                    {
                        string[] parts = message.Split(':');
                        currentWavyId = parts[1];
                        string preProcessamento = parts[4]; // CSV ou JSON
                        string volumeDados = parts[5];
                        string servidorAssociado = parts[6];

                        // Armazenar a configuração no arquivo CSV
                        SaveWavyConfiguration(currentWavyId, preProcessamento, volumeDados, servidorAssociado);

                        string response = $"ACK_INIT:AGREGADOR:{currentWavyId}:OK<|EOM|>";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        await wavySocket.SendAsync(responseBytes, SocketFlags.None);

                        Console.WriteLine($"[AGREGADOR] Enviou ACK_INIT para {currentWavyId}: \"{response}\"");

                        isInitDone = true;
                    }
                    else
                    {
                        DateTime now = DateTime.Now;

                        if (message.TrimStart().StartsWith("{") && message.TrimEnd().EndsWith("}"))
                        {
                            ProcessAndSaveDataFromJson(currentWavyId, now, message);
                        }
                        else
                        {
                            ProcessAndSaveDataFromCsv(currentWavyId, now, message);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR] Erro: {ex.Message}");
        }
    }

    static void ProcessAndSaveDataFromCsv(string wavyId, DateTime timestamp, string data)
    {
        var parts = data.Split(';');
        if (parts.Length != 10) return;

        string date = timestamp.ToString("dd/MM/yyyy");
        string time = timestamp.ToString("HH:mm:ss");

        AppendAndCheckCsv("acelerometro.csv", "ID Wavy;Data;Hora;X;Y;Z", $"{wavyId};{date};{time};{parts[0]};{parts[1]};{parts[2]}", wavyId);
        AppendAndCheckCsv("giroscopio.csv", "ID Wavy;Data;Hora;X;Y;Z", $"{wavyId};{date};{time};{parts[3]};{parts[4]};{parts[5]}", wavyId);
        AppendAndCheckCsv("transdutor.csv", "ID Wavy;Data;Hora;Amplitude;Frequencia", $"{wavyId};{date};{time};{parts[6]};{parts[7]}", wavyId);
        AppendAndCheckCsv("hidrofone.csv", "ID Wavy;Data;Hora;Tempo;Amplitude", $"{wavyId};{date};{time};{parts[8]};{parts[9]}", wavyId);

        // Atualizar status da WAVY
        UpdateWavyStatus(wavyId, "active", "acelerometro, giroscopio, transdutor, hidrofone", DateTime.Now);
    }

    static void ProcessAndSaveDataFromJson(string wavyId, DateTime timestamp, string data)
    {
        string date = timestamp.ToString("dd/MM/yyyy");
        string time = timestamp.ToString("HH:mm:ss");
        try
        {
            var root = JsonDocument.Parse(data).RootElement;

            var acel = root.GetProperty("acelerometro");
            AppendAndCheckCsv("acelerometro.csv", "ID Wavy;Data;Hora;X;Y;Z", $"{wavyId};{date};{time};{acel.GetProperty("x")};{acel.GetProperty("y")};{acel.GetProperty("z")}", wavyId);

            var giro = root.GetProperty("giroscopio");
            AppendAndCheckCsv("giroscopio.csv", "ID Wavy;Data;Hora;X;Y;Z", $"{wavyId};{date};{time};{giro.GetProperty("x")};{giro.GetProperty("y")};{giro.GetProperty("z")}", wavyId);

            var transd = root.GetProperty("transdutor");
            AppendAndCheckCsv("transdutor.csv", "ID Wavy;Data;Hora;Amplitude;Frequencia", $"{wavyId};{date};{time};{transd.GetProperty("amplitude")};{transd.GetProperty("frequencia")}", wavyId);

            var hidro = root.GetProperty("hidrofone");
            AppendAndCheckCsv("hidrofone.csv", "ID Wavy;Data;Hora;Tempo;Amplitude", $"{wavyId};{date};{time};{hidro.GetProperty("tempo")};{hidro.GetProperty("amplitude")}", wavyId);

            // Atualizar status da WAVY
            UpdateWavyStatus(wavyId, "active", "acelerometro, giroscopio, transdutor, hidrofone", DateTime.Now);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR] ERRO ao processar JSON: {ex.Message}");
        }
    }

    static void UpdateWavyStatus(string wavyId, string status, string dataTypes, DateTime lastSync)
    {
        string filePath = "wavy_config.csv";
        bool fileExists = File.Exists(filePath);

        if (!fileExists)
        {
            File.WriteAllText(filePath, "WAVY_ID;Status;Sensores;Ultima Sincronizacao\n");
        }

        var lines = File.ReadAllLines(filePath).ToList();
        var existingLine = lines.FirstOrDefault(line => line.StartsWith(wavyId + ";"));

        if (existingLine != null)
        {
            lines.Remove(existingLine);
        }

        string newLine = $"{wavyId};{status};{dataTypes};{lastSync:dd/MM/yyyy HH:mm:ss}";
        lines.Add(newLine);

        File.WriteAllLines(filePath, lines);
    }

    static void SaveWavyConfiguration(string wavyId, string preProcessamento, string volumeDados, string servidorAssociado)
    {
        string filePath = "configuracoes_wavy.csv";
        bool fileExists = File.Exists(filePath);

        // Se o arquivo não existir, cria com o cabeçalho
        if (!fileExists)
        {
            File.WriteAllText(filePath, "WAVY_ID;pre_processamento;volume_dados_enviar;servidor_associado\n");
        }

        // Adicionar a nova linha de configuração
        string newLine = $"{wavyId};{preProcessamento};{volumeDados};{servidorAssociado}";
        File.AppendAllText(filePath, newLine + Environment.NewLine);

        Console.WriteLine($"[AGREGADOR] Configuração da WAVY {wavyId} salva.");
    }

    static void AppendAndCheckCsv(string filePath, string header, string newLine, string wavyId)
    {
        bool fileExists = File.Exists(filePath);

        // Se o ficheiro não existir, criamos com o cabeçalho
        if (!fileExists)
        {
            File.WriteAllText(filePath, header + Environment.NewLine);
        }

        // Escrevemos a nova linha com os dados
        File.AppendAllText(filePath, newLine + Environment.NewLine);

        // Verificamos quantas linhas tem (cabeçalho + dados)
        var lines = File.ReadAllLines(filePath);

        if (lines.Length >= 16) // 1 cabeçalho + 15 linhas de dados
        {
            // Enviar para o servidor
            string contentToSend = File.ReadAllText(filePath);
            Task.Run(() => NotifyServidorCentral(contentToSend, Path.GetFileName(filePath)));

            // Limpar o ficheiro e voltar a colocar apenas o cabeçalho
            File.WriteAllText(filePath, lines[0] + Environment.NewLine);
        }
    }

    static async Task NotifyServidorCentral(string content, string sensorFile)
    {
        try
        {
            using var client = new TcpClient("127.0.0.1", 12000);
            using NetworkStream stream = client.GetStream();
            string message = $"{sensorFile}<|SEP|>{content}<|EOM|>";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(bytes, 0, bytes.Length);

            Console.WriteLine($"[AGREGADOR] Dados enviados para o SERVIDOR CENTRAL: {sensorFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR] Erro ao enviar para servidor central: {ex.Message}");
        }
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("\n===== MENU AGREGADOR =====");
            Console.WriteLine("1. Listar todas as WAVYs");
            Console.WriteLine("2. Ver estado de uma WAVY específica");
            Console.WriteLine("0. Sair do menu");
            Console.Write("Escolha uma opção: ");
            string input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    ListAllWavys();
                    break;
                case "2":
                    Console.Write("ID da WAVY: ");
                    string id = Console.ReadLine();
                    ShowWavyStatus(id);
                    break;
                case "0":
                    Console.WriteLine("A sair do menu...\n");
                    return;
                default:
                    Console.WriteLine("Opção inválida!");
                    break;
            }
        }
    }

    static void ListAllWavys()
    {
        string filePath = "wavy_config.csv";
        if (!File.Exists(filePath))
        {
            Console.WriteLine("Nenhuma WAVY registada.");
            return;
        }

        var lines = File.ReadAllLines(filePath);
        Console.WriteLine("\n--- Lista de WAVYs ---");

        foreach (var line in lines.Skip(1)) // Ignorar o cabeçalho
        {
            var parts = line.Split(';');
            if (parts.Length >= 1)
            {
                Console.WriteLine($"• {parts[0]}");
            }
        }
    }


    static void ShowWavyStatus(string wavyId)
    {
        string filePath = "wavy_config.csv";
        if (!File.Exists(filePath))
        {
            Console.WriteLine("Nenhuma WAVY registada.");
            return;
        }

        var lines = File.ReadAllLines(filePath);
        var line = lines.FirstOrDefault(l => l.StartsWith(wavyId + ";"));

        if (line != null)
        {
            var parts = line.Split(';');
            Console.WriteLine($"\n--- Estado da WAVY {wavyId} ---");
            Console.WriteLine($"Status       : {parts[1]}");
            Console.WriteLine($"Sensores     : {parts[2]}");
            Console.WriteLine($"Último Sync  : {parts[3]}");
        }
        else
        {
            Console.WriteLine($"WAVY com ID {wavyId} não encontrada.");
        }
    }


}