namespace Healer.Ui
{
    /// <summary>
    /// Maintenir un bouton (souris, doigt) ou une touche de sort enchaîne le sort tant qu'il est disponible : pas
    /// besoin de taper plusieurs fois (D-050). Ne décide QUE de « répéter ou non » : le choix de la cible, du sort
    /// et du moment de le maintenir reste au joueur. Pur : le client l'appelle une fois par image.
    /// </summary>
    public sealed class HoldRepeat
    {
        public string? HeldSkillId { get; private set; }

        /// <summary>Appuie sur un sort. Si un autre est déjà maintenu, il est remplacé (le dernier appui gagne).</summary>
        public void Press(string skillId) => HeldSkillId = skillId;

        /// <summary>Relâche un sort. Sans effet si ce n'est pas celui qui est maintenu (deux touches relâchées dans le désordre).</summary>
        public void Release(string skillId)
        {
            if (HeldSkillId == skillId) HeldSkillId = null;
        }

        public void ReleaseAll() => HeldSkillId = null;

        /// <summary>
        /// Faut-il lancer le sort maintenu maintenant ? Oui seulement en combat actif ET si le sort est réellement
        /// lançable : jamais de message « choisissez un allié » répété, jamais de lancer pendant la pause.
        /// </summary>
        public bool ShouldCast(ScreenState state, CastResolution resolution) =>
            HeldSkillId != null && state == ScreenState.Playing && resolution.Kind == CastKind.Cast;
    }
}
