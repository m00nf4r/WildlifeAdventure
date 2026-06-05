using System.Collections.Generic;

namespace WildlifeAdventure
{
    /// <summary>
    /// Central store for the game's educational content. The active lists
    /// (Species, Quiz) are replaced with cloud data by <see cref="Backend"/> when
    /// the player is signed in; otherwise they hold the built-in defaults below,
    /// which also serve as the seed source and the offline fallback.
    /// This is the code equivalent of the report's "Wildlife Facts" table plus
    /// the quiz bank, kept separate from gameplay logic so content can grow.
    /// </summary>
    public static class WildlifeDatabase
    {
        // Active content (may be swapped for cloud data at runtime).
        public static List<WildlifeData> Species = DefaultSpecies();
        public static List<QuizQuestion> Quiz = DefaultQuiz();

        public static WildlifeData GetById(string id)
        {
            foreach (var s in Species)
                if (s.id == id) return s;
            return null;
        }

        public static int TotalSpecies => Species.Count;

        // ---------- Built-in defaults (seed + offline fallback) ----------
        // Real Malaysian wildlife of the Belum-Temenggor rainforest.
        public static List<WildlifeData> DefaultSpecies()
        {
            return new List<WildlifeData>
            {
                new WildlifeData(
                    "malayan_tapir", "Malayan Tapir", "Tapirus indicus", "Endangered",
                    "The Malayan Tapir is the largest of all tapirs. Its black-and-white " +
                    "coat looks bold to us, but at night it works like camouflage, " +
                    "breaking up its shape so predators can't spot it. Baby tapirs are " +
                    "born brown with stripes and spots, like a walking watermelon!",
                    "Tapir"),

                new WildlifeData(
                    "malayan_tiger", "Malayan Tiger", "Panthera tigris jacksoni", "Critically Endangered",
                    "The Malayan Tiger is a national symbol of Malaysia, appearing on the " +
                    "country's coat of arms. Fewer than 150 are believed to remain in the " +
                    "wild. Each tiger's stripes are unique, just like a human fingerprint.",
                    "Tiger"),

                new WildlifeData(
                    "sumatran_rhino", "Sumatran Rhinoceros", "Dicerorhinus sumatrensis", "Critically Endangered",
                    "The Sumatran Rhino is the smallest rhino on Earth and the only Asian " +
                    "rhino with two horns. It is also the hairiest, with a reddish-brown " +
                    "coat. Sadly, it is one of the rarest large mammals in the world.",
                    "rhinoceros"),

                new WildlifeData(
                    "asian_elephant", "Asian Elephant", "Elephas maximus", "Endangered",
                    "Asian Elephants are gardeners of the forest. They spread seeds across " +
                    "huge distances in their dung, helping new trees grow. They are smaller " +
                    "than African elephants and have one finger-like tip on their trunk.",
                    "elephant"),
            };
        }

        public static List<QuizQuestion> DefaultQuiz()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion(
                    "Which of these is a flagship species for Malaysian rainforest conservation?",
                    new[] { "Koala", "Giant Panda", "Malayan Tiger", "African Lion" }, 2,
                    "The Malayan Tiger is a flagship species and appears on Malaysia's coat of arms."),

                new QuizQuestion(
                    "What does the Malayan Tapir's black-and-white coat help it do at night?",
                    new[] { "Glow in the dark", "Stay camouflaged", "Swim faster", "Attract a mate" }, 1,
                    "At night the bold pattern breaks up its body shape, hiding it from predators."),

                new QuizQuestion(
                    "How is the Sumatran Rhino different from other rhinos?",
                    new[] { "It has no horn", "It is the largest rhino", "It has two horns and is hairy", "It can fly" }, 2,
                    "The Sumatran Rhino is the smallest, hairiest rhino and the only Asian rhino with two horns."),

                new QuizQuestion(
                    "Why are Asian Elephants called gardeners of the forest?",
                    new[] { "They plant trees with their trunks", "They spread seeds in their dung",
                            "They water the plants", "They cut down dead trees" }, 1,
                    "Elephants spread seeds over long distances in their dung, helping new trees grow."),

                new QuizQuestion(
                    "What is the biggest threat to wildlife shown in this game?",
                    new[] { "Too much rain", "Pollution and habitat loss", "Bright sunlight", "Cold weather" }, 1,
                    "Pollution and the loss of forest habitat are major threats to Malaysian wildlife."),
            };
        }
    }
}
