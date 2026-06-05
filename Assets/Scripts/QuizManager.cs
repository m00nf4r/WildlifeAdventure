using System;
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

        readonly char[] letters = { 'A', 'B', 'C', 'D' };

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("QuizCanvas", 30);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Panel2(root, UIFactory.Hex("E8F3E0"),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

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
            canvas.gameObject.SetActive(true);
            ShowQuestion();
        }

        void ShowQuestion()
        {
            answered = false;
            feedbackPanel.SetActive(false);
            var q = WildlifeDatabase.Quiz[index];
            questionText.text = q.question;
            progressText.text = "Question " + (index + 1) + " of " + WildlifeDatabase.Quiz.Count;
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
            var q = WildlifeDatabase.Quiz[index];

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
            nextLabel.text = (index + 1 >= WildlifeDatabase.Quiz.Count) ? "See Results" : "Next";
            feedbackPanel.SetActive(true);
        }

        void Next()
        {
            index++;
            if (index >= WildlifeDatabase.Quiz.Count)
            {
                canvas.gameObject.SetActive(false);
                onComplete?.Invoke(correctCount);
            }
            else ShowQuestion();
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
