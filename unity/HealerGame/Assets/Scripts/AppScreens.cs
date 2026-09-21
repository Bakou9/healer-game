using System;
using System.Linq;
using System.Text;
using Healer.Combat.Progress;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Écrans hors combat en UI Toolkit : menu principal, choix du niveau, atelier, réglages. Aucune règle de jeu :
    /// lecture du profil et de la progression, gestes vers GameFlow. Chaque geste passe par InputGate. L'écran est
    /// reconstruit quand sa « signature » (état affiché) change : les listes sont courtes, pas besoin de liaisons.
    /// </summary>
    public sealed class AppScreens
    {
        public const float BackdropAlpha = 0.72f;

        /// <summary>Voile derrière l'écran actuel : presque transparent dans la galerie, pour voir le modèle.</summary>
        public float BackdropAlphaNow => _flow.Screen == AppScreen.Gallery ? 0.12f : BackdropAlpha;

        private readonly UiRoot _root;
        private readonly GameFlow _flow;
        private readonly VisualElement _container;
        private string _signature = "";

        public bool Active => _flow.Screen != AppScreen.Battle;

        public AppScreens(UiRoot root, GameFlow flow)
        {
            _root = root;
            _flow = flow;
            _container = new VisualElement { pickingMode = PickingMode.Ignore };
            _container.style.position = Position.Absolute;
            _container.style.left = 0; _container.style.top = 0; _container.style.right = 0; _container.style.bottom = 0;
            root.Modal.Add(_container);
        }

        public void Refresh()
        {
            _container.style.display = Active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!Active) { _signature = ""; return; }
            string sig = Signature();
            if (_flow.Screen == AppScreen.Credits) ScrollCredits();
            if (sig == _signature) return;
            _signature = sig;
            _container.Clear();
            switch (_flow.Screen)
            {
                case AppScreen.MainMenu: BuildMenu(); break;
                case AppScreen.Workshop: BuildWorkshop(); break;
                case AppScreen.Settings: BuildSettings(); break;
                case AppScreen.Credits: BuildCredits(); break;
                case AppScreen.Gallery: BuildGallery(); break;
                default: BuildLevels(); BuildFooter(); break;
            }
        }

        private string Signature()
        {
            var p = _flow.Profile;
            var sb = new StringBuilder();
            sb.Append(_flow.Screen).Append('|').Append(p.Wallet.Balance(Wallet.Gold)).Append('|').Append(p.TotalStars);
            foreach (var kv in p.Loadout.Equipment.OrderBy(k => k.Key)) sb.Append('|').Append(kv.Key).Append(kv.Value);
            foreach (var kv in p.Loadout.Talents.OrderBy(k => k.Key)) sb.Append('|').Append(kv.Key).Append(kv.Value);
            foreach (var kv in p.Levels.OrderBy(k => k.Key)) sb.Append('|').Append(kv.Key).Append(kv.Value.BestStars).Append(kv.Value.Clears).Append(kv.Value.BestTimeMs);
            var s = p.Settings;
            sb.Append('|').Append(s.Muted).Append(s.MusicVolume).Append(s.SfxVolume).Append(s.ScreenShake);
            sb.Append('|').Append(Time.realtimeSinceStartup < _flow.NoticeUntil ? _flow.Notice : "");
            if (_flow.Screen == AppScreen.Gallery && _flow.Gallery != null) sb.Append('|').Append(_flow.Gallery.Signature);
            return sb.ToString();
        }

        private VisualElement Add(Rect r, Color fill, float radius = 10f, Color? stroke = null, float w = 0) => Ui.Box(_container, r, fill, radius, stroke, w);

        private void Act(UiAction action, Action run)
        {
            if (InputGate.Allows(_flow.Screen, ScreenState.Playing, action)) run();
        }

        private VisualElement Btn(Rect r, string text, int size, Color fill, Color stroke, UiAction action, Action run, bool enabled = true) =>
            Ui.Button(_container, r, text, size, fill, stroke, () => Act(action, run), enabled);

        private Label Txt(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false) =>
            Ui.Text(_container, r, text, size, color, anchor, bold, wrap);

        // ---- Menu principal ----------------------------------------------------------------------

        private void BuildMenu()
        {
            float w = (float)Layout.GameW;
            Txt(new Rect(0, 100, w, 90), "Healer Game", 72, Color.white, TextAnchor.MiddleCenter, true);
            Txt(new Rect(0, 192, w, 32), "Gardez votre équipe en vie.", Layout.Font.Title, Ui.Muted, TextAnchor.MiddleCenter);
            Btn(Ui.R(Layout.MenuPlay), "Jouer", 30, Ui.PrimaryFill, Ui.PrimaryStroke, UiAction.MenuPlay, _flow.OpenLevels);
            Btn(Ui.R(Layout.MenuWorkshop), "Atelier", Layout.Font.Title, Ui.ButtonFill, Ui.Gold, UiAction.MenuWorkshop, _flow.OpenWorkshop);
            Btn(Ui.R(Layout.MenuSettings), "Réglages", Layout.Font.Title, Ui.ButtonFill, Ui.ButtonStroke, UiAction.MenuSettings, _flow.OpenSettings);
            Btn(Ui.R(Layout.MenuSound), _flow.Profile.Settings.Muted ? "Son : coupé" : "Son : activé", Layout.Font.Title, Ui.ButtonFill, Ui.ButtonStroke, UiAction.MenuToggleSound, _flow.ToggleMute);
            if (CanQuit) Btn(Ui.R(Layout.MenuQuit), "Quitter", Layout.Font.Title, Ui.ButtonFill, Ui.ButtonStroke, UiAction.MenuQuit, _flow.Quit);
            if (_flow.DevMode) Txt(new Rect(0, 236, w, 24), "MODE DÉVELOPPEUR · sauvegarde séparée", Layout.Font.Small, Palette.Hex("FFB347"), TextAnchor.MiddleCenter, true);
            if (_flow.DevMode) Btn(Ui.R(Layout.MenuGallery), "Galerie de modèles", Layout.Font.Body, Palette.Hex("4A3A12"), Palette.Hex("FFB347"), UiAction.MenuGallery, _flow.OpenGallery);
            Btn(Ui.R(Layout.MenuCredits), "Crédits", Layout.Font.Body, Ui.ButtonFill, Ui.ButtonStroke, UiAction.MenuCredits, _flow.OpenCredits);
            if (Keyboard.current != null) Txt(new Rect(0, 624, w, 24), "Entrée : jouer · M : son · F8 : noter un retour", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
        }

        /// <summary>Un bouton « Quitter » n'a de sens que sur PC (les téléphones ferment l'application autrement).</summary>
        private static bool CanQuit => Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer;

        private void BackButton() =>
            Btn(Ui.R(Layout.BackButton), "← Menu", Layout.Font.Body, Ui.ButtonFill, Ui.ButtonStroke, UiAction.BackToMenu, _flow.BackToMenu);

        // ---- Galerie de modèles (mode développeur) -----------------------------------------------------

        private void BuildGallery()
        {
            var g = _flow.Gallery;
            BackButton();
            if (g == null || g.Entries.Count == 0) return;
            float w = (float)Layout.GameW;
            Txt(new Rect(0, 14, w, 40), "Galerie de modèles", 30, Color.white, TextAnchor.MiddleCenter, true);
            Txt(new Rect(0, 50, w, 20), "MODE DÉVELOPPEUR", Layout.Font.Small, Palette.Hex("FFB347"), TextAnchor.MiddleCenter, true);
            var dark = Palette.Hex("4A3A12"); var amber = Palette.Hex("FFB347");

            // Liste des modèles (à gauche).
            for (int i = 0; i < g.Entries.Count; i++)
            {
                int index = i; var e = g.Entries[i]; bool sel = i == g.Index;
                Btn(new Rect(12, 80 + i * 50, 240, 44), (e.IsBoss ? "Boss · " : "") + e.Name, Layout.Font.Body, sel ? Ui.PrimaryFill : Ui.ButtonFill, sel ? Ui.PrimaryStroke : Ui.ButtonStroke, UiAction.GalleryControl, () => g.Select(index));
            }

            // Commandes (à droite).
            float x = w - 272, y = 80;
            void Row(string label, string[] names, int selected, System.Action<int> pick)
            {
                Txt(new Rect(x, y, 260, 22), label, Layout.Font.Small, Ui.Muted, TextAnchor.MiddleLeft, true);
                float bw = (260f - 8f * (names.Length - 1)) / names.Length;
                for (int i = 0; i < names.Length; i++)
                {
                    int k = i; bool on = i == selected;
                    Btn(new Rect(x + i * (bw + 8f), y + 24, bw, 38), names[i], Layout.Font.Small, on ? Ui.PrimaryFill : Ui.ButtonFill, on ? Ui.PrimaryStroke : Ui.ButtonStroke, UiAction.GalleryControl, () => pick(k));
                }
                y += 76;
            }
            if (g.Current.IsBoss) Row("Phase", new[] { "Calme", "Fureur" }, g.Fury ? 1 : 0, k => g.SetFury(k == 1));
            else
            {
                Row("Arme", ModelGallery.TierLabels, g.WeaponTier, g.SetWeaponTier);
                Row("Armure", ModelGallery.TierLabels, g.ArmorTier, g.SetArmorTier);
            }
            Txt(new Rect(x, y, 240, 22), "Pose", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleLeft, true);
            for (int i = 0; i < ModelGallery.PoseNames.Length; i++)
            {
                int k = i; bool on = i == g.Pose;
                Btn(new Rect(x, y + 24 + i * 44, 260, 38), ModelGallery.PoseNames[i], Layout.Font.Small, on ? Ui.PrimaryFill : Ui.ButtonFill, on ? Ui.PrimaryStroke : Ui.ButtonStroke, UiAction.GalleryControl, () => g.SetPose(k));
            }
            y += 24 + ModelGallery.PoseNames.Length * 44 + 8;
            Btn(new Rect(x, y, 78, 38), "◀", Layout.Font.Title, Ui.ButtonFill, Ui.ButtonStroke, UiAction.GalleryControl, () => g.Turn(-35f));
            Btn(new Rect(x + 91, y, 78, 38), g.AutoRotate ? "Auto ✓" : "Auto", Layout.Font.Small, g.AutoRotate ? Ui.PrimaryFill : Ui.ButtonFill, g.AutoRotate ? Ui.PrimaryStroke : Ui.ButtonStroke, UiAction.GalleryControl, g.ToggleAutoRotate);
            Btn(new Rect(x + 182, y, 78, 38), "▶", Layout.Font.Title, Ui.ButtonFill, Ui.ButtonStroke, UiAction.GalleryControl, () => g.Turn(35f));
            Btn(new Rect(x, y + 46, 126, 38), "Zoom −", Layout.Font.Small, Ui.ButtonFill, Ui.ButtonStroke, UiAction.GalleryControl, () => g.Zoom(0.85f));
            Btn(new Rect(x + 134, y + 46, 126, 38), "Zoom +", Layout.Font.Small, Ui.ButtonFill, Ui.ButtonStroke, UiAction.GalleryControl, () => g.Zoom(1.18f));
            Btn(new Rect(x, y + 92, 260, 38), g.SlowMo ? "Ralenti ✓" : "Ralenti", Layout.Font.Small, g.SlowMo ? Ui.PrimaryFill : Ui.ButtonFill, g.SlowMo ? Ui.PrimaryStroke : Ui.ButtonStroke, UiAction.GalleryControl, g.ToggleSlowMo);

            Txt(new Rect(270, 672, w - 540, 30), g.Info, Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, false);
        }

        // ---- Générique ---------------------------------------------------------------------------

        private VisualElement? _creditsRoll;
        private float _creditsHeight, _creditsStart;
        private const float CreditsViewTop = 70f, CreditsViewHeight = 590f, CreditsSpeed = 46f;

        private void BuildCredits()
        {
            float w = (float)Layout.GameW;
            BackButton();
            var lines = CreditsRoll.Build(_flow.Credits);
            _creditsHeight = (float)CreditsRoll.TotalHeight(lines);
            var view = Add(new Rect(0, CreditsViewTop, w, CreditsViewHeight), Color.clear, 0);
            view.style.overflow = Overflow.Hidden;
            _creditsRoll = new VisualElement { pickingMode = PickingMode.Ignore };
            _creditsRoll.style.position = Position.Absolute;
            _creditsRoll.style.left = 0; _creditsRoll.style.width = w; _creditsRoll.style.height = _creditsHeight;
            view.Add(_creditsRoll);
            float y = 0;
            foreach (var l in lines)
            {
                var r = new Rect(80, y, w - 160, (float)l.Height);
                switch (l.Kind)
                {
                    case CreditLineKind.Title: Ui.Text(_creditsRoll, r, l.Text, 64, Color.white, TextAnchor.MiddleCenter, true); break;
                    case CreditLineKind.Heading: Ui.Text(_creditsRoll, r, l.Text, 34, Ui.Gold, TextAnchor.MiddleCenter, true); break;
                    case CreditLineKind.Name: Ui.Text(_creditsRoll, r, l.Text, Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true); break;
                    case CreditLineKind.Thanks: Ui.Text(_creditsRoll, r, l.Text, Layout.Font.Title, Palette.Heal, TextAnchor.MiddleCenter, true); break;
                    case CreditLineKind.Detail: Ui.Text(_creditsRoll, r, l.Text, Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter); break;
                }
                y += (float)l.Height;
            }
            _creditsStart = Time.realtimeSinceStartup;
        }

        /// <summary>Le générique défile tout seul, en boucle (46 px par seconde).</summary>
        private void ScrollCredits()
        {
            if (_creditsRoll == null) return;
            float travel = CreditsViewHeight + _creditsHeight;
            float offset = ((Time.realtimeSinceStartup - _creditsStart) * CreditsSpeed) % travel;
            _creditsRoll.style.top = CreditsViewHeight - offset;
        }

        // ---- Réglages ----------------------------------------------------------------------------

        private void BuildSettings()
        {
            float w = (float)Layout.GameW;
            var s = _flow.Profile.Settings;
            Txt(new Rect(0, 60, w, 60), "Réglages", 44, Color.white, TextAnchor.MiddleCenter, true);
            BackButton();

            string[] labels = { "Musique", "Effets sonores", "Secousse de l'écran" };
            for (int row = 0; row < Layout.SettingsRows; row++)
            {
                int rowIndex = row;
                var r = Ui.R(Layout.SettingsRow(row));
                Add(r, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.94f), 10, Ui.PanelStroke, 1.5f);
                Txt(new Rect(r.x + 20, r.y, 300, r.height), labels[row], Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);

                bool toggle = row == 2;
                Btn(Ui.R(Layout.SettingsMinus(row)), toggle ? "Non" : "−", toggle ? Layout.Font.Body : Layout.Font.Banner, Ui.ButtonFill, Ui.ButtonStroke, UiAction.AdjustSetting, () => _flow.AdjustSetting(rowIndex, -1));
                Btn(Ui.R(Layout.SettingsPlus(row)), toggle ? "Oui" : "+", toggle ? Layout.Font.Body : Layout.Font.Banner, Ui.ButtonFill, Ui.ButtonStroke, UiAction.AdjustSetting, () => _flow.AdjustSetting(rowIndex, +1));
                var bar = Ui.R(Layout.SettingsBar(row));
                Add(bar, new Color(0, 0, 0, 0.55f), 6);
                if (toggle) Txt(bar, s.ScreenShake ? "Activée" : "Coupée", Layout.Font.Small, s.ScreenShake ? Palette.Heal : Ui.Muted, TextAnchor.MiddleCenter, true);
                else
                {
                    int value = row == 0 ? s.MusicVolume : s.SfxVolume;
                    if (value > 0) Add(new Rect(bar.x, bar.y, Mathf.Max(6f, bar.width * value / 100f), bar.height), Palette.Hex("4AA8FF"), 6);
                    Txt(bar, value + " %", Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
                }
            }
            Txt(new Rect(0, 470, w, 26), "Le bouton « Son » du menu (ou la touche M) coupe tout sans perdre ces réglages.", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
        }

        // ---- Atelier -----------------------------------------------------------------------------

        private static string StatName(string stat) => stat switch
        {
            "maxHp" => "PV",
            "atk" => "attaque",
            "def" => "défense",
            "maxMana" => "mana max",
            "manaRegen" => "régénération de mana",
            _ => stat,
        };

        private static string FieldName(string field) => field switch
        {
            "healAmount" => "de soin",
            "shieldAmount" => "de bouclier",
            "manaCost" => "de coût en mana",
            "cooldownMs" => "de recharge",
            _ => field,
        };

        /// <summary>« +4 % attaque », « +5 % de soin (Soin) » : décrit un effet à partir des données.</summary>
        private string Describe(UpgradeEffect e, int times = 1)
        {
            int pct = e.Pct * times;
            string sign = pct > 0 ? "+" : "";
            if (e.Stat != null) return $"{sign}{pct} % {StatName(e.Stat)}";
            string skill = e.Skill == "*" ? "tous les sorts" : _flow.Content.Skills.Find(s => s.Id == e.Skill)?.Name ?? e.Skill!;
            return $"{sign}{pct} % {FieldName(e.Field!)} ({skill})";
        }

        private static Color RoleAccent(string characterId) =>
            characterId == "tank" ? Palette.Tank : characterId == "healer" ? Palette.Healer : characterId == "dps2" ? Palette.Mage : Palette.Archer;

        private void BuildWorkshop()
        {
            float w = (float)Layout.GameW;
            var content = _flow.Content;
            var profile = _flow.Profile;
            var catalog = content.Upgrades;
            int gold = profile.Wallet.Balance(Wallet.Gold);

            Txt(new Rect(0, 14, w, 44), "Atelier", 38, Color.white, TextAnchor.MiddleCenter, true);
            Txt(new Rect(w - 300, 14, 288, 44), "Or : " + Format.Number(gold), Layout.Font.Strong, Ui.Gold, TextAnchor.MiddleRight, true);
            BackButton();
            if (_flow.DevMode)
            {
                Txt(new Rect(w / 2 - 200, 60, 400, 20), "MODE DÉVELOPPEUR", Layout.Font.Small, Palette.Hex("FFB347"), TextAnchor.MiddleCenter, true);
                Btn(new Rect(w - 470, 10, 160, 44), "Or → " + Format.Number(Workshop.DevGold), Layout.Font.Small, Palette.Hex("4A3A12"), Palette.Hex("FFB347"), UiAction.BuyEquipment, _flow.DevSetGold);
            }
            Txt(new Rect((float)Layout.WorkshopEquipment.X, 58, 300, 24), "Équipement", Layout.Font.Body, Ui.Muted, TextAnchor.MiddleLeft, true);
            Txt(new Rect((float)Layout.WorkshopTalents.X, 58, 400, 24), "Talents du soigneur (un choix par palier)", Layout.Font.Body, Ui.Muted, TextAnchor.MiddleLeft, true);

            var cards = Layout.WorkshopEquipmentCards(catalog.Equipment.Count);
            for (int i = 0; i < catalog.Equipment.Count; i++)
            {
                var track = catalog.Equipment[i];
                var r = Ui.R(cards[i]);
                int level = profile.Loadout.LevelOf(track.Id);
                bool maxed = level >= track.MaxLevel;
                int cost = Workshop.NextCost(profile, track);
                bool canBuy = !maxed && gold >= cost;
                var character = content.Characters.Find(c => c.Id == track.CharacterId);

                Add(r, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.94f), 10, maxed ? Ui.Gold : Ui.PanelStroke, maxed ? 2 : 1.5f);
                Add(new Rect(r.x + 8, r.y + 8, 6, r.height - 16), RoleAccent(track.CharacterId), 3);
                Txt(new Rect(r.x + 24, r.y + 8, r.width - 160, 26), track.Name, Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);
                Txt(new Rect(r.x + 24, r.y + 34, r.width - 160, 20), character?.Name ?? track.CharacterId, Layout.Font.Small, Ui.Muted);
                for (int p = 0; p < track.MaxLevel; p++)
                    Add(new Rect(r.x + 26 + p * 24, r.y + 62, 18, 18), p < level ? Ui.Gold : new Color(0.2f, 0.22f, 0.3f, 1f), 4);
                Txt(new Rect(r.x + 26 + track.MaxLevel * 24 + 8, r.y + 60, 190, 22), "Aspect : " + content.Appearance.TierName(level), Layout.Font.Small, level > 0 ? Ui.Selected : Ui.Muted, TextAnchor.MiddleLeft);
                string perLevel = "Par niveau : " + string.Join(", ", track.PerLevel.ConvertAll(e => Describe(e)));
                float y = r.y + 88;
                if (level > 0)
                {
                    Txt(new Rect(r.x + 24, y, r.width - 40, 20), "Actuel : " + string.Join(", ", track.PerLevel.ConvertAll(e => Describe(e, level))), Layout.Font.Small, Ui.Selected);
                    y += 22;
                }
                if (!maxed) Txt(new Rect(r.x + 24, y, r.width - 40, r.yMax - y - 4), perLevel, Layout.Font.Small, Ui.Muted, TextAnchor.UpperLeft, false, true);

                string trackId = track.Id;
                if (_flow.DevMode)
                {
                    var buyRect = Ui.R(Layout.WorkshopBuyButton(cards[i]));
                    Btn(new Rect(buyRect.x + 10, buyRect.y + 54, 44, 36), "−", Layout.Font.Title, Palette.Hex("4A3A12"), Palette.Hex("FFB347"), UiAction.BuyEquipment, () => _flow.DevAdjustEquipment(trackId, -1));
                    Btn(new Rect(buyRect.x + 62, buyRect.y + 54, 44, 36), "+", Layout.Font.Title, Palette.Hex("4A3A12"), Palette.Hex("FFB347"), UiAction.BuyEquipment, () => _flow.DevAdjustEquipment(trackId, +1));
                }
                Btn(Ui.R(Layout.WorkshopBuyButton(cards[i])), maxed ? "Max" : Format.Number(cost) + " or", Layout.Font.Body,
                    canBuy ? Ui.PrimaryFill : Palette.Hex("1B1E2C"), canBuy ? Ui.PrimaryStroke : Ui.PanelStroke,
                    UiAction.BuyEquipment, () => { if (!maxed) _flow.BuyEquipment(trackId); }, canBuy || maxed);
            }

            for (int i = 0; i < catalog.TalentTiers.Count; i++)
            {
                var tier = catalog.TalentTiers[i];
                var tr = Ui.R(Layout.WorkshopTalentTier(i, catalog.TalentTiers.Count));
                bool bought = profile.Loadout.Talents.TryGetValue(tier.Tier, out var chosen);
                var availability = Workshop.TierAvailability(profile, content, tier.Tier);
                bool open = bought || availability == PurchaseResult.Ok;
                string status = bought ? "Acquis"
                    : availability == PurchaseResult.Locked ? $"Il faut {tier.RequiresStars} étoiles (vous : {profile.TotalStars})"
                    : availability == PurchaseResult.NeedPreviousTier ? "Choisissez d'abord le palier " + (tier.Tier - 1)
                    : Format.Number(tier.Cost) + " or";
                Txt(new Rect(tr.x, tr.y, tr.width, 30), $"Palier {tier.Tier}  ·  {status}", Layout.Font.Body, bought ? Ui.Gold : (open ? Color.white : Ui.Muted), TextAnchor.MiddleLeft, true);

                for (int o = 0; o < tier.Options.Count; o++)
                {
                    var option = tier.Options[o];
                    var r = Ui.R(Layout.WorkshopTalentOption(i, catalog.TalentTiers.Count, o));
                    bool active = bought && chosen == option.Id;
                    bool canPick = open && !active && (bought || gold >= tier.Cost);
                    var card = Add(r, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, open ? 0.94f : 0.6f), 10,
                        active ? Ui.Gold : (canPick ? Ui.ButtonStroke : Ui.PanelStroke), active ? 3 : 1.5f);
                    Txt(new Rect(r.x + 12, r.y + 6, r.width - 24, 26), option.Name, Layout.Font.Strong, open ? Color.white : Ui.Muted, TextAnchor.MiddleLeft, true);
                    Txt(new Rect(r.x + 12, r.y + 34, r.width - 24, r.height - 62), option.Description, Layout.Font.Small, open ? Palette.Hex("C9CDE0") : Ui.Muted, TextAnchor.UpperLeft, false, true);
                    string tag = active ? "Actif" : !open ? "Verrouillé" : bought ? "Changer (gratuit)" : (gold >= tier.Cost ? "Choisir" : "Or insuffisant");
                    Txt(new Rect(r.x + 12, r.yMax - 28, r.width - 24, 22), tag, Layout.Font.Small, active ? Ui.Gold : (canPick ? Palette.Heal : Ui.Muted), TextAnchor.MiddleLeft, true);
                    int tierNumber = tier.Tier; string optionId = option.Id;
                    if (open && !active) Ui.OnClick(card, () => Act(UiAction.PickTalent, () => _flow.PickTalent(tierNumber, optionId)));
                }
            }

            if (Time.realtimeSinceStartup < _flow.NoticeUntil)
                Txt(new Rect(0, 678, w, 30), _flow.Notice, Layout.Font.Strong, _flow.NoticeIsError ? Palette.Damage : Palette.Heal, TextAnchor.MiddleCenter, true);
        }

        // ---- Choix du niveau ---------------------------------------------------------------------

        private void BuildLevels()
        {
            float w = (float)Layout.GameW;
            var content = _flow.Content;
            var profile = _flow.Profile;
            Txt(new Rect(0, 40, w, 50), "Choisir un niveau", 40, Color.white, TextAnchor.MiddleCenter, true);
            BackButton();

            var rects = Layout.LevelCardRects(content.Levels.Count);
            for (int i = 0; i < content.Levels.Count; i++)
            {
                var level = content.Levels[i];
                var r = Ui.R(rects[i]);
                bool unlocked = profile.IsUnlocked(level);
                var record = profile.RecordOf(level.Id);
                var accent = ModelFactory.BossColors(level.BossId).calm;

                var card = Add(r, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, unlocked ? 0.94f : 0.7f), 10, unlocked ? Ui.Selected : Ui.PanelStroke, unlocked ? 2 : 1.5f);
                Add(new Rect(r.x + 10, r.y + 10, r.width - 20, 8), new Color(accent.r, accent.g, accent.b, unlocked ? 1f : 0.3f), 4);
                Txt(new Rect(r.x, r.y + 28, r.width, 22), "Niveau " + (i + 1), Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
                Txt(new Rect(r.x + 6, r.y + 52, r.width - 12, 30), level.Name, Layout.Font.Title, unlocked ? Color.white : Ui.Muted, TextAnchor.MiddleCenter, true);
                Txt(new Rect(r.x, r.y + 84, r.width, 22), "Boss : " + content.BossById(level.BossId).Name, Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);

                if (unlocked)
                {
                    Ui.Stars(_container, new Rect(r.x, r.y + 120, r.width, 60), record.BestStars, 44);
                    Txt(new Rect(r.x, r.y + 196, r.width, 22), record.Completed ? "Meilleur temps : " + Format.Seconds(record.BestTimeMs) : "Pas encore terminé", Layout.Font.Body, Color.white, TextAnchor.MiddleCenter);
                    Txt(new Rect(r.x, r.y + 220, r.width, 22), record.Clears > 0 ? "Victoires : " + record.Clears : " ", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
                    Txt(new Rect(r.x, r.y + 250, r.width, 22),
                        record.Completed ? "Répétition : " + Format.Number(level.RepeatGold) + " or" : "Première victoire : " + Format.Number(level.RewardGold) + " or",
                        Layout.Font.Small, Ui.Gold, TextAnchor.MiddleCenter);
                    var play = new Rect(r.x + 40, r.yMax - 66, r.width - 80, 48);
                    Add(play, Ui.PrimaryFill, 8, Ui.PrimaryStroke, 2);
                    Txt(play, record.Completed ? "Rejouer" : "Jouer", Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true);
                }
                else
                {
                    var prev = level.Requires != null ? content.LevelById(level.Requires).Name : "";
                    Txt(new Rect(r.x, r.y + 150, r.width, 30), "Verrouillé", Layout.Font.Title, Ui.Muted, TextAnchor.MiddleCenter, true);
                    Txt(new Rect(r.x + 10, r.y + 190, r.width - 20, 44), "Terminez d'abord\n« " + prev + " »", Layout.Font.Small, Ui.Muted, TextAnchor.UpperCenter, false, true);
                }

                var cap = BattleKeys.LevelLabel(i);
                if (cap != null && unlocked) KeyCap(new Rect(r.x + 8, r.y + 26, 24, 24), cap);
                string levelId = level.Id;
                Ui.OnClick(card, () => Act(UiAction.PickLevel, () => _flow.StartLevel(levelId)));
            }
        }

        private void KeyCap(Rect cap, string label)
        {
            Add(cap, new Color(0f, 0f, 0f, 0.6f), 5, new Color(1f, 1f, 1f, 0.5f), 1);
            Txt(cap, label, Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
        }

        private void BuildFooter()
        {
            var p = _flow.Profile;
            int max = _flow.Content.Levels.Count * Progression.MaxStars;
            Txt(new Rect(0, 664, (float)Layout.GameW, 30), $"Étoiles : {p.TotalStars} / {max}      ·      Or : {Format.Number(p.Wallet.Balance(Wallet.Gold))}", Layout.Font.Body, Ui.Gold, TextAnchor.MiddleCenter);
        }
    }
}
