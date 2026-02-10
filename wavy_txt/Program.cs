using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class SensorSimulator
{
    static Random random = new Random();

    static async Task Main()
    {
        string wavyId = "WAVY123";
        string ipAgregador = "127.0.0.1";
        int port = 11000;

        // Configurações adicionais para enviar
        string preProcessamento = "CSV"; // ou "JSON"
        string volumeDadosEnviar = "15"; // exemplo de quantidade de dados
        string servidorAssociado = "127.0.0.1:12000"; // Servidor para enviar os dados

        while (true)
        {
            try
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(ipAgregador), port);
                using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                Console.WriteLine($"[WAVY {wavyId}] Tentando conectar ao AGREGADOR em {ipAgregador}:{port}...");
                await client.ConnectAsync(ipEndPoint);
                Console.WriteLine("[WAVY] Conectado ao AGREGADOR!");

                // Enviar mensagem INIT com as configurações
                var initMessage = $"INIT:{wavyId}:AGREGADOR:HELLO:{preProcessamento}:{volumeDadosEnviar}:{servidorAssociado}<|EOM|>";
                await client.SendAsync(Encoding.UTF8.GetBytes(initMessage), SocketFlags.None);
                Console.WriteLine($"[WAVY] INIT enviado: {initMessage}");

                // Processar a resposta do AGREGADOR
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
                    var dataToSend = $"{GenerateAccelerometerData()};{GenerateGyroscopeData()};{GenerateAcousticTransducerData()};{GenerateHydrophoneData()}<|EOM|>";
                    await client.SendAsync(Encoding.UTF8.GetBytes(dataToSend), SocketFlags.None);
                    Console.WriteLine($"[WAVY {wavyId}] Dados enviados: {dataToSend}");

                    await Task.Delay(5000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WAVY] Erro: {ex.Message}. Repetindo tentativa em 5s...");
                await Task.Delay(5000);
            }
        }
    }


    static string GenerateAccelerometerData() => $"{(random.NextDouble() * 2 - 1):F2};{(9.78 + random.NextDouble() * 0.06):F2};{(random.NextDouble() * 2 - 1):F2}";
    static string GenerateGyroscopeData() => $"{(random.NextDouble() * 500 - 250):F2};{(random.NextDouble() * 500 - 250):F2};{(random.NextDouble() * 500 - 250):F2}";
    static string GenerateAcousticTransducerData() => $"{(random.NextDouble() * 0.1):F3};{random.Next(20, 20000)}";
    static string GenerateHydrophoneData() => $"{(random.NextDouble() * 0.1):F3};{(random.NextDouble() * 0.5):F3}";
}
