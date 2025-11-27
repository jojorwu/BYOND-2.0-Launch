using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Launcher.Services
{
    public class ServerPinger
    {
        public async Task CheckServerStatusAsync(Server server)
        {
            server.Status = "Проверка...";
            server.Ping = -1;

            using var tcpClient = new TcpClient();
            var stopwatch = new Stopwatch();

            try
            {
                var connectTask = tcpClient.ConnectAsync(server.IpAddress, server.Port);
                stopwatch.Start();

                if (await Task.WhenAny(connectTask, Task.Delay(server.Timeout)) == connectTask && !connectTask.IsFaulted)
                {
                    stopwatch.Stop();
                    server.Status = "Онлайн";
                    server.Ping = stopwatch.ElapsedMilliseconds;
                }
                else
                {
                    server.Status = "Офлайн";
                }
            }
            catch
            {
                server.Status = "Ошибка";
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
