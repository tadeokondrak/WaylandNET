/// Copyright © 2008-2011 Kristian Høgsberg
/// Copyright © 2010-2011 Intel Corporation
/// Copyright © 2012-2013 Collabora, Ltd.
/// 
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation files
/// (the "Software"), to deal in the Software without restriction,
/// including without limitation the rights to use, copy, modify, merge,
/// publish, distribute, sublicense, and/or sell copies of the Software,
/// and to permit persons to whom the Software is furnished to do so,
/// subject to the following conditions:
/// 
/// The above copyright notice and this permission notice (including the
/// next paragraph) shall be included in all copies or substantial
/// portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
/// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT.  IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
/// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
/// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.
#pragma warning disable 0162
using System;
using WaylandNET;
using WaylandNET.Client;
namespace WaylandNET.Client.Protocol
{
    /// wl_display version 1
    /// <summary>
    /// core global object
    /// <para>
    /// The core global object.  This is a special singleton object.  It
    /// is used for internal Wayland protocol features.
    /// </para>
    /// </summary>
    public sealed class WlDisplay : WaylandClientObject
    {
        public WlDisplay(uint id, uint version, WaylandClientConnection connection) : base("wl_display", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Sync,
            GetRegistry,
        }
        public enum EventOpcode : ushort
        {
            Error,
            DeleteId,
        }
        /// <summary>
        /// fatal error event
        /// <para>
        /// The error event is sent out when a fatal (non-recoverable)
        /// error has occurred.  The object_id argument is the object
        /// where the error occurred, most often in response to a request
        /// to that object.  The code identifies the error and is defined
        /// by the object interface.  As such, each interface defines its
        /// own set of error codes.  The message is a brief description
        /// of the error, for (debugging) convenience.
        /// </para>
        /// </summary>
        /// <param name="objectId">object where the error occurred</param>
        /// <param name="code">error code</param>
        /// <param name="message">error description</param>
        public delegate void ErrorHandler(WlDisplay wlDisplay, WaylandClientObject objectId, uint code, string message);
        /// <summary>
        /// acknowledge object ID deletion
        /// <para>
        /// This event is used internally by the object ID management
        /// logic. When a client deletes an object that it had created,
        /// the server will send this event to acknowledge that it has
        /// seen the delete request. When the client receives this event,
        /// it will know that it can safely reuse the object ID.
        /// </para>
        /// </summary>
        /// <param name="id">deleted object ID</param>
        public delegate void DeleteIdHandler(WlDisplay wlDisplay, uint id);
        public event ErrorHandler Error;
        public event DeleteIdHandler DeleteId;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Error:
                    {
                        var objectId = (WaylandClientObject)arguments[0];
                        var code = (uint)arguments[1];
                        var message = (string)arguments[2];
                        Error?.Invoke(this, objectId, code, message);
                        break;
                    }
                case EventOpcode.DeleteId:
                    {
                        var id = (uint)arguments[0];
                        DeleteId?.Invoke(this, id);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Error:
                    return new WaylandType[]
                    {
                        WaylandType.Object,
                        WaylandType.UInt,
                        WaylandType.String,
                    };
                    break;
                case EventOpcode.DeleteId:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// global error values
        /// <para>
        /// These errors are global and can be emitted in response to any
        /// server request.
        /// </para>
        /// </summary>
        public enum ErrorEnum : int
        {
            InvalidObject = 0,
            InvalidMethod = 1,
            NoMemory = 2,
            Implementation = 3,
        }
        /// <summary>
        /// asynchronous roundtrip
        /// <para>
        /// The sync request asks the server to emit the 'done' event
        /// on the returned wl_callback object.  Since requests are
        /// handled in-order and events are delivered in-order, this can
        /// be used as a barrier to ensure all previous requests and the
        /// resulting events have been handled.
        /// 
        /// The object returned by this request will be destroyed by the
        /// compositor after the callback is fired and as such the client must not
        /// attempt to use it after that point.
        /// 
        /// The callback_data passed in the callback is the event serial.
        /// </para>
        /// </summary>
        /// <returns>callback object for the sync request</returns>
        public WlCallback Sync()
        {
            uint callback = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.Sync, callback);
            Connection[callback] = new WlCallback(callback, Version, ClientConnection);
            return (WlCallback)Connection[callback];
        }
        /// <summary>
        /// get global registry object
        /// <para>
        /// This request creates a registry object that allows the client
        /// to list and bind the global objects available from the
        /// compositor.
        /// 
        /// It should be noted that the server side resources consumed in
        /// response to a get_registry request can only be released when the
        /// client disconnects, not when the client side proxy is destroyed.
        /// Therefore, clients should invoke get_registry as infrequently as
        /// possible to avoid wasting memory.
        /// </para>
        /// </summary>
        /// <returns>global registry object</returns>
        public WlRegistry GetRegistry()
        {
            uint registry = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetRegistry, registry);
            Connection[registry] = new WlRegistry(registry, Version, ClientConnection);
            return (WlRegistry)Connection[registry];
        }
    }
    /// wl_registry version 1
    /// <summary>
    /// global registry object
    /// <para>
    /// The singleton global registry object.  The server has a number of
    /// global objects that are available to all clients.  These objects
    /// typically represent an actual object in the server (for example,
    /// an input device) or they are singleton objects that provide
    /// extension functionality.
    /// 
    /// When a client creates a registry object, the registry object
    /// will emit a global event for each global currently in the
    /// registry.  Globals come and go as a result of device or
    /// monitor hotplugs, reconfiguration or other events, and the
    /// registry will send out global and global_remove events to
    /// keep the client up to date with the changes.  To mark the end
    /// of the initial burst of events, the client can use the
    /// wl_display.sync request immediately after calling
    /// wl_display.get_registry.
    /// 
    /// A client can bind to a global object by using the bind
    /// request.  This creates a client-side handle that lets the object
    /// emit events to the client and lets the client invoke requests on
    /// the object.
    /// </para>
    /// </summary>
    public sealed class WlRegistry : WaylandClientObject
    {
        public WlRegistry(uint id, uint version, WaylandClientConnection connection) : base("wl_registry", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Bind,
        }
        public enum EventOpcode : ushort
        {
            Global,
            GlobalRemove,
        }
        /// <summary>
        /// announce global object
        /// <para>
        /// Notify the client of global objects.
        /// 
        /// The event notifies the client that a global object with
        /// the given name is now available, and it implements the
        /// given version of the given interface.
        /// </para>
        /// </summary>
        /// <param name="name">numeric name of the global object</param>
        /// <param name="@interface">interface implemented by the object</param>
        /// <param name="version">interface version</param>
        public delegate void GlobalHandler(WlRegistry wlRegistry, uint name, string @interface, uint version);
        /// <summary>
        /// announce removal of global object
        /// <para>
        /// Notify the client of removed global objects.
        /// 
        /// This event notifies the client that the global identified
        /// by name is no longer available.  If the client bound to
        /// the global using the bind request, the client should now
        /// destroy that object.
        /// 
        /// The object remains valid and requests to the object will be
        /// ignored until the client destroys it, to avoid races between
        /// the global going away and a client sending a request to it.
        /// </para>
        /// </summary>
        /// <param name="name">numeric name of the global object</param>
        public delegate void GlobalRemoveHandler(WlRegistry wlRegistry, uint name);
        public event GlobalHandler Global;
        public event GlobalRemoveHandler GlobalRemove;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Global:
                    {
                        var name = (uint)arguments[0];
                        var @interface = (string)arguments[1];
                        var version = (uint)arguments[2];
                        Global?.Invoke(this, name, @interface, version);
                        break;
                    }
                case EventOpcode.GlobalRemove:
                    {
                        var name = (uint)arguments[0];
                        GlobalRemove?.Invoke(this, name);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Global:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.String,
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.GlobalRemove:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// bind an object to the display
        /// <para>
        /// Binds a new, client-created object to the server using the
        /// specified name as the identifier.
        /// </para>
        /// </summary>
        /// <param name="name">unique numeric name of the object</param>
        /// <returns>bounded object</returns>
        public T Bind<T>(uint name, string @interface, uint version) where T : WaylandClientObject
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.Bind, name, @interface, version, id);
            Connection[id] = (WaylandClientObject)Activator.CreateInstance(typeof(T), id, version, ClientConnection);
            return (T)Connection[id];
        }
    }
    /// wl_callback version 1
    /// <summary>
    /// callback object
    /// <para>
    /// Clients can handle the 'done' event to get notified when
    /// the related request is done.
    /// </para>
    /// </summary>
    public sealed class WlCallback : WaylandClientObject
    {
        public WlCallback(uint id, uint version, WaylandClientConnection connection) : base("wl_callback", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
        }
        public enum EventOpcode : ushort
        {
            Done,
        }
        /// <summary>
        /// done event
        /// <para>
        /// Notify the client when the related request is done.
        /// </para>
        /// </summary>
        /// <param name="callbackData">request-specific data for the callback</param>
        public delegate void DoneHandler(WlCallback wlCallback, uint callbackData);
        public event DoneHandler Done;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Done:
                    {
                        var callbackData = (uint)arguments[0];
                        Done?.Invoke(this, callbackData);
                        Die();
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Done:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
    }
}
