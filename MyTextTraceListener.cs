namespace QuotesCheck
{
    using System.Diagnostics;
    using System.IO;

    public class MyTextTraceListener : TextWriterTraceListener
    {
        public MyTextTraceListener()
        {
        }

        public MyTextTraceListener(Stream stream)
            : base(stream)
        {
        }

        public MyTextTraceListener(TextWriter writer)
            : base(writer)
        {
        }

        public MyTextTraceListener(string fileName)
            : base(fileName)
        {
        }

        public MyTextTraceListener(Stream stream, string name)
            : base(stream, name)
        {
        }

        public MyTextTraceListener(TextWriter writer, string name)
            : base(writer, name)
        {
        }

        public MyTextTraceListener(string fileName, string name)
            : base(fileName, name)
        {
        }

        public override void Write(string x)
        {
            if ((x == null) || x.StartsWith("QuotesCheck.exe"))
            {
                return;
            }

            // Use whatever format you want here...
            base.Write(x);
        }

        public override void WriteLine(string x)
        {
            // Use whatever format you want here...
            base.WriteLine(x);
        }
    }
}