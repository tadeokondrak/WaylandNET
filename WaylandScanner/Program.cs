using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace WaylandScanner
{
    class Program
    {
        static string Recase(string input, bool initial)
        {
            StringBuilder sb = new StringBuilder();
            bool upper = initial;
            foreach (char c in input)
            {
                if (c == '_')
                {
                    upper = true;
                }
                else if (upper)
                {
                    upper = false;
                    sb.Append(Char.ToUpper(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            var s = sb.ToString();
            if (s == "class" || s == "interface")
                return $"@{s}";
            else
                return s;
        }

        static string PascalCase(string input)
        {
            return Recase(input, true);
        }

        static string CamelCase(string input)
        {
            return Recase(input, false);
        }

        static string TypeForArgument(Argument argument, bool raw)
        {
            if (argument.Enum != null && !raw)
                return String.Join('.', argument.Enum.Split('.').Select(PascalCase));
            switch (argument.Type)
            {
                case "int":
                    return "int";
                case "uint":
                    return "uint";
                case "fixed":
                    return "double";
                case "string":
                    return "string";
                case "object":
                    return argument.Interface != null
                        ? PascalCase(argument.Interface)
                        : "WaylandClientObject";
                case "new_id":
                    return argument.Interface != null
                        ? PascalCase(argument.Interface)
                        : "WaylandClientObject";
                case "array":
                    return "byte[]";
                case "fd":
                    return "IntPtr";
                default:
                    throw new ArgumentException($"unknown wayland type {argument.Type}");
            }
        }

        static string TypeEnumForArgument(Argument argument)
        {
            switch (argument.Type)
            {
                case "int":
                    return "WaylandType.Int";
                case "uint":
                    return "WaylandType.UInt";
                case "fixed":
                    return "WaylandType.Fixed";
                case "string":
                    return "WaylandType.String";
                case "object":
                    return "WaylandType.Object";
                case "new_id":
                    return "WaylandType.NewId";
                case "array":
                    return "WaylandType.Array";
                case "fd":
                    return "WaylandType.Handle";
                default:
                    throw new ArgumentException($"unknown wayland type {argument.Type}");
            }
        }

        static void GenerateDocComment(CodeGenerator gen, string text)
        {
            foreach (var line in text.Trim().Split('\n'))
                gen.AppendLine($"/// {line.Trim()}");
        }

        static void GenerateCopyrightComment(CodeGenerator gen, Copyright copyright)
        {
            if (!String.IsNullOrWhiteSpace(copyright?.Content))
                GenerateDocComment(gen, copyright.Content);
        }

        static void GenerateDescriptionComment(CodeGenerator gen, Description description)
        {
            if (description == null)
                return;
            gen.AppendLine($"/// <summary>");
            GenerateDocComment(gen, description.Summary);
            if (!String.IsNullOrWhiteSpace(description.Content))
            {
                gen.AppendLine($"/// <para>");
                GenerateDocComment(gen, description.Content);
                gen.AppendLine($"/// </para>");
            }
            gen.AppendLine($"/// </summary>");
        }

        static void GenerateArgumentComment(CodeGenerator gen, Argument argument, bool isReturn)
        {
            if (!String.IsNullOrEmpty(argument.Summary))
            {
                if (isReturn)
                {
                    gen.AppendLine($"/// <returns>{argument.Summary}</returns>");
                }
                else
                {
                    gen.AppendLine($"/// <param name=\"{CamelCase(argument.Name)}\">"
                        + $"{argument.Summary}</param>");
                }
            }
        }

        static void GenerateInterfaceConstructor(CodeGenerator gen, Interface @interface)
        {
            using (gen.Block($"public {PascalCase(@interface.Name)}"
                + $"(uint id, uint version, WaylandClientConnection connection)"
                + $" : base(\"{@interface.Name}\", id, version, connection)"))
            {
            }
        }

        static void GenerateInterfaceRequestOpcode(CodeGenerator gen, Interface @interface)
        {
            using (gen.Block($"public enum RequestOpcode : ushort"))
            {
                foreach (var request in @interface.Requests)
                    gen.AppendLine($"{PascalCase(request.Name)},");
            }
        }

        static void GenerateInterfaceEventOpcode(CodeGenerator gen, Interface @interface)
        {
            using (gen.Block($"public enum EventOpcode : ushort"))
            {
                foreach (var @event in @interface.Events)
                    gen.AppendLine($"{PascalCase(@event.Name)},");
            }
        }

        static void GenerateInterfaceListenerInterface(CodeGenerator gen, Interface @interface)
        {
            using (gen.Block($"public interface IListener"))
            {
                foreach (var @event in @interface.Events)
                {
                    var name = PascalCase(@event.Name);
                    GenerateDescriptionComment(gen, @event.Description);
                    foreach (var argument in @event.Arguments)
                        GenerateArgumentComment(gen, argument, false);
                    var args = new List<string>()
                    {
                        $"{PascalCase(@interface.Name)} {CamelCase(@interface.Name)}",
                    };
                    foreach (var argument in @event.Arguments)
                        args.Add($"{TypeForArgument(argument, false)} {CamelCase(argument.Name)}");
                    gen.AppendLine($"void {name}({String.Join(", ", args)});");
                }
            }
        }

        static void GenerateInterfaceListenerProperty(CodeGenerator gen, Interface @interface)
        {
            gen.AppendLine("public IListener Listener { get; set; }");
        }

        static void GenerateInterfaceHandleMethod(CodeGenerator gen, Interface @interface)
        {
            using (gen.Block("public override void Handle(ushort opcode, params object[] arguments)"))
            {
                using (gen.Block("switch ((EventOpcode)opcode)"))
                {
                    foreach (var @event in @interface.Events)
                    {
                        var name = PascalCase(@event.Name);
                        using (gen.Case($"case EventOpcode.{name}:"))
                        {
                            using (gen.Block())
                            {
                                int i = 0;
                                foreach (var argument in @event.Arguments)
                                {
                                    if (argument.Enum != null)
                                    {
                                        gen.AppendLine($"var {CamelCase(argument.Name)} = "
                                            + $"({TypeForArgument(argument, false)})"
                                            + $"({TypeForArgument(argument, true)})arguments[{i}];");
                                    }
                                    else
                                    {
                                        gen.AppendLine($"var {CamelCase(argument.Name)} = "
                                            + $"({TypeForArgument(argument, true)})arguments[{i}];");
                                    }
                                    i++;
                                }
                                var args = new List<string>()
                                {
                                    "this",
                                };
                                foreach (var argument in @event.Arguments)
                                    args.Add($"{CamelCase(argument.Name)}");
                                using (gen.Block("if (Listener != null)"))
                                    gen.AppendLine($"Listener.{name}({String.Join(", ", args)});");
                                using (gen.Block("else"))
                                    gen.AppendLine(
                                        "Console.WriteLine($\"Warning: no listener on {this}\");");
                                if (@event.Type == "destructor")
                                    gen.AppendLine($"Die();");
                                gen.AppendLine("break;");
                            }

                        }
                    }
                    using (gen.Case($"default:"))
                        gen.AppendLine("throw new ArgumentOutOfRangeException(\"unknown event\");");
                }
            }
        }

        static void GenerateInterfaceArgumentsMethod(CodeGenerator gen, Interface @interface)
        {
            using (gen.Block("public override WaylandType[] Arguments(ushort opcode)"))
            {
                using (gen.Block("switch ((EventOpcode)opcode)"))
                {
                    foreach (var @event in @interface.Events)
                    {
                        using (gen.Case($"case EventOpcode.{PascalCase(@event.Name)}:"))
                        {
                            using (gen.SemicolonBlock("return new WaylandType[]"))
                            {
                                foreach (var argument in @event.Arguments)
                                    gen.AppendLine($"{TypeEnumForArgument(argument)},");
                            }
                            gen.AppendLine("break;");
                        }
                    }
                    using (gen.Case($"default:"))
                    {
                        gen.AppendLine("throw new ArgumentOutOfRangeException(\"unknown event\");");
                    }
                }
            }
        }

        static void GenerateEnum(CodeGenerator gen, Enum @enum)
        {
            GenerateDescriptionComment(gen, @enum.Description);
            if (@enum.Bitfield)
                gen.AppendLine("[Flags]");
            using (gen.Block($"public enum {PascalCase(@enum.Name)} : int"))
            {
                foreach (var entry in @enum.Entries)
                {
                    var val = entry.Value.StartsWith("0x")
                        ? Convert.ToInt32(entry.Value.Substring(2), 16)
                        : Convert.ToInt32(entry.Value);
                    var name = Char.IsDigit(entry.Name[0])
                        ? $"_{PascalCase(entry.Name)}"
                        : $"{PascalCase(entry.Name)}";
                    gen.AppendLine($"{name} = {val},");
                }
            }
        }

        static void GenerateRequestMethod(CodeGenerator gen, Message request)
        {
            Argument returnArgument = null;
            foreach (var argument in request.Arguments)
            {
                if (argument.Type == "new_id")
                    returnArgument = argument;
            }
            var returnType = "void";
            var generics = "";
            var genericsWhere = "";
            if (returnArgument != null)
            {
                if (returnArgument.Interface != null)
                {
                    returnType = PascalCase(returnArgument.Interface);
                }
                else
                {
                    returnType = "T";
                    generics = "<T>";
                    genericsWhere = " where T : WaylandClientObject";
                }
            }
            var args = new List<string>();
            foreach (var argument in request.Arguments)
            {
                if (argument == returnArgument)
                {
                    if (argument.Interface == null)
                    {
                        args.Add($"string @interface");
                        args.Add($"uint version");
                    }
                }
                else
                {
                    args.Add($"{TypeForArgument(argument, false)} {CamelCase(argument.Name)}");
                }
            }
            GenerateDescriptionComment(gen, request.Description);
            foreach (var argument in request.Arguments)
                GenerateArgumentComment(gen, argument, argument == returnArgument);
            using (gen.Block($"public {returnType} {PascalCase(request.Name)}{generics}"
                + $"({String.Join(", ", args)}){genericsWhere}"))
            {
                if (returnArgument != null)
                    gen.AppendLine($"uint {CamelCase(returnArgument.Name)} = Connection.AllocateId();");
                var marshalArgs = new List<string>()
                {
                    $"(ushort)RequestOpcode.{PascalCase(request.Name)}",
                };
                foreach (var argument in request.Arguments)
                {
                    if (argument.Type == "new_id" && argument.Interface == null)
                    {
                        marshalArgs.Add("@interface");
                        marshalArgs.Add("version");
                    }
                    if (argument.Type == "object")
                        marshalArgs.Add($"{CamelCase(argument.Name)}.Id");
                    else if (argument.Enum != null)
                        marshalArgs.Add(
                            $"({TypeForArgument(argument, true)}){CamelCase(argument.Name)}");
                    else
                        marshalArgs.Add(CamelCase(argument.Name));
                }
                gen.AppendLine($"Marshal({String.Join(", ", marshalArgs)});");
                if (request.Type == "destructor")
                    gen.AppendLine($"Die();");
                if (returnArgument != null)
                {
                    if (returnArgument.Interface != null)
                    {
                        gen.AppendLine($"Connection[{CamelCase(returnArgument.Name)}] = "
                            + $"new {returnType}({CamelCase(returnArgument.Name)}, Version, "
                            + $"ClientConnection);");
                    }
                    else
                    {
                        gen.AppendLine($"Connection[{CamelCase(returnArgument.Name)}] = "
                            + $"(WaylandClientObject)Activator.CreateInstance(typeof({returnType}), "
                            + $"{CamelCase(returnArgument.Name)}, version, "
                            + $"ClientConnection);");
                    }
                    gen.AppendLine($"return ({returnType})Connection"
                        + $"[{CamelCase(returnArgument.Name)}];");
                }
            }
        }

        static void GenerateInterface(CodeGenerator gen, Interface @interface)
        {
            gen.AppendLine($"/// {@interface.Name} version {@interface.Version}");
            GenerateDescriptionComment(gen, @interface.Description);
            using (gen.Block(
                $"public sealed class {PascalCase(@interface.Name)} : WaylandClientObject"))
            {
                GenerateInterfaceConstructor(gen, @interface);
                GenerateInterfaceRequestOpcode(gen, @interface);
                GenerateInterfaceEventOpcode(gen, @interface);
                GenerateInterfaceListenerInterface(gen, @interface);
                GenerateInterfaceListenerProperty(gen, @interface);
                GenerateInterfaceHandleMethod(gen, @interface);
                GenerateInterfaceArgumentsMethod(gen, @interface);
                foreach (var @enum in @interface.Enums)
                    GenerateEnum(gen, @enum);
                foreach (var request in @interface.Requests)
                    GenerateRequestMethod(gen, request);
            }
        }

        static void GenerateProtocol(CodeGenerator gen, Protocol protocol)
        {
            GenerateCopyrightComment(gen, protocol.Copyright);
            gen.AppendLine("#pragma warning disable 0162");
            gen.AppendLine("using System;");
            gen.AppendLine("using Wayland;");
            gen.AppendLine("using Wayland.Client;");
            GenerateDescriptionComment(gen, protocol.Description);
            using (gen.Block("namespace Wayland.Client.Protocol"))
            {
                foreach (var @interface in protocol.Interfaces)
                    GenerateInterface(gen, @interface);
            }
        }

        static void Main(string[] args)
        {
            var serializer = new XmlSerializer(typeof(Protocol));
            using (var reader = new FileStream(args[0], FileMode.Open, FileAccess.Read))
            {
                var gen = new CodeGenerator();
                var protocol = (Protocol)serializer.Deserialize(reader);
                GenerateProtocol(gen, protocol);
                Console.Write(gen.ToString());
            }
        }
    }
}
