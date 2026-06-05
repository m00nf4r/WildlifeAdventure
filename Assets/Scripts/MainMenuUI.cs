using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    public class MainMenuUI : MonoBehaviour
    {
        Canvas canvas;
        Text bestText;
        Text welcomeText;
        Button leaderboardButton;
        Button logoutButton;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("MenuCanvas", 50);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            // Background
            UIFactory.Panel2(root, UIFactory.Hex("E8F3E0"),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");
            UIFactory.Panel2(root, UIFactory.Hex("C8E6C9"),
                new Vector2(0, 0), new Vector2(1, 0.32f), Vector2.zero, Vector2.zero, "BgBottom");

            // Hornbill mascot
            UIFactory.SpriteImage(root, "hornbill", 220, 220, -380, 40);

            // Title
            UIFactory.LabelAt(root, "Wildlife Adventure", 64, UIFactory.GreenDark,
                900, 90, 60, 210, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.LabelAt(root, "Explore the Belum-Temenggor rainforest as Wira the Hornbill",
                24, UIFactory.Ink, 900, 40, 60, 150);

            welcomeText = UIFactory.LabelAt(root, "", 22, UIFactory.Green,
                900, 30, 60, 112, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Buttons
            UIFactory.MakeButton(root, "Start New Game", UIFactory.Green, Color.white,
                360, 60, 60, 50, () => GameManager.Instance.StartGame(false));

            if (SaveSystem.HasSave())
                UIFactory.MakeButton(root, "Continue", UIFactory.GreenLight, UIFactory.GreenDark,
                    360, 50, 60, -16, () => GameManager.Instance.StartGame(true));

            leaderboardButton = UIFactory.MakeButton(root, "Leaderboard", UIFactory.Blue, Color.white,
                360, 50, 60, -78, () => GameManager.Instance.OpenLeaderboard(), 22);

            UIFactory.MakeButton(root, "Reset Local Progress", UIFactory.Hex("B0BEC5"), UIFactory.Ink,
                360, 44, 60, -138, () => { SaveSystem.ClearSave(); RefreshBest(); }, 18);

            logoutButton = UIFactory.MakeButton(root, "Log Out", UIFactory.Hex("EF9A9A"), UIFactory.Ink,
                170, 44, 475, 285, () => GameManager.Instance.Logout(), 18);

            bestText = UIFactory.LabelAt(root, "", 22, UIFactory.GreenDark,
                900, 36, 60, -196, TextAnchor.MiddleCenter, FontStyle.Bold);

            // How to play
            UIFactory.LabelAt(root,
                "Move: Arrow Keys / WASD   •   Scan or Clean: E   •   Field Journal: J",
                20, UIFactory.Ink, 1100, 30, 0, -300);

            UIFactory.LabelAt(root, "An educational game for biodiversity awareness",
                16, new Color(0.3f, 0.4f, 0.3f), 900, 24, 0, -326);

            UIFactory.LabelAt(root, "Illustrated by Deeja & Myn",
                18, UIFactory.GreenDark, 900, 26, 0, -350, TextAnchor.MiddleCenter, FontStyle.Bold);

            RefreshBest();
            Hide();
        }

        void RefreshBest()
        {
            int best = SaveSystem.LoadBestScore();
            bestText.text = SaveSystem.HasSave()
                ? "Best Score: " + best + "   |   Rank: " + SaveSystem.RankForScore(best)
                : "";
        }

        public void Show()
        {
            RefreshBest();
            var be = Backend.Instance;
            bool signedIn = be != null && be.IsSignedIn;

            welcomeText.text = signedIn && !string.IsNullOrEmpty(be.DisplayName)
                ? "Welcome back, " + be.DisplayName + "!"
                : (be != null && be.IsConfigured ? "" : "Playing offline");

            if (leaderboardButton != null)
                leaderboardButton.gameObject.SetActive(be != null && be.IsConfigured);
            if (logoutButton != null)
                logoutButton.gameObject.SetActive(signedIn);

            canvas.gameObject.SetActive(true);
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
