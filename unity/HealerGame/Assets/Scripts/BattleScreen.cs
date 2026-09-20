using System.Collections.Generic;
using System.Linq;
using Healer.Combat;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Interface de combat en UI Toolkit. Le HUD est construit une fois par combat (équipe, sorts, boss) puis
    /// « lié » à l'état à chaque image ; les fenêtres modales (départ, pause, fin) sont reconstruites quand leur
    /// contenu change. Sorts en icônes seules (D-054) ; la fiche détaillée des sorts, avec et sans bonus, est
    /// dans le menu de pause. Aucune règle de jeu ici : lecture d'état et gestes (InputGate).
    /// </summary>
    public sealed class BattleScreen
    {
        private readonly UiRoot _root;
        private readonly GameFlow _flow;
        private readonly BattleController _ctl;
        private readonly VisualElement _hud;
        private readonly VisualElement _modal;

        private Battle? _builtFor;
        private string _modalSignature = "";
        public float BackdropAlpha { get; private set; }

        // Éléments liés à l'état
        private Label _bossTitle = null!, _enrage = null!, _telegraph = null!, _lastAction = null!, _pauseLabel = null!;
        private VisualElement _bossFill = null!, _gaugeBg = null!, _gaugeFill = null!;
        private VisualElement _manaFill = null!;
        private Label _manaLabel = null!, _targetLabel = null!, _castLabel = null!;
        private VisualElement _castBarBg = null!, _castBarFill = null!;
        private readonly List<CardView> _cards = new List<CardView>();
        private readonly List<SkillView> _skillViews = new List<SkillView>();

        private sealed class CardView
        {
            public string Id = "";
            public VisualElement Glow = null!, Panel = null!, RoleBar = null!, HpFill = null!, PillBox = null!;
            public Label HpText = null!, Threat = null!, Shield = null!, Pill = null!;
            public VisualElement Guide = null!;
            public Rect Rect;
        }

        private sealed class SkillView
        {
            public SkillDef Skill = null!;
            public VisualElement Root = null!, Icon = null!, Cooldown = null!, Guide = null!;
            public Label Mana = null!, CooldownText = null!;
            public Rect Rect;
        }

        public BattleScreen(UiRoot root, GameFlow flow)
        {
            _root = root;
            _flow = flow;
            _ctl = flow.Ctl;
            _hud = Container(root.Hud);
            _modal = Container(root.Modal);
            root.Vignette.style.backgroundImage = new StyleBackground(MakeVignette());
            root.Vignette.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            root.Danger.style.display = DisplayStyle.None;
        }

        private static VisualElement Container(VisualElement parent)
        {
            var c = new VisualElement { pickingMode = PickingMode.Ignore };
            c.style.position = Position.Absolute;
            c.style.left = 0; c.style.top = 0; c.style.right = 0; c.style.bottom = 0;
            parent.Add(c);
            return c;
        }

        private static Texture2D MakeVignette()
        {
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x / (n - 1f) - 0.5f) * 2f, dy = (y / (n - 1f) - 0.5f) * 2f;
                    float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.9f);
                    tex.SetPixel(x, y, new Color(0f, 0f, 0.02f, d * d * 0.7f));
                }
            tex.Apply();
            return tex;
        }

        private static Color HpColor(double ratio) => Layout.HpBandFor(ratio) switch
        {
            HpBand.High => Palette.Hex("5DAE6A"),
            HpBand.Mid => Palette.Hex("D49A3E"),
            _ => Palette.Hex("C2453F"),
        };

        private static Color RoleColor(UnitState a) =>
            a.Role == "tank" ? Palette.Tank : a.Role == "healer" ? Palette.Healer : (a.Id == "dps2" ? Palette.Mage : Palette.Archer);

        private bool Allowed(UiAction action) => InputGate.Allows(_flow.Screen, _ctl.State, action);

        // ---- Cycle de vie ------------------------------------------------------------------------

        public void Refresh()
        {
            bool active = _flow.Screen == AppScreen.Battle && _ctl.Battle != null;
            var display = active ? DisplayStyle.Flex : DisplayStyle.None;
            _hud.style.display = display;
            _modal.style.display = display;
            _root.Vignette.style.display = display;
            if (!active)
            {
                BackdropAlpha = 0f;
                _root.Danger.style.display = DisplayStyle.None;
                _builtFor = null;
                _modalSignature = "";
                return;
            }
            if (!ReferenceEquals(_builtFor, _ctl.Battle) || _skillViews.Count != _ctl.Skills.Count) BuildHud();
            BindHud();
            RefreshModal();
        }

        // ---- HUD : construction -------------------------------------------------------------------

        private void BuildHud()
        {
            _builtFor = _ctl.Battle;
            _hud.Clear();
            _cards.Clear();
            _skillViews.Clear();
            _modalSignature = "";
            var allies = _ctl.Battle.GetAllies();

            // Boss
            var bar = Ui.R(Layout.BossHpBar);
            _bossTitle = Ui.Text(_hud, new Rect(bar.x, (float)Layout.Zones.TopBar.Y + 2, bar.width, 28), "", Layout.Font.Strong, Color.white, TextAnchor.MiddleCenter, true);
            Ui.Box(_hud, bar, new Color(0, 0, 0, 0.55f), 6, new Color(0, 0, 0, 0.6f), 2);
            _bossFill = Ui.Box(_hud, bar, Palette.Hex("D9455F"), 6);
            _enrage = Ui.Text(_hud, new Rect(24, 14, 270, 30), "", Layout.Font.Strong, Palette.Danger, TextAnchor.MiddleLeft, true);
            var pause = Ui.R(Layout.PauseButton);
            var pauseBox = Ui.Box(_hud, pause, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.85f), 8, Ui.PanelStroke, 1.5f);
            _pauseLabel = Ui.Text(pauseBox, new Rect(0, 0, pause.width, pause.height), "II", Layout.Font.Banner, Color.white, TextAnchor.MiddleCenter, true);
            Ui.OnClick(pauseBox, () => { if (Allowed(UiAction.TogglePause)) _ctl.TogglePause(); });
            float w = (float)Layout.GameW;
            var line = new Rect(0, (float)Layout.Zones.Boss.Y + 2, w, 26);
            _telegraph = Ui.Text(_hud, line, "", Layout.Font.Strong, Palette.Danger, TextAnchor.MiddleCenter, true);
            _lastAction = Ui.Text(_hud, line, "", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
            var gauge = new Rect(w / 2 - 130, line.y + 30, 260, 10);
            _gaugeBg = Ui.Box(_hud, gauge, new Color(0, 0, 0, 0.6f), 5);
            _gaugeFill = Ui.Box(_hud, gauge, Palette.Danger, 5);

            // Équipe
            var rects = Layout.TeamCardRects(allies.Count);
            for (int i = 0; i < allies.Count; i++)
            {
                var a = allies[i];
                var r = Ui.R(rects[i]);
                var v = new CardView { Id = a.Id, Rect = r };
                v.Glow = Ui.Box(_hud, new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Color.clear, 14, new Color(Ui.Selected.r, Ui.Selected.g, Ui.Selected.b, 0.3f), 8);
                v.Panel = Ui.Box(_hud, r, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.88f), 8, Ui.PanelStroke, 1.5f);
                v.RoleBar = Ui.Box(v.Panel, new Rect(8, 8, r.width - 16, 5), RoleColor(a), 3);
                Ui.Text(v.Panel, new Rect(40, 14, r.width - 48, 24), a.Name, Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);
                Ui.Text(v.Panel, new Rect(40, 38, r.width - 48, 20), a.Role == "tank" ? "Tank" : a.Role == "healer" ? "Soin" : "Dégâts", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleLeft);
                var hpBar = new Rect(12, 62, r.width - 24, 20);
                Ui.Box(v.Panel, hpBar, new Color(0, 0, 0, 0.55f), 5);
                v.HpFill = Ui.Box(v.Panel, hpBar, Color.green, 5);
                v.HpText = Ui.Text(v.Panel, new Rect(0, 84, r.width, 26), "", Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true);
                v.Threat = Ui.Text(v.Panel, new Rect(r.width - 74, 10, 66, 20), "Menace", Layout.Font.Small, Palette.Danger, TextAnchor.MiddleRight, true);
                v.Shield = Ui.Text(v.Panel, new Rect(8, 110, r.width * 0.5f, 20), "", Layout.Font.Small, Palette.Shield, TextAnchor.MiddleLeft);
                v.PillBox = Ui.Box(v.Panel, new Rect(r.width * 0.5f, 108, r.width * 0.5f - 8, 22), Palette.Hex("5B2F86"), 8);
                v.Pill = Ui.Text(v.PillBox, new Rect(0, 0, r.width * 0.5f - 8, 22), "", Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
                KeyCap(v.Panel, new Rect(8, 8, 24, 24), BattleKeys.AllyLabel(i));
                v.Guide = MakeGuide(_hud, r, "1");
                string id = a.Id;
                Ui.OnClick(v.Panel, () => { if (Allowed(UiAction.TapAlly)) _ctl.TapAlly(id); });
                _cards.Add(v);
            }

            // Mana
            var strip = Ui.R(Layout.Zones.Strip);
            Ui.Box(_hud, strip, new Color(0, 0, 0, 0.55f), 8);
            _manaFill = Ui.Box(_hud, strip, Palette.Hex("4AA8FF"), 8);
            _manaLabel = Ui.Text(_hud, strip, "", Layout.Font.Body, Color.white, TextAnchor.MiddleCenter, true);

            // Cible et incantation
            var z = Ui.R(Layout.Zones.Band);
            _targetLabel = Ui.Text(_hud, z, "", Layout.Font.Body, Color.white, TextAnchor.MiddleCenter, true);
            _castLabel = Ui.Text(_hud, new Rect(z.x, z.y - 4, z.width, 24), "", Layout.Font.Body, Color.white, TextAnchor.MiddleCenter, true);
            var castBar = new Rect(z.x + z.width * 0.2f, z.y + 24, z.width * 0.6f, 16);
            _castBarBg = Ui.Box(_hud, castBar, new Color(0, 0, 0, 0.6f), 6, new Color(1f, 1f, 1f, 0.35f), 1);
            _castBarFill = Ui.Box(_hud, castBar, Palette.Heal, 6);

            // Sorts : icônes seules
            var skills = _ctl.Skills;
            var sr = Layout.SkillButtonRects(skills.Count);
            for (int i = 0; i < skills.Count; i++)
            {
                var s = skills[i];
                var r = Ui.R(sr[i]);
                var accent = IconKit.Accent(s.Id);
                var v = new SkillView { Skill = s, Rect = r };
                v.Root = Ui.Box(_hud, r, Palette.Hex("22273B"), 12, Ui.ButtonStroke, 2);
                v.Root.pickingMode = PickingMode.Position;
                v.Icon = Ui.Box(v.Root, new Rect(r.width / 2 - 44, 14, 88, 88), Color.clear, 0);
                v.Icon.style.backgroundImage = new StyleBackground(IconKit.For(s.Id));
                v.Icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                var manaPill = Ui.Box(v.Root, new Rect(r.width - 46, r.height - 26, 40, 20), new Color(0, 0, 0, 0.6f), 8);
                v.Mana = Ui.Text(manaPill, new Rect(0, 0, 40, 20), Format.Number(s.ManaCost), Layout.Font.Small, Palette.Hex("8FCBFF"), TextAnchor.MiddleCenter, true);
                v.Cooldown = Ui.Box(v.Root, new Rect(0, 0, r.width, r.height), new Color(0, 0, 0, 0.55f), 12);
                v.CooldownText = Ui.Text(v.Root, new Rect(0, r.height / 2 - 22, r.width, 44), "", Layout.Font.Banner, Palette.Hex("FF9D9D"), TextAnchor.MiddleCenter, true);
                KeyCap(v.Root, new Rect(8, 8, 24, 24), BattleKeys.SkillLabel(i));
                v.Guide = MakeGuide(_hud, r, "2");
                var skill = s;
                var root = v.Root;
                root.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button != 0 || !Allowed(UiAction.TapSkill)) return;
                    _ctl.PressSkill(skill, true); // lancer tout de suite ; maintenu, le sort s'enchaîne
                    root.CapturePointer(e.pointerId);
                });
                root.RegisterCallback<PointerUpEvent>(e => { _ctl.ReleaseSkill(); if (root.HasPointerCapture(e.pointerId)) root.ReleasePointer(e.pointerId); });
                root.RegisterCallback<PointerCancelEvent>(_ => _ctl.ReleaseSkill());
                _skillViews.Add(v);
            }
        }

        private static void KeyCap(VisualElement parent, Rect cap, string? label)
        {
            if (label == null) return;
            var box = Ui.Box(parent, cap, new Color(0f, 0f, 0f, 0.6f), 5, new Color(1f, 1f, 1f, 0.5f), 1);
            Ui.Text(box, new Rect(0, 0, cap.width, cap.height), label, Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
        }

        /// <summary>Anneau pulsant et pastille numérotée qui guident le premier geste (masqués par défaut).</summary>
        private static VisualElement MakeGuide(VisualElement parent, Rect r, string number)
        {
            var g = Ui.Box(parent, new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Color.clear, 14, Ui.Selected, 4);
            var badge = Ui.Box(g, new Rect(r.width - 22, -8, 34, 34), Ui.Selected, 17);
            Ui.Text(badge, new Rect(0, 0, 34, 34), number, Layout.Font.Title, Palette.Hex("1C1A36"), TextAnchor.MiddleCenter, true);
            g.style.display = DisplayStyle.None;
            return g;
        }

        private static void Pulse(VisualElement guide, Rect r)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            Ui.Place(guide, new Rect(r.x - 4 - pulse * 3, r.y - 4 - pulse * 3, r.width + 8 + pulse * 6, r.height + 8 + pulse * 6));
            Ui.Stroke(guide, new Color(Ui.Selected.r, Ui.Selected.g, Ui.Selected.b, 0.55f + 0.4f * pulse), 4);
        }

        // ---- HUD : liaison à l'état ---------------------------------------------------------------

        private static void Width(VisualElement e, float px) => e.style.width = px;

        private void BindHud()
        {
            var b = _ctl.Battle;
            var allies = b.GetAllies();

            double hp = b.GetBossHp(), max = b.GetBossMaxHp();
            var phase = b.GetBossPhase();
            var bar = Ui.R(Layout.BossHpBar);
            _bossTitle.text = $"{b.GetBossName()}  {Format.Number(hp)} / {Format.Number(max)}" + (phase.Index > 0 ? $"  · {phase.Name}" : "");
            _bossFill.style.display = hp > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Width(_bossFill, Mathf.Max(8f, bar.width * (float)(hp / max)));
            _bossFill.style.backgroundColor = phase.Index > 0 ? Palette.Hex("FF6A3D") : Palette.Hex("D9455F");

            int enrage = b.GetEnrageLevel();
            _enrage.style.display = enrage > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (enrage > 0)
            {
                int pct = (int)((b.GetEnrageMultiplier() - 1) * 100 + 1e-6); // tronqué
                _enrage.text = $"ENRAGÉ  +{pct} %";
                _enrage.style.color = new Color(Palette.Danger.r, Palette.Danger.g, Palette.Danger.b, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f));
            }
            _pauseLabel.text = _ctl.Paused ? ">" : "II";

            var tele = b.GetTelegraph();
            bool big = tele != null && tele.Type == "bigAttack";
            _telegraph.style.display = big ? DisplayStyle.Flex : DisplayStyle.None;
            _gaugeBg.style.display = big ? DisplayStyle.Flex : DisplayStyle.None;
            _gaugeFill.style.display = big ? DisplayStyle.Flex : DisplayStyle.None;
            _lastAction.style.display = !big && !string.IsNullOrEmpty(_ctl.LastAction) ? DisplayStyle.Flex : DisplayStyle.None;
            if (big)
            {
                _telegraph.text = $"ATTAQUE DE ZONE dans {Format.Seconds(tele!.MsRemaining)}";
                Width(_gaugeFill, Mathf.Max(6f, 260f * (float)(tele.MsRemaining / System.Math.Max(1.0, tele.TotalMs))));
            }
            else _lastAction.text = _ctl.LastAction;
            BindDanger(tele);

            // Équipe
            string? threatId = b.GetTopThreatId();
            string? guideAlly = !_ctl.HasCast && _ctl.Selection.Selected == null ? GuideAllyId(allies) : null;
            for (int i = 0; i < _cards.Count && i < allies.Count; i++)
            {
                var v = _cards[i];
                var a = allies[i];
                bool selected = _ctl.Selection.Selected == a.Id;
                v.Panel.style.opacity = a.Alive ? 1f : 0.5f;
                v.Glow.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
                Ui.Stroke(v.Panel, selected ? Ui.Selected : Ui.PanelStroke, selected ? 3 : 1.5f);
                double ratio = a.MaxHp > 0 ? System.Math.Max(0, a.Hp / a.MaxHp) : 0;
                v.HpFill.style.display = ratio > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                Width(v.HpFill, Mathf.Max(6f, (v.Rect.width - 24) * (float)ratio));
                v.HpFill.style.backgroundColor = HpColor(ratio);
                v.HpText.text = a.Alive ? Format.Ratio(a.Hp, a.MaxHp) : "K.O.";
                v.Threat.style.display = a.Alive && a.Id == threatId && a.Role != "healer" ? DisplayStyle.Flex : DisplayStyle.None;
                v.Shield.style.display = a.Shield > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                if (a.Shield > 0) v.Shield.text = $"Bouclier {Format.Number(a.Shield)}";
                bool hasEffect = a.Effects.Count > 0;
                v.PillBox.style.display = hasEffect ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasEffect) v.Pill.text = $"{a.Effects[0].Name} {Format.Seconds(a.Effects[0].MsRemaining)}";
                bool guide = guideAlly == a.Id && a.Alive;
                v.Guide.style.display = guide ? DisplayStyle.Flex : DisplayStyle.None;
                if (guide) Pulse(v.Guide, v.Rect);
            }

            // Mana
            var healer = allies.FirstOrDefault(x => x.Id == BattleController.HealerId);
            double mana = healer?.Mana ?? 0, maxMana = healer?.MaxMana ?? 1;
            var strip = Ui.R(Layout.Zones.Strip);
            _manaFill.style.display = mana > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Width(_manaFill, Mathf.Max(10f, strip.width * (float)(mana / maxMana)));
            _manaLabel.text = $"Mana {Format.Ratio(mana, maxMana)}";

            // Cible / incantation
            var cast = b.GetCast();
            _castLabel.style.display = _castBarBg.style.display = _castBarFill.style.display = cast != null ? DisplayStyle.Flex : DisplayStyle.None;
            _targetLabel.style.display = cast == null ? DisplayStyle.Flex : DisplayStyle.None;
            var band = Ui.R(Layout.Zones.Band);
            if (cast != null)
            {
                string skillName = _ctl.Skills.FirstOrDefault(s => s.Id == cast.SkillId)?.Name ?? cast.SkillId;
                string? targetName = allies.FirstOrDefault(a => a.Id == cast.TargetId)?.Name;
                _castLabel.text = targetName != null ? $"{skillName} → {targetName}" : skillName;
                Width(_castBarFill, Mathf.Max(6f, band.width * 0.6f * (float)cast.Progress));
            }
            else
            {
                string? selectedName = allies.FirstOrDefault(x => x.Id == _ctl.Selection.Selected)?.Name;
                if (_ctl.HintActive) Ui.SetText(_targetLabel, "Choisissez d'abord un allié !", Layout.Font.Title, Palette.Danger, TextAnchor.MiddleCenter, true);
                else if (selectedName != null) Ui.SetText(_targetLabel, $"Cible : {selectedName}", Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true);
                else Ui.SetText(_targetLabel, "Touchez un allié (à gauche) pour le cibler", Layout.Font.Body, Ui.Muted, TextAnchor.MiddleCenter);
            }

            // Sorts
            foreach (var v in _skillViews)
            {
                var s = v.Skill;
                bool usable = b.CanUseSkillNow(BattleController.HealerId, s.Id);
                double cd = b.GetCooldownRemaining(BattleController.HealerId, s.Id);
                bool enoughMana = mana >= s.ManaCost;
                var accent = IconKit.Accent(s.Id);
                v.Root.style.backgroundColor = usable ? Palette.Hex("22273B") : Palette.Hex("141621");
                Ui.Stroke(v.Root, usable ? accent : Ui.PanelStroke, usable ? 2.5f : 1.5f);
                v.Icon.style.unityBackgroundImageTintColor = usable ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f);
                v.Mana.style.color = enoughMana ? Palette.Hex("8FCBFF") : Palette.Damage;
                v.Cooldown.style.display = cd > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                if (cd > 0)
                {
                    double total = System.Math.Max(1.0, s.CooldownMs);
                    Ui.Place(v.Cooldown, new Rect(0, 0, v.Rect.width, v.Rect.height * Mathf.Clamp01((float)(cd / total))));
                    v.CooldownText.text = Format.Seconds(cd);
                }
                v.CooldownText.style.display = cd >= 100 ? DisplayStyle.Flex : DisplayStyle.None;
                bool guide = !_ctl.HasCast && _ctl.Selection.Selected != null && s.Id == "heal_single";
                v.Guide.style.display = guide ? DisplayStyle.Flex : DisplayStyle.None;
                if (guide) Pulse(v.Guide, v.Rect);
            }
        }

        /// <summary>Allié à soigner en premier pour le guidage : le plus abîmé, le tank à égalité.</summary>
        private static string? GuideAllyId(List<UnitState> allies)
        {
            var list = allies.Where(a => a.Alive && a.Role != "healer").OrderBy(a => a.Hp / a.MaxHp).ThenBy(a => a.Role == "tank" ? 0 : 1).ToList();
            return list.Count > 0 ? list[0].Id : null;
        }

        private void BindDanger(Telegraph? tele)
        {
            var danger = _root.Danger;
            if (tele == null || tele.Type != "bigAttack") { danger.style.display = DisplayStyle.None; return; }
            danger.style.display = DisplayStyle.Flex;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 14f);
            float urgency = 1f - Mathf.Clamp01((float)(tele.MsRemaining / Mathf.Max(1f, (float)tele.TotalMs)));
            var c = new Color(Palette.Danger.r, Palette.Danger.g, Palette.Danger.b, (0.25f + 0.55f * urgency) * pulse);
            Ui.Stroke(danger, c, 10f + 8f * urgency);
        }

        // ---- Fenêtres modales : départ, pause, fin --------------------------------------------------

        private enum ModalKind { None, Start, Pause, End }

        private void RefreshModal()
        {
            var result = _ctl.Battle.GetResult();
            ModalKind kind = !_ctl.Started ? ModalKind.Start : result != BattleResults.Ongoing ? ModalKind.End : _ctl.Paused ? ModalKind.Pause : ModalKind.None;
            BackdropAlpha = kind switch { ModalKind.Start => 0.86f, ModalKind.End => 0.8f, ModalKind.Pause => 0.93f, _ => 0f };
            string sig = kind + "|" + (kind == ModalKind.End ? (_flow.LastReward?.GetHashCode() ?? 0) + "|" + result : "");
            if (sig == _modalSignature) return;
            _modalSignature = sig;
            _modal.Clear();
            switch (kind)
            {
                case ModalKind.Start: BuildStart(); break;
                case ModalKind.Pause: BuildPause(); break;
                case ModalKind.End: BuildEnd(); break;
            }
        }

        private VisualElement M(Rect r, Color fill, float radius = 10f, Color? stroke = null, float w = 0) => Ui.Box(_modal, r, fill, radius, stroke, w);
        private Label T(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false) => Ui.Text(_modal, r, text, size, color, anchor, bold, wrap);
        private VisualElement B(Rect r, string text, int size, Color fill, Color stroke, UiAction action, System.Action run) =>
            Ui.Button(_modal, r, text, size, fill, stroke, () => { if (Allowed(action)) run(); });

        private void BuildStart()
        {
            float w = (float)Layout.GameW;
            var level = _flow.CurrentLevel;
            T(new Rect(0, 60, w, 56), level?.Name ?? "Healer Game", 44, Color.white, TextAnchor.MiddleCenter, true);
            T(new Rect(0, 120, w, 30), "Soignez votre équipe face au " + _ctl.Battle.GetBossName(), Layout.Font.Strong, Ui.Muted, TextAnchor.MiddleCenter);
            var panel = new Rect(w / 2 - 300, 180, 600, 260);
            M(panel, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.96f), 14, Ui.PanelStroke, 2);
            string[] tips =
            {
                "1  Touchez un allié à GAUCHE pour le cibler",
                "2  Touchez un sort à DROITE pour le lancer sur lui",
                "Soin de zone : sans cible, pour toute l'équipe",
                "Bouclier : à poser AVANT l'attaque annoncée",
                "Purge : retire poisons et brûlures",
                "Le mana est limité : ne le gaspillez pas",
            };
            for (int i = 0; i < tips.Length; i++)
                T(new Rect(panel.x + 22, panel.y + 14 + i * 38, panel.width - 44, 34), tips[i], Layout.Font.Body, i < 2 ? Ui.Selected : Color.white, TextAnchor.MiddleLeft, i < 2);
            B(Ui.R(Layout.StartButton), "Jouer", 26, Ui.PrimaryFill, Ui.PrimaryStroke, UiAction.StartFight, _ctl.StartFight);
            if (Keyboard.current != null)
            {
                string sorts = string.Join(" ", new[] { 0, 1, 2, 3 }.Select(BattleKeys.SkillLabel));
                T(new Rect(0, 556, w, 24), "Clavier : 1-4 cibler · " + sorts + " sorts · Tab suivant", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
                T(new Rect(0, 580, w, 24), "Espace : jouer / pause · Échap : retour · M : son · F8 : noter un retour", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
            }
            else T(new Rect(0, 560, w, 24), "Touchez Jouer pour commencer", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
            B(Ui.R(Layout.BackButton), "← Carte", Layout.Font.Body, Ui.ButtonFill, Ui.ButtonStroke, UiAction.BackToMap, _flow.LeaveBattle);
        }

        private void BuildPause()
        {
            float w = (float)Layout.GameW;
            T(new Rect(0, 14, w, 56), "PAUSE", 44, Color.white, TextAnchor.MiddleCenter, true);
            var skills = _ctl.Skills;
            var baseSkills = _flow.Content.Skills;
            int n = System.Math.Max(1, skills.Count);
            const float gap = 8f, top = 76f, height = 250f;
            float cardW = (w - 24f - gap * (n - 1)) / n;
            for (int i = 0; i < skills.Count; i++)
            {
                var s = skills[i];
                var baseSkill = baseSkills.FirstOrDefault(x => x.Id == s.Id) ?? s;
                float x = 12f + i * (cardW + gap);
                M(new Rect(x, top, cardW, height), new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.96f), 12, IconKit.Accent(s.Id), 2);
                var icon = M(new Rect(x + 12, top + 12, 48, 48), Color.clear, 0);
                icon.style.backgroundImage = new StyleBackground(IconKit.For(s.Id));
                icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                T(new Rect(x + 68, top + 12, cardW - 80, 48), s.Name, Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);
                T(new Rect(x + 12, top + 64, cardW - 24, 44), s.Description ?? "", Layout.Font.Small, Ui.Muted, TextAnchor.UpperLeft, false, true);
                float colX = x + 96, colW = (cardW - 96 - 12) / 2f;
                T(new Rect(colX, top + 108, colW, 18), "Base", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleLeft, true);
                T(new Rect(colX + colW, top + 108, colW, 18), "Avec bonus", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleLeft, true);
                float y = top + 130;
                foreach (var stat in SkillDescriber.Stats(baseSkill, s))
                {
                    T(new Rect(x + 12, y, 84, 20), stat.Label, Layout.Font.Small, Ui.Muted, TextAnchor.MiddleLeft);
                    if (!stat.Changed && stat.Base.Length > 16)
                        T(new Rect(colX, y, colW * 2, 20), stat.Base, Layout.Font.Small, Color.white, TextAnchor.MiddleLeft);
                    else
                    {
                        T(new Rect(colX, y, colW, 20), stat.Base, Layout.Font.Small, Color.white, TextAnchor.MiddleLeft);
                        T(new Rect(colX + colW, y, colW, 20), stat.Current, Layout.Font.Small, stat.Changed ? Palette.Heal : Color.white, TextAnchor.MiddleLeft, stat.Changed);
                    }
                    y += 22;
                }
            }
            Debug.Log($"[Healer] fiche des sorts : {skills.Count} sorts");
            B(Ui.R(Layout.PauseResume), "Reprendre", Layout.Font.Title, Ui.PrimaryFill, Ui.PrimaryStroke, UiAction.TogglePause, _ctl.TogglePause);
            B(Ui.R(Layout.PauseLeave), "Quitter le niveau", Layout.Font.Body, Ui.ButtonFill, Ui.ButtonStroke, UiAction.BackToMap, _flow.LeaveBattle);
            T(new Rect(0, 530, w, 24), "Base : le sort sans aucun bonus  ·  Avec bonus : équipement et talents actifs (Atelier)", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleCenter);
        }

        private void BuildEnd()
        {
            var result = _ctl.Battle.GetResult();
            bool win = result == BattleResults.Victory;
            float w = (float)Layout.GameW;
            var reward = _flow.LastReward;
            var level = _flow.CurrentLevel;
            T(new Rect(0, 30, w, 54), win ? "Victoire !" : "Défaite…", 44, win ? Palette.Heal : Palette.Damage, TextAnchor.MiddleCenter, true);
            if (win && reward != null) Ui.Stars(_modal, new Rect(w / 2 - 200, 92, 400, 64), reward.Stars, 56);

            var s = _ctl.Stats;
            var panel = Ui.R(Layout.EndStatsPanel);
            M(panel, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.96f), 14, Ui.PanelStroke, 2);
            BuildDamagePanel();
            string[] labels = { "Durée du combat", "Soins effectifs", "Dégâts encaissés", "dont absorbés par boucliers", "Sorts lancés", "Alliés K.O.", "Poisons et brûlures purgés" };
            string[] values =
            {
                Format.Seconds(s.DurationMs), Format.Number(s.HealingDone), Format.Number(s.DamageTaken),
                Format.Number(s.DamageAbsorbed), s.Casts.ToString(), s.Deaths.ToString(), s.Purges.ToString(),
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var row = new Rect(panel.x + 20, panel.y + 12 + i * 31, panel.width - 40, 28);
                T(row, labels[i], Layout.Font.Body, i == 3 ? Ui.Muted : Color.white, TextAnchor.MiddleLeft);
                T(row, values[i], Layout.Font.Body, i == 5 && s.Deaths > 0 ? Palette.Damage : Palette.Heal, TextAnchor.MiddleRight, true);
            }

            float y = 418;
            if (win && reward != null)
            {
                T(new Rect(0, y, w, 30), "+" + Format.Number(reward.GoldGained) + " or" + (reward.FirstClear ? "  ·  première victoire !" : ""), Layout.Font.Title, Ui.Gold, TextAnchor.MiddleCenter, true);
                y += 30;
                if (reward.NewStars > 0) { T(new Rect(0, y, w, 24), reward.NewStars + (reward.NewStars > 1 ? " nouvelles étoiles" : " nouvelle étoile"), Layout.Font.Body, Ui.Selected, TextAnchor.MiddleCenter); y += 24; }
                if (reward.NewBestTime) { T(new Rect(0, y, w, 24), "Nouveau record de temps !", Layout.Font.Body, Ui.Selected, TextAnchor.MiddleCenter); y += 24; }
                foreach (var id in reward.UnlockedLevelIds)
                {
                    T(new Rect(0, y, w, 24), "Niveau débloqué : " + _flow.Content.LevelById(id).Name, Layout.Font.Body, Palette.Heal, TextAnchor.MiddleCenter, true);
                    y += 24;
                }
                if (level != null && reward.Stars < 3)
                    T(new Rect(0, Mathf.Max(y + 6, 520), w, 40),
                        "★ victoire   ★★ sans allié K.O.   ★★★ et moins de " + Format.Number(level.ThreeStarMaxDamageTaken) + " dégâts encaissés",
                        Layout.Font.Small, Ui.Muted, TextAnchor.UpperCenter);
            }
            else if (!win)
                T(new Rect(w / 2 - 320, 430, 640, 60), "Astuce : posez un Bouclier pendant l'annonce de l'attaque de zone,\net Purgez poisons et brûlures dès qu'ils apparaissent.", Layout.Font.Body, Ui.Muted, TextAnchor.UpperCenter, false, true);

            var next = win ? _flow.NextLevel : null;
            var actions = new List<(string label, UiAction action, bool primary)> { ("Recommencer", UiAction.Restart, !win || next == null) };
            if (next != null) actions.Add(("Niveau suivant", UiAction.NextLevel, true));
            actions.Add(("Carte", UiAction.BackToMap, false));
            var rects = Layout.EndButtonRects(actions.Count);
            for (int i = 0; i < actions.Count; i++)
            {
                var (label, action, primary) = actions[i];
                B(Ui.R(rects[i]), label, Layout.Font.Title, primary ? Ui.PrimaryFill : Ui.ButtonFill, primary ? Ui.PrimaryStroke : Ui.ButtonStroke, action, () =>
                {
                    if (action == UiAction.Restart) _flow.Restart();
                    else if (action == UiAction.NextLevel) _flow.NextLevelNow();
                    else _flow.LeaveBattle();
                });
            }
        }

        /// <summary>Dégâts infligés au boss par chaque membre : barre proportionnelle, valeur tronquée et part en %.</summary>
        private void BuildDamagePanel()
        {
            var p = Ui.R(Layout.EndDamagePanel);
            M(p, new Color(Ui.Panel.r, Ui.Panel.g, Ui.Panel.b, 0.96f), 14, Ui.PanelStroke, 2);
            var s = _ctl.Stats;
            T(new Rect(p.x + 20, p.y + 8, p.width - 40, 28), "Dégâts infligés au boss", Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);
            var members = _ctl.Battle.GetAllies().Where(a => s.DamageByAlly.ContainsKey(a.Id)).OrderByDescending(a => s.DamageByAlly[a.Id]).ToList();
            if (members.Count == 0)
            {
                T(new Rect(p.x + 20, p.y + 60, p.width - 40, 30), "Aucun dégât infligé", Layout.Font.Body, Ui.Muted, TextAnchor.MiddleLeft);
                return;
            }
            double max = s.DamageByAlly[members[0].Id];
            float valueW = 150f, nameW = 110f;
            float barX = p.x + 20 + nameW, barMax = p.width - 40 - nameW - valueW;
            for (int i = 0; i < members.Count; i++)
            {
                var a = members[i];
                double dmg = s.DamageByAlly[a.Id];
                float y = p.y + 46 + i * 42;
                T(new Rect(p.x + 20, y, nameW, 30), a.Name, Layout.Font.Body, Color.white, TextAnchor.MiddleLeft, true);
                var bar = new Rect(barX, y + 5, barMax, 20);
                M(bar, new Color(0, 0, 0, 0.5f), 5);
                M(new Rect(bar.x, bar.y, Mathf.Max(4f, bar.width * (float)(dmg / max)), bar.height), RoleColor(a), 5);
                int percent = (int)(s.DamageShare(a.Id) * 100); // tronqué, jamais arrondi vers le haut
                T(new Rect(barX + barMax + 8, y, valueW - 8, 30), $"{Format.Number(dmg)}  ({percent} %)", Layout.Font.Body, Color.white, TextAnchor.MiddleRight, true);
            }
            T(new Rect(p.x + 20, p.yMax - 32, p.width - 40, 26), $"Critiques : {s.Crits}  ·  Esquives : {s.Dodges}  ·  Total : {Format.Number(s.DamageToBoss)}", Layout.Font.Small, Ui.Muted, TextAnchor.MiddleRight);
        }
    }
}
