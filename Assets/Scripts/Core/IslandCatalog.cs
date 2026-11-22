using UnityEngine;

namespace Lumo.Core
{
    /// <summary>Metadata for each of the 13 themed Wonder Islands mapped onto levels 1..13.</summary>
    public struct IslandInfo
    {
        public int LevelId;
        public string IslandName;
        public string Subject;
        public string Subtitle;
        public string AccentHex;
        public string IconSprite;
    }

    /// <summary>Catalog of all 13 islands, pairing each level with its island theme, sprite, and accent.</summary>
    public static class IslandCatalog
    {
        public static readonly IslandInfo[] All =
        {
            new IslandInfo { LevelId = 1,  IslandName = "Math Mountain",   Subject = "Maths",              Subtitle = "Fruit Village (Addition)",  AccentHex = "#FF9F1C", IconSprite = "obj_apple" },
            new IslandInfo { LevelId = 2,  IslandName = "Word Forest",     Subject = "Reading",            Subtitle = "Animal & Color Garden",     AccentHex = "#34A853", IconSprite = "obj_cat" },
            new IslandInfo { LevelId = 3,  IslandName = "Memory Garden",   Subject = "Memory",             Subtitle = "The Matching Bridge",       AccentHex = "#14B8A6", IconSprite = "obj_star" },
            new IslandInfo { LevelId = 4,  IslandName = "Learning Island", Subject = "First Logic",        Subtitle = "Subtraction Castle",        AccentHex = "#1E88E5", IconSprite = "obj_key" },
            new IslandInfo { LevelId = 5,  IslandName = "Art Studio",      Subject = "Letters & Craft",    Subtitle = "Magic Spelling Library",    AccentHex = "#FF6FA5", IconSprite = "obj_sun" },
            new IslandInfo { LevelId = 6,  IslandName = "Science Lab",     Subject = "Science",            Subtitle = "Magic Potion Lab",          AccentHex = "#7E5BEF", IconSprite = "obj_cauldron" },
            new IslandInfo { LevelId = 7,  IslandName = "Sky Kingdom",     Subject = "Number Skies",       Subtitle = "Number Mountain",           AccentHex = "#38BDF8", IconSprite = "obj_bird" },
            new IslandInfo { LevelId = 8,  IslandName = "Ocean Reef",      Subject = "Sea & Words",        Subtitle = "Word Explorer",             AccentHex = "#0EA5E9", IconSprite = "obj_fish" },
            new IslandInfo { LevelId = 9,  IslandName = "Puzzle Park",     Subject = "Puzzles",            Subtitle = "Treasure Map",              AccentHex = "#10B981", IconSprite = "obj_chest" },
            new IslandInfo { LevelId = 10, IslandName = "Space Station",   Subject = "Space Logic",        Subtitle = "Royal Math Mission",        AccentHex = "#6366F1", IconSprite = "obj_gem" },
            new IslandInfo { LevelId = 11, IslandName = "Kindness Corner", Subject = "Stories & Kindness", Subtitle = "Sentence Kingdom",          AccentHex = "#F59E0B", IconSprite = "obj_house" },
            new IslandInfo { LevelId = 12, IslandName = "Dino Valley",     Subject = "Discovery",          Subtitle = "Royal Workshop",            AccentHex = "#8D6E63", IconSprite = "obj_tree" },
            new IslandInfo { LevelId = 13, IslandName = "Music Room",      Subject = "Crystal Finale",     Subtitle = "Crystal of Knowledge",      AccentHex = "#FFC53D", IconSprite = "obj_crystal" },
        };

        /// <summary>Returns island info for levelId, clamped safely to 1..13.</summary>
        public static IslandInfo Get(int levelId)
        {
            int clamped = Mathf.Clamp(levelId, 1, All.Length);
            return All[clamped - 1];
        }
    }
}
