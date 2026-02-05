// Copyright (c) 2025, 2026 Nicholas Hayes
// SPDX-License-Identifier: MIT

namespace BuildingStyleToolsCommon
{
    public interface IBuildingStyleManager
    {
        Dictionary<uint, BuildingStyleInfo> GetBuildingStyles();

        void SetBuildingStyles(Dictionary<uint, BuildingStyleInfo> value);
    }
}
