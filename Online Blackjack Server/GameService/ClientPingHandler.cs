using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace Online_Blackjack_Server
{

    // Will check periodically to remove clients if they have not been pinging back in a while
    // If they don't ping back after 3 tries, remove them
    class ClientPingHandler
    {
        const int TIME_PERIOD = 60000; // Check every 60 seconds
        long timeChecked;
        const int THRESHOLD = 5000; // Extra 5 seconds to account for latency

        System.Timers.Timer timer;

        public void Start()
        {
            StartLoop();
        }

        // Will check CheckClients() every TIME_PERIOD
        private void StartLoop()
        {
            timer = new System.Timers.Timer(TIME_PERIOD);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(CheckClients);
            timer.Start();
        }

        private void CheckClients(object sender, ElapsedEventArgs e)
        {
            timeChecked = Client.GetCurrentMilli() - TIME_PERIOD;
            foreach (Client client in Server.GetActiveClients().Values)
            {
                if ((client.lastTimePinged - timeChecked) < -THRESHOLD)
                {
                    client.Ping();
                    client.numOfRetries++;
                } else if ((client.lastTimePinged - timeChecked) >= -THRESHOLD)
                {
                    client.numOfRetries = 0;
                }

                if (client.numOfRetries > 3)
                {
                    Console.WriteLine($"Client {client.uniqueID} has been removed for inactivity");
                    Server.RemoveClient(client.uniqueID);
                }
            }

            // Resend lobby info
            ResendLobbyInfo();
        }

        private void ResendLobbyInfo()
        {
            foreach (Client client in Server.GetActiveClients().Values)
            {
                client.SendLobbyInfo();
            }
        }
    }
}
