// Copyright (c) 2025, 2026 Nicholas Hayes
// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace BuildingStyleToolsCommon
{
    public sealed class BuildingStylesOnlineSpreadsheet
    {
        // The URL for the Building Styles Master IID spreadsheet.
        // The /gviz/tq?tqx=out:json suffix is the Google "Chart Tools data source protocol".
        // See https://stackoverflow.com/a/33727897
        private const string RequestUri = @"https://docs.google.com/spreadsheets/d/1lG7vdgh-eAG3McqOL1GdPGgoEjg1V4jQJuVEqKuVCaM/gviz/tq?tqx=out:json";

        private static readonly HttpClient httpClient = new();

        private readonly Dictionary<uint, BuildingStyleInfo> buildingStyles;
        private uint nextBlockStyleId;
        private string blockAuthor;

        private BuildingStylesOnlineSpreadsheet()
        {
            buildingStyles = [];
            nextBlockStyleId = 0;
            blockAuthor = string.Empty;
        }

        internal static Dictionary<uint, BuildingStyleInfo> Load()
        {
            Dictionary<uint, BuildingStyleInfo> buildingStyles;

            using (HttpRequestMessage requestMessage = new(HttpMethod.Get, RequestUri))
            {
                // This header is required to make Google return plain JSON data
                // instead of their query language.
                requestMessage.Headers.Add("X-DataSource-Auth", "");

                using (HttpResponseMessage message = httpClient.Send(requestMessage).EnsureSuccessStatusCode())
                {
                    using (Stream stream = message.Content.ReadAsStream())
                    {
                        long oldPosition = stream.Position;

                        // Google prefixes the returned JSON data with )]}'\n to reduce the chances of a browser executing it.
                        // See https://developers.google.com/chart/interactive/docs/dev/implementing_data_source#security-considerations
                        ReadOnlySpan<byte> jsonSecurityPrefix = ")]}'\n"u8;

                        Span<byte> bytes = stackalloc byte[jsonSecurityPrefix.Length];

                        stream.ReadExactly(bytes);

                        if (!bytes.SequenceEqual(jsonSecurityPrefix))
                        {
                            // The prefix is not present, treat the stream as plain JSON data.
                            stream.Position = oldPosition;
                        }

                        BuildingStylesOnlineSpreadsheet parser = new();
                        parser.ParseJson(stream);
                        buildingStyles = parser.buildingStyles;
                    }
                }
            }

            return buildingStyles;
        }

        private static string? GetCellValueAsString(JsonElement cell)
        {
            string? result = null;

            if (TryGetCellValue(cell, out JsonElement element))
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    result = element.GetString();
                }
            }

            return result;
        }

        private static bool TryGetCellValue(JsonElement element, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                return element.TryGetProperty("v"u8, out value);
            }

            value = default;
            return false;
        }

        private void ParseJson(Stream stream)
        {
            JsonDocumentOptions options = new()
            {
                CommentHandling = JsonCommentHandling.Skip,
            };

            using (JsonDocument document = JsonDocument.Parse(stream, options))
            {
                // The JSON blob has the following structure:
                // [
                //   "version": "0.6",
                //   "reqId": "0",
                //   "status": "ok",
                //   "sig": "337015668",
                //   "table": {
                //     "cols": [
                //       {
                //         "id": "A",
                //         "label": "",
                //         "type": "string"
                //       },
                //       ... ]
                //     "rows": [
                //       {
                //         "c": [
                //         {
                //           "v": "Building Styles Master IID"
                //         },
                //         ... ]
                //       }
                //     }
                // ]
                //
                // We only care about a subset of the cell values in each the row.

                if (document.RootElement.TryGetProperty("table"u8, out JsonElement table))
                {
                    if (table.TryGetProperty("rows"u8, out JsonElement rows))
                    {
                        if (rows.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in rows.EnumerateArray())
                            {
                                if (item.TryGetProperty("c"u8, out JsonElement rowCells))
                                {
                                    if (rowCells.ValueKind == JsonValueKind.Array)
                                    {
                                        ParseSpreadsheetRow(rowCells);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ParseSpreadsheetRow(JsonElement rowCells)
        {
            string? styleName = GetCellValueAsString(rowCells[2]);

            if (!string.IsNullOrEmpty(styleName))
            {
                uint styleId;
                string? iid = GetCellValueAsString(rowCells[0]);
                string? author = GetCellValueAsString(rowCells[4]);

                if (string.IsNullOrWhiteSpace(iid))
                {
                    if (nextBlockStyleId == 0)
                    {
                        throw new FormatException("Unable to determine the style id number.");
                    }

                    styleId = nextBlockStyleId;
                    nextBlockStyleId++;
                }
                else
                {
                    if (iid.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        int hyphenIndex = iid.IndexOf('-');

                        if (hyphenIndex != -1)
                        {
                            // The IID is in the form: 0x000020A0 - 0x000020AF
                            // Grab the first number in the range and cache it
                            // to allow future ids to be calculated.
                            ReadOnlySpan<char> chars = iid.AsSpan(0, hyphenIndex).Trim();
                            styleId = BuildingStyleIdParsing.ParseStyleNumber(chars);
                            nextBlockStyleId = styleId + 1;

                            // Cache the author field when reading the first cell in the
                            // style block because the other cells may leave this blank.
                            blockAuthor = !string.IsNullOrWhiteSpace(author) ? author : string.Empty;
                        }
                        else
                        {
                            // The IID is in the form 0x00002003
                            styleId = BuildingStyleIdParsing.ParseStyleNumber(iid);
                            nextBlockStyleId = styleId + 1;
                            blockAuthor = string.Empty;
                        }
                    }
                    else
                    {
                        // Skip any unknown Style ID field values.
                        return;
                    }
                }

                if (string.IsNullOrEmpty(author) && !string.IsNullOrEmpty(blockAuthor))
                {
                    author = blockAuthor;
                }

                string? description = GetCellValueAsString(rowCells[6]);

                buildingStyles.TryAdd(styleId, new BuildingStyleInfo(styleName, author, description));
            }
        }
    }
}
