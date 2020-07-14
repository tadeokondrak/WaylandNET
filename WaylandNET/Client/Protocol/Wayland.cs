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
    /// wl_compositor version 4
    /// <summary>
    /// the compositor singleton
    /// <para>
    /// A compositor.  This object is a singleton global.  The
    /// compositor is in charge of combining the contents of multiple
    /// surfaces into one displayable output.
    /// </para>
    /// </summary>
    public sealed class WlCompositor : WaylandClientObject
    {
        public WlCompositor(uint id, uint version, WaylandClientConnection connection) : base("wl_compositor", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            CreateSurface,
            CreateRegion,
        }
        public enum EventOpcode : ushort
        {
        }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// create new surface
        /// <para>
        /// Ask the compositor to create a new surface.
        /// </para>
        /// </summary>
        /// <returns>the new surface</returns>
        public WlSurface CreateSurface()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.CreateSurface, id);
            Connection[id] = new WlSurface(id, Version, ClientConnection);
            return (WlSurface)Connection[id];
        }
        /// <summary>
        /// create new region
        /// <para>
        /// Ask the compositor to create a new region.
        /// </para>
        /// </summary>
        /// <returns>the new region</returns>
        public WlRegion CreateRegion()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.CreateRegion, id);
            Connection[id] = new WlRegion(id, Version, ClientConnection);
            return (WlRegion)Connection[id];
        }
    }
    /// wl_shm_pool version 1
    /// <summary>
    /// a shared memory pool
    /// <para>
    /// The wl_shm_pool object encapsulates a piece of memory shared
    /// between the compositor and client.  Through the wl_shm_pool
    /// object, the client can allocate shared memory wl_buffer objects.
    /// All objects created through the same pool share the same
    /// underlying mapped memory. Reusing the mapped memory avoids the
    /// setup/teardown overhead and is useful when interactively resizing
    /// a surface or for many small buffers.
    /// </para>
    /// </summary>
    public sealed class WlShmPool : WaylandClientObject
    {
        public WlShmPool(uint id, uint version, WaylandClientConnection connection) : base("wl_shm_pool", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            CreateBuffer,
            Destroy,
            Resize,
        }
        public enum EventOpcode : ushort
        {
        }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// create a buffer from the pool
        /// <para>
        /// Create a wl_buffer object from the pool.
        /// 
        /// The buffer is created offset bytes into the pool and has
        /// width and height as specified.  The stride argument specifies
        /// the number of bytes from the beginning of one row to the beginning
        /// of the next.  The format is the pixel format of the buffer and
        /// must be one of those advertised through the wl_shm.format event.
        /// 
        /// A buffer will keep a reference to the pool it was created from
        /// so it is valid to destroy the pool immediately after creating
        /// a buffer from it.
        /// </para>
        /// </summary>
        /// <returns>buffer to create</returns>
        /// <param name="offset">buffer byte offset within the pool</param>
        /// <param name="width">buffer width, in pixels</param>
        /// <param name="height">buffer height, in pixels</param>
        /// <param name="stride">number of bytes from the beginning of one row to the beginning of the next row</param>
        /// <param name="format">buffer pixel format</param>
        public WlBuffer CreateBuffer(int offset, int width, int height, int stride, WlShm.FormatEnum format)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.CreateBuffer, id, offset, width, height, stride, (uint)format);
            Connection[id] = new WlBuffer(id, Version, ClientConnection);
            return (WlBuffer)Connection[id];
        }
        /// <summary>
        /// destroy the pool
        /// <para>
        /// Destroy the shared memory pool.
        /// 
        /// The mmapped memory will be released when all
        /// buffers that have been created from this pool
        /// are gone.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// change the size of the pool mapping
        /// <para>
        /// This request will cause the server to remap the backing memory
        /// for the pool from the file descriptor passed when the pool was
        /// created, but using the new size.  This request can only be
        /// used to make the pool bigger.
        /// </para>
        /// </summary>
        /// <param name="size">new size of the pool, in bytes</param>
        public void Resize(int size)
        {
            Marshal((ushort)RequestOpcode.Resize, size);
        }
    }
    /// wl_shm version 1
    /// <summary>
    /// shared memory support
    /// <para>
    /// A singleton global object that provides support for shared
    /// memory.
    /// 
    /// Clients can create wl_shm_pool objects using the create_pool
    /// request.
    /// 
    /// At connection setup time, the wl_shm object emits one or more
    /// format events to inform clients about the valid pixel formats
    /// that can be used for buffers.
    /// </para>
    /// </summary>
    public sealed class WlShm : WaylandClientObject
    {
        public WlShm(uint id, uint version, WaylandClientConnection connection) : base("wl_shm", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            CreatePool,
        }
        public enum EventOpcode : ushort
        {
            Format,
        }
        /// <summary>
        /// pixel format description
        /// <para>
        /// Informs the client about a valid pixel format that
        /// can be used for buffers. Known formats include
        /// argb8888 and xrgb8888.
        /// </para>
        /// </summary>
        /// <param name="format">buffer pixel format</param>
        public delegate void FormatHandler(WlShm wlShm, FormatEnum format);
        public event FormatHandler Format;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Format:
                    {
                        var format = (FormatEnum)(uint)arguments[0];
                        Format?.Invoke(this, format);
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
                case EventOpcode.Format:
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
        /// wl_shm error values
        /// <para>
        /// These errors can be emitted in response to wl_shm requests.
        /// </para>
        /// </summary>
        public enum Error : int
        {
            InvalidFormat = 0,
            InvalidStride = 1,
            InvalidFd = 2,
        }
        /// <summary>
        /// pixel formats
        /// <para>
        /// This describes the memory layout of an individual pixel.
        /// 
        /// All renderers should support argb8888 and xrgb8888 but any other
        /// formats are optional and may not be supported by the particular
        /// renderer in use.
        /// 
        /// The drm format codes match the macros defined in drm_fourcc.h, except
        /// argb8888 and xrgb8888. The formats actually supported by the compositor
        /// will be reported by the format event.
        /// </para>
        /// </summary>
        public enum FormatEnum : int
        {
            Argb8888 = 0,
            Xrgb8888 = 1,
            C8 = 538982467,
            Rgb332 = 943867730,
            Bgr233 = 944916290,
            Xrgb4444 = 842093144,
            Xbgr4444 = 842089048,
            Rgbx4444 = 842094674,
            Bgrx4444 = 842094658,
            Argb4444 = 842093121,
            Abgr4444 = 842089025,
            Rgba4444 = 842088786,
            Bgra4444 = 842088770,
            Xrgb1555 = 892424792,
            Xbgr1555 = 892420696,
            Rgbx5551 = 892426322,
            Bgrx5551 = 892426306,
            Argb1555 = 892424769,
            Abgr1555 = 892420673,
            Rgba5551 = 892420434,
            Bgra5551 = 892420418,
            Rgb565 = 909199186,
            Bgr565 = 909199170,
            Rgb888 = 875710290,
            Bgr888 = 875710274,
            Xbgr8888 = 875709016,
            Rgbx8888 = 875714642,
            Bgrx8888 = 875714626,
            Abgr8888 = 875708993,
            Rgba8888 = 875708754,
            Bgra8888 = 875708738,
            Xrgb2101010 = 808669784,
            Xbgr2101010 = 808665688,
            Rgbx1010102 = 808671314,
            Bgrx1010102 = 808671298,
            Argb2101010 = 808669761,
            Abgr2101010 = 808665665,
            Rgba1010102 = 808665426,
            Bgra1010102 = 808665410,
            Yuyv = 1448695129,
            Yvyu = 1431918169,
            Uyvy = 1498831189,
            Vyuy = 1498765654,
            Ayuv = 1448433985,
            Nv12 = 842094158,
            Nv21 = 825382478,
            Nv16 = 909203022,
            Nv61 = 825644622,
            Yuv410 = 961959257,
            Yvu410 = 961893977,
            Yuv411 = 825316697,
            Yvu411 = 825316953,
            Yuv420 = 842093913,
            Yvu420 = 842094169,
            Yuv422 = 909202777,
            Yvu422 = 909203033,
            Yuv444 = 875713881,
            Yvu444 = 875714137,
            R8 = 538982482,
            R16 = 540422482,
            Rg88 = 943212370,
            Gr88 = 943215175,
            Rg1616 = 842221394,
            Gr1616 = 842224199,
            Xrgb16161616f = 1211388504,
            Xbgr16161616f = 1211384408,
            Argb16161616f = 1211388481,
            Abgr16161616f = 1211384385,
            Xyuv8888 = 1448434008,
            Vuy888 = 875713878,
            Vuy101010 = 808670550,
            Y210 = 808530521,
            Y212 = 842084953,
            Y216 = 909193817,
            Y410 = 808531033,
            Y412 = 842085465,
            Y416 = 909194329,
            Xvyu2101010 = 808670808,
            Xvyu1216161616 = 909334104,
            Xvyu16161616 = 942954072,
            Y0l0 = 810299481,
            X0l0 = 810299480,
            Y0l2 = 843853913,
            X0l2 = 843853912,
            Yuv4208bit = 942691673,
            Yuv42010bit = 808539481,
            Xrgb8888A8 = 943805016,
            Xbgr8888A8 = 943800920,
            Rgbx8888A8 = 943806546,
            Bgrx8888A8 = 943806530,
            Rgb888A8 = 943798354,
            Bgr888A8 = 943798338,
            Rgb565A8 = 943797586,
            Bgr565A8 = 943797570,
            Nv24 = 875714126,
            Nv42 = 842290766,
            P210 = 808530512,
            P010 = 808530000,
            P012 = 842084432,
            P016 = 909193296,
        }
        /// <summary>
        /// create a shm pool
        /// <para>
        /// Create a new wl_shm_pool object.
        /// 
        /// The pool can be used to create shared memory based buffer
        /// objects.  The server will mmap size bytes of the passed file
        /// descriptor, to use as backing memory for the pool.
        /// </para>
        /// </summary>
        /// <returns>pool to create</returns>
        /// <param name="fd">file descriptor for the pool</param>
        /// <param name="size">pool size, in bytes</param>
        public WlShmPool CreatePool(IntPtr fd, int size)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.CreatePool, id, fd, size);
            Connection[id] = new WlShmPool(id, Version, ClientConnection);
            return (WlShmPool)Connection[id];
        }
    }
    /// wl_buffer version 1
    /// <summary>
    /// content for a wl_surface
    /// <para>
    /// A buffer provides the content for a wl_surface. Buffers are
    /// created through factory interfaces such as wl_drm, wl_shm or
    /// similar. It has a width and a height and can be attached to a
    /// wl_surface, but the mechanism by which a client provides and
    /// updates the contents is defined by the buffer factory interface.
    /// </para>
    /// </summary>
    public sealed class WlBuffer : WaylandClientObject
    {
        public WlBuffer(uint id, uint version, WaylandClientConnection connection) : base("wl_buffer", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
        }
        public enum EventOpcode : ushort
        {
            Release,
        }
        /// <summary>
        /// compositor releases buffer
        /// <para>
        /// Sent when this wl_buffer is no longer used by the compositor.
        /// The client is now free to reuse or destroy this buffer and its
        /// backing storage.
        /// 
        /// If a client receives a release event before the frame callback
        /// requested in the same wl_surface.commit that attaches this
        /// wl_buffer to a surface, then the client is immediately free to
        /// reuse the buffer and its backing storage, and does not need a
        /// second buffer for the next surface content update. Typically
        /// this is possible, when the compositor maintains a copy of the
        /// wl_surface contents, e.g. as a GL texture. This is an important
        /// optimization for GL(ES) compositors with wl_shm clients.
        /// </para>
        /// </summary>
        public delegate void ReleaseHandler(WlBuffer wlBuffer);
        public event ReleaseHandler Release;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Release:
                    {
                        Release?.Invoke(this);
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
                case EventOpcode.Release:
                    return new WaylandType[]
                    {
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// destroy a buffer
        /// <para>
        /// Destroy a buffer. If and how you need to release the backing
        /// storage is defined by the buffer factory interface.
        /// 
        /// For possible side-effects to a surface, see wl_surface.attach.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
    }
    /// wl_data_offer version 3
    /// <summary>
    /// offer to transfer data
    /// <para>
    /// A wl_data_offer represents a piece of data offered for transfer
    /// by another client (the source client).  It is used by the
    /// copy-and-paste and drag-and-drop mechanisms.  The offer
    /// describes the different mime types that the data can be
    /// converted to and provides the mechanism for transferring the
    /// data directly from the source client.
    /// </para>
    /// </summary>
    public sealed class WlDataOffer : WaylandClientObject
    {
        public WlDataOffer(uint id, uint version, WaylandClientConnection connection) : base("wl_data_offer", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Accept,
            Receive,
            Destroy,
            Finish,
            SetActions,
        }
        public enum EventOpcode : ushort
        {
            Offer,
            SourceActions,
            Action,
        }
        /// <summary>
        /// advertise offered mime type
        /// <para>
        /// Sent immediately after creating the wl_data_offer object.  One
        /// event per offered mime type.
        /// </para>
        /// </summary>
        /// <param name="mimeType">offered mime type</param>
        public delegate void OfferHandler(WlDataOffer wlDataOffer, string mimeType);
        /// <summary>
        /// notify the source-side available actions
        /// <para>
        /// This event indicates the actions offered by the data source. It
        /// will be sent right after wl_data_device.enter, or anytime the source
        /// side changes its offered actions through wl_data_source.set_actions.
        /// </para>
        /// </summary>
        /// <param name="sourceActions">actions offered by the data source</param>
        public delegate void SourceActionsHandler(WlDataOffer wlDataOffer, WlDataDeviceManager.DndAction sourceActions);
        /// <summary>
        /// notify the selected action
        /// <para>
        /// This event indicates the action selected by the compositor after
        /// matching the source/destination side actions. Only one action (or
        /// none) will be offered here.
        /// 
        /// This event can be emitted multiple times during the drag-and-drop
        /// operation in response to destination side action changes through
        /// wl_data_offer.set_actions.
        /// 
        /// This event will no longer be emitted after wl_data_device.drop
        /// happened on the drag-and-drop destination, the client must
        /// honor the last action received, or the last preferred one set
        /// through wl_data_offer.set_actions when handling an "ask" action.
        /// 
        /// Compositors may also change the selected action on the fly, mainly
        /// in response to keyboard modifier changes during the drag-and-drop
        /// operation.
        /// 
        /// The most recent action received is always the valid one. Prior to
        /// receiving wl_data_device.drop, the chosen action may change (e.g.
        /// due to keyboard modifiers being pressed). At the time of receiving
        /// wl_data_device.drop the drag-and-drop destination must honor the
        /// last action received.
        /// 
        /// Action changes may still happen after wl_data_device.drop,
        /// especially on "ask" actions, where the drag-and-drop destination
        /// may choose another action afterwards. Action changes happening
        /// at this stage are always the result of inter-client negotiation, the
        /// compositor shall no longer be able to induce a different action.
        /// 
        /// Upon "ask" actions, it is expected that the drag-and-drop destination
        /// may potentially choose a different action and/or mime type,
        /// based on wl_data_offer.source_actions and finally chosen by the
        /// user (e.g. popping up a menu with the available options). The
        /// final wl_data_offer.set_actions and wl_data_offer.accept requests
        /// must happen before the call to wl_data_offer.finish.
        /// </para>
        /// </summary>
        /// <param name="dndAction">action selected by the compositor</param>
        public delegate void ActionHandler(WlDataOffer wlDataOffer, WlDataDeviceManager.DndAction dndAction);
        public event OfferHandler Offer;
        public event SourceActionsHandler SourceActions;
        public event ActionHandler Action;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Offer:
                    {
                        var mimeType = (string)arguments[0];
                        Offer?.Invoke(this, mimeType);
                        break;
                    }
                case EventOpcode.SourceActions:
                    {
                        var sourceActions = (WlDataDeviceManager.DndAction)(uint)arguments[0];
                        SourceActions?.Invoke(this, sourceActions);
                        break;
                    }
                case EventOpcode.Action:
                    {
                        var dndAction = (WlDataDeviceManager.DndAction)(uint)arguments[0];
                        Action?.Invoke(this, dndAction);
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
                case EventOpcode.Offer:
                    return new WaylandType[]
                    {
                        WaylandType.String,
                    };
                    break;
                case EventOpcode.SourceActions:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.Action:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            InvalidFinish = 0,
            InvalidActionMask = 1,
            InvalidAction = 2,
            InvalidOffer = 3,
        }
        /// <summary>
        /// accept one of the offered mime types
        /// <para>
        /// Indicate that the client can accept the given mime type, or
        /// NULL for not accepted.
        /// 
        /// For objects of version 2 or older, this request is used by the
        /// client to give feedback whether the client can receive the given
        /// mime type, or NULL if none is accepted; the feedback does not
        /// determine whether the drag-and-drop operation succeeds or not.
        /// 
        /// For objects of version 3 or newer, this request determines the
        /// final result of the drag-and-drop operation. If the end result
        /// is that no mime types were accepted, the drag-and-drop operation
        /// will be cancelled and the corresponding drag source will receive
        /// wl_data_source.cancelled. Clients may still use this event in
        /// conjunction with wl_data_source.action for feedback.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the accept request</param>
        /// <param name="mimeType">mime type accepted by the client</param>
        public void Accept(uint serial, string mimeType)
        {
            Marshal((ushort)RequestOpcode.Accept, serial, mimeType);
        }
        /// <summary>
        /// request that the data is transferred
        /// <para>
        /// To transfer the offered data, the client issues this request
        /// and indicates the mime type it wants to receive.  The transfer
        /// happens through the passed file descriptor (typically created
        /// with the pipe system call).  The source client writes the data
        /// in the mime type representation requested and then closes the
        /// file descriptor.
        /// 
        /// The receiving client reads from the read end of the pipe until
        /// EOF and then closes its end, at which point the transfer is
        /// complete.
        /// 
        /// This request may happen multiple times for different mime types,
        /// both before and after wl_data_device.drop. Drag-and-drop destination
        /// clients may preemptively fetch data or examine it more closely to
        /// determine acceptance.
        /// </para>
        /// </summary>
        /// <param name="mimeType">mime type desired by receiver</param>
        /// <param name="fd">file descriptor for data transfer</param>
        public void Receive(string mimeType, IntPtr fd)
        {
            Marshal((ushort)RequestOpcode.Receive, mimeType, fd);
        }
        /// <summary>
        /// destroy data offer
        /// <para>
        /// Destroy the data offer.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// the offer will no longer be used
        /// <para>
        /// Notifies the compositor that the drag destination successfully
        /// finished the drag-and-drop operation.
        /// 
        /// Upon receiving this request, the compositor will emit
        /// wl_data_source.dnd_finished on the drag source client.
        /// 
        /// It is a client error to perform other requests than
        /// wl_data_offer.destroy after this one. It is also an error to perform
        /// this request after a NULL mime type has been set in
        /// wl_data_offer.accept or no action was received through
        /// wl_data_offer.action.
        /// 
        /// If wl_data_offer.finish request is received for a non drag and drop
        /// operation, the invalid_finish protocol error is raised.
        /// </para>
        /// </summary>
        public void Finish()
        {
            Marshal((ushort)RequestOpcode.Finish);
        }
        /// <summary>
        /// set the available/preferred drag-and-drop actions
        /// <para>
        /// Sets the actions that the destination side client supports for
        /// this operation. This request may trigger the emission of
        /// wl_data_source.action and wl_data_offer.action events if the compositor
        /// needs to change the selected action.
        /// 
        /// This request can be called multiple times throughout the
        /// drag-and-drop operation, typically in response to wl_data_device.enter
        /// or wl_data_device.motion events.
        /// 
        /// This request determines the final result of the drag-and-drop
        /// operation. If the end result is that no action is accepted,
        /// the drag source will receive wl_data_source.cancelled.
        /// 
        /// The dnd_actions argument must contain only values expressed in the
        /// wl_data_device_manager.dnd_actions enum, and the preferred_action
        /// argument must only contain one of those values set, otherwise it
        /// will result in a protocol error.
        /// 
        /// While managing an "ask" action, the destination drag-and-drop client
        /// may perform further wl_data_offer.receive requests, and is expected
        /// to perform one last wl_data_offer.set_actions request with a preferred
        /// action other than "ask" (and optionally wl_data_offer.accept) before
        /// requesting wl_data_offer.finish, in order to convey the action selected
        /// by the user. If the preferred action is not in the
        /// wl_data_offer.source_actions mask, an error will be raised.
        /// 
        /// If the "ask" action is dismissed (e.g. user cancellation), the client
        /// is expected to perform wl_data_offer.destroy right away.
        /// 
        /// This request can only be made on drag-and-drop offers, a protocol error
        /// will be raised otherwise.
        /// </para>
        /// </summary>
        /// <param name="dndActions">actions supported by the destination client</param>
        /// <param name="preferredAction">action preferred by the destination client</param>
        public void SetActions(WlDataDeviceManager.DndAction dndActions, WlDataDeviceManager.DndAction preferredAction)
        {
            Marshal((ushort)RequestOpcode.SetActions, (uint)dndActions, (uint)preferredAction);
        }
    }
    /// wl_data_source version 3
    /// <summary>
    /// offer to transfer data
    /// <para>
    /// The wl_data_source object is the source side of a wl_data_offer.
    /// It is created by the source client in a data transfer and
    /// provides a way to describe the offered data and a way to respond
    /// to requests to transfer the data.
    /// </para>
    /// </summary>
    public sealed class WlDataSource : WaylandClientObject
    {
        public WlDataSource(uint id, uint version, WaylandClientConnection connection) : base("wl_data_source", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Offer,
            Destroy,
            SetActions,
        }
        public enum EventOpcode : ushort
        {
            Target,
            Send,
            Cancelled,
            DndDropPerformed,
            DndFinished,
            Action,
        }
        /// <summary>
        /// a target accepts an offered mime type
        /// <para>
        /// Sent when a target accepts pointer_focus or motion events.  If
        /// a target does not accept any of the offered types, type is NULL.
        /// 
        /// Used for feedback during drag-and-drop.
        /// </para>
        /// </summary>
        /// <param name="mimeType">mime type accepted by the target</param>
        public delegate void TargetHandler(WlDataSource wlDataSource, string mimeType);
        /// <summary>
        /// send the data
        /// <para>
        /// Request for data from the client.  Send the data as the
        /// specified mime type over the passed file descriptor, then
        /// close it.
        /// </para>
        /// </summary>
        /// <param name="mimeType">mime type for the data</param>
        /// <param name="fd">file descriptor for the data</param>
        public delegate void SendHandler(WlDataSource wlDataSource, string mimeType, IntPtr fd);
        /// <summary>
        /// selection was cancelled
        /// <para>
        /// This data source is no longer valid. There are several reasons why
        /// this could happen:
        /// 
        /// - The data source has been replaced by another data source.
        /// - The drag-and-drop operation was performed, but the drop destination
        /// did not accept any of the mime types offered through
        /// wl_data_source.target.
        /// - The drag-and-drop operation was performed, but the drop destination
        /// did not select any of the actions present in the mask offered through
        /// wl_data_source.action.
        /// - The drag-and-drop operation was performed but didn't happen over a
        /// surface.
        /// - The compositor cancelled the drag-and-drop operation (e.g. compositor
        /// dependent timeouts to avoid stale drag-and-drop transfers).
        /// 
        /// The client should clean up and destroy this data source.
        /// 
        /// For objects of version 2 or older, wl_data_source.cancelled will
        /// only be emitted if the data source was replaced by another data
        /// source.
        /// </para>
        /// </summary>
        public delegate void CancelledHandler(WlDataSource wlDataSource);
        /// <summary>
        /// the drag-and-drop operation physically finished
        /// <para>
        /// The user performed the drop action. This event does not indicate
        /// acceptance, wl_data_source.cancelled may still be emitted afterwards
        /// if the drop destination does not accept any mime type.
        /// 
        /// However, this event might however not be received if the compositor
        /// cancelled the drag-and-drop operation before this event could happen.
        /// 
        /// Note that the data_source may still be used in the future and should
        /// not be destroyed here.
        /// </para>
        /// </summary>
        public delegate void DndDropPerformedHandler(WlDataSource wlDataSource);
        /// <summary>
        /// the drag-and-drop operation concluded
        /// <para>
        /// The drop destination finished interoperating with this data
        /// source, so the client is now free to destroy this data source and
        /// free all associated data.
        /// 
        /// If the action used to perform the operation was "move", the
        /// source can now delete the transferred data.
        /// </para>
        /// </summary>
        public delegate void DndFinishedHandler(WlDataSource wlDataSource);
        /// <summary>
        /// notify the selected action
        /// <para>
        /// This event indicates the action selected by the compositor after
        /// matching the source/destination side actions. Only one action (or
        /// none) will be offered here.
        /// 
        /// This event can be emitted multiple times during the drag-and-drop
        /// operation, mainly in response to destination side changes through
        /// wl_data_offer.set_actions, and as the data device enters/leaves
        /// surfaces.
        /// 
        /// It is only possible to receive this event after
        /// wl_data_source.dnd_drop_performed if the drag-and-drop operation
        /// ended in an "ask" action, in which case the final wl_data_source.action
        /// event will happen immediately before wl_data_source.dnd_finished.
        /// 
        /// Compositors may also change the selected action on the fly, mainly
        /// in response to keyboard modifier changes during the drag-and-drop
        /// operation.
        /// 
        /// The most recent action received is always the valid one. The chosen
        /// action may change alongside negotiation (e.g. an "ask" action can turn
        /// into a "move" operation), so the effects of the final action must
        /// always be applied in wl_data_offer.dnd_finished.
        /// 
        /// Clients can trigger cursor surface changes from this point, so
        /// they reflect the current action.
        /// </para>
        /// </summary>
        /// <param name="dndAction">action selected by the compositor</param>
        public delegate void ActionHandler(WlDataSource wlDataSource, WlDataDeviceManager.DndAction dndAction);
        public event TargetHandler Target;
        public event SendHandler Send;
        public event CancelledHandler Cancelled;
        public event DndDropPerformedHandler DndDropPerformed;
        public event DndFinishedHandler DndFinished;
        public event ActionHandler Action;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Target:
                    {
                        var mimeType = (string)arguments[0];
                        Target?.Invoke(this, mimeType);
                        break;
                    }
                case EventOpcode.Send:
                    {
                        var mimeType = (string)arguments[0];
                        var fd = (IntPtr)arguments[1];
                        Send?.Invoke(this, mimeType, fd);
                        break;
                    }
                case EventOpcode.Cancelled:
                    {
                        Cancelled?.Invoke(this);
                        break;
                    }
                case EventOpcode.DndDropPerformed:
                    {
                        DndDropPerformed?.Invoke(this);
                        break;
                    }
                case EventOpcode.DndFinished:
                    {
                        DndFinished?.Invoke(this);
                        break;
                    }
                case EventOpcode.Action:
                    {
                        var dndAction = (WlDataDeviceManager.DndAction)(uint)arguments[0];
                        Action?.Invoke(this, dndAction);
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
                case EventOpcode.Target:
                    return new WaylandType[]
                    {
                        WaylandType.String,
                    };
                    break;
                case EventOpcode.Send:
                    return new WaylandType[]
                    {
                        WaylandType.String,
                        WaylandType.Handle,
                    };
                    break;
                case EventOpcode.Cancelled:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.DndDropPerformed:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.DndFinished:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Action:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            InvalidActionMask = 0,
            InvalidSource = 1,
        }
        /// <summary>
        /// add an offered mime type
        /// <para>
        /// This request adds a mime type to the set of mime types
        /// advertised to targets.  Can be called several times to offer
        /// multiple types.
        /// </para>
        /// </summary>
        /// <param name="mimeType">mime type offered by the data source</param>
        public void Offer(string mimeType)
        {
            Marshal((ushort)RequestOpcode.Offer, mimeType);
        }
        /// <summary>
        /// destroy the data source
        /// <para>
        /// Destroy the data source.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// set the available drag-and-drop actions
        /// <para>
        /// Sets the actions that the source side client supports for this
        /// operation. This request may trigger wl_data_source.action and
        /// wl_data_offer.action events if the compositor needs to change the
        /// selected action.
        /// 
        /// The dnd_actions argument must contain only values expressed in the
        /// wl_data_device_manager.dnd_actions enum, otherwise it will result
        /// in a protocol error.
        /// 
        /// This request must be made once only, and can only be made on sources
        /// used in drag-and-drop, so it must be performed before
        /// wl_data_device.start_drag. Attempting to use the source other than
        /// for drag-and-drop will raise a protocol error.
        /// </para>
        /// </summary>
        /// <param name="dndActions">actions supported by the data source</param>
        public void SetActions(WlDataDeviceManager.DndAction dndActions)
        {
            Marshal((ushort)RequestOpcode.SetActions, (uint)dndActions);
        }
    }
    /// wl_data_device version 3
    /// <summary>
    /// data transfer device
    /// <para>
    /// There is one wl_data_device per seat which can be obtained
    /// from the global wl_data_device_manager singleton.
    /// 
    /// A wl_data_device provides access to inter-client data transfer
    /// mechanisms such as copy-and-paste and drag-and-drop.
    /// </para>
    /// </summary>
    public sealed class WlDataDevice : WaylandClientObject
    {
        public WlDataDevice(uint id, uint version, WaylandClientConnection connection) : base("wl_data_device", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            StartDrag,
            SetSelection,
            Release,
        }
        public enum EventOpcode : ushort
        {
            DataOffer,
            Enter,
            Leave,
            Motion,
            Drop,
            Selection,
        }
        /// <summary>
        /// introduce a new wl_data_offer
        /// <para>
        /// The data_offer event introduces a new wl_data_offer object,
        /// which will subsequently be used in either the
        /// data_device.enter event (for drag-and-drop) or the
        /// data_device.selection event (for selections).  Immediately
        /// following the data_device_data_offer event, the new data_offer
        /// object will send out data_offer.offer events to describe the
        /// mime types it offers.
        /// </para>
        /// </summary>
        /// <param name="id">the new data_offer object</param>
        public delegate void DataOfferHandler(WlDataDevice wlDataDevice, WlDataOffer id);
        /// <summary>
        /// initiate drag-and-drop session
        /// <para>
        /// This event is sent when an active drag-and-drop pointer enters
        /// a surface owned by the client.  The position of the pointer at
        /// enter time is provided by the x and y arguments, in surface-local
        /// coordinates.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the enter event</param>
        /// <param name="surface">client surface entered</param>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        /// <param name="id">source data_offer object</param>
        public delegate void EnterHandler(WlDataDevice wlDataDevice, uint serial, WlSurface surface, double x, double y, WlDataOffer id);
        /// <summary>
        /// end drag-and-drop session
        /// <para>
        /// This event is sent when the drag-and-drop pointer leaves the
        /// surface and the session ends.  The client must destroy the
        /// wl_data_offer introduced at enter time at this point.
        /// </para>
        /// </summary>
        public delegate void LeaveHandler(WlDataDevice wlDataDevice);
        /// <summary>
        /// drag-and-drop session motion
        /// <para>
        /// This event is sent when the drag-and-drop pointer moves within
        /// the currently focused surface. The new position of the pointer
        /// is provided by the x and y arguments, in surface-local
        /// coordinates.
        /// </para>
        /// </summary>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        public delegate void MotionHandler(WlDataDevice wlDataDevice, uint time, double x, double y);
        /// <summary>
        /// end drag-and-drop session successfully
        /// <para>
        /// The event is sent when a drag-and-drop operation is ended
        /// because the implicit grab is removed.
        /// 
        /// The drag-and-drop destination is expected to honor the last action
        /// received through wl_data_offer.action, if the resulting action is
        /// "copy" or "move", the destination can still perform
        /// wl_data_offer.receive requests, and is expected to end all
        /// transfers with a wl_data_offer.finish request.
        /// 
        /// If the resulting action is "ask", the action will not be considered
        /// final. The drag-and-drop destination is expected to perform one last
        /// wl_data_offer.set_actions request, or wl_data_offer.destroy in order
        /// to cancel the operation.
        /// </para>
        /// </summary>
        public delegate void DropHandler(WlDataDevice wlDataDevice);
        /// <summary>
        /// advertise new selection
        /// <para>
        /// The selection event is sent out to notify the client of a new
        /// wl_data_offer for the selection for this device.  The
        /// data_device.data_offer and the data_offer.offer events are
        /// sent out immediately before this event to introduce the data
        /// offer object.  The selection event is sent to a client
        /// immediately before receiving keyboard focus and when a new
        /// selection is set while the client has keyboard focus.  The
        /// data_offer is valid until a new data_offer or NULL is received
        /// or until the client loses keyboard focus.  The client must
        /// destroy the previous selection data_offer, if any, upon receiving
        /// this event.
        /// </para>
        /// </summary>
        /// <param name="id">selection data_offer object</param>
        public delegate void SelectionHandler(WlDataDevice wlDataDevice, WlDataOffer id);
        public event DataOfferHandler DataOffer;
        public event EnterHandler Enter;
        public event LeaveHandler Leave;
        public event MotionHandler Motion;
        public event DropHandler Drop;
        public event SelectionHandler Selection;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.DataOffer:
                    {
                        var id = (WlDataOffer)arguments[0];
                        DataOffer?.Invoke(this, id);
                        break;
                    }
                case EventOpcode.Enter:
                    {
                        var serial = (uint)arguments[0];
                        var surface = (WlSurface)arguments[1];
                        var x = (double)arguments[2];
                        var y = (double)arguments[3];
                        var id = (WlDataOffer)arguments[4];
                        Enter?.Invoke(this, serial, surface, x, y, id);
                        break;
                    }
                case EventOpcode.Leave:
                    {
                        Leave?.Invoke(this);
                        break;
                    }
                case EventOpcode.Motion:
                    {
                        var time = (uint)arguments[0];
                        var x = (double)arguments[1];
                        var y = (double)arguments[2];
                        Motion?.Invoke(this, time, x, y);
                        break;
                    }
                case EventOpcode.Drop:
                    {
                        Drop?.Invoke(this);
                        break;
                    }
                case EventOpcode.Selection:
                    {
                        var id = (WlDataOffer)arguments[0];
                        Selection?.Invoke(this, id);
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
                case EventOpcode.DataOffer:
                    return new WaylandType[]
                    {
                        WaylandType.NewId,
                    };
                    break;
                case EventOpcode.Enter:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Object,
                        WaylandType.Fixed,
                        WaylandType.Fixed,
                        WaylandType.Object,
                    };
                    break;
                case EventOpcode.Leave:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Motion:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Fixed,
                        WaylandType.Fixed,
                    };
                    break;
                case EventOpcode.Drop:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Selection:
                    return new WaylandType[]
                    {
                        WaylandType.Object,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            Role = 0,
        }
        /// <summary>
        /// start drag-and-drop operation
        /// <para>
        /// This request asks the compositor to start a drag-and-drop
        /// operation on behalf of the client.
        /// 
        /// The source argument is the data source that provides the data
        /// for the eventual data transfer. If source is NULL, enter, leave
        /// and motion events are sent only to the client that initiated the
        /// drag and the client is expected to handle the data passing
        /// internally.
        /// 
        /// The origin surface is the surface where the drag originates and
        /// the client must have an active implicit grab that matches the
        /// serial.
        /// 
        /// The icon surface is an optional (can be NULL) surface that
        /// provides an icon to be moved around with the cursor.  Initially,
        /// the top-left corner of the icon surface is placed at the cursor
        /// hotspot, but subsequent wl_surface.attach request can move the
        /// relative position. Attach requests must be confirmed with
        /// wl_surface.commit as usual. The icon surface is given the role of
        /// a drag-and-drop icon. If the icon surface already has another role,
        /// it raises a protocol error.
        /// 
        /// The current and pending input regions of the icon wl_surface are
        /// cleared, and wl_surface.set_input_region is ignored until the
        /// wl_surface is no longer used as the icon surface. When the use
        /// as an icon ends, the current and pending input regions become
        /// undefined, and the wl_surface is unmapped.
        /// </para>
        /// </summary>
        /// <param name="source">data source for the eventual transfer</param>
        /// <param name="origin">surface where the drag originates</param>
        /// <param name="icon">drag-and-drop icon surface</param>
        /// <param name="serial">serial number of the implicit grab on the origin</param>
        public void StartDrag(WlDataSource source, WlSurface origin, WlSurface icon, uint serial)
        {
            Marshal((ushort)RequestOpcode.StartDrag, source.Id, origin.Id, icon.Id, serial);
        }
        /// <summary>
        /// copy data to the selection
        /// <para>
        /// This request asks the compositor to set the selection
        /// to the data from the source on behalf of the client.
        /// 
        /// To unset the selection, set the source to NULL.
        /// </para>
        /// </summary>
        /// <param name="source">data source for the selection</param>
        /// <param name="serial">serial number of the event that triggered this request</param>
        public void SetSelection(WlDataSource source, uint serial)
        {
            Marshal((ushort)RequestOpcode.SetSelection, source.Id, serial);
        }
        /// <summary>
        /// destroy data device
        /// <para>
        /// This request destroys the data device.
        /// </para>
        /// </summary>
        public void Release()
        {
            Marshal((ushort)RequestOpcode.Release);
            Die();
        }
    }
    /// wl_data_device_manager version 3
    /// <summary>
    /// data transfer interface
    /// <para>
    /// The wl_data_device_manager is a singleton global object that
    /// provides access to inter-client data transfer mechanisms such as
    /// copy-and-paste and drag-and-drop.  These mechanisms are tied to
    /// a wl_seat and this interface lets a client get a wl_data_device
    /// corresponding to a wl_seat.
    /// 
    /// Depending on the version bound, the objects created from the bound
    /// wl_data_device_manager object will have different requirements for
    /// functioning properly. See wl_data_source.set_actions,
    /// wl_data_offer.accept and wl_data_offer.finish for details.
    /// </para>
    /// </summary>
    public sealed class WlDataDeviceManager : WaylandClientObject
    {
        public WlDataDeviceManager(uint id, uint version, WaylandClientConnection connection) : base("wl_data_device_manager", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            CreateDataSource,
            GetDataDevice,
        }
        public enum EventOpcode : ushort
        {
        }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// drag and drop actions
        /// <para>
        /// This is a bitmask of the available/preferred actions in a
        /// drag-and-drop operation.
        /// 
        /// In the compositor, the selected action is a result of matching the
        /// actions offered by the source and destination sides.  "action" events
        /// with a "none" action will be sent to both source and destination if
        /// there is no match. All further checks will effectively happen on
        /// (source actions ∩ destination actions).
        /// 
        /// In addition, compositors may also pick different actions in
        /// reaction to key modifiers being pressed. One common design that
        /// is used in major toolkits (and the behavior recommended for
        /// compositors) is:
        /// 
        /// - If no modifiers are pressed, the first match (in bit order)
        /// will be used.
        /// - Pressing Shift selects "move", if enabled in the mask.
        /// - Pressing Control selects "copy", if enabled in the mask.
        /// 
        /// Behavior beyond that is considered implementation-dependent.
        /// Compositors may for example bind other modifiers (like Alt/Meta)
        /// or drags initiated with other buttons than BTN_LEFT to specific
        /// actions (e.g. "ask").
        /// </para>
        /// </summary>
        [Flags]
        public enum DndAction : int
        {
            None = 0,
            Copy = 1,
            Move = 2,
            Ask = 4,
        }
        /// <summary>
        /// create a new data source
        /// <para>
        /// Create a new data source.
        /// </para>
        /// </summary>
        /// <returns>data source to create</returns>
        public WlDataSource CreateDataSource()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.CreateDataSource, id);
            Connection[id] = new WlDataSource(id, Version, ClientConnection);
            return (WlDataSource)Connection[id];
        }
        /// <summary>
        /// create a new data device
        /// <para>
        /// Create a new data device for a given seat.
        /// </para>
        /// </summary>
        /// <returns>data device to create</returns>
        /// <param name="seat">seat associated with the data device</param>
        public WlDataDevice GetDataDevice(WlSeat seat)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetDataDevice, id, seat.Id);
            Connection[id] = new WlDataDevice(id, Version, ClientConnection);
            return (WlDataDevice)Connection[id];
        }
    }
    /// wl_shell version 1
    /// <summary>
    /// create desktop-style surfaces
    /// <para>
    /// This interface is implemented by servers that provide
    /// desktop-style user interfaces.
    /// 
    /// It allows clients to associate a wl_shell_surface with
    /// a basic surface.
    /// 
    /// Note! This protocol is deprecated and not intended for production use.
    /// For desktop-style user interfaces, use xdg_shell.
    /// </para>
    /// </summary>
    public sealed class WlShell : WaylandClientObject
    {
        public WlShell(uint id, uint version, WaylandClientConnection connection) : base("wl_shell", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            GetShellSurface,
        }
        public enum EventOpcode : ushort
        {
        }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            Role = 0,
        }
        /// <summary>
        /// create a shell surface from a surface
        /// <para>
        /// Create a shell surface for an existing surface. This gives
        /// the wl_surface the role of a shell surface. If the wl_surface
        /// already has another role, it raises a protocol error.
        /// 
        /// Only one shell surface can be associated with a given surface.
        /// </para>
        /// </summary>
        /// <returns>shell surface to create</returns>
        /// <param name="surface">surface to be given the shell surface role</param>
        public WlShellSurface GetShellSurface(WlSurface surface)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetShellSurface, id, surface.Id);
            Connection[id] = new WlShellSurface(id, Version, ClientConnection);
            return (WlShellSurface)Connection[id];
        }
    }
    /// wl_shell_surface version 1
    /// <summary>
    /// desktop-style metadata interface
    /// <para>
    /// An interface that may be implemented by a wl_surface, for
    /// implementations that provide a desktop-style user interface.
    /// 
    /// It provides requests to treat surfaces like toplevel, fullscreen
    /// or popup windows, move, resize or maximize them, associate
    /// metadata like title and class, etc.
    /// 
    /// On the server side the object is automatically destroyed when
    /// the related wl_surface is destroyed. On the client side,
    /// wl_shell_surface_destroy() must be called before destroying
    /// the wl_surface object.
    /// </para>
    /// </summary>
    public sealed class WlShellSurface : WaylandClientObject
    {
        public WlShellSurface(uint id, uint version, WaylandClientConnection connection) : base("wl_shell_surface", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Pong,
            Move,
            Resize,
            SetToplevel,
            SetTransient,
            SetFullscreen,
            SetPopup,
            SetMaximized,
            SetTitle,
            SetClass,
        }
        public enum EventOpcode : ushort
        {
            Ping,
            Configure,
            PopupDone,
        }
        /// <summary>
        /// ping client
        /// <para>
        /// Ping a client to check if it is receiving events and sending
        /// requests. A client is expected to reply with a pong request.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the ping</param>
        public delegate void PingHandler(WlShellSurface wlShellSurface, uint serial);
        /// <summary>
        /// suggest resize
        /// <para>
        /// The configure event asks the client to resize its surface.
        /// 
        /// The size is a hint, in the sense that the client is free to
        /// ignore it if it doesn't resize, pick a smaller size (to
        /// satisfy aspect ratio or resize in steps of NxM pixels).
        /// 
        /// The edges parameter provides a hint about how the surface
        /// was resized. The client may use this information to decide
        /// how to adjust its content to the new size (e.g. a scrolling
        /// area might adjust its content position to leave the viewable
        /// content unmoved).
        /// 
        /// The client is free to dismiss all but the last configure
        /// event it received.
        /// 
        /// The width and height arguments specify the size of the window
        /// in surface-local coordinates.
        /// </para>
        /// </summary>
        /// <param name="edges">how the surface was resized</param>
        /// <param name="width">new width of the surface</param>
        /// <param name="height">new height of the surface</param>
        public delegate void ConfigureHandler(WlShellSurface wlShellSurface, ResizeEnum edges, int width, int height);
        /// <summary>
        /// popup interaction is done
        /// <para>
        /// The popup_done event is sent out when a popup grab is broken,
        /// that is, when the user clicks a surface that doesn't belong
        /// to the client owning the popup surface.
        /// </para>
        /// </summary>
        public delegate void PopupDoneHandler(WlShellSurface wlShellSurface);
        public event PingHandler Ping;
        public event ConfigureHandler Configure;
        public event PopupDoneHandler PopupDone;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Ping:
                    {
                        var serial = (uint)arguments[0];
                        Ping?.Invoke(this, serial);
                        break;
                    }
                case EventOpcode.Configure:
                    {
                        var edges = (ResizeEnum)(uint)arguments[0];
                        var width = (int)arguments[1];
                        var height = (int)arguments[2];
                        Configure?.Invoke(this, edges, width, height);
                        break;
                    }
                case EventOpcode.PopupDone:
                    {
                        PopupDone?.Invoke(this);
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
                case EventOpcode.Ping:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.Configure:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Int,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.PopupDone:
                    return new WaylandType[]
                    {
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// edge values for resizing
        /// <para>
        /// These values are used to indicate which edge of a surface
        /// is being dragged in a resize operation. The server may
        /// use this information to adapt its behavior, e.g. choose
        /// an appropriate cursor image.
        /// </para>
        /// </summary>
        [Flags]
        public enum ResizeEnum : int
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 4,
            TopLeft = 5,
            BottomLeft = 6,
            Right = 8,
            TopRight = 9,
            BottomRight = 10,
        }
        /// <summary>
        /// details of transient behaviour
        /// <para>
        /// These flags specify details of the expected behaviour
        /// of transient surfaces. Used in the set_transient request.
        /// </para>
        /// </summary>
        [Flags]
        public enum Transient : int
        {
            Inactive = 1,
        }
        /// <summary>
        /// different method to set the surface fullscreen
        /// <para>
        /// Hints to indicate to the compositor how to deal with a conflict
        /// between the dimensions of the surface and the dimensions of the
        /// output. The compositor is free to ignore this parameter.
        /// </para>
        /// </summary>
        public enum FullscreenMethod : int
        {
            Default = 0,
            Scale = 1,
            Driver = 2,
            Fill = 3,
        }
        /// <summary>
        /// respond to a ping event
        /// <para>
        /// A client must respond to a ping event with a pong request or
        /// the client may be deemed unresponsive.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the ping event</param>
        public void Pong(uint serial)
        {
            Marshal((ushort)RequestOpcode.Pong, serial);
        }
        /// <summary>
        /// start an interactive move
        /// <para>
        /// Start a pointer-driven move of the surface.
        /// 
        /// This request must be used in response to a button press event.
        /// The server may ignore move requests depending on the state of
        /// the surface (e.g. fullscreen or maximized).
        /// </para>
        /// </summary>
        /// <param name="seat">seat whose pointer is used</param>
        /// <param name="serial">serial number of the implicit grab on the pointer</param>
        public void Move(WlSeat seat, uint serial)
        {
            Marshal((ushort)RequestOpcode.Move, seat.Id, serial);
        }
        /// <summary>
        /// start an interactive resize
        /// <para>
        /// Start a pointer-driven resizing of the surface.
        /// 
        /// This request must be used in response to a button press event.
        /// The server may ignore resize requests depending on the state of
        /// the surface (e.g. fullscreen or maximized).
        /// </para>
        /// </summary>
        /// <param name="seat">seat whose pointer is used</param>
        /// <param name="serial">serial number of the implicit grab on the pointer</param>
        /// <param name="edges">which edge or corner is being dragged</param>
        public void Resize(WlSeat seat, uint serial, ResizeEnum edges)
        {
            Marshal((ushort)RequestOpcode.Resize, seat.Id, serial, (uint)edges);
        }
        /// <summary>
        /// make the surface a toplevel surface
        /// <para>
        /// Map the surface as a toplevel surface.
        /// 
        /// A toplevel surface is not fullscreen, maximized or transient.
        /// </para>
        /// </summary>
        public void SetToplevel()
        {
            Marshal((ushort)RequestOpcode.SetToplevel);
        }
        /// <summary>
        /// make the surface a transient surface
        /// <para>
        /// Map the surface relative to an existing surface.
        /// 
        /// The x and y arguments specify the location of the upper left
        /// corner of the surface relative to the upper left corner of the
        /// parent surface, in surface-local coordinates.
        /// 
        /// The flags argument controls details of the transient behaviour.
        /// </para>
        /// </summary>
        /// <param name="parent">parent surface</param>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        /// <param name="flags">transient surface behavior</param>
        public void SetTransient(WlSurface parent, int x, int y, Transient flags)
        {
            Marshal((ushort)RequestOpcode.SetTransient, parent.Id, x, y, (uint)flags);
        }
        /// <summary>
        /// make the surface a fullscreen surface
        /// <para>
        /// Map the surface as a fullscreen surface.
        /// 
        /// If an output parameter is given then the surface will be made
        /// fullscreen on that output. If the client does not specify the
        /// output then the compositor will apply its policy - usually
        /// choosing the output on which the surface has the biggest surface
        /// area.
        /// 
        /// The client may specify a method to resolve a size conflict
        /// between the output size and the surface size - this is provided
        /// through the method parameter.
        /// 
        /// The framerate parameter is used only when the method is set
        /// to "driver", to indicate the preferred framerate. A value of 0
        /// indicates that the client does not care about framerate.  The
        /// framerate is specified in mHz, that is framerate of 60000 is 60Hz.
        /// 
        /// A method of "scale" or "driver" implies a scaling operation of
        /// the surface, either via a direct scaling operation or a change of
        /// the output mode. This will override any kind of output scaling, so
        /// that mapping a surface with a buffer size equal to the mode can
        /// fill the screen independent of buffer_scale.
        /// 
        /// A method of "fill" means we don't scale up the buffer, however
        /// any output scale is applied. This means that you may run into
        /// an edge case where the application maps a buffer with the same
        /// size of the output mode but buffer_scale 1 (thus making a
        /// surface larger than the output). In this case it is allowed to
        /// downscale the results to fit the screen.
        /// 
        /// The compositor must reply to this request with a configure event
        /// with the dimensions for the output on which the surface will
        /// be made fullscreen.
        /// </para>
        /// </summary>
        /// <param name="method">method for resolving size conflict</param>
        /// <param name="framerate">framerate in mHz</param>
        /// <param name="output">output on which the surface is to be fullscreen</param>
        public void SetFullscreen(FullscreenMethod method, uint framerate, WlOutput output)
        {
            Marshal((ushort)RequestOpcode.SetFullscreen, (uint)method, framerate, output.Id);
        }
        /// <summary>
        /// make the surface a popup surface
        /// <para>
        /// Map the surface as a popup.
        /// 
        /// A popup surface is a transient surface with an added pointer
        /// grab.
        /// 
        /// An existing implicit grab will be changed to owner-events mode,
        /// and the popup grab will continue after the implicit grab ends
        /// (i.e. releasing the mouse button does not cause the popup to
        /// be unmapped).
        /// 
        /// The popup grab continues until the window is destroyed or a
        /// mouse button is pressed in any other client's window. A click
        /// in any of the client's surfaces is reported as normal, however,
        /// clicks in other clients' surfaces will be discarded and trigger
        /// the callback.
        /// 
        /// The x and y arguments specify the location of the upper left
        /// corner of the surface relative to the upper left corner of the
        /// parent surface, in surface-local coordinates.
        /// </para>
        /// </summary>
        /// <param name="seat">seat whose pointer is used</param>
        /// <param name="serial">serial number of the implicit grab on the pointer</param>
        /// <param name="parent">parent surface</param>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        /// <param name="flags">transient surface behavior</param>
        public void SetPopup(WlSeat seat, uint serial, WlSurface parent, int x, int y, Transient flags)
        {
            Marshal((ushort)RequestOpcode.SetPopup, seat.Id, serial, parent.Id, x, y, (uint)flags);
        }
        /// <summary>
        /// make the surface a maximized surface
        /// <para>
        /// Map the surface as a maximized surface.
        /// 
        /// If an output parameter is given then the surface will be
        /// maximized on that output. If the client does not specify the
        /// output then the compositor will apply its policy - usually
        /// choosing the output on which the surface has the biggest surface
        /// area.
        /// 
        /// The compositor will reply with a configure event telling
        /// the expected new surface size. The operation is completed
        /// on the next buffer attach to this surface.
        /// 
        /// A maximized surface typically fills the entire output it is
        /// bound to, except for desktop elements such as panels. This is
        /// the main difference between a maximized shell surface and a
        /// fullscreen shell surface.
        /// 
        /// The details depend on the compositor implementation.
        /// </para>
        /// </summary>
        /// <param name="output">output on which the surface is to be maximized</param>
        public void SetMaximized(WlOutput output)
        {
            Marshal((ushort)RequestOpcode.SetMaximized, output.Id);
        }
        /// <summary>
        /// set surface title
        /// <para>
        /// Set a short title for the surface.
        /// 
        /// This string may be used to identify the surface in a task bar,
        /// window list, or other user interface elements provided by the
        /// compositor.
        /// 
        /// The string must be encoded in UTF-8.
        /// </para>
        /// </summary>
        /// <param name="title">surface title</param>
        public void SetTitle(string title)
        {
            Marshal((ushort)RequestOpcode.SetTitle, title);
        }
        /// <summary>
        /// set surface class
        /// <para>
        /// Set a class for the surface.
        /// 
        /// The surface class identifies the general class of applications
        /// to which the surface belongs. A common convention is to use the
        /// file name (or the full path if it is a non-standard location) of
        /// the application's .desktop file as the class.
        /// </para>
        /// </summary>
        /// <param name="@class">surface class</param>
        public void SetClass(string @class)
        {
            Marshal((ushort)RequestOpcode.SetClass, @class);
        }
    }
    /// wl_surface version 4
    /// <summary>
    /// an onscreen surface
    /// <para>
    /// A surface is a rectangular area that may be displayed on zero
    /// or more outputs, and shown any number of times at the compositor's
    /// discretion. They can present wl_buffers, receive user input, and
    /// define a local coordinate system.
    /// 
    /// The size of a surface (and relative positions on it) is described
    /// in surface-local coordinates, which may differ from the buffer
    /// coordinates of the pixel content, in case a buffer_transform
    /// or a buffer_scale is used.
    /// 
    /// A surface without a "role" is fairly useless: a compositor does
    /// not know where, when or how to present it. The role is the
    /// purpose of a wl_surface. Examples of roles are a cursor for a
    /// pointer (as set by wl_pointer.set_cursor), a drag icon
    /// (wl_data_device.start_drag), a sub-surface
    /// (wl_subcompositor.get_subsurface), and a window as defined by a
    /// shell protocol (e.g. wl_shell.get_shell_surface).
    /// 
    /// A surface can have only one role at a time. Initially a
    /// wl_surface does not have a role. Once a wl_surface is given a
    /// role, it is set permanently for the whole lifetime of the
    /// wl_surface object. Giving the current role again is allowed,
    /// unless explicitly forbidden by the relevant interface
    /// specification.
    /// 
    /// Surface roles are given by requests in other interfaces such as
    /// wl_pointer.set_cursor. The request should explicitly mention
    /// that this request gives a role to a wl_surface. Often, this
    /// request also creates a new protocol object that represents the
    /// role and adds additional functionality to wl_surface. When a
    /// client wants to destroy a wl_surface, they must destroy this 'role
    /// object' before the wl_surface.
    /// 
    /// Destroying the role object does not remove the role from the
    /// wl_surface, but it may stop the wl_surface from "playing the role".
    /// For instance, if a wl_subsurface object is destroyed, the wl_surface
    /// it was created for will be unmapped and forget its position and
    /// z-order. It is allowed to create a wl_subsurface for the same
    /// wl_surface again, but it is not allowed to use the wl_surface as
    /// a cursor (cursor is a different role than sub-surface, and role
    /// switching is not allowed).
    /// </para>
    /// </summary>
    public sealed class WlSurface : WaylandClientObject
    {
        public WlSurface(uint id, uint version, WaylandClientConnection connection) : base("wl_surface", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            Attach,
            Damage,
            Frame,
            SetOpaqueRegion,
            SetInputRegion,
            Commit,
            SetBufferTransform,
            SetBufferScale,
            DamageBuffer,
        }
        public enum EventOpcode : ushort
        {
            Enter,
            Leave,
        }
        /// <summary>
        /// surface enters an output
        /// <para>
        /// This is emitted whenever a surface's creation, movement, or resizing
        /// results in some part of it being within the scanout region of an
        /// output.
        /// 
        /// Note that a surface may be overlapping with zero or more outputs.
        /// </para>
        /// </summary>
        /// <param name="output">output entered by the surface</param>
        public delegate void EnterHandler(WlSurface wlSurface, WlOutput output);
        /// <summary>
        /// surface leaves an output
        /// <para>
        /// This is emitted whenever a surface's creation, movement, or resizing
        /// results in it no longer having any part of it within the scanout region
        /// of an output.
        /// </para>
        /// </summary>
        /// <param name="output">output left by the surface</param>
        public delegate void LeaveHandler(WlSurface wlSurface, WlOutput output);
        public event EnterHandler Enter;
        public event LeaveHandler Leave;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Enter:
                    {
                        var output = (WlOutput)arguments[0];
                        Enter?.Invoke(this, output);
                        break;
                    }
                case EventOpcode.Leave:
                    {
                        var output = (WlOutput)arguments[0];
                        Leave?.Invoke(this, output);
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
                case EventOpcode.Enter:
                    return new WaylandType[]
                    {
                        WaylandType.Object,
                    };
                    break;
                case EventOpcode.Leave:
                    return new WaylandType[]
                    {
                        WaylandType.Object,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// wl_surface error values
        /// <para>
        /// These errors can be emitted in response to wl_surface requests.
        /// </para>
        /// </summary>
        public enum Error : int
        {
            InvalidScale = 0,
            InvalidTransform = 1,
        }
        /// <summary>
        /// delete surface
        /// <para>
        /// Deletes the surface and invalidates its object ID.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// set the surface contents
        /// <para>
        /// Set a buffer as the content of this surface.
        /// 
        /// The new size of the surface is calculated based on the buffer
        /// size transformed by the inverse buffer_transform and the
        /// inverse buffer_scale. This means that the supplied buffer
        /// must be an integer multiple of the buffer_scale.
        /// 
        /// The x and y arguments specify the location of the new pending
        /// buffer's upper left corner, relative to the current buffer's upper
        /// left corner, in surface-local coordinates. In other words, the
        /// x and y, combined with the new surface size define in which
        /// directions the surface's size changes.
        /// 
        /// Surface contents are double-buffered state, see wl_surface.commit.
        /// 
        /// The initial surface contents are void; there is no content.
        /// wl_surface.attach assigns the given wl_buffer as the pending
        /// wl_buffer. wl_surface.commit makes the pending wl_buffer the new
        /// surface contents, and the size of the surface becomes the size
        /// calculated from the wl_buffer, as described above. After commit,
        /// there is no pending buffer until the next attach.
        /// 
        /// Committing a pending wl_buffer allows the compositor to read the
        /// pixels in the wl_buffer. The compositor may access the pixels at
        /// any time after the wl_surface.commit request. When the compositor
        /// will not access the pixels anymore, it will send the
        /// wl_buffer.release event. Only after receiving wl_buffer.release,
        /// the client may reuse the wl_buffer. A wl_buffer that has been
        /// attached and then replaced by another attach instead of committed
        /// will not receive a release event, and is not used by the
        /// compositor.
        /// 
        /// If a pending wl_buffer has been committed to more than one wl_surface,
        /// the delivery of wl_buffer.release events becomes undefined. A well
        /// behaved client should not rely on wl_buffer.release events in this
        /// case. Alternatively, a client could create multiple wl_buffer objects
        /// from the same backing storage or use wp_linux_buffer_release.
        /// 
        /// Destroying the wl_buffer after wl_buffer.release does not change
        /// the surface contents. However, if the client destroys the
        /// wl_buffer before receiving the wl_buffer.release event, the surface
        /// contents become undefined immediately.
        /// 
        /// If wl_surface.attach is sent with a NULL wl_buffer, the
        /// following wl_surface.commit will remove the surface content.
        /// </para>
        /// </summary>
        /// <param name="buffer">buffer of surface contents</param>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        public void Attach(WlBuffer buffer, int x, int y)
        {
            Marshal((ushort)RequestOpcode.Attach, buffer.Id, x, y);
        }
        /// <summary>
        /// mark part of the surface damaged
        /// <para>
        /// This request is used to describe the regions where the pending
        /// buffer is different from the current surface contents, and where
        /// the surface therefore needs to be repainted. The compositor
        /// ignores the parts of the damage that fall outside of the surface.
        /// 
        /// Damage is double-buffered state, see wl_surface.commit.
        /// 
        /// The damage rectangle is specified in surface-local coordinates,
        /// where x and y specify the upper left corner of the damage rectangle.
        /// 
        /// The initial value for pending damage is empty: no damage.
        /// wl_surface.damage adds pending damage: the new pending damage
        /// is the union of old pending damage and the given rectangle.
        /// 
        /// wl_surface.commit assigns pending damage as the current damage,
        /// and clears pending damage. The server will clear the current
        /// damage as it repaints the surface.
        /// 
        /// Note! New clients should not use this request. Instead damage can be
        /// posted with wl_surface.damage_buffer which uses buffer coordinates
        /// instead of surface coordinates.
        /// </para>
        /// </summary>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        /// <param name="width">width of damage rectangle</param>
        /// <param name="height">height of damage rectangle</param>
        public void Damage(int x, int y, int width, int height)
        {
            Marshal((ushort)RequestOpcode.Damage, x, y, width, height);
        }
        /// <summary>
        /// request a frame throttling hint
        /// <para>
        /// Request a notification when it is a good time to start drawing a new
        /// frame, by creating a frame callback. This is useful for throttling
        /// redrawing operations, and driving animations.
        /// 
        /// When a client is animating on a wl_surface, it can use the 'frame'
        /// request to get notified when it is a good time to draw and commit the
        /// next frame of animation. If the client commits an update earlier than
        /// that, it is likely that some updates will not make it to the display,
        /// and the client is wasting resources by drawing too often.
        /// 
        /// The frame request will take effect on the next wl_surface.commit.
        /// The notification will only be posted for one frame unless
        /// requested again. For a wl_surface, the notifications are posted in
        /// the order the frame requests were committed.
        /// 
        /// The server must send the notifications so that a client
        /// will not send excessive updates, while still allowing
        /// the highest possible update rate for clients that wait for the reply
        /// before drawing again. The server should give some time for the client
        /// to draw and commit after sending the frame callback events to let it
        /// hit the next output refresh.
        /// 
        /// A server should avoid signaling the frame callbacks if the
        /// surface is not visible in any way, e.g. the surface is off-screen,
        /// or completely obscured by other opaque surfaces.
        /// 
        /// The object returned by this request will be destroyed by the
        /// compositor after the callback is fired and as such the client must not
        /// attempt to use it after that point.
        /// 
        /// The callback_data passed in the callback is the current time, in
        /// milliseconds, with an undefined base.
        /// </para>
        /// </summary>
        /// <returns>callback object for the frame request</returns>
        public WlCallback Frame()
        {
            uint callback = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.Frame, callback);
            Connection[callback] = new WlCallback(callback, Version, ClientConnection);
            return (WlCallback)Connection[callback];
        }
        /// <summary>
        /// set opaque region
        /// <para>
        /// This request sets the region of the surface that contains
        /// opaque content.
        /// 
        /// The opaque region is an optimization hint for the compositor
        /// that lets it optimize the redrawing of content behind opaque
        /// regions.  Setting an opaque region is not required for correct
        /// behaviour, but marking transparent content as opaque will result
        /// in repaint artifacts.
        /// 
        /// The opaque region is specified in surface-local coordinates.
        /// 
        /// The compositor ignores the parts of the opaque region that fall
        /// outside of the surface.
        /// 
        /// Opaque region is double-buffered state, see wl_surface.commit.
        /// 
        /// wl_surface.set_opaque_region changes the pending opaque region.
        /// wl_surface.commit copies the pending region to the current region.
        /// Otherwise, the pending and current regions are never changed.
        /// 
        /// The initial value for an opaque region is empty. Setting the pending
        /// opaque region has copy semantics, and the wl_region object can be
        /// destroyed immediately. A NULL wl_region causes the pending opaque
        /// region to be set to empty.
        /// </para>
        /// </summary>
        /// <param name="region">opaque region of the surface</param>
        public void SetOpaqueRegion(WlRegion region)
        {
            Marshal((ushort)RequestOpcode.SetOpaqueRegion, region.Id);
        }
        /// <summary>
        /// set input region
        /// <para>
        /// This request sets the region of the surface that can receive
        /// pointer and touch events.
        /// 
        /// Input events happening outside of this region will try the next
        /// surface in the server surface stack. The compositor ignores the
        /// parts of the input region that fall outside of the surface.
        /// 
        /// The input region is specified in surface-local coordinates.
        /// 
        /// Input region is double-buffered state, see wl_surface.commit.
        /// 
        /// wl_surface.set_input_region changes the pending input region.
        /// wl_surface.commit copies the pending region to the current region.
        /// Otherwise the pending and current regions are never changed,
        /// except cursor and icon surfaces are special cases, see
        /// wl_pointer.set_cursor and wl_data_device.start_drag.
        /// 
        /// The initial value for an input region is infinite. That means the
        /// whole surface will accept input. Setting the pending input region
        /// has copy semantics, and the wl_region object can be destroyed
        /// immediately. A NULL wl_region causes the input region to be set
        /// to infinite.
        /// </para>
        /// </summary>
        /// <param name="region">input region of the surface</param>
        public void SetInputRegion(WlRegion region)
        {
            Marshal((ushort)RequestOpcode.SetInputRegion, region.Id);
        }
        /// <summary>
        /// commit pending surface state
        /// <para>
        /// Surface state (input, opaque, and damage regions, attached buffers,
        /// etc.) is double-buffered. Protocol requests modify the pending state,
        /// as opposed to the current state in use by the compositor. A commit
        /// request atomically applies all pending state, replacing the current
        /// state. After commit, the new pending state is as documented for each
        /// related request.
        /// 
        /// On commit, a pending wl_buffer is applied first, and all other state
        /// second. This means that all coordinates in double-buffered state are
        /// relative to the new wl_buffer coming into use, except for
        /// wl_surface.attach itself. If there is no pending wl_buffer, the
        /// coordinates are relative to the current surface contents.
        /// 
        /// All requests that need a commit to become effective are documented
        /// to affect double-buffered state.
        /// 
        /// Other interfaces may add further double-buffered surface state.
        /// </para>
        /// </summary>
        public void Commit()
        {
            Marshal((ushort)RequestOpcode.Commit);
        }
        /// <summary>
        /// sets the buffer transformation
        /// <para>
        /// This request sets an optional transformation on how the compositor
        /// interprets the contents of the buffer attached to the surface. The
        /// accepted values for the transform parameter are the values for
        /// wl_output.transform.
        /// 
        /// Buffer transform is double-buffered state, see wl_surface.commit.
        /// 
        /// A newly created surface has its buffer transformation set to normal.
        /// 
        /// wl_surface.set_buffer_transform changes the pending buffer
        /// transformation. wl_surface.commit copies the pending buffer
        /// transformation to the current one. Otherwise, the pending and current
        /// values are never changed.
        /// 
        /// The purpose of this request is to allow clients to render content
        /// according to the output transform, thus permitting the compositor to
        /// use certain optimizations even if the display is rotated. Using
        /// hardware overlays and scanning out a client buffer for fullscreen
        /// surfaces are examples of such optimizations. Those optimizations are
        /// highly dependent on the compositor implementation, so the use of this
        /// request should be considered on a case-by-case basis.
        /// 
        /// Note that if the transform value includes 90 or 270 degree rotation,
        /// the width of the buffer will become the surface height and the height
        /// of the buffer will become the surface width.
        /// 
        /// If transform is not one of the values from the
        /// wl_output.transform enum the invalid_transform protocol error
        /// is raised.
        /// </para>
        /// </summary>
        /// <param name="transform">transform for interpreting buffer contents</param>
        public void SetBufferTransform(WlOutput.Transform transform)
        {
            Marshal((ushort)RequestOpcode.SetBufferTransform, (int)transform);
        }
        /// <summary>
        /// sets the buffer scaling factor
        /// <para>
        /// This request sets an optional scaling factor on how the compositor
        /// interprets the contents of the buffer attached to the window.
        /// 
        /// Buffer scale is double-buffered state, see wl_surface.commit.
        /// 
        /// A newly created surface has its buffer scale set to 1.
        /// 
        /// wl_surface.set_buffer_scale changes the pending buffer scale.
        /// wl_surface.commit copies the pending buffer scale to the current one.
        /// Otherwise, the pending and current values are never changed.
        /// 
        /// The purpose of this request is to allow clients to supply higher
        /// resolution buffer data for use on high resolution outputs. It is
        /// intended that you pick the same buffer scale as the scale of the
        /// output that the surface is displayed on. This means the compositor
        /// can avoid scaling when rendering the surface on that output.
        /// 
        /// Note that if the scale is larger than 1, then you have to attach
        /// a buffer that is larger (by a factor of scale in each dimension)
        /// than the desired surface size.
        /// 
        /// If scale is not positive the invalid_scale protocol error is
        /// raised.
        /// </para>
        /// </summary>
        /// <param name="scale">positive scale for interpreting buffer contents</param>
        public void SetBufferScale(int scale)
        {
            Marshal((ushort)RequestOpcode.SetBufferScale, scale);
        }
        /// <summary>
        /// mark part of the surface damaged using buffer coordinates
        /// <para>
        /// This request is used to describe the regions where the pending
        /// buffer is different from the current surface contents, and where
        /// the surface therefore needs to be repainted. The compositor
        /// ignores the parts of the damage that fall outside of the surface.
        /// 
        /// Damage is double-buffered state, see wl_surface.commit.
        /// 
        /// The damage rectangle is specified in buffer coordinates,
        /// where x and y specify the upper left corner of the damage rectangle.
        /// 
        /// The initial value for pending damage is empty: no damage.
        /// wl_surface.damage_buffer adds pending damage: the new pending
        /// damage is the union of old pending damage and the given rectangle.
        /// 
        /// wl_surface.commit assigns pending damage as the current damage,
        /// and clears pending damage. The server will clear the current
        /// damage as it repaints the surface.
        /// 
        /// This request differs from wl_surface.damage in only one way - it
        /// takes damage in buffer coordinates instead of surface-local
        /// coordinates. While this generally is more intuitive than surface
        /// coordinates, it is especially desirable when using wp_viewport
        /// or when a drawing library (like EGL) is unaware of buffer scale
        /// and buffer transform.
        /// 
        /// Note: Because buffer transformation changes and damage requests may
        /// be interleaved in the protocol stream, it is impossible to determine
        /// the actual mapping between surface and buffer damage until
        /// wl_surface.commit time. Therefore, compositors wishing to take both
        /// kinds of damage into account will have to accumulate damage from the
        /// two requests separately and only transform from one to the other
        /// after receiving the wl_surface.commit.
        /// </para>
        /// </summary>
        /// <param name="x">buffer-local x coordinate</param>
        /// <param name="y">buffer-local y coordinate</param>
        /// <param name="width">width of damage rectangle</param>
        /// <param name="height">height of damage rectangle</param>
        public void DamageBuffer(int x, int y, int width, int height)
        {
            Marshal((ushort)RequestOpcode.DamageBuffer, x, y, width, height);
        }
    }
    /// wl_seat version 7
    /// <summary>
    /// group of input devices
    /// <para>
    /// A seat is a group of keyboards, pointer and touch devices. This
    /// object is published as a global during start up, or when such a
    /// device is hot plugged.  A seat typically has a pointer and
    /// maintains a keyboard focus and a pointer focus.
    /// </para>
    /// </summary>
    public sealed class WlSeat : WaylandClientObject
    {
        public WlSeat(uint id, uint version, WaylandClientConnection connection) : base("wl_seat", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            GetPointer,
            GetKeyboard,
            GetTouch,
            Release,
        }
        public enum EventOpcode : ushort
        {
            Capabilities,
            Name,
        }
        /// <summary>
        /// seat capabilities changed
        /// <para>
        /// This is emitted whenever a seat gains or loses the pointer,
        /// keyboard or touch capabilities.  The argument is a capability
        /// enum containing the complete set of capabilities this seat has.
        /// 
        /// When the pointer capability is added, a client may create a
        /// wl_pointer object using the wl_seat.get_pointer request. This object
        /// will receive pointer events until the capability is removed in the
        /// future.
        /// 
        /// When the pointer capability is removed, a client should destroy the
        /// wl_pointer objects associated with the seat where the capability was
        /// removed, using the wl_pointer.release request. No further pointer
        /// events will be received on these objects.
        /// 
        /// In some compositors, if a seat regains the pointer capability and a
        /// client has a previously obtained wl_pointer object of version 4 or
        /// less, that object may start sending pointer events again. This
        /// behavior is considered a misinterpretation of the intended behavior
        /// and must not be relied upon by the client. wl_pointer objects of
        /// version 5 or later must not send events if created before the most
        /// recent event notifying the client of an added pointer capability.
        /// 
        /// The above behavior also applies to wl_keyboard and wl_touch with the
        /// keyboard and touch capabilities, respectively.
        /// </para>
        /// </summary>
        /// <param name="capabilities">capabilities of the seat</param>
        public delegate void CapabilitiesHandler(WlSeat wlSeat, Capability capabilities);
        /// <summary>
        /// unique identifier for this seat
        /// <para>
        /// In a multiseat configuration this can be used by the client to help
        /// identify which physical devices the seat represents. Based on
        /// the seat configuration used by the compositor.
        /// </para>
        /// </summary>
        /// <param name="name">seat identifier</param>
        public delegate void NameHandler(WlSeat wlSeat, string name);
        public event CapabilitiesHandler Capabilities;
        public event NameHandler Name;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Capabilities:
                    {
                        var capabilities = (Capability)(uint)arguments[0];
                        Capabilities?.Invoke(this, capabilities);
                        break;
                    }
                case EventOpcode.Name:
                    {
                        var name = (string)arguments[0];
                        Name?.Invoke(this, name);
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
                case EventOpcode.Capabilities:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.Name:
                    return new WaylandType[]
                    {
                        WaylandType.String,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// seat capability bitmask
        /// <para>
        /// This is a bitmask of capabilities this seat has; if a member is
        /// set, then it is present on the seat.
        /// </para>
        /// </summary>
        [Flags]
        public enum Capability : int
        {
            Pointer = 1,
            Keyboard = 2,
            Touch = 4,
        }
        /// <summary>
        /// return pointer object
        /// <para>
        /// The ID provided will be initialized to the wl_pointer interface
        /// for this seat.
        /// 
        /// This request only takes effect if the seat has the pointer
        /// capability, or has had the pointer capability in the past.
        /// It is a protocol violation to issue this request on a seat that has
        /// never had the pointer capability.
        /// </para>
        /// </summary>
        /// <returns>seat pointer</returns>
        public WlPointer GetPointer()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetPointer, id);
            Connection[id] = new WlPointer(id, Version, ClientConnection);
            return (WlPointer)Connection[id];
        }
        /// <summary>
        /// return keyboard object
        /// <para>
        /// The ID provided will be initialized to the wl_keyboard interface
        /// for this seat.
        /// 
        /// This request only takes effect if the seat has the keyboard
        /// capability, or has had the keyboard capability in the past.
        /// It is a protocol violation to issue this request on a seat that has
        /// never had the keyboard capability.
        /// </para>
        /// </summary>
        /// <returns>seat keyboard</returns>
        public WlKeyboard GetKeyboard()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetKeyboard, id);
            Connection[id] = new WlKeyboard(id, Version, ClientConnection);
            return (WlKeyboard)Connection[id];
        }
        /// <summary>
        /// return touch object
        /// <para>
        /// The ID provided will be initialized to the wl_touch interface
        /// for this seat.
        /// 
        /// This request only takes effect if the seat has the touch
        /// capability, or has had the touch capability in the past.
        /// It is a protocol violation to issue this request on a seat that has
        /// never had the touch capability.
        /// </para>
        /// </summary>
        /// <returns>seat touch interface</returns>
        public WlTouch GetTouch()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetTouch, id);
            Connection[id] = new WlTouch(id, Version, ClientConnection);
            return (WlTouch)Connection[id];
        }
        /// <summary>
        /// release the seat object
        /// <para>
        /// Using this request a client can tell the server that it is not going to
        /// use the seat object anymore.
        /// </para>
        /// </summary>
        public void Release()
        {
            Marshal((ushort)RequestOpcode.Release);
            Die();
        }
    }
    /// wl_pointer version 7
    /// <summary>
    /// pointer input device
    /// <para>
    /// The wl_pointer interface represents one or more input devices,
    /// such as mice, which control the pointer location and pointer_focus
    /// of a seat.
    /// 
    /// The wl_pointer interface generates motion, enter and leave
    /// events for the surfaces that the pointer is located over,
    /// and button and axis events for button presses, button releases
    /// and scrolling.
    /// </para>
    /// </summary>
    public sealed class WlPointer : WaylandClientObject
    {
        public WlPointer(uint id, uint version, WaylandClientConnection connection) : base("wl_pointer", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            SetCursor,
            Release,
        }
        public enum EventOpcode : ushort
        {
            Enter,
            Leave,
            Motion,
            Button,
            Axis,
            Frame,
            AxisSource,
            AxisStop,
            AxisDiscrete,
        }
        /// <summary>
        /// enter event
        /// <para>
        /// Notification that this seat's pointer is focused on a certain
        /// surface.
        /// 
        /// When a seat's focus enters a surface, the pointer image
        /// is undefined and a client should respond to this event by setting
        /// an appropriate pointer image with the set_cursor request.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the enter event</param>
        /// <param name="surface">surface entered by the pointer</param>
        /// <param name="surfaceX">surface-local x coordinate</param>
        /// <param name="surfaceY">surface-local y coordinate</param>
        public delegate void EnterHandler(WlPointer wlPointer, uint serial, WlSurface surface, double surfaceX, double surfaceY);
        /// <summary>
        /// leave event
        /// <para>
        /// Notification that this seat's pointer is no longer focused on
        /// a certain surface.
        /// 
        /// The leave notification is sent before the enter notification
        /// for the new focus.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the leave event</param>
        /// <param name="surface">surface left by the pointer</param>
        public delegate void LeaveHandler(WlPointer wlPointer, uint serial, WlSurface surface);
        /// <summary>
        /// pointer motion event
        /// <para>
        /// Notification of pointer location change. The arguments
        /// surface_x and surface_y are the location relative to the
        /// focused surface.
        /// </para>
        /// </summary>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="surfaceX">surface-local x coordinate</param>
        /// <param name="surfaceY">surface-local y coordinate</param>
        public delegate void MotionHandler(WlPointer wlPointer, uint time, double surfaceX, double surfaceY);
        /// <summary>
        /// pointer button event
        /// <para>
        /// Mouse button click and release notifications.
        /// 
        /// The location of the click is given by the last motion or
        /// enter event.
        /// The time argument is a timestamp with millisecond
        /// granularity, with an undefined base.
        /// 
        /// The button is a button code as defined in the Linux kernel's
        /// linux/input-event-codes.h header file, e.g. BTN_LEFT.
        /// 
        /// Any 16-bit button code value is reserved for future additions to the
        /// kernel's event code list. All other button codes above 0xFFFF are
        /// currently undefined but may be used in future versions of this
        /// protocol.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the button event</param>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="button">button that produced the event</param>
        /// <param name="state">physical state of the button</param>
        public delegate void ButtonHandler(WlPointer wlPointer, uint serial, uint time, uint button, ButtonState state);
        /// <summary>
        /// axis event
        /// <para>
        /// Scroll and other axis notifications.
        /// 
        /// For scroll events (vertical and horizontal scroll axes), the
        /// value parameter is the length of a vector along the specified
        /// axis in a coordinate space identical to those of motion events,
        /// representing a relative movement along the specified axis.
        /// 
        /// For devices that support movements non-parallel to axes multiple
        /// axis events will be emitted.
        /// 
        /// When applicable, for example for touch pads, the server can
        /// choose to emit scroll events where the motion vector is
        /// equivalent to a motion event vector.
        /// 
        /// When applicable, a client can transform its content relative to the
        /// scroll distance.
        /// </para>
        /// </summary>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="axis">axis type</param>
        /// <param name="value">length of vector in surface-local coordinate space</param>
        public delegate void AxisHandler(WlPointer wlPointer, uint time, AxisEnum axis, double value);
        /// <summary>
        /// end of a pointer event sequence
        /// <para>
        /// Indicates the end of a set of events that logically belong together.
        /// A client is expected to accumulate the data in all events within the
        /// frame before proceeding.
        /// 
        /// All wl_pointer events before a wl_pointer.frame event belong
        /// logically together. For example, in a diagonal scroll motion the
        /// compositor will send an optional wl_pointer.axis_source event, two
        /// wl_pointer.axis events (horizontal and vertical) and finally a
        /// wl_pointer.frame event. The client may use this information to
        /// calculate a diagonal vector for scrolling.
        /// 
        /// When multiple wl_pointer.axis events occur within the same frame,
        /// the motion vector is the combined motion of all events.
        /// When a wl_pointer.axis and a wl_pointer.axis_stop event occur within
        /// the same frame, this indicates that axis movement in one axis has
        /// stopped but continues in the other axis.
        /// When multiple wl_pointer.axis_stop events occur within the same
        /// frame, this indicates that these axes stopped in the same instance.
        /// 
        /// A wl_pointer.frame event is sent for every logical event group,
        /// even if the group only contains a single wl_pointer event.
        /// Specifically, a client may get a sequence: motion, frame, button,
        /// frame, axis, frame, axis_stop, frame.
        /// 
        /// The wl_pointer.enter and wl_pointer.leave events are logical events
        /// generated by the compositor and not the hardware. These events are
        /// also grouped by a wl_pointer.frame. When a pointer moves from one
        /// surface to another, a compositor should group the
        /// wl_pointer.leave event within the same wl_pointer.frame.
        /// However, a client must not rely on wl_pointer.leave and
        /// wl_pointer.enter being in the same wl_pointer.frame.
        /// Compositor-specific policies may require the wl_pointer.leave and
        /// wl_pointer.enter event being split across multiple wl_pointer.frame
        /// groups.
        /// </para>
        /// </summary>
        public delegate void FrameHandler(WlPointer wlPointer);
        /// <summary>
        /// axis source event
        /// <para>
        /// Source information for scroll and other axes.
        /// 
        /// This event does not occur on its own. It is sent before a
        /// wl_pointer.frame event and carries the source information for
        /// all events within that frame.
        /// 
        /// The source specifies how this event was generated. If the source is
        /// wl_pointer.axis_source.finger, a wl_pointer.axis_stop event will be
        /// sent when the user lifts the finger off the device.
        /// 
        /// If the source is wl_pointer.axis_source.wheel,
        /// wl_pointer.axis_source.wheel_tilt or
        /// wl_pointer.axis_source.continuous, a wl_pointer.axis_stop event may
        /// or may not be sent. Whether a compositor sends an axis_stop event
        /// for these sources is hardware-specific and implementation-dependent;
        /// clients must not rely on receiving an axis_stop event for these
        /// scroll sources and should treat scroll sequences from these scroll
        /// sources as unterminated by default.
        /// 
        /// This event is optional. If the source is unknown for a particular
        /// axis event sequence, no event is sent.
        /// Only one wl_pointer.axis_source event is permitted per frame.
        /// 
        /// The order of wl_pointer.axis_discrete and wl_pointer.axis_source is
        /// not guaranteed.
        /// </para>
        /// </summary>
        /// <param name="axisSource">source of the axis event</param>
        public delegate void AxisSourceHandler(WlPointer wlPointer, AxisSourceEnum axisSource);
        /// <summary>
        /// axis stop event
        /// <para>
        /// Stop notification for scroll and other axes.
        /// 
        /// For some wl_pointer.axis_source types, a wl_pointer.axis_stop event
        /// is sent to notify a client that the axis sequence has terminated.
        /// This enables the client to implement kinetic scrolling.
        /// See the wl_pointer.axis_source documentation for information on when
        /// this event may be generated.
        /// 
        /// Any wl_pointer.axis events with the same axis_source after this
        /// event should be considered as the start of a new axis motion.
        /// 
        /// The timestamp is to be interpreted identical to the timestamp in the
        /// wl_pointer.axis event. The timestamp value may be the same as a
        /// preceding wl_pointer.axis event.
        /// </para>
        /// </summary>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="axis">the axis stopped with this event</param>
        public delegate void AxisStopHandler(WlPointer wlPointer, uint time, AxisEnum axis);
        /// <summary>
        /// axis click event
        /// <para>
        /// Discrete step information for scroll and other axes.
        /// 
        /// This event carries the axis value of the wl_pointer.axis event in
        /// discrete steps (e.g. mouse wheel clicks).
        /// 
        /// This event does not occur on its own, it is coupled with a
        /// wl_pointer.axis event that represents this axis value on a
        /// continuous scale. The protocol guarantees that each axis_discrete
        /// event is always followed by exactly one axis event with the same
        /// axis number within the same wl_pointer.frame. Note that the protocol
        /// allows for other events to occur between the axis_discrete and
        /// its coupled axis event, including other axis_discrete or axis
        /// events.
        /// 
        /// This event is optional; continuous scrolling devices
        /// like two-finger scrolling on touchpads do not have discrete
        /// steps and do not generate this event.
        /// 
        /// The discrete value carries the directional information. e.g. a value
        /// of -2 is two steps towards the negative direction of this axis.
        /// 
        /// The axis number is identical to the axis number in the associated
        /// axis event.
        /// 
        /// The order of wl_pointer.axis_discrete and wl_pointer.axis_source is
        /// not guaranteed.
        /// </para>
        /// </summary>
        /// <param name="axis">axis type</param>
        /// <param name="discrete">number of steps</param>
        public delegate void AxisDiscreteHandler(WlPointer wlPointer, AxisEnum axis, int discrete);
        public event EnterHandler Enter;
        public event LeaveHandler Leave;
        public event MotionHandler Motion;
        public event ButtonHandler Button;
        public event AxisHandler Axis;
        public event FrameHandler Frame;
        public event AxisSourceHandler AxisSource;
        public event AxisStopHandler AxisStop;
        public event AxisDiscreteHandler AxisDiscrete;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Enter:
                    {
                        var serial = (uint)arguments[0];
                        var surface = (WlSurface)arguments[1];
                        var surfaceX = (double)arguments[2];
                        var surfaceY = (double)arguments[3];
                        Enter?.Invoke(this, serial, surface, surfaceX, surfaceY);
                        break;
                    }
                case EventOpcode.Leave:
                    {
                        var serial = (uint)arguments[0];
                        var surface = (WlSurface)arguments[1];
                        Leave?.Invoke(this, serial, surface);
                        break;
                    }
                case EventOpcode.Motion:
                    {
                        var time = (uint)arguments[0];
                        var surfaceX = (double)arguments[1];
                        var surfaceY = (double)arguments[2];
                        Motion?.Invoke(this, time, surfaceX, surfaceY);
                        break;
                    }
                case EventOpcode.Button:
                    {
                        var serial = (uint)arguments[0];
                        var time = (uint)arguments[1];
                        var button = (uint)arguments[2];
                        var state = (ButtonState)(uint)arguments[3];
                        Button?.Invoke(this, serial, time, button, state);
                        break;
                    }
                case EventOpcode.Axis:
                    {
                        var time = (uint)arguments[0];
                        var axis = (AxisEnum)(uint)arguments[1];
                        var value = (double)arguments[2];
                        Axis?.Invoke(this, time, axis, value);
                        break;
                    }
                case EventOpcode.Frame:
                    {
                        Frame?.Invoke(this);
                        break;
                    }
                case EventOpcode.AxisSource:
                    {
                        var axisSource = (AxisSourceEnum)(uint)arguments[0];
                        AxisSource?.Invoke(this, axisSource);
                        break;
                    }
                case EventOpcode.AxisStop:
                    {
                        var time = (uint)arguments[0];
                        var axis = (AxisEnum)(uint)arguments[1];
                        AxisStop?.Invoke(this, time, axis);
                        break;
                    }
                case EventOpcode.AxisDiscrete:
                    {
                        var axis = (AxisEnum)(uint)arguments[0];
                        var discrete = (int)arguments[1];
                        AxisDiscrete?.Invoke(this, axis, discrete);
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
                case EventOpcode.Enter:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Object,
                        WaylandType.Fixed,
                        WaylandType.Fixed,
                    };
                    break;
                case EventOpcode.Leave:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Object,
                    };
                    break;
                case EventOpcode.Motion:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Fixed,
                        WaylandType.Fixed,
                    };
                    break;
                case EventOpcode.Button:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.Axis:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.Fixed,
                    };
                    break;
                case EventOpcode.Frame:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.AxisSource:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.AxisStop:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.AxisDiscrete:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Int,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            Role = 0,
        }
        /// <summary>
        /// physical button state
        /// <para>
        /// Describes the physical state of a button that produced the button
        /// event.
        /// </para>
        /// </summary>
        public enum ButtonState : int
        {
            Released = 0,
            Pressed = 1,
        }
        /// <summary>
        /// axis types
        /// <para>
        /// Describes the axis types of scroll events.
        /// </para>
        /// </summary>
        public enum AxisEnum : int
        {
            VerticalScroll = 0,
            HorizontalScroll = 1,
        }
        /// <summary>
        /// axis source types
        /// <para>
        /// Describes the source types for axis events. This indicates to the
        /// client how an axis event was physically generated; a client may
        /// adjust the user interface accordingly. For example, scroll events
        /// from a "finger" source may be in a smooth coordinate space with
        /// kinetic scrolling whereas a "wheel" source may be in discrete steps
        /// of a number of lines.
        /// 
        /// The "continuous" axis source is a device generating events in a
        /// continuous coordinate space, but using something other than a
        /// finger. One example for this source is button-based scrolling where
        /// the vertical motion of a device is converted to scroll events while
        /// a button is held down.
        /// 
        /// The "wheel tilt" axis source indicates that the actual device is a
        /// wheel but the scroll event is not caused by a rotation but a
        /// (usually sideways) tilt of the wheel.
        /// </para>
        /// </summary>
        public enum AxisSourceEnum : int
        {
            Wheel = 0,
            Finger = 1,
            Continuous = 2,
            WheelTilt = 3,
        }
        /// <summary>
        /// set the pointer surface
        /// <para>
        /// Set the pointer surface, i.e., the surface that contains the
        /// pointer image (cursor). This request gives the surface the role
        /// of a cursor. If the surface already has another role, it raises
        /// a protocol error.
        /// 
        /// The cursor actually changes only if the pointer
        /// focus for this device is one of the requesting client's surfaces
        /// or the surface parameter is the current pointer surface. If
        /// there was a previous surface set with this request it is
        /// replaced. If surface is NULL, the pointer image is hidden.
        /// 
        /// The parameters hotspot_x and hotspot_y define the position of
        /// the pointer surface relative to the pointer location. Its
        /// top-left corner is always at (x, y) - (hotspot_x, hotspot_y),
        /// where (x, y) are the coordinates of the pointer location, in
        /// surface-local coordinates.
        /// 
        /// On surface.attach requests to the pointer surface, hotspot_x
        /// and hotspot_y are decremented by the x and y parameters
        /// passed to the request. Attach must be confirmed by
        /// wl_surface.commit as usual.
        /// 
        /// The hotspot can also be updated by passing the currently set
        /// pointer surface to this request with new values for hotspot_x
        /// and hotspot_y.
        /// 
        /// The current and pending input regions of the wl_surface are
        /// cleared, and wl_surface.set_input_region is ignored until the
        /// wl_surface is no longer used as the cursor. When the use as a
        /// cursor ends, the current and pending input regions become
        /// undefined, and the wl_surface is unmapped.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the enter event</param>
        /// <param name="surface">pointer surface</param>
        /// <param name="hotspotX">surface-local x coordinate</param>
        /// <param name="hotspotY">surface-local y coordinate</param>
        public void SetCursor(uint serial, WlSurface surface, int hotspotX, int hotspotY)
        {
            Marshal((ushort)RequestOpcode.SetCursor, serial, surface.Id, hotspotX, hotspotY);
        }
        /// <summary>
        /// release the pointer object
        /// <para>
        /// Using this request a client can tell the server that it is not going to
        /// use the pointer object anymore.
        /// 
        /// This request destroys the pointer proxy object, so clients must not call
        /// wl_pointer_destroy() after using this request.
        /// </para>
        /// </summary>
        public void Release()
        {
            Marshal((ushort)RequestOpcode.Release);
            Die();
        }
    }
    /// wl_keyboard version 7
    /// <summary>
    /// keyboard input device
    /// <para>
    /// The wl_keyboard interface represents one or more keyboards
    /// associated with a seat.
    /// </para>
    /// </summary>
    public sealed class WlKeyboard : WaylandClientObject
    {
        public WlKeyboard(uint id, uint version, WaylandClientConnection connection) : base("wl_keyboard", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Release,
        }
        public enum EventOpcode : ushort
        {
            Keymap,
            Enter,
            Leave,
            Key,
            Modifiers,
            RepeatInfo,
        }
        /// <summary>
        /// keyboard mapping
        /// <para>
        /// This event provides a file descriptor to the client which can be
        /// memory-mapped to provide a keyboard mapping description.
        /// 
        /// From version 7 onwards, the fd must be mapped with MAP_PRIVATE by
        /// the recipient, as MAP_SHARED may fail.
        /// </para>
        /// </summary>
        /// <param name="format">keymap format</param>
        /// <param name="fd">keymap file descriptor</param>
        /// <param name="size">keymap size, in bytes</param>
        public delegate void KeymapHandler(WlKeyboard wlKeyboard, KeymapFormat format, IntPtr fd, uint size);
        /// <summary>
        /// enter event
        /// <para>
        /// Notification that this seat's keyboard focus is on a certain
        /// surface.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the enter event</param>
        /// <param name="surface">surface gaining keyboard focus</param>
        /// <param name="keys">the currently pressed keys</param>
        public delegate void EnterHandler(WlKeyboard wlKeyboard, uint serial, WlSurface surface, byte[] keys);
        /// <summary>
        /// leave event
        /// <para>
        /// Notification that this seat's keyboard focus is no longer on
        /// a certain surface.
        /// 
        /// The leave notification is sent before the enter notification
        /// for the new focus.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the leave event</param>
        /// <param name="surface">surface that lost keyboard focus</param>
        public delegate void LeaveHandler(WlKeyboard wlKeyboard, uint serial, WlSurface surface);
        /// <summary>
        /// key event
        /// <para>
        /// A key was pressed or released.
        /// The time argument is a timestamp with millisecond
        /// granularity, with an undefined base.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the key event</param>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="key">key that produced the event</param>
        /// <param name="state">physical state of the key</param>
        public delegate void KeyHandler(WlKeyboard wlKeyboard, uint serial, uint time, uint key, KeyState state);
        /// <summary>
        /// modifier and group state
        /// <para>
        /// Notifies clients that the modifier and/or group state has
        /// changed, and it should update its local state.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the modifiers event</param>
        /// <param name="modsDepressed">depressed modifiers</param>
        /// <param name="modsLatched">latched modifiers</param>
        /// <param name="modsLocked">locked modifiers</param>
        /// <param name="group">keyboard layout</param>
        public delegate void ModifiersHandler(WlKeyboard wlKeyboard, uint serial, uint modsDepressed, uint modsLatched, uint modsLocked, uint group);
        /// <summary>
        /// repeat rate and delay
        /// <para>
        /// Informs the client about the keyboard's repeat rate and delay.
        /// 
        /// This event is sent as soon as the wl_keyboard object has been created,
        /// and is guaranteed to be received by the client before any key press
        /// event.
        /// 
        /// Negative values for either rate or delay are illegal. A rate of zero
        /// will disable any repeating (regardless of the value of delay).
        /// 
        /// This event can be sent later on as well with a new value if necessary,
        /// so clients should continue listening for the event past the creation
        /// of wl_keyboard.
        /// </para>
        /// </summary>
        /// <param name="rate">the rate of repeating keys in characters per second</param>
        /// <param name="delay">delay in milliseconds since key down until repeating starts</param>
        public delegate void RepeatInfoHandler(WlKeyboard wlKeyboard, int rate, int delay);
        public event KeymapHandler Keymap;
        public event EnterHandler Enter;
        public event LeaveHandler Leave;
        public event KeyHandler Key;
        public event ModifiersHandler Modifiers;
        public event RepeatInfoHandler RepeatInfo;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Keymap:
                    {
                        var format = (KeymapFormat)(uint)arguments[0];
                        var fd = (IntPtr)arguments[1];
                        var size = (uint)arguments[2];
                        Keymap?.Invoke(this, format, fd, size);
                        break;
                    }
                case EventOpcode.Enter:
                    {
                        var serial = (uint)arguments[0];
                        var surface = (WlSurface)arguments[1];
                        var keys = (byte[])arguments[2];
                        Enter?.Invoke(this, serial, surface, keys);
                        break;
                    }
                case EventOpcode.Leave:
                    {
                        var serial = (uint)arguments[0];
                        var surface = (WlSurface)arguments[1];
                        Leave?.Invoke(this, serial, surface);
                        break;
                    }
                case EventOpcode.Key:
                    {
                        var serial = (uint)arguments[0];
                        var time = (uint)arguments[1];
                        var key = (uint)arguments[2];
                        var state = (KeyState)(uint)arguments[3];
                        Key?.Invoke(this, serial, time, key, state);
                        break;
                    }
                case EventOpcode.Modifiers:
                    {
                        var serial = (uint)arguments[0];
                        var modsDepressed = (uint)arguments[1];
                        var modsLatched = (uint)arguments[2];
                        var modsLocked = (uint)arguments[3];
                        var group = (uint)arguments[4];
                        Modifiers?.Invoke(this, serial, modsDepressed, modsLatched, modsLocked, group);
                        break;
                    }
                case EventOpcode.RepeatInfo:
                    {
                        var rate = (int)arguments[0];
                        var delay = (int)arguments[1];
                        RepeatInfo?.Invoke(this, rate, delay);
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
                case EventOpcode.Keymap:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Handle,
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.Enter:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Object,
                        WaylandType.Array,
                    };
                    break;
                case EventOpcode.Leave:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Object,
                    };
                    break;
                case EventOpcode.Key:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.Modifiers:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.UInt,
                    };
                    break;
                case EventOpcode.RepeatInfo:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Int,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// keyboard mapping format
        /// <para>
        /// This specifies the format of the keymap provided to the
        /// client with the wl_keyboard.keymap event.
        /// </para>
        /// </summary>
        public enum KeymapFormat : int
        {
            NoKeymap = 0,
            XkbV1 = 1,
        }
        /// <summary>
        /// physical key state
        /// <para>
        /// Describes the physical state of a key that produced the key event.
        /// </para>
        /// </summary>
        public enum KeyState : int
        {
            Released = 0,
            Pressed = 1,
        }
        /// <summary>
        /// release the keyboard object
        /// </summary>
        public void Release()
        {
            Marshal((ushort)RequestOpcode.Release);
            Die();
        }
    }
    /// wl_touch version 7
    /// <summary>
    /// touchscreen input device
    /// <para>
    /// The wl_touch interface represents a touchscreen
    /// associated with a seat.
    /// 
    /// Touch interactions can consist of one or more contacts.
    /// For each contact, a series of events is generated, starting
    /// with a down event, followed by zero or more motion events,
    /// and ending with an up event. Events relating to the same
    /// contact point can be identified by the ID of the sequence.
    /// </para>
    /// </summary>
    public sealed class WlTouch : WaylandClientObject
    {
        public WlTouch(uint id, uint version, WaylandClientConnection connection) : base("wl_touch", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Release,
        }
        public enum EventOpcode : ushort
        {
            Down,
            Up,
            Motion,
            Frame,
            Cancel,
            Shape,
            Orientation,
        }
        /// <summary>
        /// touch down event and beginning of a touch sequence
        /// <para>
        /// A new touch point has appeared on the surface. This touch point is
        /// assigned a unique ID. Future events from this touch point reference
        /// this ID. The ID ceases to be valid after a touch up event and may be
        /// reused in the future.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the touch down event</param>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="surface">surface touched</param>
        /// <param name="id">the unique ID of this touch point</param>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        public delegate void DownHandler(WlTouch wlTouch, uint serial, uint time, WlSurface surface, int id, double x, double y);
        /// <summary>
        /// end of a touch event sequence
        /// <para>
        /// The touch point has disappeared. No further events will be sent for
        /// this touch point and the touch point's ID is released and may be
        /// reused in a future touch down event.
        /// </para>
        /// </summary>
        /// <param name="serial">serial number of the touch up event</param>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="id">the unique ID of this touch point</param>
        public delegate void UpHandler(WlTouch wlTouch, uint serial, uint time, int id);
        /// <summary>
        /// update of touch point coordinates
        /// <para>
        /// A touch point has changed coordinates.
        /// </para>
        /// </summary>
        /// <param name="time">timestamp with millisecond granularity</param>
        /// <param name="id">the unique ID of this touch point</param>
        /// <param name="x">surface-local x coordinate</param>
        /// <param name="y">surface-local y coordinate</param>
        public delegate void MotionHandler(WlTouch wlTouch, uint time, int id, double x, double y);
        /// <summary>
        /// end of touch frame event
        /// <para>
        /// Indicates the end of a set of events that logically belong together.
        /// A client is expected to accumulate the data in all events within the
        /// frame before proceeding.
        /// 
        /// A wl_touch.frame terminates at least one event but otherwise no
        /// guarantee is provided about the set of events within a frame. A client
        /// must assume that any state not updated in a frame is unchanged from the
        /// previously known state.
        /// </para>
        /// </summary>
        public delegate void FrameHandler(WlTouch wlTouch);
        /// <summary>
        /// touch session cancelled
        /// <para>
        /// Sent if the compositor decides the touch stream is a global
        /// gesture. No further events are sent to the clients from that
        /// particular gesture. Touch cancellation applies to all touch points
        /// currently active on this client's surface. The client is
        /// responsible for finalizing the touch points, future touch points on
        /// this surface may reuse the touch point ID.
        /// </para>
        /// </summary>
        public delegate void CancelHandler(WlTouch wlTouch);
        /// <summary>
        /// update shape of touch point
        /// <para>
        /// Sent when a touchpoint has changed its shape.
        /// 
        /// This event does not occur on its own. It is sent before a
        /// wl_touch.frame event and carries the new shape information for
        /// any previously reported, or new touch points of that frame.
        /// 
        /// Other events describing the touch point such as wl_touch.down,
        /// wl_touch.motion or wl_touch.orientation may be sent within the
        /// same wl_touch.frame. A client should treat these events as a single
        /// logical touch point update. The order of wl_touch.shape,
        /// wl_touch.orientation and wl_touch.motion is not guaranteed.
        /// A wl_touch.down event is guaranteed to occur before the first
        /// wl_touch.shape event for this touch ID but both events may occur within
        /// the same wl_touch.frame.
        /// 
        /// A touchpoint shape is approximated by an ellipse through the major and
        /// minor axis length. The major axis length describes the longer diameter
        /// of the ellipse, while the minor axis length describes the shorter
        /// diameter. Major and minor are orthogonal and both are specified in
        /// surface-local coordinates. The center of the ellipse is always at the
        /// touchpoint location as reported by wl_touch.down or wl_touch.move.
        /// 
        /// This event is only sent by the compositor if the touch device supports
        /// shape reports. The client has to make reasonable assumptions about the
        /// shape if it did not receive this event.
        /// </para>
        /// </summary>
        /// <param name="id">the unique ID of this touch point</param>
        /// <param name="major">length of the major axis in surface-local coordinates</param>
        /// <param name="minor">length of the minor axis in surface-local coordinates</param>
        public delegate void ShapeHandler(WlTouch wlTouch, int id, double major, double minor);
        /// <summary>
        /// update orientation of touch point
        /// <para>
        /// Sent when a touchpoint has changed its orientation.
        /// 
        /// This event does not occur on its own. It is sent before a
        /// wl_touch.frame event and carries the new shape information for
        /// any previously reported, or new touch points of that frame.
        /// 
        /// Other events describing the touch point such as wl_touch.down,
        /// wl_touch.motion or wl_touch.shape may be sent within the
        /// same wl_touch.frame. A client should treat these events as a single
        /// logical touch point update. The order of wl_touch.shape,
        /// wl_touch.orientation and wl_touch.motion is not guaranteed.
        /// A wl_touch.down event is guaranteed to occur before the first
        /// wl_touch.orientation event for this touch ID but both events may occur
        /// within the same wl_touch.frame.
        /// 
        /// The orientation describes the clockwise angle of a touchpoint's major
        /// axis to the positive surface y-axis and is normalized to the -180 to
        /// +180 degree range. The granularity of orientation depends on the touch
        /// device, some devices only support binary rotation values between 0 and
        /// 90 degrees.
        /// 
        /// This event is only sent by the compositor if the touch device supports
        /// orientation reports.
        /// </para>
        /// </summary>
        /// <param name="id">the unique ID of this touch point</param>
        /// <param name="orientation">angle between major axis and positive surface y-axis in degrees</param>
        public delegate void OrientationHandler(WlTouch wlTouch, int id, double orientation);
        public event DownHandler Down;
        public event UpHandler Up;
        public event MotionHandler Motion;
        public event FrameHandler Frame;
        public event CancelHandler Cancel;
        public event ShapeHandler Shape;
        public event OrientationHandler Orientation;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Down:
                    {
                        var serial = (uint)arguments[0];
                        var time = (uint)arguments[1];
                        var surface = (WlSurface)arguments[2];
                        var id = (int)arguments[3];
                        var x = (double)arguments[4];
                        var y = (double)arguments[5];
                        Down?.Invoke(this, serial, time, surface, id, x, y);
                        break;
                    }
                case EventOpcode.Up:
                    {
                        var serial = (uint)arguments[0];
                        var time = (uint)arguments[1];
                        var id = (int)arguments[2];
                        Up?.Invoke(this, serial, time, id);
                        break;
                    }
                case EventOpcode.Motion:
                    {
                        var time = (uint)arguments[0];
                        var id = (int)arguments[1];
                        var x = (double)arguments[2];
                        var y = (double)arguments[3];
                        Motion?.Invoke(this, time, id, x, y);
                        break;
                    }
                case EventOpcode.Frame:
                    {
                        Frame?.Invoke(this);
                        break;
                    }
                case EventOpcode.Cancel:
                    {
                        Cancel?.Invoke(this);
                        break;
                    }
                case EventOpcode.Shape:
                    {
                        var id = (int)arguments[0];
                        var major = (double)arguments[1];
                        var minor = (double)arguments[2];
                        Shape?.Invoke(this, id, major, minor);
                        break;
                    }
                case EventOpcode.Orientation:
                    {
                        var id = (int)arguments[0];
                        var orientation = (double)arguments[1];
                        Orientation?.Invoke(this, id, orientation);
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
                case EventOpcode.Down:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.Object,
                        WaylandType.Int,
                        WaylandType.Fixed,
                        WaylandType.Fixed,
                    };
                    break;
                case EventOpcode.Up:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.UInt,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.Motion:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Int,
                        WaylandType.Fixed,
                        WaylandType.Fixed,
                    };
                    break;
                case EventOpcode.Frame:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Cancel:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Shape:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Fixed,
                        WaylandType.Fixed,
                    };
                    break;
                case EventOpcode.Orientation:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Fixed,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// release the touch object
        /// </summary>
        public void Release()
        {
            Marshal((ushort)RequestOpcode.Release);
            Die();
        }
    }
    /// wl_output version 3
    /// <summary>
    /// compositor output region
    /// <para>
    /// An output describes part of the compositor geometry.  The
    /// compositor works in the 'compositor coordinate system' and an
    /// output corresponds to a rectangular area in that space that is
    /// actually visible.  This typically corresponds to a monitor that
    /// displays part of the compositor space.  This object is published
    /// as global during start up, or when a monitor is hotplugged.
    /// </para>
    /// </summary>
    public sealed class WlOutput : WaylandClientObject
    {
        public WlOutput(uint id, uint version, WaylandClientConnection connection) : base("wl_output", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Release,
        }
        public enum EventOpcode : ushort
        {
            Geometry,
            Mode,
            Done,
            Scale,
        }
        /// <summary>
        /// properties of the output
        /// <para>
        /// The geometry event describes geometric properties of the output.
        /// The event is sent when binding to the output object and whenever
        /// any of the properties change.
        /// 
        /// The physical size can be set to zero if it doesn't make sense for this
        /// output (e.g. for projectors or virtual outputs).
        /// 
        /// Note: wl_output only advertises partial information about the output
        /// position and identification. Some compositors, for instance those not
        /// implementing a desktop-style output layout or those exposing virtual
        /// outputs, might fake this information. Instead of using x and y, clients
        /// should use xdg_output.logical_position. Instead of using make and model,
        /// clients should use xdg_output.name and xdg_output.description.
        /// </para>
        /// </summary>
        /// <param name="x">x position within the global compositor space</param>
        /// <param name="y">y position within the global compositor space</param>
        /// <param name="physicalWidth">width in millimeters of the output</param>
        /// <param name="physicalHeight">height in millimeters of the output</param>
        /// <param name="subpixel">subpixel orientation of the output</param>
        /// <param name="make">textual description of the manufacturer</param>
        /// <param name="model">textual description of the model</param>
        /// <param name="transform">transform that maps framebuffer to output</param>
        public delegate void GeometryHandler(WlOutput wlOutput, int x, int y, int physicalWidth, int physicalHeight, Subpixel subpixel, string make, string model, Transform transform);
        /// <summary>
        /// advertise available modes for the output
        /// <para>
        /// The mode event describes an available mode for the output.
        /// 
        /// The event is sent when binding to the output object and there
        /// will always be one mode, the current mode.  The event is sent
        /// again if an output changes mode, for the mode that is now
        /// current.  In other words, the current mode is always the last
        /// mode that was received with the current flag set.
        /// 
        /// The size of a mode is given in physical hardware units of
        /// the output device. This is not necessarily the same as
        /// the output size in the global compositor space. For instance,
        /// the output may be scaled, as described in wl_output.scale,
        /// or transformed, as described in wl_output.transform. Clients
        /// willing to retrieve the output size in the global compositor
        /// space should use xdg_output.logical_size instead.
        /// 
        /// The vertical refresh rate can be set to zero if it doesn't make
        /// sense for this output (e.g. for virtual outputs).
        /// 
        /// Clients should not use the refresh rate to schedule frames. Instead,
        /// they should use the wl_surface.frame event or the presentation-time
        /// protocol.
        /// 
        /// Note: this information is not always meaningful for all outputs. Some
        /// compositors, such as those exposing virtual outputs, might fake the
        /// refresh rate or the size.
        /// </para>
        /// </summary>
        /// <param name="flags">bitfield of mode flags</param>
        /// <param name="width">width of the mode in hardware units</param>
        /// <param name="height">height of the mode in hardware units</param>
        /// <param name="refresh">vertical refresh rate in mHz</param>
        public delegate void ModeHandler(WlOutput wlOutput, ModeEnum flags, int width, int height, int refresh);
        /// <summary>
        /// sent all information about output
        /// <para>
        /// This event is sent after all other properties have been
        /// sent after binding to the output object and after any
        /// other property changes done after that. This allows
        /// changes to the output properties to be seen as
        /// atomic, even if they happen via multiple events.
        /// </para>
        /// </summary>
        public delegate void DoneHandler(WlOutput wlOutput);
        /// <summary>
        /// output scaling properties
        /// <para>
        /// This event contains scaling geometry information
        /// that is not in the geometry event. It may be sent after
        /// binding the output object or if the output scale changes
        /// later. If it is not sent, the client should assume a
        /// scale of 1.
        /// 
        /// A scale larger than 1 means that the compositor will
        /// automatically scale surface buffers by this amount
        /// when rendering. This is used for very high resolution
        /// displays where applications rendering at the native
        /// resolution would be too small to be legible.
        /// 
        /// It is intended that scaling aware clients track the
        /// current output of a surface, and if it is on a scaled
        /// output it should use wl_surface.set_buffer_scale with
        /// the scale of the output. That way the compositor can
        /// avoid scaling the surface, and the client can supply
        /// a higher detail image.
        /// </para>
        /// </summary>
        /// <param name="factor">scaling factor of output</param>
        public delegate void ScaleHandler(WlOutput wlOutput, int factor);
        public event GeometryHandler Geometry;
        public event ModeHandler Mode;
        public event DoneHandler Done;
        public event ScaleHandler Scale;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Geometry:
                    {
                        var x = (int)arguments[0];
                        var y = (int)arguments[1];
                        var physicalWidth = (int)arguments[2];
                        var physicalHeight = (int)arguments[3];
                        var subpixel = (Subpixel)(int)arguments[4];
                        var make = (string)arguments[5];
                        var model = (string)arguments[6];
                        var transform = (Transform)(int)arguments[7];
                        Geometry?.Invoke(this, x, y, physicalWidth, physicalHeight, subpixel, make, model, transform);
                        break;
                    }
                case EventOpcode.Mode:
                    {
                        var flags = (ModeEnum)(uint)arguments[0];
                        var width = (int)arguments[1];
                        var height = (int)arguments[2];
                        var refresh = (int)arguments[3];
                        Mode?.Invoke(this, flags, width, height, refresh);
                        break;
                    }
                case EventOpcode.Done:
                    {
                        Done?.Invoke(this);
                        break;
                    }
                case EventOpcode.Scale:
                    {
                        var factor = (int)arguments[0];
                        Scale?.Invoke(this, factor);
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
                case EventOpcode.Geometry:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.String,
                        WaylandType.String,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.Mode:
                    return new WaylandType[]
                    {
                        WaylandType.UInt,
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.Done:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Scale:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// subpixel geometry information
        /// <para>
        /// This enumeration describes how the physical
        /// pixels on an output are laid out.
        /// </para>
        /// </summary>
        public enum Subpixel : int
        {
            Unknown = 0,
            None = 1,
            HorizontalRgb = 2,
            HorizontalBgr = 3,
            VerticalRgb = 4,
            VerticalBgr = 5,
        }
        /// <summary>
        /// transform from framebuffer to output
        /// <para>
        /// This describes the transform that a compositor will apply to a
        /// surface to compensate for the rotation or mirroring of an
        /// output device.
        /// 
        /// The flipped values correspond to an initial flip around a
        /// vertical axis followed by rotation.
        /// 
        /// The purpose is mainly to allow clients to render accordingly and
        /// tell the compositor, so that for fullscreen surfaces, the
        /// compositor will still be able to scan out directly from client
        /// surfaces.
        /// </para>
        /// </summary>
        public enum Transform : int
        {
            Normal = 0,
            _90 = 1,
            _180 = 2,
            _270 = 3,
            Flipped = 4,
            Flipped90 = 5,
            Flipped180 = 6,
            Flipped270 = 7,
        }
        /// <summary>
        /// mode information
        /// <para>
        /// These flags describe properties of an output mode.
        /// They are used in the flags bitfield of the mode event.
        /// </para>
        /// </summary>
        [Flags]
        public enum ModeEnum : int
        {
            Current = 1,
            Preferred = 2,
        }
        /// <summary>
        /// release the output object
        /// <para>
        /// Using this request a client can tell the server that it is not going to
        /// use the output object anymore.
        /// </para>
        /// </summary>
        public void Release()
        {
            Marshal((ushort)RequestOpcode.Release);
            Die();
        }
    }
    /// wl_region version 1
    /// <summary>
    /// region interface
    /// <para>
    /// A region object describes an area.
    /// 
    /// Region objects are used to describe the opaque and input
    /// regions of a surface.
    /// </para>
    /// </summary>
    public sealed class WlRegion : WaylandClientObject
    {
        public WlRegion(uint id, uint version, WaylandClientConnection connection) : base("wl_region", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            Add,
            Subtract,
        }
        public enum EventOpcode : ushort
        {
        }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        /// <summary>
        /// destroy region
        /// <para>
        /// Destroy the region.  This will invalidate the object ID.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// add rectangle to region
        /// <para>
        /// Add the specified rectangle to the region.
        /// </para>
        /// </summary>
        /// <param name="x">region-local x coordinate</param>
        /// <param name="y">region-local y coordinate</param>
        /// <param name="width">rectangle width</param>
        /// <param name="height">rectangle height</param>
        public void Add(int x, int y, int width, int height)
        {
            Marshal((ushort)RequestOpcode.Add, x, y, width, height);
        }
        /// <summary>
        /// subtract rectangle from region
        /// <para>
        /// Subtract the specified rectangle from the region.
        /// </para>
        /// </summary>
        /// <param name="x">region-local x coordinate</param>
        /// <param name="y">region-local y coordinate</param>
        /// <param name="width">rectangle width</param>
        /// <param name="height">rectangle height</param>
        public void Subtract(int x, int y, int width, int height)
        {
            Marshal((ushort)RequestOpcode.Subtract, x, y, width, height);
        }
    }
    /// wl_subcompositor version 1
    /// <summary>
    /// sub-surface compositing
    /// <para>
    /// The global interface exposing sub-surface compositing capabilities.
    /// A wl_surface, that has sub-surfaces associated, is called the
    /// parent surface. Sub-surfaces can be arbitrarily nested and create
    /// a tree of sub-surfaces.
    /// 
    /// The root surface in a tree of sub-surfaces is the main
    /// surface. The main surface cannot be a sub-surface, because
    /// sub-surfaces must always have a parent.
    /// 
    /// A main surface with its sub-surfaces forms a (compound) window.
    /// For window management purposes, this set of wl_surface objects is
    /// to be considered as a single window, and it should also behave as
    /// such.
    /// 
    /// The aim of sub-surfaces is to offload some of the compositing work
    /// within a window from clients to the compositor. A prime example is
    /// a video player with decorations and video in separate wl_surface
    /// objects. This should allow the compositor to pass YUV video buffer
    /// processing to dedicated overlay hardware when possible.
    /// </para>
    /// </summary>
    public sealed class WlSubcompositor : WaylandClientObject
    {
        public WlSubcompositor(uint id, uint version, WaylandClientConnection connection) : base("wl_subcompositor", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            GetSubsurface,
        }
        public enum EventOpcode : ushort
        {
        }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            BadSurface = 0,
        }
        /// <summary>
        /// unbind from the subcompositor interface
        /// <para>
        /// Informs the server that the client will not be using this
        /// protocol object anymore. This does not affect any other
        /// objects, wl_subsurface objects included.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// give a surface the role sub-surface
        /// <para>
        /// Create a sub-surface interface for the given surface, and
        /// associate it with the given parent surface. This turns a
        /// plain wl_surface into a sub-surface.
        /// 
        /// The to-be sub-surface must not already have another role, and it
        /// must not have an existing wl_subsurface object. Otherwise a protocol
        /// error is raised.
        /// 
        /// Adding sub-surfaces to a parent is a double-buffered operation on the
        /// parent (see wl_surface.commit). The effect of adding a sub-surface
        /// becomes visible on the next time the state of the parent surface is
        /// applied.
        /// 
        /// This request modifies the behaviour of wl_surface.commit request on
        /// the sub-surface, see the documentation on wl_subsurface interface.
        /// </para>
        /// </summary>
        /// <returns>the new sub-surface object ID</returns>
        /// <param name="surface">the surface to be turned into a sub-surface</param>
        /// <param name="parent">the parent surface</param>
        public WlSubsurface GetSubsurface(WlSurface surface, WlSurface parent)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetSubsurface, id, surface.Id, parent.Id);
            Connection[id] = new WlSubsurface(id, Version, ClientConnection);
            return (WlSubsurface)Connection[id];
        }
    }
    /// wl_subsurface version 1
    /// <summary>
    /// sub-surface interface to a wl_surface
    /// <para>
    /// An additional interface to a wl_surface object, which has been
    /// made a sub-surface. A sub-surface has one parent surface. A
    /// sub-surface's size and position are not limited to that of the parent.
    /// Particularly, a sub-surface is not automatically clipped to its
    /// parent's area.
    /// 
    /// A sub-surface becomes mapped, when a non-NULL wl_buffer is applied
    /// and the parent surface is mapped. The order of which one happens
    /// first is irrelevant. A sub-surface is hidden if the parent becomes
    /// hidden, or if a NULL wl_buffer is applied. These rules apply
    /// recursively through the tree of surfaces.
    /// 
    /// The behaviour of a wl_surface.commit request on a sub-surface
    /// depends on the sub-surface's mode. The possible modes are
    /// synchronized and desynchronized, see methods
    /// wl_subsurface.set_sync and wl_subsurface.set_desync. Synchronized
    /// mode caches the wl_surface state to be applied when the parent's
    /// state gets applied, and desynchronized mode applies the pending
    /// wl_surface state directly. A sub-surface is initially in the
    /// synchronized mode.
    /// 
    /// Sub-surfaces have also other kind of state, which is managed by
    /// wl_subsurface requests, as opposed to wl_surface requests. This
    /// state includes the sub-surface position relative to the parent
    /// surface (wl_subsurface.set_position), and the stacking order of
    /// the parent and its sub-surfaces (wl_subsurface.place_above and
    /// .place_below). This state is applied when the parent surface's
    /// wl_surface state is applied, regardless of the sub-surface's mode.
    /// As the exception, set_sync and set_desync are effective immediately.
    /// 
    /// The main surface can be thought to be always in desynchronized mode,
    /// since it does not have a parent in the sub-surfaces sense.
    /// 
    /// Even if a sub-surface is in desynchronized mode, it will behave as
    /// in synchronized mode, if its parent surface behaves as in
    /// synchronized mode. This rule is applied recursively throughout the
    /// tree of surfaces. This means, that one can set a sub-surface into
    /// synchronized mode, and then assume that all its child and grand-child
    /// sub-surfaces are synchronized, too, without explicitly setting them.
    /// 
    /// If the wl_surface associated with the wl_subsurface is destroyed, the
    /// wl_subsurface object becomes inert. Note, that destroying either object
    /// takes effect immediately. If you need to synchronize the removal
    /// of a sub-surface to the parent surface update, unmap the sub-surface
    /// first by attaching a NULL wl_buffer, update parent, and then destroy
    /// the sub-surface.
    /// 
    /// If the parent wl_surface object is destroyed, the sub-surface is
    /// unmapped.
    /// </para>
    /// </summary>
    public sealed class WlSubsurface : WaylandClientObject
    {
        public WlSubsurface(uint id, uint version, WaylandClientConnection connection) : base("wl_subsurface", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            SetPosition,
            PlaceAbove,
            PlaceBelow,
            SetSync,
            SetDesync,
        }
        public enum EventOpcode : ushort
        {
        }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public override WaylandType[] Arguments(ushort opcode)
        {
            switch ((EventOpcode)opcode)
            {
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            BadSurface = 0,
        }
        /// <summary>
        /// remove sub-surface interface
        /// <para>
        /// The sub-surface interface is removed from the wl_surface object
        /// that was turned into a sub-surface with a
        /// wl_subcompositor.get_subsurface request. The wl_surface's association
        /// to the parent is deleted, and the wl_surface loses its role as
        /// a sub-surface. The wl_surface is unmapped immediately.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// reposition the sub-surface
        /// <para>
        /// This schedules a sub-surface position change.
        /// The sub-surface will be moved so that its origin (top left
        /// corner pixel) will be at the location x, y of the parent surface
        /// coordinate system. The coordinates are not restricted to the parent
        /// surface area. Negative values are allowed.
        /// 
        /// The scheduled coordinates will take effect whenever the state of the
        /// parent surface is applied. When this happens depends on whether the
        /// parent surface is in synchronized mode or not. See
        /// wl_subsurface.set_sync and wl_subsurface.set_desync for details.
        /// 
        /// If more than one set_position request is invoked by the client before
        /// the commit of the parent surface, the position of a new request always
        /// replaces the scheduled position from any previous request.
        /// 
        /// The initial position is 0, 0.
        /// </para>
        /// </summary>
        /// <param name="x">x coordinate in the parent surface</param>
        /// <param name="y">y coordinate in the parent surface</param>
        public void SetPosition(int x, int y)
        {
            Marshal((ushort)RequestOpcode.SetPosition, x, y);
        }
        /// <summary>
        /// restack the sub-surface
        /// <para>
        /// This sub-surface is taken from the stack, and put back just
        /// above the reference surface, changing the z-order of the sub-surfaces.
        /// The reference surface must be one of the sibling surfaces, or the
        /// parent surface. Using any other surface, including this sub-surface,
        /// will cause a protocol error.
        /// 
        /// The z-order is double-buffered. Requests are handled in order and
        /// applied immediately to a pending state. The final pending state is
        /// copied to the active state the next time the state of the parent
        /// surface is applied. When this happens depends on whether the parent
        /// surface is in synchronized mode or not. See wl_subsurface.set_sync and
        /// wl_subsurface.set_desync for details.
        /// 
        /// A new sub-surface is initially added as the top-most in the stack
        /// of its siblings and parent.
        /// </para>
        /// </summary>
        /// <param name="sibling">the reference surface</param>
        public void PlaceAbove(WlSurface sibling)
        {
            Marshal((ushort)RequestOpcode.PlaceAbove, sibling.Id);
        }
        /// <summary>
        /// restack the sub-surface
        /// <para>
        /// The sub-surface is placed just below the reference surface.
        /// See wl_subsurface.place_above.
        /// </para>
        /// </summary>
        /// <param name="sibling">the reference surface</param>
        public void PlaceBelow(WlSurface sibling)
        {
            Marshal((ushort)RequestOpcode.PlaceBelow, sibling.Id);
        }
        /// <summary>
        /// set sub-surface to synchronized mode
        /// <para>
        /// Change the commit behaviour of the sub-surface to synchronized
        /// mode, also described as the parent dependent mode.
        /// 
        /// In synchronized mode, wl_surface.commit on a sub-surface will
        /// accumulate the committed state in a cache, but the state will
        /// not be applied and hence will not change the compositor output.
        /// The cached state is applied to the sub-surface immediately after
        /// the parent surface's state is applied. This ensures atomic
        /// updates of the parent and all its synchronized sub-surfaces.
        /// Applying the cached state will invalidate the cache, so further
        /// parent surface commits do not (re-)apply old state.
        /// 
        /// See wl_subsurface for the recursive effect of this mode.
        /// </para>
        /// </summary>
        public void SetSync()
        {
            Marshal((ushort)RequestOpcode.SetSync);
        }
        /// <summary>
        /// set sub-surface to desynchronized mode
        /// <para>
        /// Change the commit behaviour of the sub-surface to desynchronized
        /// mode, also described as independent or freely running mode.
        /// 
        /// In desynchronized mode, wl_surface.commit on a sub-surface will
        /// apply the pending state directly, without caching, as happens
        /// normally with a wl_surface. Calling wl_surface.commit on the
        /// parent surface has no effect on the sub-surface's wl_surface
        /// state. This mode allows a sub-surface to be updated on its own.
        /// 
        /// If cached state exists when wl_surface.commit is called in
        /// desynchronized mode, the pending state is added to the cached
        /// state, and applied as a whole. This invalidates the cache.
        /// 
        /// Note: even if a sub-surface is set to desynchronized, a parent
        /// sub-surface may override it to behave as synchronized. For details,
        /// see wl_subsurface.
        /// 
        /// If a surface's parent surface behaves as desynchronized, then
        /// the cached state is applied on set_desync.
        /// </para>
        /// </summary>
        public void SetDesync()
        {
            Marshal((ushort)RequestOpcode.SetDesync);
        }
    }
}
