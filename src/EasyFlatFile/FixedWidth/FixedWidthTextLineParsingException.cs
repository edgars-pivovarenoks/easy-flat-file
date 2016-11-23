using System;
using System.Collections.Generic;
using System.Linq;
namespace EasyFlatFile
{
    public class FixedWidthTextLineParsingException : Exception
    {
        public FixedWidthTextLineParsingException(string message)
            : base(message)
        {
            FieldErrors = new List<string>(new[] { message });
        }
        public FixedWidthTextLineParsingException(IList<string> fieldErrors)
            : base(message: "Line parsing failed see FieldErrors for deatils")
        {
            FieldErrors = fieldErrors;
        }
        public IList<string> FieldErrors { get; private set; }
        public override string ToString()
        {
            if (FieldErrors.Any())
                return string.Concat(Message, ": ",
                    FieldErrors.ToSeparatedString());
            else
                return
                    base.ToString();
        }
    }
}
