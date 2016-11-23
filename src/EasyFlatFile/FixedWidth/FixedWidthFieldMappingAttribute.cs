using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace EasyFlatFile
{
    public class FixedWidthFieldMappingAttribute : Attribute
    {
        public FixedWidthFieldMappingAttribute()
        {
            _allowedValues = new List<string>();
            RegexCaptureGroupIndex = -1;
        }
        private List<string> _allowedValues;
        public double DecimalPrecision { get; set; }
        public string DefaultValue { get; set; }
        public string Format { get; set; }
        public int StartPosition { get; set; }
        public int Length { get; set; }
        public bool Mandatory { get; set; }
        public string AllowedValues { get { return _allowedValues.ToSeparatedString(","); } set { _allowedValues = new List<string>(value.Split(',')); } }
        public string IgnoreChar { get; set; }
        public object Minimum { get; set; }
        public object Maximum { get; set; }
        public Regex Regex { get; private set; }
        public string RegexExpression
        {
            get
            {
                if (Regex != null)
                    return Regex.ToString();

                return null;
            }
            set
            {
                if (value.HasValue())
                    Regex = new Regex(value);
                else
                    Regex = null;

            }
        }
        public int RegexCaptureGroupIndex { get; set; }
        public List<string> GetAllowedValues() { return _allowedValues; }
    }
}
