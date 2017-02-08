using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static string client = "192.168.1.121";
        static int port = 10100;

        static DirectSoundOut waveOut;
        static BufferedWaveProvider waveProvider;
        static UdpClient udpListener;
        static INetworkChatCodec selectedCodec;
        static bool connected;

        static void Main(string[] args)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(client), port);
            selectedCodec = new G722ChatCodec();
            Connect(endPoint, 0, selectedCodec);
            Console.WriteLine(String.Format("Connected to: {0}:{1}", client, port));
            Console.ReadLine();
            Disconnect();
        }

        static void Connect(IPEndPoint endPoint, int inputDeviceNumber, INetworkChatCodec codec)
        {
            udpListener = new UdpClient();
            // if running both from the same computer: (otherwise comment these next two out)
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.Bind(endPoint);

            waveOut = new DirectSoundOut();
            waveProvider = new BufferedWaveProvider(codec.RecordFormat);
            waveOut.Init(waveProvider);
            waveOut.Play();

            var state = new ListenerThreadState { Codec = codec, EndPoint = endPoint };
            ThreadPool.QueueUserWorkItem(ListenerThread, state);
        }

        static void Disconnect()
        {
            udpListener.Close();
            waveOut.Dispose();
            selectedCodec.Dispose();
        }

        class ListenerThreadState
        {
            public IPEndPoint EndPoint { get; set; }
            public INetworkChatCodec Codec { get; set; }
        }

        static void ListenerThread(object state)
        {
            var listenerThreadState = (ListenerThreadState)state;
            var endPoint = listenerThreadState.EndPoint;
            try
            {
                while (connected)
                {
                    byte[] b = udpListener.Receive(ref endPoint);
                    byte[] decoded = listenerThreadState.Codec.Decode(b, 0, b.Length);
                    waveProvider.AddSamples(decoded, 0, decoded.Length);
                }
            }
            catch (SocketException)
            {
                // usually not a problem - just means we have disconnected
            }
        }
    }
}
