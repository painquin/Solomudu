using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;


namespace Solomudu
{
    public class Connection
    {
        #region Instance
        protected List<byte> InBuffer;
        protected List<byte> OutBuffer;

        public bool NeedsPrompt { get; set; }
        public Brain Brain { get; set; }
        
        Socket Sock;

        public Connection(Socket s)
        {
            Sock = s;
            InBuffer = new List<byte>();
            OutBuffer = new List<byte>();
            NeedsPrompt = true;
        }
        
        public void Write(string fmt, params object[] args)
        {
            if (NeedsPrompt == false)
            {
                OutBuffer.AddRange(new byte[] { 13, 10 });
                NeedsPrompt = true;
            }
            OutBuffer.AddRange(ASCIIEncoding.ASCII.GetBytes(String.Format(fmt, args)));
        }

        #endregion

        #region Static
        public static int Port { get; protected set; }
        
        static Socket Listener;
        static Dictionary<Socket,Connection> Connections;
        
        public static void BeginHosting(int port)
        {
            Port = port;
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener.Bind(new IPEndPoint(IPAddress.Any, Port));
            Listener.Listen(10);
            Connections = new Dictionary<Socket, Connection>();
        }

        static IEnumerable<byte> Filter(byte[] input)
        {
            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] == 255 && ++i < input.Length) // IAC
                {
                    if (input[i] == 250)
                    {
                        while (i < input.Length && input[i] != 240) ++i;
                    }
                    else
                    {
                        ++i;
                    }
                }
                else
                {
                    yield return input[i];
                }
            }
        }

        public static void UpdateNetwork(int usecTimeout) {
            var read = Connections.Keys.ToList();
            var write = Connections.Where(p => p.Value.OutBuffer.Count > 0).Select(p => p.Key).ToList();
            read.Add(Listener);

            Socket.Select(read, write, null, usecTimeout);

            foreach (var r in read)
            {
                if (r == Listener)
                {
                    Socket n = Listener.Accept();
                    Console.WriteLine("Connection from {0}\r\n", n.RemoteEndPoint);
                    Connection c = new Connection(n);
                    c.Brain = new Nanny(c);
                    Connections.Add(n, c);
                }
                else
                {
                    int d = r.Available;
                    if (d == 0)
                    {
                        // connection severed
                        // have to remove it from the dictionary
                        Connections.Remove(r);
                        // then let the brain know it's been disconnected.
                        // (TODO)
                        // for the time being, no reconnections.
                        // there's also no logins.
                        continue;
                    }
                    byte[] input = new byte[d];
                    r.Receive(input, d, SocketFlags.None);

                    // phase 1 - filter out telnet commands, ignore them - check
                    
                    // phase 2 - make sense of them?

                    Connection c;
                    if (Connections.TryGetValue(r, out c))
                    {
                        c.InBuffer.AddRange(Filter(input));
                        for (int i = 0; i < c.InBuffer.Count - 1; ++i)
                        {
                            if (c.InBuffer[i] == '\r' &&
                                c.InBuffer[i + 1] == '\n')
                            {
                                string line = ASCIIEncoding
                                    .ASCII
                                    .GetString(c.InBuffer.Take(i).ToArray());

                                c.InBuffer.RemoveRange(0, i + 2);

                                if (c.Brain == null)
                                {
                                    // erp
                                }
                                else
                                {
                                    c.Brain.OnLine(line.Trim());
                                }
                            }
                        }
                    }
                }

            }

            foreach (Socket w in write)
            {
                Connection c;
                if (Connections.TryGetValue(w, out c))
                {
                    int sent = w.Send(c.OutBuffer.ToArray());
                    c.OutBuffer.RemoveRange(0, sent);
                }
            }

            foreach (var p in Connections)
            {
                if (p.Value.NeedsPrompt && p.Value.OutBuffer.Count == 0)
                {
                    p.Value.OutBuffer
                        .AddRange(ASCIIEncoding
                            .ASCII
                            .GetBytes(p.Value.Brain.Prompt())
                        );
                    p.Value.NeedsPrompt = false;
                }
            }
        }

        public static IDictionary<Guid,Connection> GetEntityIds() {
            return Connections
                .Values
                .Where(c => c.Brain is Player)
                .ToDictionary(c => ((Player)c.Brain).EntityID);
        }

        #endregion

    }
}
