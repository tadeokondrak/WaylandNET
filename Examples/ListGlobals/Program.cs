using WaylandNET.Client;

using var conn = new WaylandClientConnection();
var registry = conn.Display.GetRegistry();
registry.Global += (_, name, @interface, version) =>
{
    Console.WriteLine($"Global {name}: {@interface} version {version}");
};
conn.Roundtrip();
