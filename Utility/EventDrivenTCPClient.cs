using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using CSharp_Library.Extensions;

/// <summary>
/// Event driven TCP client wrapper
/// </summary>
public class EventDrivenTCPClient : IDisposable {
    #region Consts/Default values
    const int DEFAULTTIMEOUT = 5000; //Default to 5 seconds on all timeouts
    const int RECONNECTINTERVAL = 2000; //Default to 2 seconds reconnect attempt rate
    #endregion

    #region Components, Events, Delegates, and CTOR
    //Timer used to detect receive timeouts
    private System.Timers.Timer tmrReceiveTimeout = new System.Timers.Timer();
    private System.Timers.Timer tmrSendTimeout = new System.Timers.Timer();
    private System.Timers.Timer tmrConnectTimeout = new System.Timers.Timer();

    public delegate void delDataReceived(byte[] data);
    public event delDataReceived DataReceived;

    public delegate void delConnectionStatusChanged(ConnectionStatus status);
    public event delConnectionStatusChanged ConnectionStatusChanged;

    public enum ConnectionStatus {
        NeverConnected,
        Connecting,
        Connected,
        AutoReconnecting,
        DisconnectedByUser,
        DisconnectedByHost,
        ConnectFail_Timeout,
        ReceiveFail_Timeout,
        SendFail_Timeout,
        SendFail_NotConnected,
        Error
    }

    public EventDrivenTCPClient(IPAddress ip, int port, bool autoreconnect = true, bool noDelay = true) {
        _IP = ip;
        _Port = port;
        _AutoReconnect = autoreconnect;
        _client = new TcpClient(AddressFamily.InterNetwork);
        _client.NoDelay = noDelay; //Disable the nagel algorithm for simplicity

        ReceiveTimeout = DEFAULTTIMEOUT;
        SendTimeout = DEFAULTTIMEOUT;
        ConnectTimeout = DEFAULTTIMEOUT;
        ReconnectInterval = RECONNECTINTERVAL;

        tmrReceiveTimeout.AutoReset = false;
        tmrReceiveTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrReceiveTimeout_Elapsed);

