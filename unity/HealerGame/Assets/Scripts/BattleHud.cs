using System.Linq;
using Healer.Combat;
using Healer.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using Rect = UnityEngine.Rect;

namespace Healer.Client
{
    /// <summary>
    /// Interface de combat (portrait, zone du pouce) dessinée avec le module intégré IMGUI : aucun paquet à
    /// télécharger. Toute la mise en page vient de Healer.Ui.Layout (cibles ≥ 48 px, texte ≥ 14 px) et les
    /// valeurs sont TRONQUÉES via Healer.Ui.Format. Aucune règle de jeu ici : uniquement lecture d'état et gestes.
    /// </summary>
    public sealed class BattleHud : MonoBehaviour
    {
        private BattleController _ctl = null!;
        private GameFlow _flow = null!;
        private GUIStyle _label = null!;

        private static readonly Color Panel = Palette.Hex("161926");
        private static readonly Color PanelStroke = Palette.Hex("3A4058");
        private static readonly Color Selected = Palette.Hex("E0BE6A");
        private static readonly Color Muted = Palette.Hex("9A9FB4");

        public void Init(BattleController controller, GameFlow flow)
        {
            _ctl = controller;
            _flow = flow;
        }

        private static Rect R(Healer.Ui.Rect r) => new Rect((float)r.X, (float)r.Y, (float)r.W, (float)r.H);

        private static Color HpColor(double ratio) => Layout.HpBandFor(ratio) switch
        {
            HpBand.High => Palette.Hex("5DAE6A"),
            HpBand.Mid => Palette.Hex("D49A3E"),
            _ => Palette.Hex("C2453F"),
        };

        private void OnGUI()
        {
            if (_ctl == null || _ctl.Battle == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { wordWrap = false, clipping = TextClipping.Overflow };
            }
            ScreenMap.Refresh();
            DrawCinemaVignette();
            if (_flow.Screen != AppScreen.Battle) return;
            DrawDangerVignette();
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(ScreenMap.OffsetX, ScreenMap.OffsetY, 0), Quaternion.identity, new Vector3(ScreenMap.Scale, ScreenMap.Scale, 1));
            DrawBoss();
            DrawTeam();
            DrawStrip();
            DrawTarget();
            DrawSkills();
            DrawEnd();
            DrawStart();
            GUI.matrix = previous;
        }

        // ---- Primitives de dessin ----------------------------------------------------------------

