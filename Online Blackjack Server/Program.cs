namespace Online_Blackjack_Server
{
    class Program
    {
        static Server server;
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Blackjack bl = new Blackjack();
            bl.Start();
            server = new Server();
            await server.Start();
        }
    }
}
