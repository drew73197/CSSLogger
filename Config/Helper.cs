using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CSSChatLogger
{
    public class Helper
    {
        public static async Task SendToUDPServer(string UDPServerIP, int UDPServerPort, string message, ILogger logger)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                try
                {
                    logger.LogInformation("Preparing to send message to UDP server.");
                    // Convert the message string to bytes
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                    // Create an endpoint with the server IP and port
                    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(UDPServerIP), UDPServerPort);

                    // Send the message bytes to the UDP server
                    await udpClient.SendAsync(messageBytes, messageBytes.Length, serverEndpoint);
                    logger.LogInformation("Message sent to the UDP server.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"An error occurred: {ex.Message}");
                }
            }
        }

        public static void ClearVariables()
        {
            Globals.Client_Text1.Clear();
            Globals.Client_Text2.Clear();
        }
    }
}
