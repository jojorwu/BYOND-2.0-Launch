namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string serverIp = "127.0.0.1";
            int serverPort = 7777;

            if (args.Length == 2)
            {
                serverIp = args[0];
                int.TryParse(args[1], out serverPort);
            }

            var game = new Game(serverIp, serverPort);
            game.Run();
        }
    }
}
