/// Copyright © 2008-2013 Kristian Høgsberg
/// Copyright © 2013      Rafael Antognolli
/// Copyright © 2013      Jasper St. Pierre
/// Copyright © 2010-2013 Intel Corporation
/// Copyright © 2015-2017 Samsung Electronics Co., Ltd
/// Copyright © 2015-2017 Red Hat Inc.
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
    /// xdg_wm_base version 6
    /// <summary>
    /// create desktop-style surfaces
    /// <para>
    /// The xdg_wm_base interface is exposed as a global object enabling clients
    /// to turn their wl_surfaces into windows in a desktop environment. It
    /// defines the basic functionality needed for clients and the compositor to
    /// create windows that can be dragged, resized, maximized, etc, as well as
    /// creating transient windows such as popup menus.
    /// </para>
    /// </summary>
    public sealed class XdgWmBase : WaylandClientObject
    {
        public XdgWmBase(uint id, uint version, WaylandClientConnection connection) : base("xdg_wm_base", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            CreatePositioner,
            GetXdgSurface,
            Pong,
        }
        public enum EventOpcode : ushort
        {
            Ping,
        }
        /// <summary>
        /// check if the client is alive
        /// <para>
        /// The ping event asks the client if it's still alive. Pass the
        /// serial specified in the event back to the compositor by sending
        /// a "pong" request back with the specified serial. See xdg_wm_base.pong.
        /// 
        /// Compositors can use this to determine if the client is still
        /// alive. It's unspecified what will happen if the client doesn't
        /// respond to the ping request, or in what timeframe. Clients should
        /// try to respond in a reasonable amount of time. The “unresponsive”
        /// error is provided for compositors that wish to disconnect unresponsive
        /// clients.
        /// 
        /// A compositor is free to ping in any way it wants, but a client must
        /// always respond to any xdg_wm_base object it created.
        /// </para>
        /// </summary>
        /// <param name="serial">pass this to the pong request</param>
        public delegate void PingHandler(XdgWmBase xdgWmBase, uint serial);
        public event PingHandler Ping;
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
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            Role = 0,
            DefunctSurfaces = 1,
            NotTheTopmostPopup = 2,
            InvalidPopupParent = 3,
            InvalidSurfaceState = 4,
            InvalidPositioner = 5,
            Unresponsive = 6,
        }
        /// <summary>
        /// destroy xdg_wm_base
        /// <para>
        /// Destroy this xdg_wm_base object.
        /// 
        /// Destroying a bound xdg_wm_base object while there are surfaces
        /// still alive created by this xdg_wm_base object instance is illegal
        /// and will result in a defunct_surfaces error.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// create a positioner object
        /// <para>
        /// Create a positioner object. A positioner object is used to position
        /// surfaces relative to some parent surface. See the interface description
        /// and xdg_surface.get_popup for details.
        /// </para>
        /// </summary>
        public XdgPositioner CreatePositioner()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.CreatePositioner, id);
            Connection[id] = new XdgPositioner(id, Version, ClientConnection);
            return (XdgPositioner)Connection[id];
        }
        /// <summary>
        /// create a shell surface from a surface
        /// <para>
        /// This creates an xdg_surface for the given surface. While xdg_surface
        /// itself is not a role, the corresponding surface may only be assigned
        /// a role extending xdg_surface, such as xdg_toplevel or xdg_popup. It is
        /// illegal to create an xdg_surface for a wl_surface which already has an
        /// assigned role and this will result in a role error.
        /// 
        /// This creates an xdg_surface for the given surface. An xdg_surface is
        /// used as basis to define a role to a given surface, such as xdg_toplevel
        /// or xdg_popup. It also manages functionality shared between xdg_surface
        /// based surface roles.
        /// 
        /// See the documentation of xdg_surface for more details about what an
        /// xdg_surface is and how it is used.
        /// </para>
        /// </summary>
        public XdgSurface GetXdgSurface(WlSurface surface)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetXdgSurface, id, surface.Id);
            Connection[id] = new XdgSurface(id, Version, ClientConnection);
            return (XdgSurface)Connection[id];
        }
        /// <summary>
        /// respond to a ping event
        /// <para>
        /// A client must respond to a ping event with a pong request or
        /// the client may be deemed unresponsive. See xdg_wm_base.ping
        /// and xdg_wm_base.error.unresponsive.
        /// </para>
        /// </summary>
        /// <param name="serial">serial of the ping event</param>
        public void Pong(uint serial)
        {
            Marshal((ushort)RequestOpcode.Pong, serial);
        }
    }
    /// xdg_positioner version 6
    /// <summary>
    /// child surface positioner
    /// <para>
    /// The xdg_positioner provides a collection of rules for the placement of a
    /// child surface relative to a parent surface. Rules can be defined to ensure
    /// the child surface remains within the visible area's borders, and to
    /// specify how the child surface changes its position, such as sliding along
    /// an axis, or flipping around a rectangle. These positioner-created rules are
    /// constrained by the requirement that a child surface must intersect with or
    /// be at least partially adjacent to its parent surface.
    /// 
    /// See the various requests for details about possible rules.
    /// 
    /// At the time of the request, the compositor makes a copy of the rules
    /// specified by the xdg_positioner. Thus, after the request is complete the
    /// xdg_positioner object can be destroyed or reused; further changes to the
    /// object will have no effect on previous usages.
    /// 
    /// For an xdg_positioner object to be considered complete, it must have a
    /// non-zero size set by set_size, and a non-zero anchor rectangle set by
    /// set_anchor_rect. Passing an incomplete xdg_positioner object when
    /// positioning a surface raises an invalid_positioner error.
    /// </para>
    /// </summary>
    public sealed class XdgPositioner : WaylandClientObject
    {
        public XdgPositioner(uint id, uint version, WaylandClientConnection connection) : base("xdg_positioner", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            SetSize,
            SetAnchorRect,
            SetAnchor,
            SetGravity,
            SetConstraintAdjustment,
            SetOffset,
            SetReactive,
            SetParentSize,
            SetParentConfigure,
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
            InvalidInput = 0,
        }
        public enum Anchor : int
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 3,
            Right = 4,
            TopLeft = 5,
            BottomLeft = 6,
            TopRight = 7,
            BottomRight = 8,
        }
        public enum Gravity : int
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 3,
            Right = 4,
            TopLeft = 5,
            BottomLeft = 6,
            TopRight = 7,
            BottomRight = 8,
        }
        /// <summary>
        /// constraint adjustments
        /// <para>
        /// The constraint adjustment value define ways the compositor will adjust
        /// the position of the surface, if the unadjusted position would result
        /// in the surface being partly constrained.
        /// 
        /// Whether a surface is considered 'constrained' is left to the compositor
        /// to determine. For example, the surface may be partly outside the
        /// compositor's defined 'work area', thus necessitating the child surface's
        /// position be adjusted until it is entirely inside the work area.
        /// 
        /// The adjustments can be combined, according to a defined precedence: 1)
        /// Flip, 2) Slide, 3) Resize.
        /// </para>
        /// </summary>
        [Flags]
        public enum ConstraintAdjustment : int
        {
            None = 0,
            SlideX = 1,
            SlideY = 2,
            FlipX = 4,
            FlipY = 8,
            ResizeX = 16,
            ResizeY = 32,
        }
        /// <summary>
        /// destroy the xdg_positioner object
        /// <para>
        /// Notify the compositor that the xdg_positioner will no longer be used.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// set the size of the to-be positioned rectangle
        /// <para>
        /// Set the size of the surface that is to be positioned with the positioner
        /// object. The size is in surface-local coordinates and corresponds to the
        /// window geometry. See xdg_surface.set_window_geometry.
        /// 
        /// If a zero or negative size is set the invalid_input error is raised.
        /// </para>
        /// </summary>
        /// <param name="width">width of positioned rectangle</param>
        /// <param name="height">height of positioned rectangle</param>
        public void SetSize(int width, int height)
        {
            Marshal((ushort)RequestOpcode.SetSize, width, height);
        }
        /// <summary>
        /// set the anchor rectangle within the parent surface
        /// <para>
        /// Specify the anchor rectangle within the parent surface that the child
        /// surface will be placed relative to. The rectangle is relative to the
        /// window geometry as defined by xdg_surface.set_window_geometry of the
        /// parent surface.
        /// 
        /// When the xdg_positioner object is used to position a child surface, the
        /// anchor rectangle may not extend outside the window geometry of the
        /// positioned child's parent surface.
        /// 
        /// If a negative size is set the invalid_input error is raised.
        /// </para>
        /// </summary>
        /// <param name="x">x position of anchor rectangle</param>
        /// <param name="y">y position of anchor rectangle</param>
        /// <param name="width">width of anchor rectangle</param>
        /// <param name="height">height of anchor rectangle</param>
        public void SetAnchorRect(int x, int y, int width, int height)
        {
            Marshal((ushort)RequestOpcode.SetAnchorRect, x, y, width, height);
        }
        /// <summary>
        /// set anchor rectangle anchor
        /// <para>
        /// Defines the anchor point for the anchor rectangle. The specified anchor
        /// is used derive an anchor point that the child surface will be
        /// positioned relative to. If a corner anchor is set (e.g. 'top_left' or
        /// 'bottom_right'), the anchor point will be at the specified corner;
        /// otherwise, the derived anchor point will be centered on the specified
        /// edge, or in the center of the anchor rectangle if no edge is specified.
        /// </para>
        /// </summary>
        /// <param name="anchor">anchor</param>
        public void SetAnchor(Anchor anchor)
        {
            Marshal((ushort)RequestOpcode.SetAnchor, (uint)anchor);
        }
        /// <summary>
        /// set child surface gravity
        /// <para>
        /// Defines in what direction a surface should be positioned, relative to
        /// the anchor point of the parent surface. If a corner gravity is
        /// specified (e.g. 'bottom_right' or 'top_left'), then the child surface
        /// will be placed towards the specified gravity; otherwise, the child
        /// surface will be centered over the anchor point on any axis that had no
        /// gravity specified. If the gravity is not in the ‘gravity’ enum, an
        /// invalid_input error is raised.
        /// </para>
        /// </summary>
        /// <param name="gravity">gravity direction</param>
        public void SetGravity(Gravity gravity)
        {
            Marshal((ushort)RequestOpcode.SetGravity, (uint)gravity);
        }
        /// <summary>
        /// set the adjustment to be done when constrained
        /// <para>
        /// Specify how the window should be positioned if the originally intended
        /// position caused the surface to be constrained, meaning at least
        /// partially outside positioning boundaries set by the compositor. The
        /// adjustment is set by constructing a bitmask describing the adjustment to
        /// be made when the surface is constrained on that axis.
        /// 
        /// If no bit for one axis is set, the compositor will assume that the child
        /// surface should not change its position on that axis when constrained.
        /// 
        /// If more than one bit for one axis is set, the order of how adjustments
        /// are applied is specified in the corresponding adjustment descriptions.
        /// 
        /// The default adjustment is none.
        /// </para>
        /// </summary>
        /// <param name="constraintAdjustment">bit mask of constraint adjustments</param>
        public void SetConstraintAdjustment(uint constraintAdjustment)
        {
            Marshal((ushort)RequestOpcode.SetConstraintAdjustment, constraintAdjustment);
        }
        /// <summary>
        /// set surface position offset
        /// <para>
        /// Specify the surface position offset relative to the position of the
        /// anchor on the anchor rectangle and the anchor on the surface. For
        /// example if the anchor of the anchor rectangle is at (x, y), the surface
        /// has the gravity bottom|right, and the offset is (ox, oy), the calculated
        /// surface position will be (x + ox, y + oy). The offset position of the
        /// surface is the one used for constraint testing. See
        /// set_constraint_adjustment.
        /// 
        /// An example use case is placing a popup menu on top of a user interface
        /// element, while aligning the user interface element of the parent surface
        /// with some user interface element placed somewhere in the popup surface.
        /// </para>
        /// </summary>
        /// <param name="x">surface position x offset</param>
        /// <param name="y">surface position y offset</param>
        public void SetOffset(int x, int y)
        {
            Marshal((ushort)RequestOpcode.SetOffset, x, y);
        }
        /// <summary>
        /// continuously reconstrain the surface
        /// <para>
        /// When set reactive, the surface is reconstrained if the conditions used
        /// for constraining changed, e.g. the parent window moved.
        /// 
        /// If the conditions changed and the popup was reconstrained, an
        /// xdg_popup.configure event is sent with updated geometry, followed by an
        /// xdg_surface.configure event.
        /// </para>
        /// </summary>
        public void SetReactive()
        {
            Marshal((ushort)RequestOpcode.SetReactive);
        }
        /// <summary>
        /// 
        /// <para>
        /// Set the parent window geometry the compositor should use when
        /// positioning the popup. The compositor may use this information to
        /// determine the future state the popup should be constrained using. If
        /// this doesn't match the dimension of the parent the popup is eventually
        /// positioned against, the behavior is undefined.
        /// 
        /// The arguments are given in the surface-local coordinate space.
        /// </para>
        /// </summary>
        /// <param name="parentWidth">future window geometry width of parent</param>
        /// <param name="parentHeight">future window geometry height of parent</param>
        public void SetParentSize(int parentWidth, int parentHeight)
        {
            Marshal((ushort)RequestOpcode.SetParentSize, parentWidth, parentHeight);
        }
        /// <summary>
        /// set parent configure this is a response to
        /// <para>
        /// Set the serial of an xdg_surface.configure event this positioner will be
        /// used in response to. The compositor may use this information together
        /// with set_parent_size to determine what future state the popup should be
        /// constrained using.
        /// </para>
        /// </summary>
        /// <param name="serial">serial of parent configure event</param>
        public void SetParentConfigure(uint serial)
        {
            Marshal((ushort)RequestOpcode.SetParentConfigure, serial);
        }
    }
    /// xdg_surface version 6
    /// <summary>
    /// desktop user interface surface base interface
    /// <para>
    /// An interface that may be implemented by a wl_surface, for
    /// implementations that provide a desktop-style user interface.
    /// 
    /// It provides a base set of functionality required to construct user
    /// interface elements requiring management by the compositor, such as
    /// toplevel windows, menus, etc. The types of functionality are split into
    /// xdg_surface roles.
    /// 
    /// Creating an xdg_surface does not set the role for a wl_surface. In order
    /// to map an xdg_surface, the client must create a role-specific object
    /// using, e.g., get_toplevel, get_popup. The wl_surface for any given
    /// xdg_surface can have at most one role, and may not be assigned any role
    /// not based on xdg_surface.
    /// 
    /// A role must be assigned before any other requests are made to the
    /// xdg_surface object.
    /// 
    /// The client must call wl_surface.commit on the corresponding wl_surface
    /// for the xdg_surface state to take effect.
    /// 
    /// Creating an xdg_surface from a wl_surface which has a buffer attached or
    /// committed is a client error, and any attempts by a client to attach or
    /// manipulate a buffer prior to the first xdg_surface.configure call must
    /// also be treated as errors.
    /// 
    /// After creating a role-specific object and setting it up, the client must
    /// perform an initial commit without any buffer attached. The compositor
    /// will reply with initial wl_surface state such as
    /// wl_surface.preferred_buffer_scale followed by an xdg_surface.configure
    /// event. The client must acknowledge it and is then allowed to attach a
    /// buffer to map the surface.
    /// 
    /// Mapping an xdg_surface-based role surface is defined as making it
    /// possible for the surface to be shown by the compositor. Note that
    /// a mapped surface is not guaranteed to be visible once it is mapped.
    /// 
    /// For an xdg_surface to be mapped by the compositor, the following
    /// conditions must be met:
    /// (1) the client has assigned an xdg_surface-based role to the surface
    /// (2) the client has set and committed the xdg_surface state and the
    /// role-dependent state to the surface
    /// (3) the client has committed a buffer to the surface
    /// 
    /// A newly-unmapped surface is considered to have met condition (1) out
    /// of the 3 required conditions for mapping a surface if its role surface
    /// has not been destroyed, i.e. the client must perform the initial commit
    /// again before attaching a buffer.
    /// </para>
    /// </summary>
    public sealed class XdgSurface : WaylandClientObject
    {
        public XdgSurface(uint id, uint version, WaylandClientConnection connection) : base("xdg_surface", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            GetToplevel,
            GetPopup,
            SetWindowGeometry,
            AckConfigure,
        }
        public enum EventOpcode : ushort
        {
            Configure,
        }
        /// <summary>
        /// suggest a surface change
        /// <para>
        /// The configure event marks the end of a configure sequence. A configure
        /// sequence is a set of one or more events configuring the state of the
        /// xdg_surface, including the final xdg_surface.configure event.
        /// 
        /// Where applicable, xdg_surface surface roles will during a configure
        /// sequence extend this event as a latched state sent as events before the
        /// xdg_surface.configure event. Such events should be considered to make up
        /// a set of atomically applied configuration states, where the
        /// xdg_surface.configure commits the accumulated state.
        /// 
        /// Clients should arrange their surface for the new states, and then send
        /// an ack_configure request with the serial sent in this configure event at
        /// some point before committing the new surface.
        /// 
        /// If the client receives multiple configure events before it can respond
        /// to one, it is free to discard all but the last event it received.
        /// </para>
        /// </summary>
        /// <param name="serial">serial of the configure event</param>
        public delegate void ConfigureHandler(XdgSurface xdgSurface, uint serial);
        public event ConfigureHandler Configure;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Configure:
                    {
                        var serial = (uint)arguments[0];
                        Configure?.Invoke(this, serial);
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
                case EventOpcode.Configure:
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
            NotConstructed = 1,
            AlreadyConstructed = 2,
            UnconfiguredBuffer = 3,
            InvalidSerial = 4,
            InvalidSize = 5,
            DefunctRoleObject = 6,
        }
        /// <summary>
        /// destroy the xdg_surface
        /// <para>
        /// Destroy the xdg_surface object. An xdg_surface must only be destroyed
        /// after its role object has been destroyed, otherwise
        /// a defunct_role_object error is raised.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// assign the xdg_toplevel surface role
        /// <para>
        /// This creates an xdg_toplevel object for the given xdg_surface and gives
        /// the associated wl_surface the xdg_toplevel role.
        /// 
        /// See the documentation of xdg_toplevel for more details about what an
        /// xdg_toplevel is and how it is used.
        /// </para>
        /// </summary>
        public XdgToplevel GetToplevel()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetToplevel, id);
            Connection[id] = new XdgToplevel(id, Version, ClientConnection);
            return (XdgToplevel)Connection[id];
        }
        /// <summary>
        /// assign the xdg_popup surface role
        /// <para>
        /// This creates an xdg_popup object for the given xdg_surface and gives
        /// the associated wl_surface the xdg_popup role.
        /// 
        /// If null is passed as a parent, a parent surface must be specified using
        /// some other protocol, before committing the initial state.
        /// 
        /// See the documentation of xdg_popup for more details about what an
        /// xdg_popup is and how it is used.
        /// </para>
        /// </summary>
        public XdgPopup GetPopup(XdgSurface parent, XdgPositioner positioner)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetPopup, id, parent.Id, positioner.Id);
            Connection[id] = new XdgPopup(id, Version, ClientConnection);
            return (XdgPopup)Connection[id];
        }
        /// <summary>
        /// set the new window geometry
        /// <para>
        /// The window geometry of a surface is its "visible bounds" from the
        /// user's perspective. Client-side decorations often have invisible
        /// portions like drop-shadows which should be ignored for the
        /// purposes of aligning, placing and constraining windows.
        /// 
        /// The window geometry is double buffered, and will be applied at the
        /// time wl_surface.commit of the corresponding wl_surface is called.
        /// 
        /// When maintaining a position, the compositor should treat the (x, y)
        /// coordinate of the window geometry as the top left corner of the window.
        /// A client changing the (x, y) window geometry coordinate should in
        /// general not alter the position of the window.
        /// 
        /// Once the window geometry of the surface is set, it is not possible to
        /// unset it, and it will remain the same until set_window_geometry is
        /// called again, even if a new subsurface or buffer is attached.
        /// 
        /// If never set, the value is the full bounds of the surface,
        /// including any subsurfaces. This updates dynamically on every
        /// commit. This unset is meant for extremely simple clients.
        /// 
        /// The arguments are given in the surface-local coordinate space of
        /// the wl_surface associated with this xdg_surface, and may extend outside
        /// of the wl_surface itself to mark parts of the subsurface tree as part of
        /// the window geometry.
        /// 
        /// When applied, the effective window geometry will be the set window
        /// geometry clamped to the bounding rectangle of the combined
        /// geometry of the surface of the xdg_surface and the associated
        /// subsurfaces.
        /// 
        /// The effective geometry will not be recalculated unless a new call to
        /// set_window_geometry is done and the new pending surface state is
        /// subsequently applied.
        /// 
        /// The width and height of the effective window geometry must be
        /// greater than zero. Setting an invalid size will raise an
        /// invalid_size error.
        /// </para>
        /// </summary>
        public void SetWindowGeometry(int x, int y, int width, int height)
        {
            Marshal((ushort)RequestOpcode.SetWindowGeometry, x, y, width, height);
        }
        /// <summary>
        /// ack a configure event
        /// <para>
        /// When a configure event is received, if a client commits the
        /// surface in response to the configure event, then the client
        /// must make an ack_configure request sometime before the commit
        /// request, passing along the serial of the configure event.
        /// 
        /// For instance, for toplevel surfaces the compositor might use this
        /// information to move a surface to the top left only when the client has
        /// drawn itself for the maximized or fullscreen state.
        /// 
        /// If the client receives multiple configure events before it
        /// can respond to one, it only has to ack the last configure event.
        /// Acking a configure event that was never sent raises an invalid_serial
        /// error.
        /// 
        /// A client is not required to commit immediately after sending
        /// an ack_configure request - it may even ack_configure several times
        /// before its next surface commit.
        /// 
        /// A client may send multiple ack_configure requests before committing, but
        /// only the last request sent before a commit indicates which configure
        /// event the client really is responding to.
        /// 
        /// Sending an ack_configure request consumes the serial number sent with
        /// the request, as well as serial numbers sent by all configure events
        /// sent on this xdg_surface prior to the configure event referenced by
        /// the committed serial.
        /// 
        /// It is an error to issue multiple ack_configure requests referencing a
        /// serial from the same configure event, or to issue an ack_configure
        /// request referencing a serial from a configure event issued before the
        /// event identified by the last ack_configure request for the same
        /// xdg_surface. Doing so will raise an invalid_serial error.
        /// </para>
        /// </summary>
        /// <param name="serial">the serial from the configure event</param>
        public void AckConfigure(uint serial)
        {
            Marshal((ushort)RequestOpcode.AckConfigure, serial);
        }
    }
    /// xdg_toplevel version 6
    /// <summary>
    /// toplevel surface
    /// <para>
    /// This interface defines an xdg_surface role which allows a surface to,
    /// among other things, set window-like properties such as maximize,
    /// fullscreen, and minimize, set application-specific metadata like title and
    /// id, and well as trigger user interactive operations such as interactive
    /// resize and move.
    /// 
    /// A xdg_toplevel by default is responsible for providing the full intended
    /// visual representation of the toplevel, which depending on the window
    /// state, may mean things like a title bar, window controls and drop shadow.
    /// 
    /// Unmapping an xdg_toplevel means that the surface cannot be shown
    /// by the compositor until it is explicitly mapped again.
    /// All active operations (e.g., move, resize) are canceled and all
    /// attributes (e.g. title, state, stacking, ...) are discarded for
    /// an xdg_toplevel surface when it is unmapped. The xdg_toplevel returns to
    /// the state it had right after xdg_surface.get_toplevel. The client
    /// can re-map the toplevel by perfoming a commit without any buffer
    /// attached, waiting for a configure event and handling it as usual (see
    /// xdg_surface description).
    /// 
    /// Attaching a null buffer to a toplevel unmaps the surface.
    /// </para>
    /// </summary>
    public sealed class XdgToplevel : WaylandClientObject
    {
        public XdgToplevel(uint id, uint version, WaylandClientConnection connection) : base("xdg_toplevel", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            SetParent,
            SetTitle,
            SetAppId,
            ShowWindowMenu,
            Move,
            Resize,
            SetMaxSize,
            SetMinSize,
            SetMaximized,
            UnsetMaximized,
            SetFullscreen,
            UnsetFullscreen,
            SetMinimized,
        }
        public enum EventOpcode : ushort
        {
            Configure,
            Close,
            ConfigureBounds,
            WmCapabilities,
        }
        /// <summary>
        /// suggest a surface change
        /// <para>
        /// This configure event asks the client to resize its toplevel surface or
        /// to change its state. The configured state should not be applied
        /// immediately. See xdg_surface.configure for details.
        /// 
        /// The width and height arguments specify a hint to the window
        /// about how its surface should be resized in window geometry
        /// coordinates. See set_window_geometry.
        /// 
        /// If the width or height arguments are zero, it means the client
        /// should decide its own window dimension. This may happen when the
        /// compositor needs to configure the state of the surface but doesn't
        /// have any information about any previous or expected dimension.
        /// 
        /// The states listed in the event specify how the width/height
        /// arguments should be interpreted, and possibly how it should be
        /// drawn.
        /// 
        /// Clients must send an ack_configure in response to this event. See
        /// xdg_surface.configure and xdg_surface.ack_configure for details.
        /// </para>
        /// </summary>
        public delegate void ConfigureHandler(XdgToplevel xdgToplevel, int width, int height, byte[] states);
        /// <summary>
        /// surface wants to be closed
        /// <para>
        /// The close event is sent by the compositor when the user
        /// wants the surface to be closed. This should be equivalent to
        /// the user clicking the close button in client-side decorations,
        /// if your application has any.
        /// 
        /// This is only a request that the user intends to close the
        /// window. The client may choose to ignore this request, or show
        /// a dialog to ask the user to save their data, etc.
        /// </para>
        /// </summary>
        public delegate void CloseHandler(XdgToplevel xdgToplevel);
        /// <summary>
        /// recommended window geometry bounds
        /// <para>
        /// The configure_bounds event may be sent prior to a xdg_toplevel.configure
        /// event to communicate the bounds a window geometry size is recommended
        /// to constrain to.
        /// 
        /// The passed width and height are in surface coordinate space. If width
        /// and height are 0, it means bounds is unknown and equivalent to as if no
        /// configure_bounds event was ever sent for this surface.
        /// 
        /// The bounds can for example correspond to the size of a monitor excluding
        /// any panels or other shell components, so that a surface isn't created in
        /// a way that it cannot fit.
        /// 
        /// The bounds may change at any point, and in such a case, a new
        /// xdg_toplevel.configure_bounds will be sent, followed by
        /// xdg_toplevel.configure and xdg_surface.configure.
        /// </para>
        /// </summary>
        public delegate void ConfigureBoundsHandler(XdgToplevel xdgToplevel, int width, int height);
        /// <summary>
        /// compositor capabilities
        /// <para>
        /// This event advertises the capabilities supported by the compositor. If
        /// a capability isn't supported, clients should hide or disable the UI
        /// elements that expose this functionality. For instance, if the
        /// compositor doesn't advertise support for minimized toplevels, a button
        /// triggering the set_minimized request should not be displayed.
        /// 
        /// The compositor will ignore requests it doesn't support. For instance,
        /// a compositor which doesn't advertise support for minimized will ignore
        /// set_minimized requests.
        /// 
        /// Compositors must send this event once before the first
        /// xdg_surface.configure event. When the capabilities change, compositors
        /// must send this event again and then send an xdg_surface.configure
        /// event.
        /// 
        /// The configured state should not be applied immediately. See
        /// xdg_surface.configure for details.
        /// 
        /// The capabilities are sent as an array of 32-bit unsigned integers in
        /// native endianness.
        /// </para>
        /// </summary>
        /// <param name="capabilities">array of 32-bit capabilities</param>
        public delegate void WmCapabilitiesHandler(XdgToplevel xdgToplevel, byte[] capabilities);
        public event ConfigureHandler Configure;
        public event CloseHandler Close;
        public event ConfigureBoundsHandler ConfigureBounds;
        public event WmCapabilitiesHandler WmCapabilities;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Configure:
                    {
                        var width = (int)arguments[0];
                        var height = (int)arguments[1];
                        var states = (byte[])arguments[2];
                        Configure?.Invoke(this, width, height, states);
                        break;
                    }
                case EventOpcode.Close:
                    {
                        Close?.Invoke(this);
                        break;
                    }
                case EventOpcode.ConfigureBounds:
                    {
                        var width = (int)arguments[0];
                        var height = (int)arguments[1];
                        ConfigureBounds?.Invoke(this, width, height);
                        break;
                    }
                case EventOpcode.WmCapabilities:
                    {
                        var capabilities = (byte[])arguments[0];
                        WmCapabilities?.Invoke(this, capabilities);
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
                case EventOpcode.Configure:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.Array,
                    };
                    break;
                case EventOpcode.Close:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.ConfigureBounds:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.WmCapabilities:
                    return new WaylandType[]
                    {
                        WaylandType.Array,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unknown event");
            }
        }
        public enum Error : int
        {
            InvalidResizeEdge = 0,
            InvalidParent = 1,
            InvalidSize = 2,
        }
        /// <summary>
        /// edge values for resizing
        /// <para>
        /// These values are used to indicate which edge of a surface
        /// is being dragged in a resize operation.
        /// </para>
        /// </summary>
        public enum ResizeEdge : int
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
        /// types of state on the surface
        /// <para>
        /// The different state values used on the surface. This is designed for
        /// state values like maximized, fullscreen. It is paired with the
        /// configure event to ensure that both the client and the compositor
        /// setting the state can be synchronized.
        /// 
        /// States set in this way are double-buffered. They will get applied on
        /// the next commit.
        /// </para>
        /// </summary>
        public enum State : int
        {
            Maximized = 1,
            Fullscreen = 2,
            Resizing = 3,
            Activated = 4,
            TiledLeft = 5,
            TiledRight = 6,
            TiledTop = 7,
            TiledBottom = 8,
            Suspended = 9,
        }
        public enum WmCapabilitiesEnum : int
        {
            WindowMenu = 1,
            Maximize = 2,
            Fullscreen = 3,
            Minimize = 4,
        }
        /// <summary>
        /// destroy the xdg_toplevel
        /// <para>
        /// This request destroys the role surface and unmaps the surface;
        /// see "Unmapping" behavior in interface section for details.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// set the parent of this surface
        /// <para>
        /// Set the "parent" of this surface. This surface should be stacked
        /// above the parent surface and all other ancestor surfaces.
        /// 
        /// Parent surfaces should be set on dialogs, toolboxes, or other
        /// "auxiliary" surfaces, so that the parent is raised when the dialog
        /// is raised.
        /// 
        /// Setting a null parent for a child surface unsets its parent. Setting
        /// a null parent for a surface which currently has no parent is a no-op.
        /// 
        /// Only mapped surfaces can have child surfaces. Setting a parent which
        /// is not mapped is equivalent to setting a null parent. If a surface
        /// becomes unmapped, its children's parent is set to the parent of
        /// the now-unmapped surface. If the now-unmapped surface has no parent,
        /// its children's parent is unset. If the now-unmapped surface becomes
        /// mapped again, its parent-child relationship is not restored.
        /// 
        /// The parent toplevel must not be one of the child toplevel's
        /// descendants, and the parent must be different from the child toplevel,
        /// otherwise the invalid_parent protocol error is raised.
        /// </para>
        /// </summary>
        public void SetParent(XdgToplevel parent)
        {
            Marshal((ushort)RequestOpcode.SetParent, parent.Id);
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
        public void SetTitle(string title)
        {
            Marshal((ushort)RequestOpcode.SetTitle, title);
        }
        /// <summary>
        /// set application ID
        /// <para>
        /// Set an application identifier for the surface.
        /// 
        /// The app ID identifies the general class of applications to which
        /// the surface belongs. The compositor can use this to group multiple
        /// surfaces together, or to determine how to launch a new application.
        /// 
        /// For D-Bus activatable applications, the app ID is used as the D-Bus
        /// service name.
        /// 
        /// The compositor shell will try to group application surfaces together
        /// by their app ID. As a best practice, it is suggested to select app
        /// ID's that match the basename of the application's .desktop file.
        /// For example, "org.freedesktop.FooViewer" where the .desktop file is
        /// "org.freedesktop.FooViewer.desktop".
        /// 
        /// Like other properties, a set_app_id request can be sent after the
        /// xdg_toplevel has been mapped to update the property.
        /// 
        /// See the desktop-entry specification [0] for more details on
        /// application identifiers and how they relate to well-known D-Bus
        /// names and .desktop files.
        /// 
        /// [0] https://standards.freedesktop.org/desktop-entry-spec/
        /// </para>
        /// </summary>
        public void SetAppId(string appId)
        {
            Marshal((ushort)RequestOpcode.SetAppId, appId);
        }
        /// <summary>
        /// show the window menu
        /// <para>
        /// Clients implementing client-side decorations might want to show
        /// a context menu when right-clicking on the decorations, giving the
        /// user a menu that they can use to maximize or minimize the window.
        /// 
        /// This request asks the compositor to pop up such a window menu at
        /// the given position, relative to the local surface coordinates of
        /// the parent surface. There are no guarantees as to what menu items
        /// the window menu contains, or even if a window menu will be drawn
        /// at all.
        /// 
        /// This request must be used in response to some sort of user action
        /// like a button press, key press, or touch down event.
        /// </para>
        /// </summary>
        /// <param name="seat">the wl_seat of the user event</param>
        /// <param name="serial">the serial of the user event</param>
        /// <param name="x">the x position to pop up the window menu at</param>
        /// <param name="y">the y position to pop up the window menu at</param>
        public void ShowWindowMenu(WlSeat seat, uint serial, int x, int y)
        {
            Marshal((ushort)RequestOpcode.ShowWindowMenu, seat.Id, serial, x, y);
        }
        /// <summary>
        /// start an interactive move
        /// <para>
        /// Start an interactive, user-driven move of the surface.
        /// 
        /// This request must be used in response to some sort of user action
        /// like a button press, key press, or touch down event. The passed
        /// serial is used to determine the type of interactive move (touch,
        /// pointer, etc).
        /// 
        /// The server may ignore move requests depending on the state of
        /// the surface (e.g. fullscreen or maximized), or if the passed serial
        /// is no longer valid.
        /// 
        /// If triggered, the surface will lose the focus of the device
        /// (wl_pointer, wl_touch, etc) used for the move. It is up to the
        /// compositor to visually indicate that the move is taking place, such as
        /// updating a pointer cursor, during the move. There is no guarantee
        /// that the device focus will return when the move is completed.
        /// </para>
        /// </summary>
        /// <param name="seat">the wl_seat of the user event</param>
        /// <param name="serial">the serial of the user event</param>
        public void Move(WlSeat seat, uint serial)
        {
            Marshal((ushort)RequestOpcode.Move, seat.Id, serial);
        }
        /// <summary>
        /// start an interactive resize
        /// <para>
        /// Start a user-driven, interactive resize of the surface.
        /// 
        /// This request must be used in response to some sort of user action
        /// like a button press, key press, or touch down event. The passed
        /// serial is used to determine the type of interactive resize (touch,
        /// pointer, etc).
        /// 
        /// The server may ignore resize requests depending on the state of
        /// the surface (e.g. fullscreen or maximized).
        /// 
        /// If triggered, the client will receive configure events with the
        /// "resize" state enum value and the expected sizes. See the "resize"
        /// enum value for more details about what is required. The client
        /// must also acknowledge configure events using "ack_configure". After
        /// the resize is completed, the client will receive another "configure"
        /// event without the resize state.
        /// 
        /// If triggered, the surface also will lose the focus of the device
        /// (wl_pointer, wl_touch, etc) used for the resize. It is up to the
        /// compositor to visually indicate that the resize is taking place,
        /// such as updating a pointer cursor, during the resize. There is no
        /// guarantee that the device focus will return when the resize is
        /// completed.
        /// 
        /// The edges parameter specifies how the surface should be resized, and
        /// is one of the values of the resize_edge enum. Values not matching
        /// a variant of the enum will cause the invalid_resize_edge protocol error.
        /// The compositor may use this information to update the surface position
        /// for example when dragging the top left corner. The compositor may also
        /// use this information to adapt its behavior, e.g. choose an appropriate
        /// cursor image.
        /// </para>
        /// </summary>
        /// <param name="seat">the wl_seat of the user event</param>
        /// <param name="serial">the serial of the user event</param>
        /// <param name="edges">which edge or corner is being dragged</param>
        public void Resize(WlSeat seat, uint serial, ResizeEdge edges)
        {
            Marshal((ushort)RequestOpcode.Resize, seat.Id, serial, (uint)edges);
        }
        /// <summary>
        /// set the maximum size
        /// <para>
        /// Set a maximum size for the window.
        /// 
        /// The client can specify a maximum size so that the compositor does
        /// not try to configure the window beyond this size.
        /// 
        /// The width and height arguments are in window geometry coordinates.
        /// See xdg_surface.set_window_geometry.
        /// 
        /// Values set in this way are double-buffered. They will get applied
        /// on the next commit.
        /// 
        /// The compositor can use this information to allow or disallow
        /// different states like maximize or fullscreen and draw accurate
        /// animations.
        /// 
        /// Similarly, a tiling window manager may use this information to
        /// place and resize client windows in a more effective way.
        /// 
        /// The client should not rely on the compositor to obey the maximum
        /// size. The compositor may decide to ignore the values set by the
        /// client and request a larger size.
        /// 
        /// If never set, or a value of zero in the request, means that the
        /// client has no expected maximum size in the given dimension.
        /// As a result, a client wishing to reset the maximum size
        /// to an unspecified state can use zero for width and height in the
        /// request.
        /// 
        /// Requesting a maximum size to be smaller than the minimum size of
        /// a surface is illegal and will result in an invalid_size error.
        /// 
        /// The width and height must be greater than or equal to zero. Using
        /// strictly negative values for width or height will result in a
        /// invalid_size error.
        /// </para>
        /// </summary>
        public void SetMaxSize(int width, int height)
        {
            Marshal((ushort)RequestOpcode.SetMaxSize, width, height);
        }
        /// <summary>
        /// set the minimum size
        /// <para>
        /// Set a minimum size for the window.
        /// 
        /// The client can specify a minimum size so that the compositor does
        /// not try to configure the window below this size.
        /// 
        /// The width and height arguments are in window geometry coordinates.
        /// See xdg_surface.set_window_geometry.
        /// 
        /// Values set in this way are double-buffered. They will get applied
        /// on the next commit.
        /// 
        /// The compositor can use this information to allow or disallow
        /// different states like maximize or fullscreen and draw accurate
        /// animations.
        /// 
        /// Similarly, a tiling window manager may use this information to
        /// place and resize client windows in a more effective way.
        /// 
        /// The client should not rely on the compositor to obey the minimum
        /// size. The compositor may decide to ignore the values set by the
        /// client and request a smaller size.
        /// 
        /// If never set, or a value of zero in the request, means that the
        /// client has no expected minimum size in the given dimension.
        /// As a result, a client wishing to reset the minimum size
        /// to an unspecified state can use zero for width and height in the
        /// request.
        /// 
        /// Requesting a minimum size to be larger than the maximum size of
        /// a surface is illegal and will result in an invalid_size error.
        /// 
        /// The width and height must be greater than or equal to zero. Using
        /// strictly negative values for width and height will result in a
        /// invalid_size error.
        /// </para>
        /// </summary>
        public void SetMinSize(int width, int height)
        {
            Marshal((ushort)RequestOpcode.SetMinSize, width, height);
        }
        /// <summary>
        /// maximize the window
        /// <para>
        /// Maximize the surface.
        /// 
        /// After requesting that the surface should be maximized, the compositor
        /// will respond by emitting a configure event. Whether this configure
        /// actually sets the window maximized is subject to compositor policies.
        /// The client must then update its content, drawing in the configured
        /// state. The client must also acknowledge the configure when committing
        /// the new content (see ack_configure).
        /// 
        /// It is up to the compositor to decide how and where to maximize the
        /// surface, for example which output and what region of the screen should
        /// be used.
        /// 
        /// If the surface was already maximized, the compositor will still emit
        /// a configure event with the "maximized" state.
        /// 
        /// If the surface is in a fullscreen state, this request has no direct
        /// effect. It may alter the state the surface is returned to when
        /// unmaximized unless overridden by the compositor.
        /// </para>
        /// </summary>
        public void SetMaximized()
        {
            Marshal((ushort)RequestOpcode.SetMaximized);
        }
        /// <summary>
        /// unmaximize the window
        /// <para>
        /// Unmaximize the surface.
        /// 
        /// After requesting that the surface should be unmaximized, the compositor
        /// will respond by emitting a configure event. Whether this actually
        /// un-maximizes the window is subject to compositor policies.
        /// If available and applicable, the compositor will include the window
        /// geometry dimensions the window had prior to being maximized in the
        /// configure event. The client must then update its content, drawing it in
        /// the configured state. The client must also acknowledge the configure
        /// when committing the new content (see ack_configure).
        /// 
        /// It is up to the compositor to position the surface after it was
        /// unmaximized; usually the position the surface had before maximizing, if
        /// applicable.
        /// 
        /// If the surface was already not maximized, the compositor will still
        /// emit a configure event without the "maximized" state.
        /// 
        /// If the surface is in a fullscreen state, this request has no direct
        /// effect. It may alter the state the surface is returned to when
        /// unmaximized unless overridden by the compositor.
        /// </para>
        /// </summary>
        public void UnsetMaximized()
        {
            Marshal((ushort)RequestOpcode.UnsetMaximized);
        }
        /// <summary>
        /// set the window as fullscreen on an output
        /// <para>
        /// Make the surface fullscreen.
        /// 
        /// After requesting that the surface should be fullscreened, the
        /// compositor will respond by emitting a configure event. Whether the
        /// client is actually put into a fullscreen state is subject to compositor
        /// policies. The client must also acknowledge the configure when
        /// committing the new content (see ack_configure).
        /// 
        /// The output passed by the request indicates the client's preference as
        /// to which display it should be set fullscreen on. If this value is NULL,
        /// it's up to the compositor to choose which display will be used to map
        /// this surface.
        /// 
        /// If the surface doesn't cover the whole output, the compositor will
        /// position the surface in the center of the output and compensate with
        /// with border fill covering the rest of the output. The content of the
        /// border fill is undefined, but should be assumed to be in some way that
        /// attempts to blend into the surrounding area (e.g. solid black).
        /// 
        /// If the fullscreened surface is not opaque, the compositor must make
        /// sure that other screen content not part of the same surface tree (made
        /// up of subsurfaces, popups or similarly coupled surfaces) are not
        /// visible below the fullscreened surface.
        /// </para>
        /// </summary>
        public void SetFullscreen(WlOutput output)
        {
            Marshal((ushort)RequestOpcode.SetFullscreen, output.Id);
        }
        /// <summary>
        /// unset the window as fullscreen
        /// <para>
        /// Make the surface no longer fullscreen.
        /// 
        /// After requesting that the surface should be unfullscreened, the
        /// compositor will respond by emitting a configure event.
        /// Whether this actually removes the fullscreen state of the client is
        /// subject to compositor policies.
        /// 
        /// Making a surface unfullscreen sets states for the surface based on the following:
        /// * the state(s) it may have had before becoming fullscreen
        /// * any state(s) decided by the compositor
        /// * any state(s) requested by the client while the surface was fullscreen
        /// 
        /// The compositor may include the previous window geometry dimensions in
        /// the configure event, if applicable.
        /// 
        /// The client must also acknowledge the configure when committing the new
        /// content (see ack_configure).
        /// </para>
        /// </summary>
        public void UnsetFullscreen()
        {
            Marshal((ushort)RequestOpcode.UnsetFullscreen);
        }
        /// <summary>
        /// set the window as minimized
        /// <para>
        /// Request that the compositor minimize your surface. There is no
        /// way to know if the surface is currently minimized, nor is there
        /// any way to unset minimization on this surface.
        /// 
        /// If you are looking to throttle redrawing when minimized, please
        /// instead use the wl_surface.frame event for this, as this will
        /// also work with live previews on windows in Alt-Tab, Expose or
        /// similar compositor features.
        /// </para>
        /// </summary>
        public void SetMinimized()
        {
            Marshal((ushort)RequestOpcode.SetMinimized);
        }
    }
    /// xdg_popup version 6
    /// <summary>
    /// short-lived, popup surfaces for menus
    /// <para>
    /// A popup surface is a short-lived, temporary surface. It can be used to
    /// implement for example menus, popovers, tooltips and other similar user
    /// interface concepts.
    /// 
    /// A popup can be made to take an explicit grab. See xdg_popup.grab for
    /// details.
    /// 
    /// When the popup is dismissed, a popup_done event will be sent out, and at
    /// the same time the surface will be unmapped. See the xdg_popup.popup_done
    /// event for details.
    /// 
    /// Explicitly destroying the xdg_popup object will also dismiss the popup and
    /// unmap the surface. Clients that want to dismiss the popup when another
    /// surface of their own is clicked should dismiss the popup using the destroy
    /// request.
    /// 
    /// A newly created xdg_popup will be stacked on top of all previously created
    /// xdg_popup surfaces associated with the same xdg_toplevel.
    /// 
    /// The parent of an xdg_popup must be mapped (see the xdg_surface
    /// description) before the xdg_popup itself.
    /// 
    /// The client must call wl_surface.commit on the corresponding wl_surface
    /// for the xdg_popup state to take effect.
    /// </para>
    /// </summary>
    public sealed class XdgPopup : WaylandClientObject
    {
        public XdgPopup(uint id, uint version, WaylandClientConnection connection) : base("xdg_popup", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            Grab,
            Reposition,
        }
        public enum EventOpcode : ushort
        {
            Configure,
            PopupDone,
            Repositioned,
        }
        /// <summary>
        /// configure the popup surface
        /// <para>
        /// This event asks the popup surface to configure itself given the
        /// configuration. The configured state should not be applied immediately.
        /// See xdg_surface.configure for details.
        /// 
        /// The x and y arguments represent the position the popup was placed at
        /// given the xdg_positioner rule, relative to the upper left corner of the
        /// window geometry of the parent surface.
        /// 
        /// For version 2 or older, the configure event for an xdg_popup is only
        /// ever sent once for the initial configuration. Starting with version 3,
        /// it may be sent again if the popup is setup with an xdg_positioner with
        /// set_reactive requested, or in response to xdg_popup.reposition requests.
        /// </para>
        /// </summary>
        /// <param name="x">x position relative to parent surface window geometry</param>
        /// <param name="y">y position relative to parent surface window geometry</param>
        /// <param name="width">window geometry width</param>
        /// <param name="height">window geometry height</param>
        public delegate void ConfigureHandler(XdgPopup xdgPopup, int x, int y, int width, int height);
        /// <summary>
        /// popup interaction is done
        /// <para>
        /// The popup_done event is sent out when a popup is dismissed by the
        /// compositor. The client should destroy the xdg_popup object at this
        /// point.
        /// </para>
        /// </summary>
        public delegate void PopupDoneHandler(XdgPopup xdgPopup);
        /// <summary>
        /// signal the completion of a repositioned request
        /// <para>
        /// The repositioned event is sent as part of a popup configuration
        /// sequence, together with xdg_popup.configure and lastly
        /// xdg_surface.configure to notify the completion of a reposition request.
        /// 
        /// The repositioned event is to notify about the completion of a
        /// xdg_popup.reposition request. The token argument is the token passed
        /// in the xdg_popup.reposition request.
        /// 
        /// Immediately after this event is emitted, xdg_popup.configure and
        /// xdg_surface.configure will be sent with the updated size and position,
        /// as well as a new configure serial.
        /// 
        /// The client should optionally update the content of the popup, but must
        /// acknowledge the new popup configuration for the new position to take
        /// effect. See xdg_surface.ack_configure for details.
        /// </para>
        /// </summary>
        /// <param name="token">reposition request token</param>
        public delegate void RepositionedHandler(XdgPopup xdgPopup, uint token);
        public event ConfigureHandler Configure;
        public event PopupDoneHandler PopupDone;
        public event RepositionedHandler Repositioned;
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.Configure:
                    {
                        var x = (int)arguments[0];
                        var y = (int)arguments[1];
                        var width = (int)arguments[2];
                        var height = (int)arguments[3];
                        Configure?.Invoke(this, x, y, width, height);
                        break;
                    }
                case EventOpcode.PopupDone:
                    {
                        PopupDone?.Invoke(this);
                        break;
                    }
                case EventOpcode.Repositioned:
                    {
                        var token = (uint)arguments[0];
                        Repositioned?.Invoke(this, token);
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
                case EventOpcode.Configure:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.Int,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.PopupDone:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Repositioned:
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
            InvalidGrab = 0,
        }
        /// <summary>
        /// remove xdg_popup interface
        /// <para>
        /// This destroys the popup. Explicitly destroying the xdg_popup
        /// object will also dismiss the popup, and unmap the surface.
        /// 
        /// If this xdg_popup is not the "topmost" popup, the
        /// xdg_wm_base.not_the_topmost_popup protocol error will be sent.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// make the popup take an explicit grab
        /// <para>
        /// This request makes the created popup take an explicit grab. An explicit
        /// grab will be dismissed when the user dismisses the popup, or when the
        /// client destroys the xdg_popup. This can be done by the user clicking
        /// outside the surface, using the keyboard, or even locking the screen
        /// through closing the lid or a timeout.
        /// 
        /// If the compositor denies the grab, the popup will be immediately
        /// dismissed.
        /// 
        /// This request must be used in response to some sort of user action like a
        /// button press, key press, or touch down event. The serial number of the
        /// event should be passed as 'serial'.
        /// 
        /// The parent of a grabbing popup must either be an xdg_toplevel surface or
        /// another xdg_popup with an explicit grab. If the parent is another
        /// xdg_popup it means that the popups are nested, with this popup now being
        /// the topmost popup.
        /// 
        /// Nested popups must be destroyed in the reverse order they were created
        /// in, e.g. the only popup you are allowed to destroy at all times is the
        /// topmost one.
        /// 
        /// When compositors choose to dismiss a popup, they may dismiss every
        /// nested grabbing popup as well. When a compositor dismisses popups, it
        /// will follow the same dismissing order as required from the client.
        /// 
        /// If the topmost grabbing popup is destroyed, the grab will be returned to
        /// the parent of the popup, if that parent previously had an explicit grab.
        /// 
        /// If the parent is a grabbing popup which has already been dismissed, this
        /// popup will be immediately dismissed. If the parent is a popup that did
        /// not take an explicit grab, an error will be raised.
        /// 
        /// During a popup grab, the client owning the grab will receive pointer
        /// and touch events for all their surfaces as normal (similar to an
        /// "owner-events" grab in X11 parlance), while the top most grabbing popup
        /// will always have keyboard focus.
        /// </para>
        /// </summary>
        /// <param name="seat">the wl_seat of the user event</param>
        /// <param name="serial">the serial of the user event</param>
        public void Grab(WlSeat seat, uint serial)
        {
            Marshal((ushort)RequestOpcode.Grab, seat.Id, serial);
        }
        /// <summary>
        /// recalculate the popup's location
        /// <para>
        /// Reposition an already-mapped popup. The popup will be placed given the
        /// details in the passed xdg_positioner object, and a
        /// xdg_popup.repositioned followed by xdg_popup.configure and
        /// xdg_surface.configure will be emitted in response. Any parameters set
        /// by the previous positioner will be discarded.
        /// 
        /// The passed token will be sent in the corresponding
        /// xdg_popup.repositioned event. The new popup position will not take
        /// effect until the corresponding configure event is acknowledged by the
        /// client. See xdg_popup.repositioned for details. The token itself is
        /// opaque, and has no other special meaning.
        /// 
        /// If multiple reposition requests are sent, the compositor may skip all
        /// but the last one.
        /// 
        /// If the popup is repositioned in response to a configure event for its
        /// parent, the client should send an xdg_positioner.set_parent_configure
        /// and possibly an xdg_positioner.set_parent_size request to allow the
        /// compositor to properly constrain the popup.
        /// 
        /// If the popup is repositioned together with a parent that is being
        /// resized, but not in response to a configure event, the client should
        /// send an xdg_positioner.set_parent_size request.
        /// </para>
        /// </summary>
        /// <param name="token">reposition request token</param>
        public void Reposition(XdgPositioner positioner, uint token)
        {
            Marshal((ushort)RequestOpcode.Reposition, positioner.Id, token);
        }
    }
}
