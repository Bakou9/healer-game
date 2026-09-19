using System.Linq;
using Healer.Combat;
using Healer.Ui;
using UnityEngine;
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
        private GUIStyle _label = null!;

        private static readonly Color Panel = Palette.Hex("252839");
        private static readonly Color PanelStroke = Palette.Hex("555A70");
        private static readonly Color Selected = Palette.Hex("FFE066");
        private static readonly Color Muted = Palette.Hex("A7ADC2");

        public void Init(BattleController controller) => _ctl = controller;

        private static Rect R(Healer.Ui.Rect r) => new Rect((float)r.X, (float)r.Y, (float)r.W, (float)r.H);

        private static Color HpColor(double ratio) => Layout.HpBandFor(ratio) switch
        {
            HpBand.High => Palette.Hex("5FD35F"),
            HpBand.Mid => Palette.Hex("F0A23A"),
            _ => Palette.Hex("E0443E"),
        };

        private void OnGUI()
        {
            if (_ctl == null || _ctl.Battle == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { wordWrap = false, clipping = TextClipping.Overflow };
            }
            ScreenMap.Refresh();
            DrawDangerVignette();
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(ScreenMap.OffsetX, ScreenMap.OffsetY, 0), Quaternion.identity, new Vector3(ScreenMap.Scale, ScreenMap.Scale, 1));
            DrawBoss();
            DrawTeam();
            DrawStrip();
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

        private static bool Hit(Rect r) => GUI.Button(r, GUIContent.none, GUIStyle.none);

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
            Fill(pause, Panel, 12);
            Outline(pause, PanelStroke, 2, 12);
            Text(pause, _ctl.Paused ? ">" : "II", Layout.Font.Banner, Color.white, TextAnchor.MiddleCenter, true);
            if (Hit(pause)) _ctl.TogglePause();

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
                Fill(r, new Color(Panel.r, Panel.g, Panel.b, 0.92f * alpha), 12);
                Outline(r, selected ? Selected : PanelStroke, selected ? 4 : 2, 12);
                var role = a.Role == "tank" ? Palette.Tank : a.Role == "healer" ? Palette.Healer : (a.Id == "dps2" ? Palette.Mage : Palette.Archer);
                Fill(new Rect(r.x + 8, r.y + 8, r.width - 16, 6), new Color(role.r, role.g, role.b, alpha), 3);
                Text(new Rect(r.x, r.y + 16, r.width, 24), a.Name, Layout.Font.Body, Color.white, TextAnchor.MiddleCenter, true);
                Text(new Rect(r.x, r.y + 38, r.width, 20), a.Role == "tank" ? "Tank" : a.Role == "healer" ? "Soin" : "Dégâts", Layout.Font.Small, Muted, TextAnchor.MiddleCenter);

                double ratio = a.MaxHp > 0 ? System.Math.Max(0, a.Hp / a.MaxHp) : 0;
                var bar = new Rect(r.x + 12, r.y + 84, r.width - 24, 24);
                Fill(bar, new Color(0, 0, 0, 0.55f), 5);
                if (ratio > 0) Fill(new Rect(bar.x, bar.y, Mathf.Max(6f, bar.width * (float)ratio), bar.height), HpColor(ratio), 5);
                Text(new Rect(r.x, r.y + 112, r.width, 28), a.Alive ? Format.Ratio(a.Hp, a.MaxHp) : "K.O.", Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true);
                if (a.Shield > 0) Text(new Rect(r.x, r.y + 146, r.width, 20), $"Bouclier {Format.Number(a.Shield)}", Layout.Font.Small, Palette.Shield, TextAnchor.MiddleCenter);
                if (a.Effects.Count > 0)
                {
                    var e = a.Effects[0];
                    var pill = new Rect(r.x + 8, r.y + 170, r.width - 16, 22);
                    Fill(pill, Palette.Hex("5B2F86"), 8);
                    Text(pill, $"{e.Name} {Format.Seconds(e.MsRemaining)}", Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
                }
                if (!_ctl.HasCast && _ctl.Selection.Selected == null && a.Alive && a.Id == GuideAllyId(allies)) Guide(r, "1");
                if (Hit(r)) _ctl.TapAlly(a.Id);
            }
        }

        // ---- Cible et mana -----------------------------------------------------------------------

        private void DrawStrip()
        {
            var z = R(Layout.Zones.Strip);
            var battle = _ctl.Battle;
            var healer = battle.GetAllies().FirstOrDefault(x => x.Id == BattleController.HealerId);
            string selectedName = battle.GetAllies().FirstOrDefault(x => x.Id == _ctl.Selection.Selected)?.Name;
            if (_ctl.HintActive) Text(new Rect(z.x, z.y + 2, z.width, 26), "Choisissez d'abord un allié !", Layout.Font.Body, Palette.Danger, TextAnchor.MiddleLeft, true);
            else if (selectedName != null) Text(new Rect(z.x, z.y + 2, z.width, 26), $"Cible : {selectedName}", Layout.Font.Body, Color.white, TextAnchor.MiddleLeft, true);
            else Text(new Rect(z.x, z.y + 2, z.width, 26), "Touchez un allié pour le cibler", Layout.Font.Body, Muted, TextAnchor.MiddleLeft);

            double mana = healer?.Mana ?? 0, max = healer?.MaxMana ?? 1;
            var bar = new Rect(z.x, z.y + 34, z.width, 24);
            Fill(bar, new Color(0, 0, 0, 0.55f), 8);
            if (mana > 0) Fill(new Rect(bar.x, bar.y, Mathf.Max(10f, bar.width * (float)(mana / max)), bar.height), Palette.Hex("4AA8FF"), 8);
            Text(bar, $"Mana {Format.Ratio(mana, max)}", Layout.Font.Small, Color.white, TextAnchor.MiddleCenter, true);
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
                Fill(r, usable ? Palette.Hex("30344A") : Palette.Hex("1E2030"), 12);
                Outline(r, usable ? Palette.Hex("8A90B4") : PanelStroke, usable ? 3 : 2, 12);
                Fill(new Rect(r.x + 10, r.y + 10, r.width - 20, 8), new Color(accent.r, accent.g, accent.b, usable ? 1f : 0.4f), 4);
                Text(new Rect(r.x + 4, r.y + 26, r.width - 8, 60), s.Name, Layout.Font.Body, Color.white, TextAnchor.MiddleCenter, true);
                Text(new Rect(r.x, r.y + 92, r.width, 20), $"{Format.Number(s.ManaCost)} mana", Layout.Font.Small, enoughMana ? Muted : Palette.Damage, TextAnchor.MiddleCenter);
                Text(new Rect(r.x, r.y + 112, r.width, 20), s.Target == "all" ? "Toute l'équipe" : "1 allié", Layout.Font.Small, Muted, TextAnchor.MiddleCenter);
                if (cd > 0) Text(new Rect(r.x, r.y + 134, r.width, 28), Format.Seconds(cd), Layout.Font.Cooldown, Palette.Hex("FF9D9D"), TextAnchor.MiddleCenter, true);
                if (!_ctl.HasCast && _ctl.Selection.Selected != null && s.Id == "heal_single") Guide(r, "2");
                if (Hit(r)) _ctl.TapSkill(s);
            }
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
            Text(new Rect(0, 130, w, 56), "Healer Game", 44, Color.white, TextAnchor.MiddleCenter, true);
            Text(new Rect(0, 190, w, 30), "Soignez votre équipe face au " + _ctl.Battle.GetBossName(), Layout.Font.Body, Muted, TextAnchor.MiddleCenter);
            var panel = new Rect(36, 250, w - 72, 260);
            Fill(panel, new Color(Panel.r, Panel.g, Panel.b, 0.96f), 14);
            Outline(panel, PanelStroke, 2, 14);
            string[] tips =
            {
                "1  Touchez un allié pour le cibler",
                "2  Touchez un sort pour le lancer sur lui",
                "Soin de zone : sans cible, pour toute l'équipe",
                "Bouclier : à poser AVANT l'attaque annoncée",
                "Purge : retire le poison (phase 2)",
                "Le mana est limité : ne le gaspillez pas",
            };
            for (int i = 0; i < tips.Length; i++)
                Text(new Rect(panel.x + 22, panel.y + 14 + i * 38, panel.width - 44, 34), tips[i], Layout.Font.Body, i < 2 ? Selected : Color.white, TextAnchor.MiddleLeft, i < 2);
            var btn = new Rect(w / 2 - 120, 560, 240, 60);
            Fill(btn, Palette.Hex("2F7D57"), 14);
            Outline(btn, Palette.Heal, 3, 14);
            Text(btn, "Jouer", 26, Color.white, TextAnchor.MiddleCenter, true);
            Text(new Rect(0, 640, w, 24), "M : couper le son", Layout.Font.Small, Muted, TextAnchor.MiddleCenter);
            if (Hit(btn)) _ctl.StartFight();
        }

        // ---- Fin de combat -----------------------------------------------------------------------

        private void DrawEnd()
        {
            var result = _ctl.Battle.GetResult();
            if (result == BattleResults.Ongoing)
            {
                if (_ctl.Paused) Text(new Rect(0, (float)Layout.GameH / 2 - 20, (float)Layout.GameW, 40), "PAUSE", 36, Color.white, TextAnchor.MiddleCenter, true);
                return;
            }
            var full = new Rect(-2000, -2000, 5000, 5000);
            Fill(full, new Color(0, 0, 0, 0.78f), 0);
            bool win = result == BattleResults.Victory;
            float w = (float)Layout.GameW;
            Text(new Rect(0, 150, w, 54), win ? "Victoire !" : "Défaite…", 44, win ? Palette.Heal : Palette.Damage, TextAnchor.MiddleCenter, true);

            var s = _ctl.Stats;
            var panel = new Rect(40, 224, w - 80, 236);
            Fill(panel, new Color(Panel.r, Panel.g, Panel.b, 0.96f), 14);
            Outline(panel, PanelStroke, 2, 14);
            string[] labels = { "Durée du combat", "Soins effectifs", "Dégâts encaissés", "dont absorbés par boucliers", "Sorts lancés", "Alliés K.O.", "Poisons purgés" };
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
            if (!win)
                Text(new Rect(40, 470, w - 80, 60), "Astuce : posez un Bouclier pendant l'annonce de l'attaque de zone,\net Purgez le poison en phase 2.", Layout.Font.Small, Muted, TextAnchor.UpperCenter);

            var btn = new Rect(w / 2 - 120, 560, 240, 56);
            Fill(btn, Palette.Hex("333652"), 12);
            Outline(btn, Palette.Hex("8A90B4"), 3, 12);
            Text(btn, "Recommencer", Layout.Font.Title, Color.white, TextAnchor.MiddleCenter, true);
            if (Hit(btn)) _ctl.Restart();
        }
    }
}
