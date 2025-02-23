using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ConsoleApp28
{
    class CurrencyServer
    {
        private static Dictionary<string, double> exchangeRates = new Dictionary<string, double>
    {
        {"USD/EU",0.92}, 
        {"EU/USD",1.09}
    };

        private static object LogLock = new object();

        static void Main()
        {
            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Any, 5000);
            Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            ServerSocket.Bind(EndPoint);
            ServerSocket.Listen(10);
            Console.WriteLine($"Сервер запущен через порт {5000}...");

            while (true)
            {
                Socket ClientSocket = ServerSocket.Accept();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(ClientSocket);
            }
        }

        private static void HandleClient(object obj)
        {
            Socket ClientSocket = (Socket)obj;
            IPEndPoint ClientEndPoint = (IPEndPoint)ClientSocket.RemoteEndPoint;

            lock (LogLock)
            {
                Console.WriteLine($"{ClientEndPoint} подключился.");
                File.AppendAllText("server_log.txt", $"{ClientEndPoint} подключился.\n");
            }

            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int ByteRead = ClientSocket.Receive(buffer);
                    if (ByteRead == 0)
                    { 
                        break;
                    }
                    string request = Encoding.UTF8.GetString(buffer, 0, ByteRead).Trim();
                    request = request.Replace("\r", "").Replace("\n", "");
                    if (request.ToLower() == "exit")
                    {
                        break;
                    }

                    string response;
                    if (exchangeRates.TryGetValue(request.ToUpper(), out double rate))
                    {
                        response = rate.ToString("F2");
                    }
                       
                    else
                    {
                        response = "Error.";
                    }
                       
                    byte[] Data = Encoding.UTF8.GetBytes(response);
                    ClientSocket.Send(Data);

                    lock (LogLock)
                    {
                        Console.WriteLine($"{ClientEndPoint}: {request} -> {response}");
                        File.AppendAllText("server_log.txt", $"{ClientEndPoint}: {request} -> {response}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message}");
            }
            finally
            {
                lock (LogLock)
                {
                    Console.WriteLine($"{ClientEndPoint} отключился.");
                    File.AppendAllText("server_log.txt", $"{ClientEndPoint} отключился.\n");
                }
                ClientSocket.Close();
            }
        }
    }
}
