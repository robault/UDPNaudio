using Client;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static string client = "192.168.1.139";
        static int port = 10100;

        static WaveIn waveIn;
        static UdpClient udpSender;
        static INetworkChatCodec selectedCodec;

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
            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 50;
            //Microphone.DeviceNumber = inputDeviceNumber; // or just set default device in windows
            waveIn.WaveFormat = codec.RecordFormat;
            waveIn.DataAvailable += waveIn_DataAvailable;
            udpSender = new UdpClient();
            udpSender.Connect(endPoint);
            waveIn.StartRecording();
        }

        static void Disconnect()
        {
            waveIn.DataAvailable -= waveIn_DataAvailable;
            waveIn.StopRecording();
            udpSender.Close();
            waveIn.Dispose();
            selectedCodec.Dispose();
        }
        
        static void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] encoded = selectedCodec.Encode(e.Buffer, 0, e.BytesRecorded);
            udpSender.Send(encoded, encoded.Length);
        }
    }
}
