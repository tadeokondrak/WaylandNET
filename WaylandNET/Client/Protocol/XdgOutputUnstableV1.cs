/// Copyright © 2017 Red Hat Inc.
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
/// <summary>
/// Protocol to describe output regions
/// <para>
/// This protocol aims at describing outputs in a way which is more in line
/// with the concept of an output on desktop oriented systems.
/// 
/// Some information are more specific to the concept of an output for
/// a desktop oriented system and may not make sense in other applications,
/// such as IVI systems for example.
/// 
/// Typically, the global compositor space on a desktop system is made of
/// a contiguous or overlapping set of rectangular regions.
/// 
/// Some of the information provided in this protocol might be identical
/// to their counterparts already available from wl_output, in which case
/// the information provided by this protocol should be preferred to their
/// equivalent in wl_output. The goal is to move the desktop specific
/// concepts (such as output location within the global compositor space,
/// the connector name and types, etc.) out of the core wl_output protocol.
/// 
/// Warning! The protocol described in this file is experimental and
/// backward incompatible changes may be made. Backward compatible
/// changes may be added together with the corresponding interface
/// version bump.
/// Backward incompatible changes are done by bumping the version
/// number in the protocol and interface names and resetting the
/// interface version. Once the protocol is to be declared stable,
/// the 'z' prefix and the version number in the protocol and
/// interface names are removed and the interface version number is
/// reset.
/// </para>
/// </summary>
namespace WaylandNET.Client.Protocol
{
    /// zxdg_output_manager_v1 version 3
    /// <summary>
    /// manage xdg_output objects
    /// <para>
    /// A global factory interface for xdg_output objects.
    /// </para>
    /// </summary>
    public sealed class ZxdgOutputManagerV1 : WaylandClientObject
    {
        public ZxdgOutputManagerV1(uint id, uint version, WaylandClientConnection connection) : base("zxdg_output_manager_v1", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            GetXdgOutput,
        }
        public enum EventOpcode : ushort
        {
        }
        public interface IListener
        {
        }
        public IListener Listener { get; set; }
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
        /// destroy the xdg_output_manager object
        /// <para>
        /// Using this request a client can tell the server that it is not
        /// going to use the xdg_output_manager object anymore.
        /// 
        /// Any objects already created through this instance are not affected.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// create an xdg output from a wl_output
        /// <para>
        /// This creates a new xdg_output object for the given wl_output.
        /// </para>
        /// </summary>
        public ZxdgOutputV1 GetXdgOutput(WlOutput output)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.GetXdgOutput, id, output.Id);
            Connection[id] = new ZxdgOutputV1(id, Version, ClientConnection);
            return (ZxdgOutputV1)Connection[id];
        }
    }
    /// zxdg_output_v1 version 3
    /// <summary>
    /// compositor logical output region
    /// <para>
    /// An xdg_output describes part of the compositor geometry.
    /// 
    /// This typically corresponds to a monitor that displays part of the
    /// compositor space.
    /// 
    /// For objects version 3 onwards, after all xdg_output properties have been
    /// sent (when the object is created and when properties are updated), a
    /// wl_output.done event is sent. This allows changes to the output
    /// properties to be seen as atomic, even if they happen via multiple events.
    /// </para>
    /// </summary>
    public sealed class ZxdgOutputV1 : WaylandClientObject
    {
        public ZxdgOutputV1(uint id, uint version, WaylandClientConnection connection) : base("zxdg_output_v1", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
        }
        public enum EventOpcode : ushort
        {
            LogicalPosition,
            LogicalSize,
            Done,
            Name,
            Description,
        }
        public interface IListener
        {
            /// <summary>
            /// position of the output within the global compositor space
            /// <para>
            /// The position event describes the location of the wl_output within
            /// the global compositor space.
            /// 
            /// The logical_position event is sent after creating an xdg_output
            /// (see xdg_output_manager.get_xdg_output) and whenever the location
            /// of the output changes within the global compositor space.
            /// </para>
            /// </summary>
            /// <param name="x">x position within the global compositor space</param>
            /// <param name="y">y position within the global compositor space</param>
            void LogicalPosition(ZxdgOutputV1 zxdgOutputV1, int x, int y);
            /// <summary>
            /// size of the output in the global compositor space
            /// <para>
            /// The logical_size event describes the size of the output in the
            /// global compositor space.
            /// 
            /// For example, a surface without any buffer scale, transformation
            /// nor rotation set, with the size matching the logical_size will
            /// have the same size as the corresponding output when displayed.
            /// 
            /// Most regular Wayland clients should not pay attention to the
            /// logical size and would rather rely on xdg_shell interfaces.
            /// 
            /// Some clients such as Xwayland, however, need this to configure
            /// their surfaces in the global compositor space as the compositor
            /// may apply a different scale from what is advertised by the output
            /// scaling property (to achieve fractional scaling, for example).
            /// 
            /// For example, for a wl_output mode 3840×2160 and a scale factor 2:
            /// 
            /// - A compositor not scaling the surface buffers will advertise a
            /// logical size of 3840×2160,
            /// 
            /// - A compositor automatically scaling the surface buffers will
            /// advertise a logical size of 1920×1080,
            /// 
            /// - A compositor using a fractional scale of 1.5 will advertise a
            /// logical size to 2560×1620.
            /// 
            /// For example, for a wl_output mode 1920×1080 and a 90 degree rotation,
            /// the compositor will advertise a logical size of 1080x1920.
            /// 
            /// The logical_size event is sent after creating an xdg_output
            /// (see xdg_output_manager.get_xdg_output) and whenever the logical
            /// size of the output changes, either as a result of a change in the
            /// applied scale or because of a change in the corresponding output
            /// mode(see wl_output.mode) or transform (see wl_output.transform).
            /// </para>
            /// </summary>
            /// <param name="width">width in global compositor space</param>
            /// <param name="height">height in global compositor space</param>
            void LogicalSize(ZxdgOutputV1 zxdgOutputV1, int width, int height);
            /// <summary>
            /// all information about the output have been sent
            /// <para>
            /// This event is sent after all other properties of an xdg_output
            /// have been sent.
            /// 
            /// This allows changes to the xdg_output properties to be seen as
            /// atomic, even if they happen via multiple events.
            /// 
            /// For objects version 3 onwards, this event is deprecated. Compositors
            /// are not required to send it anymore and must send wl_output.done
            /// instead.
            /// </para>
            /// </summary>
            void Done(ZxdgOutputV1 zxdgOutputV1);
            /// <summary>
            /// name of this output
            /// <para>
            /// Many compositors will assign names to their outputs, show them to the
            /// user, allow them to be configured by name, etc. The client may wish to
            /// know this name as well to offer the user similar behaviors.
            /// 
            /// The naming convention is compositor defined, but limited to
            /// alphanumeric characters and dashes (-). Each name is unique among all
            /// wl_output globals, but if a wl_output global is destroyed the same name
            /// may be reused later. The names will also remain consistent across
            /// sessions with the same hardware and software configuration.
            /// 
            /// Examples of names include 'HDMI-A-1', 'WL-1', 'X11-1', etc. However, do
            /// not assume that the name is a reflection of an underlying DRM
            /// connector, X11 connection, etc.
            /// 
            /// The name event is sent after creating an xdg_output (see
            /// xdg_output_manager.get_xdg_output). This event is only sent once per
            /// xdg_output, and the name does not change over the lifetime of the
            /// wl_output global.
            /// </para>
            /// </summary>
            /// <param name="name">output name</param>
            void Name(ZxdgOutputV1 zxdgOutputV1, string name);
            /// <summary>
            /// human-readable description of this output
            /// <para>
            /// Many compositors can produce human-readable descriptions of their
            /// outputs.  The client may wish to know this description as well, to
            /// communicate the user for various purposes.
            /// 
            /// The description is a UTF-8 string with no convention defined for its
            /// contents. Examples might include 'Foocorp 11" Display' or 'Virtual X11
            /// output via :1'.
            /// 
            /// The description event is sent after creating an xdg_output (see
            /// xdg_output_manager.get_xdg_output) and whenever the description
            /// changes. The description is optional, and may not be sent at all.
            /// 
            /// For objects of version 2 and lower, this event is only sent once per
            /// xdg_output, and the description does not change over the lifetime of
            /// the wl_output global.
            /// </para>
            /// </summary>
            /// <param name="description">output description</param>
            void Description(ZxdgOutputV1 zxdgOutputV1, string description);
        }
        public IListener Listener { get; set; }
        public override void Handle(ushort opcode, params object[] arguments)
        {
            switch ((EventOpcode)opcode)
            {
                case EventOpcode.LogicalPosition:
                    {
                        var x = (int)arguments[0];
                        var y = (int)arguments[1];
                        if (Listener != null)
                        {
                            Listener.LogicalPosition(this, x, y);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: no listener on {this}");
                        }
                        break;
                    }
                case EventOpcode.LogicalSize:
                    {
                        var width = (int)arguments[0];
                        var height = (int)arguments[1];
                        if (Listener != null)
                        {
                            Listener.LogicalSize(this, width, height);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: no listener on {this}");
                        }
                        break;
                    }
                case EventOpcode.Done:
                    {
                        if (Listener != null)
                        {
                            Listener.Done(this);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: no listener on {this}");
                        }
                        break;
                    }
                case EventOpcode.Name:
                    {
                        var name = (string)arguments[0];
                        if (Listener != null)
                        {
                            Listener.Name(this, name);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: no listener on {this}");
                        }
                        break;
                    }
                case EventOpcode.Description:
                    {
                        var description = (string)arguments[0];
                        if (Listener != null)
                        {
                            Listener.Description(this, description);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: no listener on {this}");
                        }
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
                case EventOpcode.LogicalPosition:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.LogicalSize:
                    return new WaylandType[]
                    {
                        WaylandType.Int,
                        WaylandType.Int,
                    };
                    break;
                case EventOpcode.Done:
                    return new WaylandType[]
                    {
                    };
                    break;
                case EventOpcode.Name:
                    return new WaylandType[]
                    {
                        WaylandType.String,
                    };
                    break;
                case EventOpcode.Description:
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
        /// destroy the xdg_output object
        /// <para>
        /// Using this request a client can tell the server that it is not
        /// going to use the xdg_output object anymore.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
    }
}