        private static void Fill(Rect r, Color c, float radius = 10f) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, new Vector4(radius, radius, radius, radius));

        private static void Outline(Rect r, Color c, float width, float radius = 10f) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, new Vector4(width, width, width, width), new Vector4(radius, radius, radius, radius));

        private void Text(Rect r, string s, int size, Color c, TextAnchor anchor, bool bold = false)
        {
            _label.fontSize = size;
            _label.alignment = anchor;
            _label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            _label.normal.textColor = new Color(0, 0, 0, 0.75f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, _label);
            _label.normal.textColor = c;
            GUI.Label(r, s, _label);
        }

        /// <summary>Zone cliquable. Passe par InputGate : hors de son état, un élément ne capte AUCUN clic (un écran modal recouvre les cartes).</summary>
        private bool Hit(Rect r, UiAction action) => InputGate.Allows(_flow.Screen, _ctl.State, action) && GUI.Button(r, GUIContent.none, GUIStyle.none);

        private Texture2D? _vignette;

        /// <summary>Assombrit doucement les bords de l'écran : concentre le regard au centre, ambiance plus sérieuse.</summary>
        private void DrawCinemaVignette()
        {
            if (_vignette == null)
            {
                const int n = 96;
                _vignette = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x / (n - 1f) - 0.5f) * 2f, dy = (y / (n - 1f) - 0.5f) * 2f;
                        float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.9f);
                        _vignette.SetPixel(x, y, new Color(0f, 0f, 0.02f, d * d * 0.7f));
                    }
                _vignette.Apply();
            }
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _vignette, ScaleMode.StretchToFill);
        }

        private void DrawDangerVignette()
        {
            var tele = _ctl.Battle.GetTelegraph();
            if (tele == null || tele.Type != "bigAttack") return;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 14f);
            float urgency = 1f - Mathf.Clamp01((float)(tele.MsRemaining / Mathf.Max(1f, (float)tele.TotalMs)));
            var c = new Color(Palette.Danger.r, Palette.Danger.g, Palette.Danger.b, (0.25f + 0.55f * urgency) * pulse);
            float w = 10f + 8f * urgency;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, new Vector4(w, w, w, w), Vector4.zero);
        }

        // ---- Boss --------------------------------------------------------------------------------

        private void DrawBoss()
        {
            var b = _ctl.Battle;
            double hp = b.GetBossHp(), max = b.GetBossMaxHp();
            var phase = b.GetBossPhase();
            var bar = R(Layout.BossHpBar);
            string title = $"{b.GetBossName()}  {Format.Number(hp)} / {Format.Number(max)}" + (phase.Index > 0 ? $"  · {phase.Name}" : "");
            Text(new Rect(bar.x, (float)Layout.Zones.TopBar.Y + 2, bar.width, 28), title, Layout.Font.Strong, Color.white, TextAnchor.MiddleCenter, true);
            Fill(bar, new Color(0, 0, 0, 0.55f), 6);
            if (hp > 0) Fill(new Rect(bar.x, bar.y, Mathf.Max(8f, bar.width * (float)(hp / max)), bar.height), phase.Index > 0 ? Palette.Hex("FF6A3D") : Palette.Hex("D9455F"), 6);
            Outline(bar, new Color(0, 0, 0, 0.6f), 2, 6);

            var pause = R(Layout.PauseButton);
            Fill(pause, new Color(Panel.r, Panel.g, Panel.b, 0.85f), 8);
            Outline(pause, PanelStroke, 1.5f, 8);
            Text(pause, _ctl.Paused ? ">" : "II", Layout.Font.Banner, Color.white, TextAnchor.MiddleCenter, true);
            if (Hit(pause, UiAction.TogglePause)) _ctl.TogglePause();

            var tele = b.GetTelegraph();
            var line = new Rect(0, (float)Layout.Zones.Boss.Y + 2, (float)Layout.GameW, 26);
            if (tele != null && tele.Type == "bigAttack")
            {
                Text(line, $"ATTAQUE DE ZONE dans {Format.Seconds(tele.MsRemaining)}", Layout.Font.Strong, Palette.Danger, TextAnchor.MiddleCenter, true);
                var gauge = new Rect((float)Layout.GameW / 2 - 130, line.y + 30, 260, 10);
                Fill(gauge, new Color(0, 0, 0, 0.6f), 5);
                Fill(new Rect(gauge.x, gauge.y, Mathf.Max(6f, gauge.width * (float)(tele.MsRemaining / System.Math.Max(1.0, tele.TotalMs))), gauge.height), Palette.Danger, 5);
            }
            else if (!string.IsNullOrEmpty(_ctl.LastAction))
                Text(line, _ctl.LastAction, Layout.Font.Small, Muted, TextAnchor.MiddleCenter);
        }

        // ---- Équipe ------------------------------------------------------------------------------

        private void DrawTeam()
        {
            var allies = _ctl.Battle.GetAllies();
            var rects = Layout.TeamCardRects(allies.Count);
            for (int i = 0; i < allies.Count; i++)
            {
                var a = allies[i];
                var r = R(rects[i]);
                bool selected = _ctl.Selection.Selected == a.Id;
                float alpha = a.Alive ? 1f : 0.5f;
                if (selected) Outline(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(Selected.r, Selected.g, Selected.b, 0.3f), 8, 14);
                Fill(r, new Color(Panel.r, Panel.g, Panel.b, 0.88f * alpha), 8);
                Outline(r, selected ? Selected : PanelStroke, selected ? 3 : 1.5f, 8);
                var role = a.Role == "tank" ? Palette.Tank : a.Role == "healer" ? Palette.Healer : (a.Id == "dps2" ? Palette.Mage : Palette.Archer);
                Fill(new Rect(r.x + 8, r.y + 8, r.width - 16, 5), new Color(role.r, role.g, role.b, alpha), 3);
                Text(new Rect(r.x + 40, r.y + 14, r.width - 48, 24), a.Name, Layout.Font.Strong, Color.white, TextAnchor.MiddleLeft, true);
                Text(new Rect(r.x + 40, r.y + 38, r.width - 48, 20), a.Role == "tank" ? "Tank" : a.Role == "healer" ? "Soin" : "Dégâts", Layout.Font.Small, Muted, TextAnchor.MiddleLeft);

                double ratio = a.MaxHp > 0 ? System.Math.Max(0, a.Hp / a.MaxHp) : 0;
                var bar = new Rect(r.x + 12, r.y + 62, r.width - 24, 20);
                Fill(bar, new Color(0, 0, 0, 0.55f), 5);
                if (ratio > 0) Fill(new Rect(bar.x, bar.y, Mathf.Max(6f, bar.width * (float)ratio), bar.height), HpColor(ratio), 5);
                Text(new Rect(r.x, r.y + 84, r.width, 26), a.Alive ? Format.Ratio(a.Hp, a.MaxHp) : "K.O.", Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true);
                if (a.Shield > 0) Text(new Rect(r.x + 8, r.y + 110, r.width * 0.5f, 20), $"Bouclier {Format.Number(a.Shield)}", Layout.Font.Small, Palette.Shield, TextAnchor.MiddleLeft);
                if (a.Effects.Count > 0)
                {
                    var e = a.Effects[0];
                    var pill = new Rect(r.x + r.width * 0.5f, r.y + 108, r.width * 0.5f - 8, 22);
                    Fill(pill, Palette.Hex("5B2F86"), 8);
                    Text(pill, $"{e.Name} {Format.Seconds(e.MsRemaining)}", Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
                }
                if (!_ctl.HasCast && _ctl.Selection.Selected == null && a.Alive && a.Id == GuideAllyId(allies)) Guide(r, "1");
                KeyCap(r, BattleKeys.AllyLabel(i));
                if (Hit(r, UiAction.TapAlly)) _ctl.TapAlly(a.Id);
            }
        }

        // ---- Cible et mana -----------------------------------------------------------------------

        private void DrawStrip()
        {
            var battle = _ctl.Battle;
            var healer = battle.GetAllies().FirstOrDefault(x => x.Id == BattleController.HealerId);
            double mana = healer?.Mana ?? 0, max = healer?.MaxMana ?? 1;
            var bar = R(Layout.Zones.Strip);
            Fill(bar, new Color(0, 0, 0, 0.55f), 8);
            if (mana > 0) Fill(new Rect(bar.x, bar.y, Mathf.Max(10f, bar.width * (float)(mana / max)), bar.height), Palette.Hex("4AA8FF"), 8);
            Text(bar, $"Mana {Format.Ratio(mana, max)}", Layout.Font.Body, Color.white, TextAnchor.MiddleCenter, true);
        }

        /// <summary>Cible courante (ou rappel de choisir un allié), au centre bas de la scène.</summary>
        private void DrawTarget()
        {
            var z = R(Layout.Zones.Band);
            string selectedName = _ctl.Battle.GetAllies().FirstOrDefault(x => x.Id == _ctl.Selection.Selected)?.Name;
            if (_ctl.HintActive) Text(z, "Choisissez d'abord un allié !", Layout.Font.Title, Palette.Danger, TextAnchor.MiddleCenter, true);
            else if (selectedName != null) Text(z, $"Cible : {selectedName}", Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true);
            else Text(z, "Touchez un allié (à gauche) pour le cibler", Layout.Font.Body, Muted, TextAnchor.MiddleCenter);
        }

        // ---- Sorts -------------------------------------------------------------------------------

        private void DrawSkills()
        {
            var skills = _ctl.Skills;
            var rects = Layout.SkillButtonRects(skills.Count);
            var battle = _ctl.Battle;
            var healer = battle.GetAllies().FirstOrDefault(x => x.Id == BattleController.HealerId);
            for (int i = 0; i < skills.Count; i++)
            {
                var s = skills[i];
                var r = R(rects[i]);
                bool usable = battle.CanUseSkillNow(BattleController.HealerId, s.Id);
                double cd = battle.GetCooldownRemaining(BattleController.HealerId, s.Id);
                bool enoughMana = (healer?.Mana ?? 0) >= s.ManaCost;
                var accent = s.Id == "shield" ? Palette.Shield : s.Id == "purge" ? Palette.Poison : Palette.Heal;
                Fill(r, usable ? Palette.Hex("22273B") : Palette.Hex("141621"), 8);
                Outline(r, usable ? Palette.Hex("6F7698") : PanelStroke, usable ? 2 : 1.5f, 8);
                Fill(new Rect(r.x + 10, r.y + 10, r.width - 20, 6), new Color(accent.r, accent.g, accent.b, usable ? 1f : 0.4f), 3);
                Text(new Rect(r.x + 4, r.y + 20, r.width - 8, 30), s.Name, Layout.Font.Strong, Color.white, TextAnchor.MiddleCenter, true);
                Text(new Rect(r.x, r.y + 52, r.width, 20), $"{Format.Number(s.ManaCost)} mana", Layout.Font.Small, enoughMana ? Muted : Palette.Damage, TextAnchor.MiddleCenter);
                Text(new Rect(r.x, r.y + 74, r.width, 20), s.Target == "all" ? "Toute l'équipe" : "1 allié", Layout.Font.Small, Muted, TextAnchor.MiddleCenter);
                if (cd > 0)
                {
                    Fill(r, new Color(0, 0, 0, 0.45f), 8);
                    Text(new Rect(r.x, r.y + 30, r.width, 44), Format.Seconds(cd), Layout.Font.Banner, Palette.Hex("FF9D9D"), TextAnchor.MiddleCenter, true);
                }
                if (!_ctl.HasCast && _ctl.Selection.Selected != null && s.Id == "heal_single") Guide(r, "2");
                KeyCap(r, BattleKeys.SkillLabel(i));
                if (Hit(r, UiAction.TapSkill)) _ctl.TapSkill(s);
            }
        }

        /// <summary>Pastille de raccourci clavier dans le coin haut-gauche d'une carte (absente sans clavier).</summary>
        private void KeyCap(Rect card, string? label)
        {
            if (label == null) return;
            var cap = new Rect(card.x + 8, card.y + 8, 24, 24);
            Fill(cap, new Color(0f, 0f, 0f, 0.6f), 5);
            Outline(cap, new Color(1f, 1f, 1f, 0.5f), 1, 5);
            Text(cap, label, Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
        }

        /// <summary>Allié à soigner en premier pour le guidage : le plus abîmé, le tank à égalité.</summary>
        private static string GuideAllyId(System.Collections.Generic.List<UnitState> allies) =>
            allies.Where(a => a.Alive && a.Role != "healer").OrderBy(a => a.Hp / a.MaxHp).ThenBy(a => a.Role == "tank" ? 0 : 1).First().Id;

        private void Guide(Rect r, string number)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            var ring = new Rect(r.x - 4 - pulse * 3, r.y - 4 - pulse * 3, r.width + 8 + pulse * 6, r.height + 8 + pulse * 6);
            Outline(ring, new Color(Selected.r, Selected.g, Selected.b, 0.55f + 0.4f * pulse), 4, 14);
            var badge = new Rect(r.xMax - 26, r.y - 12, 34, 34);
            Fill(badge, Selected, 17);
            Text(badge, number, Layout.Font.Title, Palette.Hex("1C1A36"), TextAnchor.MiddleCenter, true);
        }

        // ---- Écran de démarrage ------------------------------------------------------------------

        private void DrawStart()
        {
            if (_ctl.Started) return;
            Fill(new Rect(-2000, -2000, 5000, 5000), new Color(0.03f, 0.04f, 0.09f, 0.86f), 0);
            float w = (float)Layout.GameW;
            var level = _flow.CurrentLevel;
            Text(new Rect(0, 60, w, 56), level?.Name ?? "Healer Game", 44, Color.white, TextAnchor.MiddleCenter, true);
            Text(new Rect(0, 120, w, 30), "Soignez votre équipe face au " + _ctl.Battle.GetBossName(), Layout.Font.Strong, Muted, TextAnchor.MiddleCenter);
            var panel = new Rect(w / 2 - 300, 180, 600, 260);
            Fill(panel, new Color(Panel.r, Panel.g, Panel.b, 0.96f), 14);
            Outline(panel, PanelStroke, 2, 14);
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
                Text(new Rect(panel.x + 22, panel.y + 14 + i * 38, panel.width - 44, 34), tips[i], Layout.Font.Body, i < 2 ? Selected : Color.white, TextAnchor.MiddleLeft, i < 2);
            var btn = R(Layout.StartButton);
            UiKit.Button(btn, "Jouer", 26, Palette.Hex("245C43"), Palette.Hex("5FB98D"));
            if (Keyboard.current != null)
            {
                string sorts = string.Join(" ", new[] { 0, 1, 2, 3 }.Select(BattleKeys.SkillLabel));
                Text(new Rect(0, 556, w, 24), "Clavier : 1-4 cibler · " + sorts + " sorts · Tab suivant", Layout.Font.Small, Muted, TextAnchor.MiddleCenter);
                Text(new Rect(0, 580, w, 24), "Espace : jouer / pause · Échap : retour · M : son", Layout.Font.Small, Muted, TextAnchor.MiddleCenter);
            }
            else Text(new Rect(0, 560, w, 24), "Touchez Jouer pour commencer", Layout.Font.Small, Muted, TextAnchor.MiddleCenter);
            var back = R(Layout.BackButton);
            UiKit.Button(back, "← Carte", Layout.Font.Body, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(back, UiAction.BackToMap)) _flow.LeaveBattle();
            if (Hit(btn, UiAction.StartFight)) _ctl.StartFight();
        }

        // ---- Pause -------------------------------------------------------------------------------

        private void DrawPause()
        {
            if (!_ctl.Paused) return;
            Fill(new Rect(-2000, -2000, 5000, 5000), new Color(0, 0, 0, 0.6f), 0);
            float w = (float)Layout.GameW;
            Text(new Rect(0, 230, w, 60), "PAUSE", 44, Color.white, TextAnchor.MiddleCenter, true);
            var resume = R(Layout.PauseResume);
            var leave = R(Layout.PauseLeave);
            UiKit.Button(resume, "Reprendre", Layout.Font.Title, Palette.Hex("245C43"), Palette.Hex("5FB98D"));
            UiKit.Button(leave, "Quitter le niveau", Layout.Font.Body, Palette.Hex("22273B"), Palette.Hex("6F7698"));
            if (Hit(resume, UiAction.TogglePause)) _ctl.TogglePause();
            if (Hit(leave, UiAction.BackToMap)) _flow.LeaveBattle();
        }

        // ---- Fin de combat -----------------------------------------------------------------------

        private void DrawEnd()
        {
            var result = _ctl.Battle.GetResult();
            if (result == BattleResults.Ongoing)
            {
                DrawPause();
                return;
            }
            Fill(new Rect(-2000, -2000, 5000, 5000), new Color(0, 0, 0, 0.8f), 0);
            bool win = result == BattleResults.Victory;
            float w = (float)Layout.GameW;
            var reward = _flow.LastReward;
            var level = _flow.CurrentLevel;
            Text(new Rect(0, 30, w, 54), win ? "Victoire !" : "Défaite…", 44, win ? Palette.Heal : Palette.Damage, TextAnchor.MiddleCenter, true);
            if (win && reward != null) UiKit.Stars(new Rect(w / 2 - 200, 92, 400, 64), reward.Stars, 56);

            var s = _ctl.Stats;
            var panel = new Rect(w / 2 - 300, 170, 600, 236);
            Fill(panel, new Color(Panel.r, Panel.g, Panel.b, 0.96f), 14);
            Outline(panel, PanelStroke, 2, 14);
            string[] labels = { "Durée du combat", "Soins effectifs", "Dégâts encaissés", "dont absorbés par boucliers", "Sorts lancés", "Alliés K.O.", "Poisons et brûlures purgés" };
            string[] values =
            {
                Format.Seconds(s.DurationMs), Format.Number(s.HealingDone), Format.Number(s.DamageTaken),
                Format.Number(s.DamageAbsorbed), s.Casts.ToString(), s.Deaths.ToString(), s.Purges.ToString(),
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var row = new Rect(panel.x + 20, panel.y + 12 + i * 31, panel.width - 40, 28);
                Text(row, labels[i], Layout.Font.Body, i == 3 ? Muted : Color.white, TextAnchor.MiddleLeft);
                Text(row, values[i], Layout.Font.Body, i == 5 && s.Deaths > 0 ? Palette.Damage : Palette.Heal, TextAnchor.MiddleRight, true);
            }

            float y = 418;
            if (win && reward != null)
            {
                Text(new Rect(0, y, w, 30), "+" + Format.Number(reward.GoldGained) + " or" + (reward.FirstClear ? "  ·  première victoire !" : ""), Layout.Font.Title, UiKit.Gold, TextAnchor.MiddleCenter, true);
                y += 30;
                if (reward.NewStars > 0) { Text(new Rect(0, y, w, 24), reward.NewStars + (reward.NewStars > 1 ? " nouvelles étoiles" : " nouvelle étoile"), Layout.Font.Body, Selected, TextAnchor.MiddleCenter); y += 24; }
                if (reward.NewBestTime) { Text(new Rect(0, y, w, 24), "Nouveau record de temps !", Layout.Font.Body, Selected, TextAnchor.MiddleCenter); y += 24; }
                foreach (var id in reward.UnlockedLevelIds)
                {
                    Text(new Rect(0, y, w, 24), "Niveau débloqué : " + _flow.Content.LevelById(id).Name, Layout.Font.Body, Palette.Heal, TextAnchor.MiddleCenter, true);
                    y += 24;
                }
                if (level != null && reward.Stars < 3)
                    Text(new Rect(0, Mathf.Max(y + 6, 520), w, 40),
                        "★ victoire   ★★ sans allié K.O.   ★★★ et moins de " + Format.Number(level.ThreeStarMaxDamageTaken) + " dégâts encaissés",
                        Layout.Font.Small, Muted, TextAnchor.UpperCenter);
            }
            else if (!win)
                Text(new Rect(w / 2 - 320, 430, 640, 60), "Astuce : posez un Bouclier pendant l'annonce de l'attaque de zone,\net Purgez poisons et brûlures dès qu'ils apparaissent.", Layout.Font.Body, Muted, TextAnchor.UpperCenter);

            var next = win ? _flow.NextLevel : null;
            var actions = new System.Collections.Generic.List<(string label, UiAction action, bool primary)> { ("Recommencer", UiAction.Restart, !win || next == null) };
            if (next != null) actions.Add(("Niveau suivant", UiAction.NextLevel, true));
            actions.Add(("Carte", UiAction.BackToMap, false));
            var rects = Layout.EndButtonRects(actions.Count);
            for (int i = 0; i < actions.Count; i++)
            {
                var r = R(rects[i]);
                var (label, action, primary) = actions[i];
                UiKit.Button(r, label, Layout.Font.Title, primary ? Palette.Hex("245C43") : Palette.Hex("22273B"), primary ? Palette.Hex("5FB98D") : Palette.Hex("6F7698"));
                if (Hit(r, action))
                {
                    if (action == UiAction.Restart) _flow.Restart();
                    else if (action == UiAction.NextLevel) _flow.NextLevelNow();
                    else _flow.LeaveBattle();
                }
            }
        }
    }
}
