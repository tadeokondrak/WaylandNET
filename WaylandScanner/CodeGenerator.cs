using System;
using System.Text;

namespace WaylandScanner
{
    class Block : IDisposable
    {
        CodeGenerator generator;
        string begin;
        string end;

        public Block(CodeGenerator generator, string begin, string end)
        {
            this.generator = generator;
            this.begin = begin;
            this.end = end;
            if (begin != null)
                generator.AppendLine(begin);
            generator.Indent();
        }

        public void Dispose()
        {
            this.generator.Dedent();
            if (end != null)
                generator.AppendLine(end);
        }
    }

    class CodeGenerator
    {
        StringBuilder sb;
        int indent;

        public CodeGenerator()
        {
            sb = new StringBuilder();
        }

        public IDisposable Block(string line = null)
        {
            if (line != null)
                AppendLine(line);
            return new Block(this, "{", "}");
        }

        public IDisposable SemicolonBlock(string line = null)
        {
            if (line != null)
                AppendLine(line);
            return new Block(this, "{", "};");
        }

        public IDisposable Case(string line = null)
        {
            if (line != null)
                AppendLine(line);
            return new Block(this, null, null);
        }

        public void Indent()
        {
            indent++;
        }

        public void Dedent()
        {
            indent--;
        }

        public void AppendLine(string line)
        {
            for (int i = 0; (i / 4) < indent; i++)
            {
                sb.Append(' ');
            }
            sb.AppendLine(line);
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}
