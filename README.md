# WaylandNET

Wayland implementation in C#.

Not bindings to libwayland, speaks the protocol itself,
which means it doesn't support using OpenGL or Vulkan.

Does not yet work on non-little-endian platforms.  
Does not yet support requests or events with file descriptors.  
Does not yet do any request buffering.  

This is the first C# I've ever written, so don't expect much.
