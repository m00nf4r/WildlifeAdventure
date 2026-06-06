using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// The Quiz Manager. Presents the level quiz one question at a time with
    /// four options, immediate feedback and a short explanation, then reports
    /// how many were answered correctly so points can be awarded.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        Canvas canvas;
        Text questionText, progressText, scoreText, feedbackText;
        Button[] optionButtons = new Button[4];
        Image[] optionBgs = new Image[4];
        Text[] optionLabels = new Text[4];
        GameObject feedbackPanel;
        Button nextButton;
        Text nextLabel;

        int index;
        int correctCount;
        bool answered;
        Action<int> onComplete;

        // A freshly shuffled copy of the questions for the current play-through.
        readonly List<QuizQuestion> quiz = new List<QuizQuestion>();

        readonly char[] letters = { 'A', 'B', 'C', 'D' };

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("QuizCanvas", 30);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            // Soft colour fallback (shows if the background image is missing).
            UIFactory.Panel2(root, UIFactory.Hex("E8F3E0"),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

            // Nature scene background, stretched to fill the screen.
            var sceneGo = new GameObject("BgScene");
            sceneGo.transform.SetParent(root, false);
            var sceneImg = sceneGo.AddComponent<Image>();
            sceneImg.sprite = Resources.Load<Sprite>("Sprites/quiz_bg");
            sceneImg.preserveAspect = false;
            sceneImg.raycastTarget = false;
            var sceneRt = sceneGo.GetComponent<RectTransform>();
            sceneRt.anchorMin = Vector2.zero;
            sceneRt.anchorMax = Vector2.one;
            sceneRt.offsetMin = Vector2.zero;
            sceneRt.offsetMax = Vector2.zero;

            // Header
            UIFactory.Panel2(root, UIFactory.Green,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), Vector2.zero, "Header");
            UIFactory.LabelAt(root, "Level Quiz", 28, Color.white, 400, 40, -480, 325,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            progressText = UIFactory.LabelAt(root, "", 22, Color.white, 300, 40, 0, 325);
            scoreText = UIFactory.LabelAt(root, "", 22, UIFactory.Amber, 250, 40, 480, 325,
                TextAnchor.MiddleRight, FontStyle.Bold);

            // Question card
            var qcard = UIFactory.PanelCentered(root, Color.white, 900, 150, 0, 170, "QCard");
            questionText = UIFactory.Label(qcard.transform, "", 28, UIFactory.Ink,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            // Option buttons (2x2)
            float[] ox = { -235, 235, -235, 235 };
            float[] oy = { 30, 30, -55, -55 };
            for (int i = 0; i < 4; i++)
            {
                int captured = i;
                var btn = UIFactory.MakeButton(root, "", Color.white, UIFactory.Ink,
                    440, 70, ox[i], oy[i], () => OnAnswer(captured), 22);
                optionButtons[i] = btn;
                optionBgs[i] = btn.GetComponent<Image>();
                optionLabels[i] = btn.GetComponentInChildren<Text>();
                optionLabels[i].alignment = TextAnchor.MiddleLeft;
            }

            // Feedback panel
            feedbackPanel = UIFactory.PanelCentered(root, UIFactory.Hex("FFF8E1"), 900, 150, 0, -210, "Feedback").gameObject;
            feedbackText = UIFactory.LabelAt(feedbackPanel.transform, "", 22, UIFactory.Ink,
                760, 90, 0, 15, TextAnchor.MiddleCenter);
            nextButton = UIFactory.MakeButton(feedbackPanel.transform, "Next", UIFactory.Green, Color.white,
                200, 48, 320, -45, Next, 22);
            nextLabel = nextButton.GetComponentInChildren<Text>();

            Hide();
        }

        public void Begin(Action<int> complete)
        {
            onComplete = complete;
            index = 0;
            correctCount = 0;
            BuildShuffledQuiz();
            canvas.gameObject.SetActive(true);
            ShowQuestion();
        }

        // How many questions to ask per play-through. The full pool is shuffled
        // first, then the top N are taken, so each play shows a different set.
        const int QuestionsPerPlay = 5;

        /// <summary>
        /// Makes a fresh copy of the question bank, shuffles the question order,
        /// shuffles the answer options within each question, and keeps only the
        /// first QuestionsPerPlay of them — so every play-through is a different,
        /// non-repeating set. The source comes from Firebase when the player is
        /// online (loaded into WildlifeDatabase.Quiz), or the built-in questions
        /// when offline.
        /// </summary>
        void BuildShuffledQuiz()
        {
            quiz.Clear();
            var src = new List<QuizQuestion>(WildlifeDatabase.Quiz);

            // Shuffle question order (Fisher-Yates).
            for (int i = src.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = src[i]; src[i] = src[j]; src[j] = tmp;
            }

            // Keep only the first N (or all of them, if the pool is smaller).
            int take = Mathf.Min(QuestionsPerPlay, src.Count);
            for (int i = 0; i < take; i++) quiz.Add(ShuffleOptions(src[i]));
        }

        QuizQuestion ShuffleOptions(QuizQuestion q)
        {
            if (q.options == null || q.options.Length < 4) return q;

            int[] order = { 0, 1, 2, 3 };
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
            }

            var opts = new string[4];
            int newCorrect = 0;
            for (int i = 0; i < 4; i++)
            {
                opts[i] = q.options[order[i]];
                if (order[i] == q.correctIndex) newCorrect = i;
            }
            return new QuizQuestion(q.question, opts, newCorrect, q.explanation);
        }

        void ShowQuestion()
        {
            answered = false;
            feedbackPanel.SetActive(false);
            var q = quiz[index];
            questionText.text = q.question;
            progressText.text = "Question " + (index + 1) + " of " + quiz.Count;
            scoreText.text = "★ " + correctCount * 100;

            for (int i = 0; i < 4; i++)
            {
                optionLabels[i].text = "   " + letters[i] + ".  " + q.options[i];
                optionLabels[i].color = UIFactory.Ink;
                optionBgs[i].color = Color.white;
                optionButtons[i].interactable = true;
            }
        }

        void OnAnswer(int choice)
        {
            if (answered) return;
            answered = true;
            var q = quiz[index];

            for (int i = 0; i < 4; i++) optionButtons[i].interactable = false;

            optionBgs[q.correctIndex].color = UIFactory.GreenLight;
            optionLabels[q.correctIndex].color = UIFactory.GreenDark;

            bool right = choice == q.correctIndex;
            if (right)
            {
                correctCount++;
                feedbackText.text = "Correct!  " + q.explanation;
            }
            else
            {
                optionBgs[choice].color = UIFactory.Hex("EF9A9A");
                feedbackText.text = "Not quite. " + q.explanation;
            }

            scoreText.text = "★ " + correctCount * 100;
            nextLabel.text = (index + 1 >= quiz.Count) ? "See Results" : "Next";
            feedbackPanel.SetActive(true);
        }

        void Next()
        {
            index++;
            if (index >= quiz.Count)
            {
                canvas.gameObject.SetActive(false);
                onComplete?.Invoke(correctCount);
            }
            else ShowQuestion();
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
