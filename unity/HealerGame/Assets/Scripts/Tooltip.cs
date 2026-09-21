using System.Collections.Generic;
using Healer.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Healer.Client
{
    /// <summary>
    /// Infobulle au survol (D-080) : sur PC, on n'apprend pas un sort en l'essayant, on lit ce qu'il fait avant de le
    /// lancer. Elle affiche la fiche calculée par le cœur (Healer.Ui.SkillDescriber), avec les bonus d'équipement en vert,
    /// et se place d'elle-même pour rester dans l'écran.
    ///
    /// Aucune règle de jeu ici : uniquement de l'affichage.
    /// </summary>
    public sealed class Tooltip
    {
        private readonly VisualElement _root;
        private readonly Label _titre;
        private readonly VisualElement _lignes;
        private string _pour = "";

        public Tooltip(VisualElement calque)
        {
            _root = new VisualElement
            {
                pickingMode = PickingMode.Ignore,   // l'infobulle ne doit jamais voler le clic destiné au bouton
                style =
                {
                    position = Position.Absolute,
                    display = DisplayStyle.None,
                    maxWidth = 340,
                    paddingLeft = 14, paddingRight = 14, paddingTop = 10, paddingBottom = 12,
                    backgroundColor = new Color(0.05f, 0.05f, 0.09f, 0.97f),
                },
            };
            Ui.Stroke(_root, new Color(0.62f, 0.56f, 0.38f, 0.9f), 2);
            _titre = new Label { style = { fontSize = 20, color = new Color(0.96f, 0.92f, 0.78f), marginBottom = 6, whiteSpace = WhiteSpace.Normal } };
            _root.Add(_titre);
            _lignes = new VisualElement { pickingMode = PickingMode.Ignore };
            _root.Add(_lignes);
            calque.Add(_root);
        }

        /// <summary>Affiche la fiche au-dessus du rectangle donné (coordonnées logiques du jeu). Sans effet si elle y est déjà.</summary>
        public void Montrer(SkillSheet fiche, UnityEngine.Rect ancre, string cle)
        {
            if (_pour == cle) return;
            _pour = cle;
            _titre.text = fiche.Name;
            _lignes.Clear();
            Ligne(fiche.Cost, fiche.Cooldown);
            Ligne(fiche.Cast, null);
            _lignes.Add(Texte(fiche.Target, new Color(0.72f, 0.74f, 0.82f), 15));
            var desc = Texte(Riche(fiche.Description), new Color(0.88f, 0.89f, 0.94f), 16);
            desc.style.marginTop = 6;
            _lignes.Add(desc);

            _root.style.display = DisplayStyle.Flex;
            // Placement : au-dessus du bouton, et rentré dans l'écran s'il déborde.
            float largeur = 340f, hauteur = 190f;
            float x = Mathf.Clamp(ancre.center.x - largeur / 2f, 8f, (float)Layout.GameW - largeur - 8f);
            float y = ancre.y - hauteur - 10f;
            if (y < 8f) y = ancre.yMax + 10f;
            _root.style.left = x;
            _root.style.top = y;
        }

        // Un seul Label par ligne, avec des balises de couleur : UI Toolkit rogne les espaces au bord de CHAQUE Label,
        // si bien qu une ligne faite de plusieurs morceaux donnait « CD :aucun » et « de170 PV ».
        private void Ligne(IReadOnlyList<SkillSpan> gauche, IReadOnlyList<SkillSpan>? droite, int taille = 16)
        {
            var t = Riche(gauche);
            if (droite != null) t += "   ·   " + Riche(droite);
            _lignes.Add(Texte(t, new Color(0.88f, 0.89f, 0.94f), taille));
        }

        /// <summary>Assemble les morceaux en un texte ; une valeur modifiée par l équipement passe en vert.</summary>
        private static string Riche(IReadOnlyList<SkillSpan> spans)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in spans) sb.Append(s.Changed ? "<color=#7CFFB2>" + s + "</color>" : s.ToString());
            return sb.ToString();
        }

        private static Label Texte(string t, Color c, int taille) =>
            new Label(t) { enableRichText = true, style = { fontSize = taille, color = c, whiteSpace = WhiteSpace.Normal } };

        public void Cacher(string cle)
        {
            if (_pour != cle) return;         // un autre bouton a pris la main entre-temps
            _root.style.display = DisplayStyle.None;
            _pour = "";
        }

        public void CacherTout()
        {
            _root.style.display = DisplayStyle.None;
            _pour = "";
        }
    }
}
