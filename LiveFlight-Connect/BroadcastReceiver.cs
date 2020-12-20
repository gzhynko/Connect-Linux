//
//  
//  LiveFlight Connect
//
//  broadcastreceiver.cs
//  Copyright © 2015 mlaban. All rights reserved.
//
//  Licensed under GPL-V3.
//  https://github.com/LiveFlightApp/Connect-Windows/blob/master/LICENSE
//

using System;
using System.Net;
using System.Net.Sockets;

namespace LiveFlight
{
    public class BroadcastReceiver
    {
        private UdpClient udp = new UdpClient(15001);

        public event EventHandler DataReceived = delegate { };

        public void StartListening()
        {
            Console.WriteLine("Starting listening server...");
            udp.BeginReceive(Receive, new object());
            //Console.WriteLine(udp.Available);
        }

        private void Receive(IAsyncResult ar)
        {
            try
            {
                var ip = new IPEndPoint(IPAddress.Any, 15001);

                if (udp != null)
                {
                    var bytes = udp.EndReceive(ar, ref ip);
                    Console.WriteLine("Received {0} bytes", bytes.Length);
                    if (bytes.Length != 0) DataReceived(bytes, EventArgs.Empty);

                    if (udp != null) udp.BeginReceive(Receive, new object());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while reading UDP data: {0}", ex);
            }
        }

        internal void Stop()
        {
            Console.WriteLine("Stopping UDP Receiver");
            try
            {
                udp.Close();
                udp = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while stopping UDP Client: {0}", ex);
            }
        }
    }
}