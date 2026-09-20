using System.Collections.Generic;
using System.Linq;

namespace Healer.Ui
{
    public enum CastKind
    {
        /// <summary>Lancer le sort (avec la cible éventuelle).</summary>
        Cast,
        /// <summary>Sort ciblé sans sélection : demander de choisir un allié.</summary>
        NeedTarget,
        /// <summary>Sort indisponible (recharge, mana) : ne rien lancer.</summary>
        Unavailable,
    }

    public readonly struct CastResolution
    {
        public CastKind Kind { get; }
        public string? TargetId { get; }

        public CastResolution(CastKind kind, string? targetId = null)
        {
            Kind = kind;
            TargetId = targetId;
        }
    }

    /// <summary>
    /// Ciblage en un geste (ticket E04-T04). Toucher un allié le sélectionne ; les sorts ciblés s'appliquent
    /// à l'allié sélectionné, et la sélection RESTE après un sort (soigner plusieurs fois la même cible = un
    /// seul geste). Pas de ciblage automatique : choisir QUI soigner est la décision centrale du jeu
    /// (décision D-019). Logique pure, sans Unity.
    /// </summary>
    public sealed class TargetSelection
    {
        public string? Selected { get; private set; }

        /// <summary>
        /// Toucher une carte (ou son raccourci) : sélectionne l'allié. Le re-toucher le garde sélectionné (D-050 :
        /// on ne désélectionne plus, un geste répété par réflexe ne doit jamais faire perdre la cible). Un allié K.O.
        /// ne se sélectionne pas.
        /// </summary>
        public void Tap(string unitId, bool isAlive)
        {
            if (!isAlive) return;
            Selected = unitId;
        }

        /// <summary>À appeler à chaque image : abandonne la sélection d'un allié qui n'est plus vivant.</summary>
        public void Sync(IEnumerable<string> aliveIds)
        {
            if (Selected != null && !aliveIds.Contains(Selected)) Selected = null;
        }

        public void Clear() => Selected = null;

        /// <summary>Que faire quand le joueur touche un sort ? La sélection n'est jamais modifiée ici.</summary>
        public CastResolution Resolve(bool targetsAll, bool canUseNow)
        {
            if (!canUseNow) return new CastResolution(CastKind.Unavailable);
            if (targetsAll) return new CastResolution(CastKind.Cast);
            return Selected == null ? new CastResolution(CastKind.NeedTarget) : new CastResolution(CastKind.Cast, Selected);
        }
    }
}
