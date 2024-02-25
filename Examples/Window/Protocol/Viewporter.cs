/// Copyright © 2013-2016 Collabora, Ltd.
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice (including the next
/// paragraph) shall be included in all copies or substantial portions of the
/// Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
/// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
/// DEALINGS IN THE SOFTWARE.
#pragma warning disable 0162
using System;
using WaylandNET;
using WaylandNET.Client;
namespace WaylandNET.Client.Protocol
{
    /// wp_viewporter version 1
    /// <summary>
    /// surface cropping and scaling
    /// <para>
    /// The global interface exposing surface cropping and scaling
    /// capabilities is used to instantiate an interface extension for a
    /// wl_surface object. This extended interface will then allow
    /// cropping and scaling the surface contents, effectively
    /// disconnecting the direct relationship between the buffer and the
    /// surface size.
    /// </para>
    /// </summary>
    public sealed class WpViewporter : WaylandClientObject
    {
        public WpViewporter(uint id, uint version, WaylandClientConnection connection) : base("wp_viewporter", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            GetViewport,
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
            ViewportExists = 0,
        }
        /// <summary>
        /// unbind from the cropping and scaling interface
        /// <para>
        /// Informs the server that the client will not be using this
        /// protocol object anymore. This does not affect any other objects,
        /// wp_viewport objects included.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// extend surface interface for crop and scale
        /// <para>
        /// Instantiate an interface extension for the given wl_surface to
        /// crop and scale its content. If the given wl_surface already has
        /// a wp_viewport object associated, the viewport_exists
        /// protocol error is raised.
        /// </para>
        /// </summary>
        /// <returns>the new viewport interface id</returns>
        /// <param name="surface">the surface</param>
        public WpViewport GetViewport(WlSurface surface)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetViewport, id, surface.Id);
            Connection[id] = new WpViewport(id, Version, ClientConnection);
            return (WpViewport)Connection[id];
        }
    }
    /// wp_viewport version 1
    /// <summary>
    /// crop and scale interface to a wl_surface
    /// <para>
    /// An additional interface to a wl_surface object, which allows the
    /// client to specify the cropping and scaling of the surface
    /// contents.
    /// 
    /// This interface works with two concepts: the source rectangle (src_x,
    /// src_y, src_width, src_height), and the destination size (dst_width,
    /// dst_height). The contents of the source rectangle are scaled to the
    /// destination size, and content outside the source rectangle is ignored.
    /// This state is double-buffered, and is applied on the next
    /// wl_surface.commit.
    /// 
    /// The two parts of crop and scale state are independent: the source
    /// rectangle, and the destination size. Initially both are unset, that
    /// is, no scaling is applied. The whole of the current wl_buffer is
    /// used as the source, and the surface size is as defined in
    /// wl_surface.attach.
    /// 
    /// If the destination size is set, it causes the surface size to become
    /// dst_width, dst_height. The source (rectangle) is scaled to exactly
    /// this size. This overrides whatever the attached wl_buffer size is,
    /// unless the wl_buffer is NULL. If the wl_buffer is NULL, the surface
    /// has no content and therefore no size. Otherwise, the size is always
    /// at least 1x1 in surface local coordinates.
    /// 
    /// If the source rectangle is set, it defines what area of the wl_buffer is
    /// taken as the source. If the source rectangle is set and the destination
    /// size is not set, then src_width and src_height must be integers, and the
    /// surface size becomes the source rectangle size. This results in cropping
    /// without scaling. If src_width or src_height are not integers and
    /// destination size is not set, the bad_size protocol error is raised when
    /// the surface state is applied.
    /// 
    /// The coordinate transformations from buffer pixel coordinates up to
    /// the surface-local coordinates happen in the following order:
    /// 1. buffer_transform (wl_surface.set_buffer_transform)
    /// 2. buffer_scale (wl_surface.set_buffer_scale)
    /// 3. crop and scale (wp_viewport.set*)
    /// This means, that the source rectangle coordinates of crop and scale
    /// are given in the coordinates after the buffer transform and scale,
    /// i.e. in the coordinates that would be the surface-local coordinates
    /// if the crop and scale was not applied.
    /// 
    /// If src_x or src_y are negative, the bad_value protocol error is raised.
    /// Otherwise, if the source rectangle is partially or completely outside of
    /// the non-NULL wl_buffer, then the out_of_buffer protocol error is raised
    /// when the surface state is applied. A NULL wl_buffer does not raise the
    /// out_of_buffer error.
    /// 
    /// If the wl_surface associated with the wp_viewport is destroyed,
    /// all wp_viewport requests except 'destroy' raise the protocol error
    /// no_surface.
    /// 
    /// If the wp_viewport object is destroyed, the crop and scale
    /// state is removed from the wl_surface. The change will be applied
    /// on the next wl_surface.commit.
    /// </para>
    /// </summary>
    public sealed class WpViewport : WaylandClientObject
    {
        public WpViewport(uint id, uint version, WaylandClientConnection connection) : base("wp_viewport", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            SetSource,
            SetDestination,
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
            BadValue = 0,
            BadSize = 1,
            OutOfBuffer = 2,
            NoSurface = 3,
        }
        /// <summary>
        /// remove scaling and cropping from the surface
        /// <para>
        /// The associated wl_surface's crop and scale state is removed.
        /// The change is applied on the next wl_surface.commit.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// set the source rectangle for cropping
        /// <para>
        /// Set the source rectangle of the associated wl_surface. See
        /// wp_viewport for the description, and relation to the wl_buffer
        /// size.
        /// 
        /// If all of x, y, width and height are -1.0, the source rectangle is
        /// unset instead. Any other set of values where width or height are zero
        /// or negative, or x or y are negative, raise the bad_value protocol
        /// error.
        /// 
        /// The crop and scale state is double-buffered state, and will be
        /// applied on the next wl_surface.commit.
        /// </para>
        /// </summary>
        /// <param name="x">source rectangle x</param>
        /// <param name="y">source rectangle y</param>
        /// <param name="width">source rectangle width</param>
        /// <param name="height">source rectangle height</param>
        public void SetSource(double x, double y, double width, double height)
        {
            Marshal((ushort)RequestOpcode.SetSource, x, y, width, height);
        }
        /// <summary>
        /// set the surface size for scaling
        /// <para>
        /// Set the destination size of the associated wl_surface. See
        /// wp_viewport for the description, and relation to the wl_buffer
        /// size.
        /// 
        /// If width is -1 and height is -1, the destination size is unset
        /// instead. Any other pair of values for width and height that
        /// contains zero or negative values raises the bad_value protocol
        /// error.
        /// 
        /// The crop and scale state is double-buffered state, and will be
        /// applied on the next wl_surface.commit.
        /// </para>
        /// </summary>
        /// <param name="width">surface width</param>
        /// <param name="height">surface height</param>
        public void SetDestination(int width, int height)
        {
            Marshal((ushort)RequestOpcode.SetDestination, width, height);
        }
    }
}
