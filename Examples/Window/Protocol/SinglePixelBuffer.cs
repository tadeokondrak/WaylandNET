/// Copyright © 2022 Simon Ser
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
/// single pixel buffer factory
/// <para>
/// This protocol extension allows clients to create single-pixel buffers.
/// 
/// Compositors supporting this protocol extension should also support the
/// viewporter protocol extension. Clients may use viewporter to scale a
/// single-pixel buffer to a desired size.
/// 
/// Warning! The protocol described in this file is currently in the testing
/// phase. Backward compatible changes may be added together with the
/// corresponding interface version bump. Backward incompatible changes can
/// only be done by creating a new major version of the extension.
/// </para>
/// </summary>
namespace WaylandNET.Client.Protocol
{
    /// wp_single_pixel_buffer_manager_v1 version 1
    /// <summary>
    /// global factory for single-pixel buffers
    /// <para>
    /// The wp_single_pixel_buffer_manager_v1 interface is a factory for
    /// single-pixel buffers.
    /// </para>
    /// </summary>
    public sealed class WpSinglePixelBufferManagerV1 : WaylandClientObject
    {
        public WpSinglePixelBufferManagerV1(uint id, uint version, WaylandClientConnection connection) : base("wp_single_pixel_buffer_manager_v1", id, version, connection)
        {
        }
        public enum RequestOpcode : ushort
        {
            Destroy,
            CreateU32RgbaBuffer,
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
        /// destroy the manager
        /// <para>
        /// Destroy the wp_single_pixel_buffer_manager_v1 object.
        /// 
        /// The child objects created via this interface are unaffected.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            Marshal((ushort)RequestOpcode.Destroy);
            Die();
        }
        /// <summary>
        /// create a 1×1 buffer from 32-bit RGBA values
        /// <para>
        /// Create a single-pixel buffer from four 32-bit RGBA values.
        /// 
        /// Unless specified in another protocol extension, the RGBA values use
        /// pre-multiplied alpha.
        /// 
        /// The width and height of the buffer are 1.
        /// </para>
        /// </summary>
        /// <param name="r">value of the buffer's red channel</param>
        /// <param name="g">value of the buffer's green channel</param>
        /// <param name="b">value of the buffer's blue channel</param>
        /// <param name="a">value of the buffer's alpha channel</param>
        public WlBuffer CreateU32RgbaBuffer(uint r, uint g, uint b, uint a)
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)RequestOpcode.CreateU32RgbaBuffer, id, r, g, b, a);
            Connection[id] = new WlBuffer(id, Version, ClientConnection);
            return (WlBuffer)Connection[id];
        }
    }
}
