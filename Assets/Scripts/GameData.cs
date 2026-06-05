using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>High-level state machine for the whole game flow.</summary>
    public enum GameState
    {
        Auth,
        MainMenu,
        Intro,
        Exploring,
        FactCard,
        FieldJournal,
        Quiz,
        Reward,
        Leaderboard
    }

    /// <summary>
    /// One record in the "Wildlife Facts" table described in the report.
    /// Plain serializable class so it needs no asset wiring.
    /// </summary>
    [System.Serializable]
    public class WildlifeData
    {
        public string id;               // unique key, e.g. "malayan_tapir"
        public string commonName;       // "Malayan Tapir"
        public string scientificName;   // "Tapirus indicus"
        public string conservationStatus; // "Endangered"
        public string fact;             // child-friendly fact text
        public string spriteName;       // file in Resources/Sprites (no extension)
        public int pointsAwarded = 250; // Conservation Points for discovery

        public WildlifeData(string id, string commonName, string scientificName,
                            string status, string fact, string spriteName)
        {
            this.id = id;
            this.commonName = commonName;
            this.scientificName = scientificName;
            this.conservationStatus = status;
            this.fact = fact;
            this.spriteName = spriteName;
        }
    }

    /// <summary>One multiple-choice quiz question (the Quiz Manager bank).</summary>
    [System.Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] options;   // exactly 4
        public int correctIndex;   // 0..3
        public string explanation;

        public QuizQuestion(string question, string[] options, int correctIndex, string explanation)
        {
            this.question = question;
            this.options = options;
            this.correctIndex = correctIndex;
            this.explanation = explanation;
        }
    }

    /// <summary>
    /// Anything in the world the player (Wira) can interact with:
    /// wildlife to discover, pollution to clean, or the quiz totem.
    /// </summary>
    public interface IInteractable
    {
        Vector3 WorldPosition { get; }
        string Prompt { get; }       // e.g. "Press E to scan"
        bool Available { get; }      // can it be interacted with right now?
        void Interact();
    }
}
