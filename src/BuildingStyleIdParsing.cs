// Copyright (c) 2025, 2026 Nicholas Hayes
// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.RegularExpressions;

namespace BuildingStyleToolsCommon
{
    public static partial class BuildingStyleIdParsing
    {
        public static bool IsValidSingleStyle(ReadOnlySpan<char> text)
        {
            return !text.IsEmpty
                && HexNumberRegex().IsMatch(text);
        }

        /// <summary>
        /// Determines whether the specified text is a is valid style list.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if the specified text is a is valid style list; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The style list is a comma separated list of hexadecimal styles ids with the 0x prefix.
        /// </remarks>
        public static bool IsValidStyleList(ReadOnlySpan<char> text)
        {
            return !text.IsEmpty
                && CommaSeparatedHexRegex().IsMatch(text);
        }

        public static List<uint> ParseStyleList(ReadOnlySpan<char> text)
        {
            if (!IsValidStyleList(text))
            {
                throw new InvalidOperationException("The style id text must be comma separated hexadecimal numbers.");
            }

            var result = new List<uint>();

            foreach (var range in text.Split(','))
            {
                result.Add(ParseStyleNumberInternal(text[range]));
            }

            return result;
        }

        public static uint ParseStyleNumber(ReadOnlySpan<char> text)
        {
            if (!IsValidSingleStyle(text))
            {
                throw new InvalidOperationException("The style id text must be a hexadecimal number with the 0x prefix.");
            }

            return ParseStyleNumberInternal(text);
        }

        public static string StyleNumberToString(uint style)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:X}", style);
        }

        private static uint ParseStyleNumberInternal(ReadOnlySpan<char> text)
        {
            // Remove the 0x prefix as uint.Parse thrown an exception if it is present.
            return uint.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        [GeneratedRegex("^0x[0-9a-f]+(?:,0x[0-9a-f]+)*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex CommaSeparatedHexRegex();

        [GeneratedRegex("^0x[0-9a-f]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex HexNumberRegex();
    }
}
