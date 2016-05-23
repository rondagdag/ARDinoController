using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// This is a simple shim to let Unity access some PeerJS functionality.
/// Please discuss any issues on the Unity WebGL platform forum.
/// </summary>
public static class UnityPeerJS
{
    private enum EventType
    {
        None = 0,
        Initialized = 1,
        Connected = 2,
        Received = 3,
        ConnClosed = 4,
        PeerDisconnected = 5,
        PeerClosed = 6,
        Error = 7
    };


    /// <summary>
    /// The Peer object provides an endpoint for peer-to-peer connections.  Unless disconnected, it 
    /// maintains a connection to PeerJS's signalling server, which is used for primitive matchmaking.
    /// 
    /// Matchmaking is all done by "id" name, filtered by app-specific "key".
    /// </summary>
    public class Peer
    {
        /// <summary>
        /// Notification when the peer is properly-registered with the signalling server
        /// </summary>
        public event Action OnOpen;

        /// <summary>
        /// Notification when a new connection is formed (regardless of who initiated it)
        /// </summary>
        public event Action<IConnection> OnConnection;

        /// <summary>
        /// Notification when the peer disconnects from the signalling server.
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// Notification when the peer is destroyed, taking client connections down too.
        /// </summary>
        public event Action OnClose;

        /// <summary>
        /// General error notification.
        /// </summary>
        public event Action<string> OnError;

        private readonly int _peerIndex;
        private readonly Dictionary<int, Connection> _connections = new Dictionary<int, Connection>(); 

        /// <summary>
        /// Create a new Peer and register with the signalling server.
        /// </summary>
        /// <param name="key">App-specific key - you can only connect to other peers who use the same key</param>
        /// <param name="id">Unique name for this peer, which other peers need to use in order to form connections</param>
        public Peer(string key, string id = null)
        {
            Init();

            var keyStr = Marshal.StringToHGlobalUni(key);
            var idStr = Marshal.StringToHGlobalUni(id);
            
            _peerIndex = OpenPeer(keyStr, idStr);

            Marshal.FreeHGlobal(keyStr);
            Marshal.FreeHGlobal(idStr);
        }

