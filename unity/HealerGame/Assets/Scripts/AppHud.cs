using System.Linq;
using Healer.Combat;
using Healer.Combat.Progress;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Écrans hors combat : menu principal et choix du niveau (IMGUI, comme le HUD de combat). Aucune règle de
    /// jeu : lecture du profil et de la progression, et gestes vers GameFlow. Chaque zone cliquable passe par
    /// InputGate, donc un écran ne capte jamais les clics d'un autre.
    /// </summary>
    public sealed class AppHud : MonoBehaviour
    {
        private GameFlow _flow = null!;

        public void Init(GameFlow flow) => _flow = flow;

        private static Rect R(Healer.Ui.Rect r) => new Rect((float)r.X, (float)r.Y, (float)r.W, (float)r.H);

        private bool Hit(Rect r, UiAction action) =>
            InputGate.Allows(_flow.Screen, ScreenState.Playing, action) && PointerInput.Tapped(r);

        private void OnGUI()
        {
            if (_flow == null || _flow.Screen == AppScreen.Battle) return;
            ScreenMap.Refresh();
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(ScreenMap.OffsetX, ScreenMap.OffsetY, 0), Quaternion.identity, new Vector3(ScreenMap.Scale, ScreenMap.Scale, 1));
            UiKit.Fill(new Rect(-2000, -2000, 5000, 5000), new Color(0.03f, 0.04f, 0.09f, 0.72f), 0);
            if (_flow.Screen == AppScreen.MainMenu) DrawMenu();
            else if (_flow.Screen == AppScreen.Workshop) DrawWorkshop();
            else if (_flow.Screen == AppScreen.Settings) DrawSettings();
            else DrawLevels();
            if (_flow.Screen != AppScreen.Workshop && _flow.Screen != AppScreen.Settings) DrawFooter();
            GUI.matrix = previous;
        }

        // ---- Menu principal ----------------------------------------------------------------------

        private void DrawMenu()
        {
            float w = (float)Layout.GameW;
            UiKit.Label(new Rect(0, 100, w, 90), "Healer Game", 72, Color.white, TextAnchor.MiddleCenter, true);
            UiKit.Label(new Rect(0, 192, w, 32), "Gardez votre équipe en vie.", Layout.Font.Title, UiKit.Muted, TextAnchor.MiddleCenter);

            var play = R(Layout.MenuPlay);
            UiKit.Button(play, "Jouer", 30, Palette.Hex("245C43"), Palette.Hex("5FB98D"));
            if (Hit(play, UiAction.MenuPlay)) _flow.OpenLevels();

            var workshop = R(Layout.MenuWorkshop);
            UiKit.Button(workshop, "Atelier", Layout.Font.Title, Palette.Hex("22273B"), UiKit.Gold);
            if (Hit(workshop, UiAction.MenuWorkshop)) _flow.OpenWorkshop();

            var settings = R(Layout.MenuSettings);
            UiKit.Button(settings, "Réglages", Layout.Font.Title, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(settings, UiAction.MenuSettings)) _flow.OpenSettings();

            var sound = R(Layout.MenuSound);
            UiKit.Button(sound, _flow.Profile.Settings.Muted ? "Son : coupé" : "Son : activé", Layout.Font.Title, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(sound, UiAction.MenuToggleSound)) _flow.ToggleMute();

            if (CanQuit)
            {
                var quit = R(Layout.MenuQuit);
                UiKit.Button(quit, "Quitter", Layout.Font.Title, Palette.Hex("22273B"), Palette.Hex("6F7698"));
                if (Hit(quit, UiAction.MenuQuit)) _flow.Quit();
            }
            if (Keyboard.current != null)
                UiKit.Label(new Rect(0, 570, w, 24), "Entrée : jouer · M : son", Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);
        }

        /// <summary>Un bouton « Quitter » n'a de sens que sur PC (les téléphones ferment l'application autrement).</summary>
        private static bool CanQuit => Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer;

        // ---- Réglages -----------------------------------------------------------------------------

        private void DrawSettings()
        {
            float w = (float)Layout.GameW;
            var s = _flow.Profile.Settings;
            UiKit.Label(new Rect(0, 60, w, 60), "Réglages", 44, Color.white, TextAnchor.MiddleCenter, true);
            var back = R(Layout.BackButton);
            UiKit.Button(back, "← Menu", Layout.Font.Body, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(back, UiAction.BackToMenu)) _flow.BackToMenu();

            string[] labels = { "Musique", "Effets sonores", "Secousse de l'écran" };
            for (int row = 0; row < Layout.SettingsRows; row++)
            {
                var r = R(Layout.SettingsRow(row));
                UiKit.Fill(r, new Color(UiKit.Panel.r, UiKit.Panel.g, UiKit.Panel.b, 0.94f), 10);
                UiKit.Outline(r, UiKit.PanelStroke, 1.5f, 10);
                UiKit.Label(new Rect(r.x + 20, r.y, 300, r.height), labels[row], Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);

                var minus = R(Layout.SettingsMinus(row));
                var plus = R(Layout.SettingsPlus(row));
                var bar = R(Layout.SettingsBar(row));
                bool toggle = row == 2;
                UiKit.Button(minus, toggle ? "Non" : "−", toggle ? Layout.Font.Body : Layout.Font.Banner, Palette.Hex("22273B"), Palette.Hex("6F7698"));
                UiKit.Button(plus, toggle ? "Oui" : "+", toggle ? Layout.Font.Body : Layout.Font.Banner, Palette.Hex("22273B"), Palette.Hex("6F7698"));
                UiKit.Fill(bar, new Color(0, 0, 0, 0.55f), 6);
                if (toggle)
                {
                    UiKit.Label(bar, s.ScreenShake ? "Activée" : "Coupée", Layout.Font.Small, s.ScreenShake ? Palette.Heal : UiKit.Muted, TextAnchor.MiddleCenter, true);
                }
                else
                {
                    int value = row == 0 ? s.MusicVolume : s.SfxVolume;
                    if (value > 0) UiKit.Fill(new Rect(bar.x, bar.y, Mathf.Max(6f, bar.width * value / 100f), bar.height), Palette.Hex("4AA8FF"), 6);
                    UiKit.Label(bar, value + " %", Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
                }
                if (Hit(minus, UiAction.AdjustSetting)) _flow.AdjustSetting(row, -1);
                if (Hit(plus, UiAction.AdjustSetting)) _flow.AdjustSetting(row, +1);
            }
            UiKit.Label(new Rect(0, 470, w, 26), "Le bouton « Son » du menu (ou la touche M) coupe tout sans perdre ces réglages.", Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);
        }

        // ---- Atelier ------------------------------------------------------------------------------

        private static string StatName(string stat)
        {
            switch (stat)
            {
                case "maxHp": return "PV";
                case "atk": return "attaque";
                case "def": return "défense";
                case "maxMana": return "mana max";
                case "manaRegen": return "régénération de mana";
                default: return stat;
            }
        }

        private static string FieldName(string field)
        {
            switch (field)
            {
                case "healAmount": return "de soin";
                case "shieldAmount": return "de bouclier";
                case "manaCost": return "de coût en mana";
                case "cooldownMs": return "de recharge";
                default: return field;
            }
        }

        /// <summary>« +4 % attaque », « +5 % de soin (Soin) » : décrit un effet à partir des données.</summary>
        private string Describe(Healer.Combat.Progress.UpgradeEffect e, int times = 1)
        {
            int pct = e.Pct * times;
            string sign = pct > 0 ? "+" : "";
            if (e.Stat != null) return $"{sign}{pct} % {StatName(e.Stat)}";
            string skill = e.Skill == "*" ? "tous les sorts" : _flow.Content.Skills.Find(s => s.Id == e.Skill)?.Name ?? e.Skill!;
            return $"{sign}{pct} % {FieldName(e.Field!)} ({skill})";
        }

        private void DrawWorkshop()
        {
            float w = (float)Layout.GameW;
            var content = _flow.Content;
            var profile = _flow.Profile;
            var catalog = content.Upgrades;
            int gold = profile.Wallet.Balance(Wallet.Gold);

            UiKit.Label(new Rect(0, 14, w, 44), "Atelier", 38, Color.white, TextAnchor.MiddleCenter, true);
            UiKit.Label(new Rect(w - 300, 14, 288, 44), "Or : " + Format.Number(gold), Layout.Font.Strong, UiKit.Gold, TextAnchor.MiddleRight, true);
            var back = R(Layout.BackButton);
            UiKit.Button(back, "← Menu", Layout.Font.Body, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(back, UiAction.BackToMenu)) _flow.BackToMenu();

            UiKit.Label(new Rect((float)Layout.WorkshopEquipment.X, 58, 300, 24), "Équipement", Layout.Font.Body, UiKit.Muted, TextAnchor.MiddleLeft, true);
            UiKit.Label(new Rect((float)Layout.WorkshopTalents.X, 58, 400, 24), "Talents du soigneur (un choix par palier)", Layout.Font.Body, UiKit.Muted, TextAnchor.MiddleLeft, true);

            // Équipement : une carte par piste.
            var cards = Layout.WorkshopEquipmentCards(catalog.Equipment.Count);
            for (int i = 0; i < catalog.Equipment.Count; i++)
            {
                var track = catalog.Equipment[i];
                var r = R(cards[i]);
                int level = profile.Loadout.LevelOf(track.Id);
                bool maxed = level >= track.MaxLevel;
                int cost = Workshop.NextCost(profile, track);
                bool canBuy = !maxed && gold >= cost;
                var character = content.Characters.Find(c => c.Id == track.CharacterId);
                var accent = track.CharacterId == "tank" ? Palette.Tank : track.CharacterId == "healer" ? Palette.Healer : track.CharacterId == "dps2" ? Palette.Mage : Palette.Archer;

                UiKit.Fill(r, new Color(UiKit.Panel.r, UiKit.Panel.g, UiKit.Panel.b, 0.94f), 10);
                UiKit.Outline(r, maxed ? UiKit.Gold : UiKit.PanelStroke, maxed ? 2 : 1.5f, 10);
                UiKit.Fill(new Rect(r.x + 8, r.y + 8, 6, r.height - 16), accent, 3);
                UiKit.Label(new Rect(r.x + 24, r.y + 8, r.width - 160, 26), track.Name, Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);
                UiKit.Label(new Rect(r.x + 24, r.y + 34, r.width - 160, 20), character?.Name ?? track.CharacterId, Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleLeft);
                for (int p = 0; p < track.MaxLevel; p++)
                {
                    var pip = new Rect(r.x + 26 + p * 24, r.y + 62, 18, 18);
                    UiKit.Fill(pip, p < level ? UiKit.Gold : new Color(0.2f, 0.22f, 0.3f, 1f), 4);
                }
                string perLevel = "Par niveau : " + string.Join(", ", track.PerLevel.ConvertAll(e => Describe(e)));
                float y = r.y + 88;
                if (level > 0)
                {
                    UiKit.Label(new Rect(r.x + 24, y, r.width - 40, 20), "Actuel : " + string.Join(", ", track.PerLevel.ConvertAll(e => Describe(e, level))), Layout.Font.Small, UiKit.Selected, TextAnchor.MiddleLeft);
                    y += 22;
                }
                if (!maxed) UiKit.Paragraph(new Rect(r.x + 24, y, r.width - 40, r.yMax - y - 4), perLevel, Layout.Font.Small, UiKit.Muted);

                var buy = R(Layout.WorkshopBuyButton(cards[i]));
                UiKit.Button(buy, maxed ? "Max" : Format.Number(cost) + " or", Layout.Font.Body,
                    canBuy ? Palette.Hex("245C43") : Palette.Hex("1B1E2C"), canBuy ? Palette.Hex("5FB98D") : UiKit.PanelStroke, canBuy || maxed);
                if (!maxed && Hit(buy, UiAction.BuyEquipment)) _flow.BuyEquipment(track.Id);
            }

            // Talents : trois paliers, deux options exclusives.
            for (int i = 0; i < catalog.TalentTiers.Count; i++)
            {
                var tier = catalog.TalentTiers[i];
                var tr = R(Layout.WorkshopTalentTier(i, catalog.TalentTiers.Count));
                bool bought = profile.Loadout.Talents.TryGetValue(tier.Tier, out var chosen);
                var availability = Workshop.TierAvailability(profile, content, tier.Tier);
                bool open = bought || availability == PurchaseResult.Ok;
                string status = bought ? "Acquis" : availability == PurchaseResult.Locked ? $"Il faut {tier.RequiresStars} étoiles (vous : {profile.TotalStars})" : availability == PurchaseResult.NeedPreviousTier ? "Choisissez d'abord le palier " + (tier.Tier - 1) : Format.Number(tier.Cost) + " or";
                UiKit.Label(new Rect(tr.x, tr.y, tr.width, 30), $"Palier {tier.Tier}  ·  {status}", Layout.Font.Body, bought ? UiKit.Gold : (open ? Color.white : UiKit.Muted), TextAnchor.MiddleLeft, true);

                for (int o = 0; o < tier.Options.Count; o++)
                {
                    var option = tier.Options[o];
                    var r = R(Layout.WorkshopTalentOption(i, catalog.TalentTiers.Count, o));
                    bool active = bought && chosen == option.Id;
                    bool canPick = open && !active && (bought || gold >= tier.Cost);
                    UiKit.Fill(r, new Color(UiKit.Panel.r, UiKit.Panel.g, UiKit.Panel.b, open ? 0.94f : 0.6f), 10);
                    UiKit.Outline(r, active ? UiKit.Gold : (canPick ? Palette.Hex("6F7698") : UiKit.PanelStroke), active ? 3 : 1.5f, 10);
                    UiKit.Label(new Rect(r.x + 12, r.y + 6, r.width - 24, 26), option.Name, Layout.Font.Strong, open ? Color.white : UiKit.Muted, TextAnchor.MiddleLeft, true);
                    UiKit.Paragraph(new Rect(r.x + 12, r.y + 34, r.width - 24, r.height - 62), option.Description, Layout.Font.Small, open ? Palette.Hex("C9CDE0") : UiKit.Muted);
                    string tag = active ? "Actif" : !open ? "Verrouillé" : bought ? "Changer (gratuit)" : (gold >= tier.Cost ? "Choisir" : "Or insuffisant");
                    UiKit.Label(new Rect(r.x + 12, r.yMax - 28, r.width - 24, 22), tag, Layout.Font.Small, active ? UiKit.Gold : (canPick ? Palette.Heal : UiKit.Muted), TextAnchor.MiddleLeft, true);
                    if (open && !active && Hit(r, UiAction.PickTalent)) _flow.PickTalent(tier.Tier, option.Id);
                }
            }

            if (Time.realtimeSinceStartup < _flow.NoticeUntil)
                UiKit.Label(new Rect(0, 678, w, 30), _flow.Notice, Layout.Font.Strong, _flow.NoticeIsError ? Palette.Damage : Palette.Heal, TextAnchor.MiddleCenter, true);
        }

        // ---- Choix du niveau ---------------------------------------------------------------------

        private void DrawLevels()
        {
            float w = (float)Layout.GameW;
            var content = _flow.Content;
            var profile = _flow.Profile;
            UiKit.Label(new Rect(0, 40, w, 50), "Choisir un niveau", 40, Color.white, TextAnchor.MiddleCenter, true);

            var back = R(Layout.BackButton);
            UiKit.Button(back, "← Menu", Layout.Font.Body, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(back, UiAction.BackToMenu)) _flow.BackToMenu();

            var rects = Layout.LevelCardRects(content.Levels.Count);
            for (int i = 0; i < content.Levels.Count; i++)
            {
                var level = content.Levels[i];
                var r = R(rects[i]);
                bool unlocked = profile.IsUnlocked(level);
                var record = profile.RecordOf(level.Id);
                var accent = ModelFactory.BossColors(level.BossId).calm;

                UiKit.Fill(r, new Color(UiKit.Panel.r, UiKit.Panel.g, UiKit.Panel.b, unlocked ? 0.94f : 0.7f), 10);
                UiKit.Outline(r, unlocked ? UiKit.Selected : UiKit.PanelStroke, unlocked ? 2 : 1.5f, 10);
                UiKit.Fill(new Rect(r.x + 10, r.y + 10, r.width - 20, 8), new Color(accent.r, accent.g, accent.b, unlocked ? 1f : 0.3f), 4);
                UiKit.Label(new Rect(r.x, r.y + 28, r.width, 22), "Niveau " + (i + 1), Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);
                UiKit.Label(new Rect(r.x + 6, r.y + 52, r.width - 12, 30), level.Name, Layout.Font.Title, unlocked ? Color.white : UiKit.Muted, TextAnchor.MiddleCenter, true);
                UiKit.Label(new Rect(r.x, r.y + 84, r.width, 22), "Boss : " + content.BossById(level.BossId).Name, Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);

                if (unlocked)
                {
                    UiKit.Stars(new Rect(r.x, r.y + 120, r.width, 60), record.BestStars, 44);
                    UiKit.Label(new Rect(r.x, r.y + 196, r.width, 22), record.Completed ? "Meilleur temps : " + Format.Seconds(record.BestTimeMs) : "Pas encore terminé", Layout.Font.Body, Color.white, TextAnchor.MiddleCenter);
                    UiKit.Label(new Rect(r.x, r.y + 220, r.width, 22), record.Clears > 0 ? "Victoires : " + record.Clears : " ", Layout.Font.Small, UiKit.Muted, TextAnchor.MiddleCenter);
                    UiKit.Label(new Rect(r.x, r.y + 250, r.width, 22),
                        record.Completed ? "Répétition : " + Format.Number(level.RepeatGold) + " or" : "Première victoire : " + Format.Number(level.RewardGold) + " or",
                        Layout.Font.Small, UiKit.Gold, TextAnchor.MiddleCenter);
                    var play = new Rect(r.x + 40, r.yMax - 66, r.width - 80, 48);
                    UiKit.Button(play, record.Completed ? "Rejouer" : "Jouer", Layout.Font.Title, Palette.Hex("245C43"), Palette.Hex("5FB98D"));
                }
                else
                {
                    var prev = level.Requires != null ? content.LevelById(level.Requires).Name : "";
                    UiKit.Label(new Rect(r.x, r.y + 150, r.width, 30), "Verrouillé", Layout.Font.Title, UiKit.Muted, TextAnchor.MiddleCenter, true);
                    UiKit.Label(new Rect(r.x + 10, r.y + 190, r.width - 20, 44), "Terminez d'abord\n« " + prev + " »", Layout.Font.Small, UiKit.Muted, TextAnchor.UpperCenter);
                }

                var cap = BattleKeys.LevelLabel(i);
                if (cap != null && unlocked) DrawKeyCap(r, cap);
                if (Hit(r, UiAction.PickLevel)) _flow.StartLevel(level.Id);
            }
        }

        private static void DrawKeyCap(Rect card, string label)
        {
            var cap = new Rect(card.x + 8, card.y + 26, 24, 24);
            UiKit.Fill(cap, new Color(0f, 0f, 0f, 0.6f), 5);
            UiKit.Outline(cap, new Color(1f, 1f, 1f, 0.5f), 1, 5);
            UiKit.Label(cap, label, Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
        }

        // ---- Pied de page : progression globale --------------------------------------------------

        private void DrawFooter()
        {
            var p = _flow.Profile;
            int max = _flow.Content.Levels.Count * Progression.MaxStars;
            float w = (float)Layout.GameW;
            UiKit.Label(new Rect(0, 664, w, 30), $"Étoiles : {p.TotalStars} / {max}      ·      Or : {Format.Number(p.Wallet.Balance(Wallet.Gold))}", Layout.Font.Body, UiKit.Gold, TextAnchor.MiddleCenter);
        }
    }
}
