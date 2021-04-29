using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Online_Blackjack_Server
{
    abstract class SessionHandler
    {
        public static List<Session> activeGameSessions = new List<Session>();
        private static int currentGameId = 0;
        const int DELAY = 100; // 100 ms delay before a new session starts to avoid Android UI not loading in
        static Session newSession;

        public static void NewSession(params Client[] clients)
        {
            newSession = new Session(clients);
            newSession.gameId = currentGameId++;
            activeGameSessions.Add(newSession);

            foreach (Client cl in clients)
            {
                cl.currentSession = newSession;
            }

            System.Timers.Timer timer = new System.Timers.Timer(DELAY);
            timer.AutoReset = false;
            timer.Elapsed += new ElapsedEventHandler(Start);
            timer.Start();
        }

        private static void Start(Object sender, ElapsedEventArgs e)
        {
            var t = Task.Run(() => newSession.StartGame());
        }
    }
}
