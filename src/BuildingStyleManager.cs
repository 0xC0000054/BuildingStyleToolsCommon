// Copyright (c) 2025, 2026 Nicholas Hayes
// SPDX-License-Identifier: MIT

namespace BuildingStyleToolsCommon
{
    public sealed class BuildingStyleManager : IBuildingStyleManager
    {
        private readonly IBuildingStyleManagerSerialization serializer;
        private readonly Lock sync;

        private Dictionary<uint, BuildingStyleInfo>? buildingStyles;

        public BuildingStyleManager(IBuildingStyleManagerSerialization serializer)
        {
            ArgumentNullException.ThrowIfNull(serializer);

            this.serializer = serializer;
            buildingStyles = null;
            sync = new Lock();
            Dirty = false;
        }

        public bool Dirty { get; private set; }

        public void Save()
        {
            if (buildingStyles is not null && Dirty)
            {
                lock (sync)
                {
                    serializer.Save(buildingStyles);
                    Dirty = false;
                }
            }
        }

        public Dictionary<uint, BuildingStyleInfo> GetBuildingStyles()
        {
            Dictionary<uint, BuildingStyleInfo> items;

            lock (sync)
            {
                if (buildingStyles is null)
                {
                    try
                    {
                        buildingStyles = serializer.Load();
                        Dirty = false;
                    }
                    catch (FileNotFoundException)
                    {
                        buildingStyles = BuildingStylesOnlineSpreadsheet.Load();
                        Dirty = true;
                    }
                }

                items = new Dictionary<uint, BuildingStyleInfo>(buildingStyles);
            }

            return items;
        }

        public void SetBuildingStyles(Dictionary<uint, BuildingStyleInfo> value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            lock (sync)
            {
                buildingStyles = new Dictionary<uint, BuildingStyleInfo>(value);
                Dirty = true;
            }
        }
    }
}
