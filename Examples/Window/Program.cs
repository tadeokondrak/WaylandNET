using WaylandNET.Client;
using WaylandNET.Client.Protocol;

using var conn = new WaylandClientConnection();

var globals = new Dictionary<string, (uint Name, uint Version)>();
var registry = conn.Display.GetRegistry();
registry.Global += (_, name, @interface, version) =>
{
    globals.Add(@interface, (name, version));
};

conn.Roundtrip();

var quit = false;

var compositor = registry.Bind<WlCompositor>(globals["wl_compositor"].Name, "wl_compositor", 4);
var wmBase = registry.Bind<XdgWmBase>(globals["xdg_wm_base"].Name, "xdg_wm_base", 1);
var viewporter = registry.Bind<WpViewporter>(globals["wp_viewporter"].Name, "wp_viewporter", 1);
var pixel_buffer_manager = registry.Bind<WpSinglePixelBufferManagerV1>(
    globals["wp_single_pixel_buffer_manager_v1"].Name, "wp_single_pixel_buffer_manager_v1", 1);

var buffer = (WlBuffer)null;

var surface = compositor.CreateSurface();
var xdgSurface = wmBase.GetXdgSurface(surface);

var toplevel = xdgSurface.GetToplevel();
var viewport = viewporter.GetViewport(surface);

surface.Commit();

var redraw = null as Action;

redraw = () =>
{
    surface.Frame().Done += (_, _) => redraw();
    buffer?.Destroy();
    var v = (uint)((Math.Sin((double)((uint)Environment.TickCount % 2500) / 2500d * Math.Tau) * 0.5 + 0.5) * uint.MaxValue);
    buffer = pixel_buffer_manager.CreateU32RgbaBuffer(v, v, v, uint.MaxValue);
    surface.Attach(buffer, 0, 0);
    surface.DamageBuffer(0, 0, 1, 1);
    surface.Commit();
};

xdgSurface.Configure += (_, serial) =>
{
    xdgSurface.AckConfigure(serial);
    redraw();
};

toplevel.Configure += (_, newWidth, newHeight, _) =>
{
    if (newWidth == 0) newWidth = 1280;
    if (newHeight == 0) newHeight = 720;
    viewport.SetSource(0, 0, 1, 1);
    viewport.SetDestination(newWidth, newHeight);
};

toplevel.Close += (_) => quit = true;
wmBase.Ping += (_, serial) => wmBase.Pong(serial);

while (!quit)
{
    conn.Read();
}
