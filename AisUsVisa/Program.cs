using System;

namespace AisUsVisa
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            VisaScrapper visaScrapper = new VisaScrapper();
            visaScrapper.Login();
            visaScrapper.StartChecking();
            Console.Read();
        }
    }
}