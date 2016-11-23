using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
namespace EasyFlatFile
{
    public class FixedWidthTextParser<TDestination> where TDestination : new()
    {
        private int _totalMappedLineLength;
        private class FieldMapper
        {
            public Type DataType { get; set; }
            public string FieldName { get; set; }
            public Action<object, object> SetValue { get; set; }
            public FixedWidthFieldMappingAttribute Mapping { get; set; }
        }
        private List<FieldMapper> _fieldMappers;
        public FixedWidthTextParser()
        {
            // Create mappers
            _fieldMappers = typeof(TDestination)
                 .GetTypeInfo()
                 .GetProperties()
                 .Where(p => p.GetCustomAttributes(typeof(FixedWidthFieldMappingAttribute), false).Any())
                 .Select(p =>
                     new FieldMapper
                     {
                         DataType = p.PropertyType,
                         FieldName = p.Name,
                         SetValue = p.SetValue,
                         Mapping = (FixedWidthFieldMappingAttribute)p.GetCustomAttributes(typeof(FixedWidthFieldMappingAttribute), true).First()
                     })
                     .ToList();

            _totalMappedLineLength =
                _fieldMappers.Sum(m => m.Mapping.Length);

            // TODO : Verify overlaps, zeros and othe typical errors etc..
        }
        private const string LINE_PARSING_ERROR_TEMPLATE = "{0} parsing exception : {1}";
        public TDestination ParseTextLine(string textLine)
        {
            var parsingErrors = new List<string>();
            var destination = new TDestination();

            if (!textLine.HasValue())
                throw new FixedWidthTextLineParsingException("Text line passed for is null or empty");

            var textLineLength = textLine.Length;
            if (_totalMappedLineLength != textLineLength)
                throw new FixedWidthTextLineParsingException(string.Format("Parsed text line Length : {0} is not equal with mapped length Total : {1}, please verify field mappings lengths.",
                    textLineLength, _totalMappedLineLength));

            // TODO : Think of few more checks
            foreach (var field in _fieldMappers)
            {
                string fieldDescriptorText = string
                    .Format("Field [{0}] at Position : {1}, Length : {2}",
                    field.FieldName,
                    field.Mapping.StartPosition,
                    field.Mapping.Length);

                try
                {
                    var sourceValueText = textLine
                        .Substring(field.Mapping.StartPosition, field.Mapping.Length);

                    // Extract only part of string according to regex
                    if (sourceValueText.HasValue()
                        && field.Mapping.Regex != null
                        && field.Mapping.RegexCaptureGroupIndex > 0)
                    {
                        var matches = field.Mapping.Regex.Match(sourceValueText);

                        if (matches.Groups.Count >= field.Mapping.RegexCaptureGroupIndex + 1)
                            sourceValueText = matches.Groups[field.Mapping.RegexCaptureGroupIndex].Value;
                    }

                    // Operate on default value if it is set
                    if (!field.Mapping.Mandatory && !sourceValueText.HasValue() && field.Mapping.DefaultValue != null)
                    {
                        sourceValueText = field.Mapping.DefaultValue;
                        fieldDescriptorText = string.Concat(fieldDescriptorText, ", default value");
                    }
                    // Just some rough clean up if some value like "0" should be ignored altogether
                    else if (field.Mapping.IgnoreChar.HasValue() && !field.Mapping.Mandatory && sourceValueText.HasValue())
                    {
                        var ignoreChar = field.Mapping.IgnoreChar.First();
                        if (sourceValueText.All(v => v.Equals(ignoreChar)))
                            sourceValueText = "";
                    }

                    object sourceValue = null;
                    var allowedValues = field.Mapping.GetAllowedValues();

                    if (field.Mapping.Mandatory && !sourceValueText.HasValue())
                        parsingErrors
                            .Add(string.Format("{0} is mandatory, but is empty", fieldDescriptorText));
                    else if (allowedValues.Any()
                             && sourceValueText.HasValue()
                             && !allowedValues.Contains(sourceValueText.ToNonNullString()))
                        parsingErrors
                            .Add(string.Format("{0} value [{1}] is not one of allowed values [{2}]", fieldDescriptorText, sourceValueText, field.Mapping.AllowedValues));
                    else if (field.DataType.GetTypeInfo().IsValueType && field.DataType.GetTypeInfo().IsAssignableFrom(typeof(int)))
                    {
                        var tempValue = sourceValueText.HasValue() ?
                            Convert.ToInt32(sourceValueText) : default(int?);

                        if (tempValue.HasValue)
                            if ((field.Mapping.Minimum != null && tempValue.Value < field.Mapping.Minimum.ToInt())
                                || (field.Mapping.Maximum != null && tempValue.Value > field.Mapping.Maximum.ToInt()))
                                parsingErrors
                                    .Add(string.Format("{0} value [{1}] is not within defined minimum and maximum range [from {2} to {3}]", fieldDescriptorText, sourceValueText, field.Mapping.Minimum, field.Mapping.Maximum));
                            else
                                sourceValue = tempValue;

                    }
                    else if (field.DataType.GetTypeInfo().IsValueType && field.DataType.GetTypeInfo().IsAssignableFrom(typeof(decimal)))
                    {
                        var decimalValue = Convert.ToDecimal(sourceValueText);
                        sourceValue = field.Mapping.DecimalPrecision == 0 ? decimalValue : decimalValue / (decimal)Math.Pow(10D, field.Mapping.DecimalPrecision);
                    }
                    else if (field.DataType.GetTypeInfo().IsAssignableFrom(typeof(DateTime)))
                        sourceValue = DateTime
                            .ParseExact(sourceValueText.ToString(), field.Mapping.Format, CultureInfo.InvariantCulture);
                    else
                        sourceValue = sourceValueText.ToStringSafe();

                    // TODO : Consider nullable fields, is everyting ok?

                    field
                        .SetValue(destination, sourceValue);
                }
                catch (Exception ex)
                {
                    parsingErrors
                        .Add(string.Format(LINE_PARSING_ERROR_TEMPLATE, fieldDescriptorText, ex.Message));
                }
            }

            if (parsingErrors.Any())
                throw new FixedWidthTextLineParsingException(parsingErrors);

            return
                destination;
        }
    }
}
