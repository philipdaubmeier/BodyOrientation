using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Windows;

namespace BodyOrientationLib
{
    public enum Context
    {
        WhileStartingListener,
        WhileReceivingData
    }

    public class ValuesReceivedEventArgs<T> : EventArgs
    {
        public T SensorReading { get; set; }
    }

    public class ExceptionOccuredEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public Context Context { get; set; }
    }

    /// <summary>
    /// Interface for a sensor reading (classes that hold values of sensor measurements).
    /// </summary>
    public interface ISensorReading
    {
        /// <summary>
        /// Has to return a constant number of values this ISensorReading class expects.
        /// </summary>
        int NumSensorValues { get; }

        void SetSensorValues(float[] values);
    }

    /// <summary>
    /// A TCP Server, that accepts incoming connections from the Windows Phone
    /// 'Sensor Emitter' App, and packs all received data into SensorReading
    /// objects.
    ///
    /// Usage: Create a SensorServer&lt;SensorEmitterReading&gt; object, connect to both 
    /// ValuesReceived and ExceptionOccured events and start the server with 
    /// the Start method.
    /// </summary>
    /// <typeparam name="T">A class, which has to inherit from ISensorReading 
    /// and supply a parameterless constructor.</typeparam>
    public class SensorServer<T> : IDisposable
        where T : ISensorReading, new()
    {
        private const int MagicNumber = 0x42fea723;

        public const int DefaultTcpPort = 3547;

        private volatile bool alive = true;
        private Thread listenThread;

        public event EventHandler<ValuesReceivedEventArgs<T>> ValuesReceived;
        public event EventHandler<ExceptionOccuredEventArgs> ExceptionOccured;

        private int numSensorValues;

        public SensorServer()
        {
            numSensorValues = new T().NumSensorValues;
        }

        /// <summary>
        /// Starts listening on the TCP port 3547 and awaits phones that connect to it
        /// via the Windows Phone 'Sensor Emitter' App.
        ///
        /// If any clients connects and sends data, the ValuesReceived event is raised
        /// for each packet that arrives successfully. If any error occurs in the process,
        /// or while starting the TCP server, the ExceptionOccured event is fired.
        /// Make sure to connect to these two events.
        /// </summary>
        public void Start() { Start(SensorServer<T>.DefaultTcpPort); }

        /// <summary>
        /// Starts listening on the given TCP port and awaits phones that connect to it
        /// via the Windows Phone 'Sensor Emitter' App.
        ///
        /// If any clients connects and sends data, the ValuesReceived event is raised
        /// for each packet that arrives successfully. If any error occurs in the process,
        /// or while starting the TCP server, the ExceptionOccured event is fired.
        /// Make sure to connect to these two events.
        /// </summary>
        /// <param name="tcpPort">The port to listen to.</param>
        public void Start(int tcpPort)
        {
            listenThread = new Thread(new ParameterizedThreadStart(ListenForClients));
            listenThread.IsBackground = true;
            listenThread.Start(tcpPort);
        }

        /// <summary>
        /// Thread, that waits for incoming connections. For each connection, a
        /// seperate thread is started then.
        /// </summary>
        private void ListenForClients(object port)
        {
            var tcpListener = new TcpListener(IPAddress.Any, (int)port);

            // Try to start the listener
            try { tcpListener.Start(); }
            catch (SocketException sockEx)
            {
                OnException(sockEx, Context.WhileStartingListener);
            }

            while (alive)
            {
                // Blocks until a client has connected to the server
                TcpClient client = tcpListener.AcceptTcpClient();

                // Create a thread to handle communication with connected client
                var clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.IsBackground = true;
                clientThread.Start(client);
            }

            try { tcpListener.Stop(); }
            catch (SocketException) { }
        }

        /// <summary>
        /// Thread, that handles the actual communication after a client connected
        /// </summary>
        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            // Write magic number as a greeting and to signal we are a compatible 'sensor emitter' server
            clientStream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(MagicNumber)), 0, 4);

            // Read from the incoming TCP stream
            using (BinaryReader br = new BinaryReader(clientStream))
            {
                bool canRead = true;
                while (canRead && alive)
                {
                    try
                    {
                        int length = IPAddress.NetworkToHostOrder(br.ReadInt32());

                        if (length < 0 || length != numSensorValues)
                        {
                            if (length < 0)
                                OnException(new ArgumentOutOfRangeException("The format of " +
                                    "the TCP stream is incorrect. It said 'a negative number " +
                                    "of items is going to follow this', which obviously does " +
                                    "not make sense, as exactly " + numSensorValues +
                                    " values are expected."), Context.WhileReceivingData);
                            else
                                OnException(new ArgumentOutOfRangeException("The format of " +
                                    "the TCP stream is incorrect. It said '" + length +
                                    " items are going to be in this single measurement " +
                                    "package', which does not match the expected value. " +
                                    "Valid packages from the SensorEmitter App have " +
                                    "exactly " + numSensorValues + " values."),
                                    Context.WhileReceivingData);
                            canRead = false;
                        }
                        else
                        {
                            // Read all measurement values
                            float[] values = new float[length];
                            for (int i = 0; i < length; i++)
                                values[i] = ReadNetworkFloat(br);

                            // Fire event, that a new pack of values was read
                            OnValuesReceived(values);
                        }
                    }
                    catch (IOException) { canRead = false; }
                }
            }

            tcpClient.Close();
        }


        /// <summary>
        /// Reads a float in network byte order (big endian) with the given binary reader.
        /// This is needed because the IPAddress class doesn't provide an overload
        /// of NetworkToHostOrder for any floating point types. This is equivalent to:
        ///
        ///     int ntohl = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(reader.ReadBytes(4), 0));
        ///     return BitConverter.ToSingle(BitConverter.GetBytes(ntohl), 0);
        ///
        /// but with better performance.
        /// </summary>
        public static float ReadNetworkFloat(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian) { Array.Reverse(bytes); }
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Fires the ExceptionOccured event back to the main thread.
        /// </summary>
        private void OnException(Exception ex, Context context)
        {
            if (ExceptionOccured != null && Application.Current != null)
                Application.Current.Dispatcher.Invoke(ExceptionOccured, this,
                        new ExceptionOccuredEventArgs() { Exception = ex, Context = context });
        }

        /// <summary>
        /// Fires the ValuesReceived event back to the main thread.
        /// </summary>
        private void OnValuesReceived(float[] values)
        {
            if (ValuesReceived != null && Application.Current != null)
            {
                T sensorReading = new T();
                sensorReading.SetSensorValues(values);

                Application.Current.Dispatcher.Invoke(ValuesReceived, this,
                        new ValuesReceivedEventArgs<T>() { SensorReading = sensorReading });
            }
        }

        /// <summary>
        /// Stops all associated threads, the TCP Server and clears up ressources of this object.
        /// </summary>
        public void Dispose()
        {
            alive = false;
            listenThread.Abort();
        }
    }
}
