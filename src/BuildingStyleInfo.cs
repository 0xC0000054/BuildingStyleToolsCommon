// Copyright (c) 2025, 2026 Nicholas Hayes
// SPDX-License-Identifier: MIT

namespace BuildingStyleToolsCommon
{
    public sealed class BuildingStyleInfo
    {
        public BuildingStyleInfo(string? name, string? author, string? description)
        {
            Name = name ?? string.Empty;
            Author = author ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string Name { get; }

        public string Author { get; }

        public string Description { get; }

        public override string ToString() => Name;
    }
}
