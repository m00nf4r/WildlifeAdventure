using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// High-level gateway between the game and Firebase. Hosts all network
    /// coroutines and exposes simple callbacks. Everything degrades gracefully:
    /// if Firebase isn't configured or a call fails, the game keeps working with
    /// the built-in content and local (PlayerPrefs) save.
    /// </summary>
    public class Backend : MonoBehaviour
    {
        public static Backend Instance { get; private set; }

        public bool IsConfigured => FirebaseConfig.IsConfigured;
        public bool IsSignedIn => FirebaseAuth.IsSignedIn;
        public string DisplayName => FirebaseAuth.DisplayName;
        public string Uid => FirebaseAuth.Uid;

        void Awake() { Instance = this; }

        public static void Configure(string apiKey, string projectId)
        {
            FirebaseConfig.ApiKey = apiKey == null ? "" : apiKey.Trim();
            FirebaseConfig.ProjectId = projectId == null ? "" : projectId.Trim();
        }

        // ---------------- Auth ----------------

        public void Register(string email, string password, string displayName, Action<bool, string> done)
        {
            StartCoroutine(FirebaseAuth.SignUp(email, password, displayName, (ok, err) =>
            {
                if (!ok) { done(false, err); return; }
                // Create the user's profile document right away.
                var profile = new UserProfile
                {
                    uid = FirebaseAuth.Uid, displayName = displayName, email = email,
                    bestScore = 0, rank = SaveSystem.RankForScore(0),
                    discoveredCount = 0, totalPlays = 0, timeBestSeconds = 0, discoveredCsv = ""
                };
                SaveUser(profile, _ => done(true, null));
            }));
        }

        public void SignIn(string email, string password, Action<bool, string> done)
        {
            StartCoroutine(FirebaseAuth.SignIn(email, password, done));
        }

        public void TrySilentLogin(Action<bool> done)
        {
            StartCoroutine(FirebaseAuth.TrySilentLogin(done));
        }

        public void Logout() { FirebaseAuth.Logout(); }

        // ---------------- Content (Wildlife Facts + Quiz) ----------------

        public void LoadContent(Action done)
        {
            StartCoroutine(LoadContentRoutine(done));
        }

        IEnumerator LoadContentRoutine(Action done)
        {
            if (IsConfigured && IsSignedIn)
            {
                yield return FirebaseAuth.EnsureFresh();

                yield return FirestoreClient.ListCollection("wildlifeFacts", (ok, docs) =>
                {
                    if (ok && docs != null && docs.Count > 0)
                    {
                        var ordered = new List<(int order, WildlifeData data)>();
                        foreach (var f in docs)
                        {
                            var d = new WildlifeData(
                                FirestoreClient.ToStr(f, "id"),
                                FirestoreClient.ToStr(f, "commonName"),
                                FirestoreClient.ToStr(f, "scientificName"),
                                FirestoreClient.ToStr(f, "conservationStatus"),
                                FirestoreClient.ToStr(f, "fact"),
                                FirestoreClient.ToStr(f, "spriteName"));
                            d.pointsAwarded = FirestoreClient.ToInt(f, "pointsAwarded", 250);
                            ordered.Add((FirestoreClient.ToInt(f, "order"), d));
                        }
                        ordered.Sort((a, b) => a.order.CompareTo(b.order));
                        var list = new List<WildlifeData>();
                        foreach (var o in ordered) list.Add(o.data);
                        WildlifeDatabase.Species = list;
                    }
                });

                yield return FirestoreClient.ListCollection("quizQuestions", (ok, docs) =>
                {
                    if (ok && docs != null && docs.Count > 0)
                    {
                        var ordered = new List<(int order, QuizQuestion q)>();
                        foreach (var f in docs)
                        {
                            var opts = new[]
                            {
                                FirestoreClient.ToStr(f, "optionA"),
                                FirestoreClient.ToStr(f, "optionB"),
                                FirestoreClient.ToStr(f, "optionC"),
                                FirestoreClient.ToStr(f, "optionD")
                            };
                            var q = new QuizQuestion(
                                FirestoreClient.ToStr(f, "question"), opts,
                                FirestoreClient.ToInt(f, "correctIndex"),
                                FirestoreClient.ToStr(f, "explanation"));
                            ordered.Add((FirestoreClient.ToInt(f, "order"), q));
                        }
                        ordered.Sort((a, b) => a.order.CompareTo(b.order));
                        var list = new List<QuizQuestion>();
                        foreach (var o in ordered) list.Add(o.q);
                        WildlifeDatabase.Quiz = list;
                    }
                });
            }

            if (done != null) done();
        }

        // ---------------- User profile ----------------

        public void LoadUser(Action<UserProfile> done)
        {
            if (!IsConfigured || !IsSignedIn) { done(null); return; }
            StartCoroutine(LoadUserRoutine(done));
        }

        IEnumerator LoadUserRoutine(Action<UserProfile> done)
        {
            yield return FirebaseAuth.EnsureFresh();
            yield return FirestoreClient.GetDocument("users/" + FirebaseAuth.Uid, (ok, f) =>
            {
                if (!ok || f == null || f.Count == 0) { done(null); return; }
                var p = new UserProfile
                {
                    uid = FirebaseAuth.Uid,
                    displayName = FirestoreClient.ToStr(f, "displayName"),
                    email = FirestoreClient.ToStr(f, "email"),
                    bestScore = FirestoreClient.ToInt(f, "bestScore"),
                    rank = FirestoreClient.ToStr(f, "rank"),
                    discoveredCount = FirestoreClient.ToInt(f, "discoveredCount"),
                    totalPlays = FirestoreClient.ToInt(f, "totalPlays"),
                    timeBestSeconds = FirestoreClient.ToInt(f, "timeBestSeconds"),
                    discoveredCsv = FirestoreClient.ToStr(f, "discoveredCsv")
                };
                if (!string.IsNullOrEmpty(p.displayName)) FirebaseAuth.DisplayName = p.displayName;
                done(p);
            });
        }

        public void SaveUser(UserProfile p, Action<bool> done)
        {
            if (!IsConfigured || !IsSignedIn) { if (done != null) done(false); return; }
            StartCoroutine(SaveUserRoutine(p, done));
        }

        IEnumerator SaveUserRoutine(UserProfile p, Action<bool> done)
        {
            yield return FirebaseAuth.EnsureFresh();
            var fields = new Dictionary<string, object>
            {
                { "uid", p.uid },
                { "displayName", p.displayName },
                { "email", p.email },
                { "bestScore", p.bestScore },
                { "rank", p.rank },
                { "discoveredCount", p.discoveredCount },
                { "totalPlays", p.totalPlays },
                { "timeBestSeconds", p.timeBestSeconds },
                { "discoveredCsv", p.discoveredCsv },
                { "updatedAt", DateTime.UtcNow.ToString("o") }
            };
            yield return FirestoreClient.PatchDocument("users/" + p.uid, fields,
                ok => { if (done != null) done(ok); });
        }

        // ---------------- Scores / leaderboard ----------------

        public void SubmitScore(ScoreEntry e, Action<bool> done)
        {
            if (!IsConfigured || !IsSignedIn) { if (done != null) done(false); return; }
            StartCoroutine(SubmitScoreRoutine(e, done));
        }

        IEnumerator SubmitScoreRoutine(ScoreEntry e, Action<bool> done)
        {
            yield return FirebaseAuth.EnsureFresh();
            var fields = new Dictionary<string, object>
            {
                { "uid", e.uid },
                { "displayName", e.displayName },
                { "rank", e.rank },
                { "score", e.score },
                { "timeSeconds", e.timeSeconds },
                { "createdAt", DateTime.UtcNow.ToString("o") }
            };
            yield return FirestoreClient.CreateDocument("scores", fields,
                ok => { if (done != null) done(ok); });
        }

        public void LoadLeaderboard(Action<List<ScoreEntry>> done)
        {
            if (!IsConfigured) { done(new List<ScoreEntry>()); return; }
            StartCoroutine(LoadLeaderboardRoutine(done));
        }

        IEnumerator LoadLeaderboardRoutine(Action<List<ScoreEntry>> done)
        {
            yield return FirebaseAuth.EnsureFresh();
            yield return FirestoreClient.ListCollection("scores", (ok, docs) =>
            {
                var list = new List<ScoreEntry>();
                if (ok && docs != null)
                {
                    foreach (var f in docs)
                        list.Add(new ScoreEntry
                        {
                            uid = FirestoreClient.ToStr(f, "uid"),
                            displayName = FirestoreClient.ToStr(f, "displayName"),
                            rank = FirestoreClient.ToStr(f, "rank"),
                            score = FirestoreClient.ToInt(f, "score"),
                            timeSeconds = FirestoreClient.ToInt(f, "timeSeconds")
                        });
                    list.Sort((a, b) => b.score.CompareTo(a.score));
                    if (list.Count > 10) list = list.GetRange(0, 10);
                }
                done(list);
            });
        }

        // ---------------- One-time content seeding ----------------

        /// <summary>Uploads the built-in wildlife facts and quiz to Firestore.</summary>
        public void SeedContent(Action<bool> done)
        {
            if (!IsConfigured || !IsSignedIn) { done(false); return; }
            StartCoroutine(SeedRoutine(done));
        }

        IEnumerator SeedRoutine(Action<bool> done)
        {
            yield return FirebaseAuth.EnsureFresh();
            bool allOk = true;

            var defaultSpecies = WildlifeDatabase.DefaultSpecies();
            for (int i = 0; i < defaultSpecies.Count; i++)
            {
                var s = defaultSpecies[i];
                var fields = new Dictionary<string, object>
                {
                    { "id", s.id }, { "commonName", s.commonName },
                    { "scientificName", s.scientificName },
                    { "conservationStatus", s.conservationStatus },
                    { "fact", s.fact }, { "spriteName", s.spriteName },
                    { "pointsAwarded", s.pointsAwarded }, { "order", i }
                };
                yield return FirestoreClient.PatchDocument("wildlifeFacts/" + s.id, fields,
                    ok => { if (!ok) allOk = false; });
            }

            var defaultQuiz = WildlifeDatabase.DefaultQuiz();
            for (int i = 0; i < defaultQuiz.Count; i++)
            {
                var q = defaultQuiz[i];
                var fields = new Dictionary<string, object>
                {
                    { "question", q.question },
                    { "optionA", q.options[0] }, { "optionB", q.options[1] },
                    { "optionC", q.options[2] }, { "optionD", q.options[3] },
                    { "correctIndex", q.correctIndex },
                    { "explanation", q.explanation }, { "order", i }
                };
                yield return FirestoreClient.PatchDocument("quizQuestions/q" + (i + 1), fields,
                    ok => { if (!ok) allOk = false; });
            }

            done(allOk);
        }
    }
}
