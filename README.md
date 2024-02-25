# WaylandNET

Wayland implementation in C#.

Not bindings to libwayland, speaks the protocol itself,
which means it doesn't support using OpenGL or Vulkan the normal way.

Does not yet work on non-little-endian platforms.
Does not yet support requests or events with file descriptors.  
Does not yet do any request buffering.  