        tmrConnectTimeout.AutoReset = false;
        tmrConnectTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrConnectTimeout_Elapsed);

        tmrSendTimeout.AutoReset = false;
        tmrSendTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrSendTimeout_Elapsed);

        ConnectionState = ConnectionStatus.NeverConnected;
    }

    #endregion

    #region Private methods/Event Handlers
    void tmrSendTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
        ConnectionState = ConnectionStatus.SendFail_Timeout;
        DisconnectByHost();
    }

    void tmrReceiveTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
        ConnectionState = ConnectionStatus.ReceiveFail_Timeout;
        DisconnectByHost();
    }

    void tmrConnectTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
        ConnectionState = ConnectionStatus.ConnectFail_Timeout;
        DisconnectByHost();
    }

    private void DisconnectByHost() {
        ConnectionState = ConnectionStatus.DisconnectedByHost;
        tmrReceiveTimeout.Stop();
        if (AutoReconnect)
            Reconnect();
    }

    private void Reconnect() {
        if (ConnectionState == ConnectionStatus.Connected)
            return;

        ConnectionState = ConnectionStatus.AutoReconnecting;
        try {
            _client.Client.BeginDisconnect(true, new AsyncCallback(cbDisconnectByHostComplete), _client.Client);
        } catch { }
    }
    #endregion

    #region Public Methods
    public void Connect() {
        if (ConnectionState == ConnectionStatus.Connected)
            return;

        ConnectionState = ConnectionStatus.Connecting;

        tmrConnectTimeout.Start();
        _client.BeginConnect(_IP, _Port, new AsyncCallback(cbConnect), _client.Client);
    }
    
    public void Disconnect() {
        if (ConnectionState != ConnectionStatus.Connected)
            return;

        _client.Client.BeginDisconnect(true, new AsyncCallback(cbDisconnectComplete), _client.Client);
    }
    
    public void Send(byte[] body) {
        if (ConnectionState != ConnectionStatus.Connected)
            throw new InvalidOperationException("Cannot send data, socket is not connected");

        Int32 dataLength = body.Length;
        byte[] header = BitConverter.GetBytes(dataLength);
        byte[] data = header.Concat(body);

        SocketError err = new SocketError();
        _client.Client.BeginSend(data, 0, data.Length, SocketFlags.None, out err, new AsyncCallback(cbSendComplete), _client.Client);
        if (err != SocketError.Success) {
            Action doDCHost = new Action(DisconnectByHost);
            doDCHost.Invoke();
        }
    }
    public void Dispose() {
        _client.Close();
        _client.Client.Dispose();
    }
    #endregion

    #region Callbacks
    private void cbConnectComplete() {
        if (_client.Connected == false) {
            ConnectionState = ConnectionStatus.Error;
            return;
        }

        tmrConnectTimeout.Stop();
        ConnectionState = ConnectionStatus.Connected;
        _client.Client.BeginReceive(_headerBuffer, 0, 4, SocketFlags.None, new AsyncCallback(cbReceivedHeader), _client.Client);
    }

    private void cbDisconnectByHostComplete(IAsyncResult result) {
        Socket socket = _client.Client;
        socket.EndDisconnect(result);

        if (AutoReconnect)
            Connect();
    }

    private void cbDisconnectComplete(IAsyncResult result) {
        Socket socket = _client.Client;
        socket.EndDisconnect(result);

        ConnectionState = ConnectionStatus.DisconnectedByUser;
    }

    void cbConnect(IAsyncResult result) {
        var sock = result.AsyncState as Socket;
        if (result == null)
            throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");

        if (!sock.Connected) {
            if (AutoReconnect) {
                System.Threading.Thread.Sleep(ReconnectInterval);
                Action reconnect = new Action(Connect);
                reconnect.Invoke();
                return;
            } else
                return;
        }

        sock.EndConnect(result);

        var callBack = new Action(cbConnectComplete);
        callBack.Invoke();
    }

    void cbSendComplete(IAsyncResult result) {
        var r = result.AsyncState as Socket;
        if (r == null)
            throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");

        SocketError err = new SocketError();
        r.EndSend(result, out err);
        if (err != SocketError.Success) {
            DisconnectByHost();
        } else {
            lock (SyncLock) {
                tmrSendTimeout.Stop();
            }
        }
    }

    private void cbChangeConnectionStateComplete(IAsyncResult result) {
        ConnectionStatusChanged.EndInvoke(result);
    }

    void cbReceivedHeader(IAsyncResult result) {
        Socket socket = _client.Client;
        int bytes = EndReceive(result);
        _headerPos += bytes;
        if (_headerPos < 4) {
            socket.BeginReceive(_headerBuffer, _headerPos, 4-_headerPos, SocketFlags.None, new AsyncCallback(cbReceivedHeader), socket);
            return;
        }
        _bodyPos = 0;
        _expectedBodyLength = BitConverter.ToInt32(_headerBuffer, 0);
        socket.BeginReceive(_bodyBuffer, 0, _expectedBodyLength, SocketFlags.None, new AsyncCallback(cbBodyReceived), socket);
    }

    private void cbBodyReceived(IAsyncResult result) {
        Socket socket = _client.Client;
        int bytes = EndReceive(result);
        _bodyPos += bytes;
        if (_bodyPos < _expectedBodyLength) {
            socket.BeginReceive(_bodyBuffer, _bodyPos, _expectedBodyLength - _bodyPos, SocketFlags.None, new AsyncCallback(cbReceivedHeader), socket);
            return;
        }

        byte[] data = _bodyBuffer.SubArray(0,_expectedBodyLength);
        if (DataReceived != null)
            DataReceived.BeginInvoke(data, new AsyncCallback(cbDataRecievedCallbackComplete), this);

        _headerPos = 0;
        _client.Client.BeginReceive(_headerBuffer, 0, 4, SocketFlags.None, new AsyncCallback(cbReceivedHeader), _client.Client);
    }

    int EndReceive(IAsyncResult result) {
        Socket sock = _client.Client;

        SocketError err = new SocketError();
        int bytes = sock.EndReceive(result, out err);
        if (bytes == 0 || err != SocketError.Success) {
            lock (SyncLock) tmrReceiveTimeout.Start();
            return 0;
        }

        lock (SyncLock) tmrReceiveTimeout.Stop();
        return bytes;
    }

    private void cbDataRecievedCallbackComplete(IAsyncResult result) {
        DataReceived.EndInvoke(result);
    }
    #endregion

    #region Properties and members
    IPAddress _IP = IPAddress.None;
    ConnectionStatus _ConStat;
    TcpClient _client;
    byte[] _bodyBuffer = new byte[1024*1024];
    int _bodyPos = 0;
    int _expectedBodyLength = 0;
    byte[] _headerBuffer = new byte[4];
    int _headerPos = 0;

    bool _AutoReconnect = false;
    int _Port = 0;
    object _SyncLock = new object();

    public object SyncLock {
        get {
            return _SyncLock;
        }
    }
    
    public ConnectionStatus ConnectionState {
        get {
            return _ConStat;
        }
        private set {
            bool raiseEvent = value != _ConStat;
            _ConStat = value;
            if (ConnectionStatusChanged != null && raiseEvent)
                ConnectionStatusChanged.BeginInvoke(_ConStat, new AsyncCallback(cbChangeConnectionStateComplete), this);
        }
    }

    public bool AutoReconnect { get; set; }
    public int ReconnectInterval { get; set; }

    public IPAddress IP {
        get {
            return _IP;
        }
    }

    public int Port {
        get {
            return _Port;
        }
    }

    public int ReceiveTimeout {
        get {
            return (int)tmrReceiveTimeout.Interval;
        }
        set {
            tmrReceiveTimeout.Interval = (double)value;
        }
    }

    public int SendTimeout {
        get {
            return (int)tmrSendTimeout.Interval;
        }
        set {
            tmrSendTimeout.Interval = (double)value;
        }
    }

    public int ConnectTimeout {
        get {
            return (int)tmrConnectTimeout.Interval;
        }
        set {
            tmrConnectTimeout.Interval = (double)value;
        }
    }
    #endregion
    
}