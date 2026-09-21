using Healer.Combat.Presentation;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Squelette léger d'un modèle : les pivots qu'on fait bouger (tête, bras, cape, buste). La scène traduit l'attitude
    /// calculée par le cœur (Healer.Combat.Presentation.UnitAnimator : élan, recul, lancer, chute) en rotations de ces
    /// pivots, en plus du souffle de repos. Aucune règle de jeu ici : uniquement de l'animation.
    /// </summary>
    /// <summary>Famille de gestes d'un modèle : coup d'épée, tir à l'arc, bâton (incantation, coup de pointe).</summary>
    public enum RigStyle { Default, Melee, Bow, Staff }

    public sealed class UnitRig : MonoBehaviour
    {
        public Transform? Head, ArmL, ArmR, Cape, Torso, Weapon;
        /// <summary>Choisit les gestes de ApplyPose ; Default = ancien mouvement simple (boss).</summary>
        public RigStyle Style = RigStyle.Default;
        /// <summary>Optional listener called with each pose (pixel-art heroes pick a sprite frame from it).</summary>
        public PoseAction? PoseHook;
        public delegate void PoseAction(in UnitPose pose);
        private Quaternion _head, _armL, _armR, _cape, _weapon, _torso;
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
            if (Torso != null) { _torsoScale = Torso.localScale; _torso = Torso.localRotation; }
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

        // ---- Gestes par phases (D-070) -----------------------------------------------------------------
        // Les valeurs viennent de UnitPose (cœur) : progression du coup, temps écoulé du geste de lancer, lâcher du sort.
        // Chaque geste = une préparation, une frappe rapide, un retour : des clés (avancement, degrés) lissées entre elles.

        private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        private static float Keys(float u, float[] kv)
        {
            if (u <= kv[0]) return kv[1];
            for (int i = 2; i < kv.Length; i += 2)
                if (u <= kv[i]) return Mathf.Lerp(kv[i - 1], kv[i + 1], Smooth(Mathf.InverseLerp(kv[i - 2], kv[i], u)));
            return kv[kv.Length - 1];
        }

        // Coup d'épée (Garde) : l'arme monte derrière, tombe vite, revient ; le buste se tord et se penche dans le coup.
        private static readonly float[] SwordArmR = { 0f, 0f, 0.30f, -150f, 0.42f, -155f, 0.56f, -28f, 0.74f, -42f, 1f, 0f };
        private static readonly float[] SwordWrist = { 0f, 0f, 0.30f, 35f, 0.42f, 40f, 0.56f, -45f, 0.74f, -15f, 1f, 0f };
        private static readonly float[] SwordArmL = { 0f, 0f, 0.30f, -25f, 0.56f, -55f, 0.80f, -20f, 1f, 0f };
        private static readonly float[] SwordTorsoX = { 0f, 0f, 0.30f, -9f, 0.56f, 15f, 0.80f, 6f, 1f, 0f };
        private static readonly float[] SwordTorsoY = { 0f, 0f, 0.30f, -22f, 0.56f, 20f, 0.80f, 8f, 1f, 0f };
        // Tir à l'arc (Archère) : bras d'arc tendu, corde tirée, lâcher net avec un petit recul.
        private static readonly float[] BowArmL = { 0f, 0f, 0.18f, -88f, 0.74f, -90f, 0.90f, -70f, 1f, 0f };
        private static readonly float[] BowArmR = { 0f, 0f, 0.18f, -96f, 0.70f, -96f, 0.78f, -78f, 1f, 0f };
        private static readonly float[] BowPull = { 0f, 0f, 0.18f, 0f, 0.70f, -42f, 0.78f, 0f, 1f, 0f };
        private static readonly float[] BowTorsoX = { 0f, 0f, 0.70f, -4f, 0.80f, 6f, 1f, 0f };
        // Coup de pointe du bâton (Mage) : le bâton se recule puis frappe droit devant.
        private static readonly float[] StabArmR = { 0f, 0f, 0.28f, -25f, 0.52f, -88f, 0.78f, -50f, 1f, 0f };
        private static readonly float[] StabTorsoX = { 0f, 0f, 0.28f, -8f, 0.52f, 13f, 1f, 0f };

        /// <summary>Anime le modèle depuis l'attitude complète du cœur (préparation, frappe, retour ; incantation tenue puis lâchée).</summary>
        public void ApplyPose(float t, float phase, in UnitPose pose)
        {
            PoseHook?.Invoke(pose);
            if (Style == RigStyle.Default)
            {
                Apply(t, phase, (float)pose.Cast, (float)pose.Lunge, (float)pose.Recoil, (float)pose.Fall);
                return;
            }
            if (!_captured) Capture();
            float breathe = Mathf.Sin(t * 1.7f + phase);
            float sway = Mathf.Sin(t * 1.1f + phase * 1.7f);
            float fall = (float)pose.Fall, recoil = (float)pose.Recoil, alive = 1f - fall;

            // Lancer : levée de l'incantation (ou geste court d'un sort instantané), tenue, puis lâcher.
            float raise = 0f, thrust = 0f, charge = 0f, tremble = 0f;
            if (pose.CastDurationMs > 0)
            {
                float e = (float)pose.CastElapsedMs, d = (float)pose.CastDurationMs;
                if (pose.Channeling)
                {
                    raise = Smooth(e / 260f);
                    charge = Mathf.Clamp01(e / d);
                    tremble = Mathf.Sin(t * 38f) * 1.6f * charge + Mathf.Sin(t * 4.2f) * 5f * raise; // vibration qui croît avec la charge + pulsation lente de la main ouverte
                }
                else
                {
                    raise = Smooth(e / 160f) * (1f - Smooth((e - (d - 170f)) / 170f));
                    thrust = Mathf.Sin(Mathf.PI * Mathf.Clamp01((e - 140f) / 200f));
                }
            }
            thrust += (float)pose.Release;

            float armR = 0f, armRz = 0f, armRy = 0f, armL = 0f, armLz = 0f, torsoX = 0f, torsoY = 0f, headX = 0f, weaponX = 0f, weaponZ = 0f, cape = 0f;
            float u = (float)pose.LungeProgress;

            switch (Style)
            {
                case RigStyle.Melee:
                    if (u > 0f) { armR = Keys(u, SwordArmR); weaponX = Keys(u, SwordWrist); armL = Keys(u, SwordArmL); torsoX = Keys(u, SwordTorsoX); torsoY = Keys(u, SwordTorsoY); cape = 24f * Smooth(1f - Mathf.Abs(u - 0.6f) * 3f); }
                    break;
                case RigStyle.Bow:
                    if (u > 0f) { armL = Keys(u, BowArmL); armR = Keys(u, BowArmR); armRy = Keys(u, BowPull); torsoX = Keys(u, BowTorsoX); }
                    break;
                case RigStyle.Staff:
                    if (u > 0f) { armR = Keys(u, StabArmR); torsoX = Keys(u, StabTorsoX); cape = 18f * Smooth(1f - Mathf.Abs(u - 0.55f) * 3f); }
                    // Incantation : la main du bâton monte à peine (le bâton reste presque droit, seule la main l'élève), l'autre main s'ouvre vers le ciel.
                    armR += -38f * raise - 24f * thrust;
                    armRz += -6f * raise;
                    armL += -100f * raise - 20f * thrust + tremble;
                    armLz += 16f * raise;
                    torsoX += -7f * raise + 12f * thrust;
                    headX += -10f * raise + 5f * thrust;
                    weaponX = -armR * 0.85f - 5f * raise - 8f * thrust;
                    weaponZ = -armRz * 0.9f;
                    cape += 12f * raise + 26f * thrust;
                    break;
            }

            if (Torso != null)
            {
                Torso.localScale = new Vector3(_torsoScale.x * (1f + 0.012f * breathe), _torsoScale.y * (1f + 0.02f * breathe), _torsoScale.z);
                Torso.localRotation = _torso * Quaternion.Euler(torsoX * alive, torsoY * alive, 0f);
            }
            if (Head != null) Head.localRotation = _head * Quaternion.Euler((headX + 16f * recoil + 2f * breathe) * alive, 3f * sway * alive, 0f);
            if (ArmR != null) ArmR.localRotation = _armR * Quaternion.Euler(armR + 4f * breathe * alive - 30f * fall, armRy, armRz - 8f * recoil);
            if (ArmL != null) ArmL.localRotation = _armL * Quaternion.Euler(armL - 4f * breathe * alive - 30f * fall, 0f, armLz + 10f * recoil);
            if (Weapon != null) Weapon.localRotation = _weapon * Quaternion.Euler(weaponX, 0f, weaponZ + 6f * Mathf.Sin(t * 2.2f + phase) * alive);
            if (Cape != null) Cape.localRotation = _cape * Quaternion.Euler((7f * Mathf.Sin(t * 2.1f + phase) + cape) * alive, 0f, 3f * sway);
        }
    }
}
