// Copyright (c) 2026 Nicholas Hayes
// SPDX-License-Identifier: MIT

namespace BuildingStyleToolsCommon
{
    public interface IBuildingStyleManagerSerialization
    {
        public Dictionary<uint, BuildingStyleInfo> Load();

        public void Save(Dictionary<uint, BuildingStyleInfo> buildingStyles);
    }
}
