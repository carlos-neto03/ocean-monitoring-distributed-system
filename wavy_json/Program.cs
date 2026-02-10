using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class WavyJsonFormat
{
    static Random random = new Random();

    static async Task Main()
    {
        string wavyId = "WAVY_JSON";
        string ipAgregador = "127.0.0.1";
        int port = 11000;

        // Leitura de configuração (exemplo simples)
        string preProcessamento = "JSON"; // ou "CSV"
        string volumeDadosEnviar = "15"; // exemplo de quantidade de dados
        string servidorAssociado = "127.0.0.1:12000"; // Servidor para enviar os dados

        while (true)
        {
            try
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(ipAgregador), port);
                using Socket client = new(
                    ipEndPoint.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                Console.WriteLine($"[WAVY {wavyId}] Tentando conectar ao AGREGADOR em {ipAgregador}:{port}...");
                await client.ConnectAsync(ipEndPoint);
                Console.WriteLine("[WAVY] Conectado ao AGREGADOR!");

                // Mensagem INIT com configuração adicional
                var initMessage = $"INIT:{wavyId}:AGREGADOR:HELLO:{preProcessamento}:{volumeDadosEnviar}:{servidorAssociado}<|EOM|>";
                await client.SendAsync(Encoding.UTF8.GetBytes(initMessage), SocketFlags.None);
                Console.WriteLine($"[WAVY] INIT enviado: {initMessage}");

                var buffer = new byte[1024];
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                var response = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"[WAVY] Resposta recebida: {response}");

                if (!response.StartsWith("ACK_INIT"))
                {
                    Console.WriteLine("[WAVY] Falha na handshake. Tentando novamente em 5 segundos.");
                    await Task.Delay(5000);
                    continue;
                }

                // Enviar dados em loop
                while (true)
                {
                    // Gerar os dados do sensor
                    var dados = new
                    {
                        acelerometro = new { x = GenerateAcelX(), y = GenerateAcelY(), z = GenerateAcelZ() },
                        giroscopio = new { x = GenerateGyro(), y = GenerateGyro(), z = GenerateGyro() },
                        transdutor = new { amplitude = GenerateTransdutorAmplitude(), frequencia = GenerateTransdutorFreq() },
                        hidrofone = new { tempo = GenerateHydroTempo(), amplitude = GenerateHydroAmp() }
                    };

                    // Serializar os dados para JSON
                    string json = JsonSerializer.Serialize(dados);

                    // Construir a mensagem a ser enviada
                    string mensagem = json + "<|EOM|>";
                    await client.SendAsync(Encoding.UTF8.GetBytes(mensagem), SocketFlags.None);

                    Console.WriteLine($"[WAVY {wavyId}] JSON enviado: {json}");

                    // Atraso entre os envios de dados
                    await Task.Delay(5000); // 5 segundos
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WAVY] Erro: {ex.Message}. Repetindo tentativa em 5s...");
                await Task.Delay(5000); // Atraso para tentar reconectar
            }
        }
    }
    // Geradores realistas
    static double GenerateAcelX() => Math.Round(random.NextDouble() * 2 - 1, 2);
    static double GenerateAcelY() => Math.Round(9.78 + random.NextDouble() * 0.06, 2);
    static double GenerateAcelZ() => Math.Round(random.NextDouble() * 2 - 1, 2);
    static double GenerateGyro() => Math.Round(random.NextDouble() * 500 - 250, 2);
    static double GenerateTransdutorAmplitude() => Math.Round(random.NextDouble() * 0.1, 3);
    static int GenerateTransdutorFreq() => random.Next(20, 20000);
    static double GenerateHydroTempo() => Math.Round(random.NextDouble() * 0.1, 3);
    static double GenerateHydroAmp() => Math.Round(random.NextDouble() * 0.5, 3);
}