        /// <summary>
        /// Peers need periodic pumping in order to process their event queues
        /// </summary>
        public void Pump()
        {
            EventType eventType;
            while ((eventType = (EventType)NextEventType(_peerIndex)) != EventType.None)
            {
                switch (eventType)
                {
                    case EventType.Initialized:
                    {
                        PopInitializedEvent(_peerIndex);

                        if (OnOpen != null)
                            OnOpen();

                        break;
                    }

                    case EventType.Connected:
                    {
                        var buffer = new byte[256];
                        var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                        
                        int connIndex = PopConnectedEvent(_peerIndex, pinnedBuffer.AddrOfPinnedObject(), buffer.Length);
                        
                        pinnedBuffer.Free();

                        var remoteId = DecodeUtf16Z(buffer);

                        _connections[connIndex] = new Connection(this, connIndex, remoteId);

                        if (OnConnection != null)
                            OnConnection(_connections[connIndex]);

                        break;
                    }

                    case EventType.Received:
                    {
                        var size = PeekReceivedEventSize(_peerIndex);

                        var buffer = new byte[size];
                        var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                        var connIndex = PopReceivedEvent(_peerIndex, pinnedBuffer.AddrOfPinnedObject(), size);

                        pinnedBuffer.Free();

                        _connections[connIndex].EmitOnData(buffer);

                        break;
                    }

                    case EventType.ConnClosed:
                    {
                        var connIndex = PopConnClosedEvent(_peerIndex);
                        _connections[connIndex].EmitOnClose();
                        break;
                    }

                    case EventType.PeerDisconnected:
                    {
                        PopPeerDisconnectedEvent(_peerIndex);
                        if (OnDisconnected != null)
                            OnDisconnected();
                        break;
                    }

                    case EventType.PeerClosed:
                    {
                        PopPeerClosedEvent(_peerIndex);
                        if (OnClose != null)
                            OnClose();
                        break;
                    }

                    case EventType.Error:
                    {
                        var buffer = new byte[256];
                        var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                        PopErrorEvent(_peerIndex, pinnedBuffer.AddrOfPinnedObject(), buffer.Length);

                        pinnedBuffer.Free();

                        if (OnError != null)
                            OnError(DecodeUtf16Z(buffer));

                        break;
                    }

                    default:
                    {
                        Debug.Log(String.Format("Unhandled event type {0}", eventType));
                        PopAnyEvent(_peerIndex);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Connect to another client
        /// </summary>
        /// <param name="remoteId">Peer ID of target client</param>
        public void Connect(string remoteId)
        {
            var remoteIdStr = Marshal.StringToHGlobalUni(remoteId);
            
            UnityPeerJS.Connect(_peerIndex, remoteIdStr);

            Marshal.FreeHGlobal(remoteIdStr);
        }

        /// <summary>
        /// Disconnect from the server but keep client connections open
        /// </summary>
        public void Disconnect()
        {
            PeerDisconnect(_peerIndex);
        }

        /// <summary>
        /// Disconnect from the server and close all client connections
        /// </summary>
        public void Destroy()
        {
            PeerDestroy(_peerIndex);
        }

        private string DecodeUtf16Z(byte[] buffer)
        {
            var length = 0;
            while (length + 1 < buffer.Length && (buffer[length] != 0 || buffer[length + 1] != 0))
                length += 2;
            return Encoding.Unicode.GetString(buffer, 0, length);
        }

        /// <summary>
        /// Public interface for a peer-to-peer client connection
        /// </summary>
        public interface IConnection
        {
            /// <summary>
            /// Triggered when data is received
            /// </summary>
            event Action<byte[]> OnData;

            /// <summary>
            /// Triggered when the connection is closed
            /// </summary>
            event Action OnClose;

            Peer Peer { get; }

            string RemoteId { get; }

            /// <summary>
            /// Send some data
            /// </summary>
            /// <param name="buffer"></param>
            void Send(byte[] buffer);

            /// <summary>
            /// Close this connection
            /// </summary>
            void Close();
        }

        /// <summary>
        /// Peer-to-peer client connection
        /// </summary>
        private class Connection : IConnection
        {
            public event Action<byte[]> OnData;
            public event Action OnClose;

            public Peer Peer { get { return _peer; } }
            public string RemoteId { get { return _remoteId; } }

            private readonly Peer _peer;
            private readonly int _connIndex;
            private readonly string _remoteId;

            public Connection(Peer peer, int connIndex, string remoteId)
            {
                _peer = peer;
                _connIndex = connIndex;
                _remoteId = remoteId;
            }

            public void EmitOnData(byte[] buffer)
            {
                if (OnData != null)
                    OnData(buffer);
            }

            public void EmitOnClose()
            {
                if (OnClose != null)
                    OnClose();
            }

            public void Send(byte[] buffer)
            {
                var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                UnityPeerJS.Send(_peer._peerIndex, _connIndex, pinnedBuffer.AddrOfPinnedObject(), buffer.Length);
                pinnedBuffer.Free();
            }

            public void Close()
            {
                ConnClose(_peer._peerIndex, _connIndex);
            }
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Init();

    [DllImport("__Internal")]
    private static extern int OpenPeer(IntPtr keyStr, IntPtr idStr);

    [DllImport("__Internal")]
    private static extern void Connect(int peerInstance, IntPtr remoteIdStr);

    [DllImport("__Internal")]
    private static extern void Send(int peerInstance, int connInstance, IntPtr ptr, int length);

    [DllImport("__Internal")]
    private static extern void ConnClose(int peerInstance, int connInstance);

    [DllImport("__Internal")]
    private static extern void PeerDisconnect(int peerInstance);

    [DllImport("__Internal")]
    private static extern void PeerDestroy(int peerInstance);

    [DllImport("__Internal")]
    private static extern int NextEventType(int peerInstance);

    [DllImport("__Internal")]
    private static extern void PopAnyEvent(int peerInstance);

    [DllImport("__Internal")]
    private static extern void PopInitializedEvent(int peerInstance);

    [DllImport("__Internal")]
    private static extern int PopConnectedEvent(int peerInstance, IntPtr remoteIdPtr, int remoteIdMaxLength);

    [DllImport("__Internal")]
    private static extern int PeekReceivedEventSize(int peerInstance);

    [DllImport("__Internal")]
    private static extern int PopReceivedEvent(int peerInstance, IntPtr dataPtr, int dataMaxLength);

    [DllImport("__Internal")]
    private static extern int PopConnClosedEvent(int peerInstance);

    [DllImport("__Internal")]
    private static extern void PopPeerDisconnectedEvent(int peerInstance);

    [DllImport("__Internal")]
    private static extern void PopPeerClosedEvent(int peerInstance);

    [DllImport("__Internal")]
    private static extern void PopErrorEvent(int peerInstance, IntPtr errorPtr, int errorMaxLength);
#else

    private class Event
    {
        public EventType Type;
        public int Conn;
        public byte[] Data;
    }

    private class MockPeer
    {
        public class MockConn
        {
            public string Target;
        }

        private static Queue<Event> _eventQueue = new Queue<Event>();
        private List<MockConn> _mockConns = new List<MockConn>();
        public IList<MockConn> Conn { get { return _mockConns; } }
            
        public MockPeer()
        {
            _eventQueue.Enqueue(new Event {Type = EventType.Initialized});
        }

        public void Connect(string target)
        {
            int connId = _mockConns.Count;
            _mockConns.Add(new MockConn { Target = target });
            _eventQueue.Enqueue(new Event {Type = EventType.Connected, Conn = connId});
        }

        public int NextEventType()
        {
            if (_eventQueue.Count == 0)
                return 0;
            return (int)_eventQueue.Peek().Type;
        }

        public Event PopEvent(EventType et)
        {
            var ev = _eventQueue.Dequeue();
            if (et != EventType.None && et != ev.Type)
                Debug.LogError("Wrong event type");
            return ev;
        }

        public void Send(int connInstance, byte[] bytes)
        {
            _eventQueue.Enqueue(new Event {Type = EventType.Received, Conn = connInstance, Data = bytes});
        }

        public Event PeekEvent(EventType et)
        {
            var ev = _eventQueue.Peek();
            if (et != EventType.None && et != ev.Type)
                Debug.LogError("Wrong event type");
            return ev;
        }
    }

    private static readonly List<MockPeer> _mockPeers = new List<MockPeer>();

    private static void Init()
    {
    }

    private static int OpenPeer(IntPtr keyStr, IntPtr idStr)
    {
        int id = _mockPeers.Count;
        _mockPeers.Add(new MockPeer());
        return id;
    }

    private static void Connect(int peerInstance, IntPtr remoteIdStr)
    {
        _mockPeers[peerInstance].Connect(Marshal.PtrToStringUni(remoteIdStr));
    }

    private static void Send(int peerInstance, int connInstance, IntPtr ptr, int length)
    {
        var bytes = new byte[length];
        Marshal.Copy(ptr, bytes, 0, length);
        var peer = _mockPeers[peerInstance];
        peer.Send(connInstance, bytes);
    }

    private static void ConnClose(int peerInstance, int connInstance)
    {
        throw new NotImplementedException();
    }

    private static void PeerDisconnect(int peerInstance)
    {
        throw new NotImplementedException();
    }

    private static void PeerDestroy(int peerInstance)
    {
        throw new NotImplementedException();
    }

    private static int NextEventType(int peerInstance)
    {
        return _mockPeers[peerInstance].NextEventType();
    }

    private static void PopAnyEvent(int peerInstance)
    {
        _mockPeers[peerInstance].PopEvent(EventType.None);
    }

    private static void PopInitializedEvent(int peerInstance)
    {
        _mockPeers[peerInstance].PopEvent(EventType.Initialized);
    }

    private static int PopConnectedEvent(int peerInstance, IntPtr remoteIdPtr, int remoteIdMaxLength)
    {
        var ev = _mockPeers[peerInstance].PopEvent(EventType.Connected);
        var conn = _mockPeers[peerInstance].Conn[ev.Conn];

        var bytes = Encoding.Unicode.GetBytes(conn.Target);
        int length = bytes.Length;
        if (length > remoteIdMaxLength) length = remoteIdMaxLength;
        Marshal.Copy(bytes, 0, remoteIdPtr, length);

        return ev.Conn;
    }

    private static int PeekReceivedEventSize(int peerInstance)
    {
        var ev = _mockPeers[peerInstance].PeekEvent(EventType.Received);
        return ev.Data.Length;
    }

    private static int PopReceivedEvent(int peerInstance, IntPtr dataPtr, int dataMaxLength)
    {
        var ev = _mockPeers[peerInstance].PopEvent(EventType.Received);
        int length = ev.Data.Length;
        if (length > dataMaxLength) length = dataMaxLength;
        Marshal.Copy(ev.Data, 0, dataPtr, length);
        return ev.Conn;
    }

    private static int PopConnClosedEvent(int peerInstance)
    {
        throw new NotImplementedException();
    }

    private static void PopPeerDisconnectedEvent(int peerInstance)
    {
        throw new NotImplementedException();
    }

    private static void PopPeerClosedEvent(int peerInstance)
    {
        throw new NotImplementedException();
    }

    private static void PopErrorEvent(int peerInstance, IntPtr errorPtr, int errorMaxLength)
    {
        throw new NotImplementedException();
    }
#endif
}
