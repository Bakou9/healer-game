using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Squelette léger d'un modèle : les pivots qu'on fait bouger (tête, bras, cape, buste). La scène traduit l'attitude
    /// calculée par le cœur (Healer.Combat.Presentation.UnitAnimator : élan, recul, lancer, chute) en rotations de ces
    /// pivots, en plus du souffle de repos. Aucune règle de jeu ici : uniquement de l'animation.
    /// </summary>
    public sealed class UnitRig : MonoBehaviour
    {
        public Transform? Head, ArmL, ArmR, Cape, Torso, Weapon;
        private Quaternion _head, _armL, _armR, _cape, _weapon;
        private Vector3 _torsoScale = Vector3.one;
        private bool _captured;

        /// <summary>Mémorise la pose de repos (à appeler quand le modèle est construit).</summary>
        public void Capture()
        {
            if (Head != null) _head = Head.localRotation;
            if (ArmL != null) _armL = ArmL.localRotation;
            if (ArmR != null) _armR = ArmR.localRotation;
            if (Cape != null) _cape = Cape.localRotation;
            if (Weapon != null) _weapon = Weapon.localRotation;
            if (Torso != null) _torsoScale = Torso.localScale;
            _captured = true;
        }

        /// <param name="t">Temps (s).</param>
        /// <param name="phase">Déphasage propre à l'unité (elles ne respirent pas toutes en même temps).</param>
        /// <param name="cast">Geste de lancer (0 à 1) : bras levés.</param>
        /// <param name="lunge">Coup porté (0 à 1) : bras droit en avant.</param>
        /// <param name="recoil">Coup reçu (0 à 1) : tête rejetée, bras écartés.</param>
        /// <param name="fall">Chute (0 à 1).</param>
        public void Apply(float t, float phase, float cast, float lunge, float recoil, float fall)
        {
            if (!_captured) Capture();
            float breathe = Mathf.Sin(t * 1.7f + phase);
            float sway = Mathf.Sin(t * 1.1f + phase * 1.7f);
            float alive = 1f - fall;

            if (Torso != null) Torso.localScale = new Vector3(_torsoScale.x * (1f + 0.012f * breathe), _torsoScale.y * (1f + 0.02f * breathe), _torsoScale.z);
            if (Head != null) Head.localRotation = _head * Quaternion.Euler((-8f * cast + 16f * recoil + 2f * breathe) * alive, 3f * sway * alive, 0f);
            if (ArmR != null) ArmR.localRotation = _armR * Quaternion.Euler((-108f * cast - 96f * lunge + 4f * breathe * alive - 30f * fall), 0f, (-8f * recoil - 6f * cast));
            if (ArmL != null) ArmL.localRotation = _armL * Quaternion.Euler((-72f * cast - 10f * lunge - 4f * breathe * alive - 30f * fall), 0f, (10f * recoil + 6f * cast));
            if (Weapon != null) Weapon.localRotation = _weapon * Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(t * 2.2f + phase) * alive);
            if (Cape != null) Cape.localRotation = _cape * Quaternion.Euler((7f * Mathf.Sin(t * 2.1f + phase) + 22f * lunge + 10f * cast) * alive, 0f, 3f * sway);
        }
    }
}
