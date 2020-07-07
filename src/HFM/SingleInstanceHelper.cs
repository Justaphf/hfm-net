/*
 * Class based primarily on code from: http://www.codeproject.com/KB/threads/SingleInstancingWithIpc.aspx
 */

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Permissions;
using System.Threading;

namespace HFM
{
    internal sealed class SingleInstanceHelper : IDisposable
    {
        private Mutex _mutex;

        private const string ObjectName = "SingleInstanceProxy";
        private static readonly string AssemblyGuid = GetAssemblyGuid();
        private static readonly string MutexName = String.Format(CultureInfo.InvariantCulture, "Global\\hfm-{0}-{1}", Environment.UserName, AssemblyGuid);

        public bool Start()
        {
            // Issue 236
            // Under Mono there seems to be an issue with the original Mutex
            // being released even after the process has died... usually when
            // a user manually kills the process.
            // In fact, Mono 2.8 has turned off shared file handles all together.
            // http://www.mono-project.com/Release_Notes_Mono_2.8

            bool onlyInstance;
            _mutex = new Mutex(true, MutexName, out onlyInstance);
            return onlyInstance;
        }

        public static void RegisterIpcChannel(EventHandler<NewInstanceDetectedEventArgs> handler)
        {
            IChannel ipcChannel = new IpcServerChannel(String.Format(CultureInfo.InvariantCulture, "hfm-{0}-{1}", Environment.UserName, AssemblyGuid));
            ChannelServices.RegisterChannel(ipcChannel, false);

            var obj = new IpcObject(handler);
            RemotingServices.Marshal(obj, ObjectName);
        }

        public static void SignalFirstInstance(string[] args)
        {
            // Issue 236
            // The actual exception comes from this method, but
            // if we accurately detected if another instance was
            // running or not, then this would not be a problem.

            string objectUri = String.Format(CultureInfo.InvariantCulture, "ipc://hfm-{0}-{1}/{2}", Environment.UserName, AssemblyGuid, ObjectName);

            IChannel ipcChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(ipcChannel, false);

            var obj = (IpcObject)Activator.GetObject(typeof(IpcObject), objectUri);
            obj.SignalNewInstance(args);
        }

        private static string GetAssemblyGuid()
        {
            object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
            if (attributes.Length == 0)
            {
                return String.Empty;
            }
            return ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;
        }

        #region IDisposable Members

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                }

                // Mutex is an unmanaged resource
                if (_mutex != null)
                {
                    _mutex.Close();
                }
            }

            _disposed = true;
        }

        ~SingleInstanceHelper()
        {
            Dispose(false);
        }

        #endregion
    }

    public class IpcObject : MarshalByRefObject
    {
        public event EventHandler<NewInstanceDetectedEventArgs> NewInstanceDetected;

        public IpcObject(EventHandler<NewInstanceDetectedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            NewInstanceDetected += handler;
        }

        public void SignalNewInstance(string[] args)
        {
            NewInstanceDetected(this, new NewInstanceDetectedEventArgs(args));
        }

        // Make sure the object exists "forever"
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }

    public class NewInstanceDetectedEventArgs : EventArgs
    {
        private readonly string[] _args;

        public string[] Args
        {
            get { return _args; }
        }

        public NewInstanceDetectedEventArgs(string[] args)
        {
            _args = args;
        }
    }
}
