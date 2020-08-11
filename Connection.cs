using System;
using System.Net;
using System.Net.Sockets;

namespace ProgramABB
{
    class Connection
    {
        public bool Read(String addressIP)
        {
            try
            {
                WebClient webClient = new WebClient();
                ReceivedData = webClient.DownloadString("http://" + addressIP + "/");
                webClient = null;
            }
            catch (Exception)
            {
                return false;
            }

            SortData();

            return true;
        }

        public bool Send(String addressIP, String message)
        {
            try
            {
                int port = 80;
                TcpClient client = new TcpClient(addressIP, port);

                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                stream.Close();
                client.Close();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void SortData()
        {
            ReceivedData = ReceivedData.Replace(Environment.NewLine, String.Empty);
        }

        public String ReceivedData { get; private set; }
    }
}
