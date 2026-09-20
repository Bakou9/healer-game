using System;
using Healer.Combat;
using Healer.Combat.Progress;
using Healer.Ui;
using UnityEngine;

namespace Healer.Client
{
    /// <summary>
    /// Chef d'orchestre de l'application : quel écran (menu, choix du niveau, combat), quel niveau, quel profil.
    /// Aucune règle : la navigation est le Navigator du cœur, les récompenses sont la Progression du cœur ;
    /// ici on ne fait que les relier à Unity (scène, sauvegarde disque, sons).
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        public GameContent Content { get; private set; } = null!;
        public PlayerProfile Profile { get; private set; } = null!;
        public Navigator Nav { get; } = new Navigator();
        public BattleController Ctl { get; private set; } = null!;
        public RewardResult? LastReward { get; private set; }

        /// <summary>Dernier message de l'atelier (achat réussi, or insuffisant…) et l'instant où il s'efface.</summary>
        public string Notice { get; private set; } = "";
        public float NoticeUntil { get; private set; }
        public bool NoticeIsError { get; private set; }

        private ProfileStorage _storage = null!;
        private BattleStage _stage = null!;

        public AppScreen Screen => Nav.Screen;
        public LevelDef? CurrentLevel => Ctl.Level;

        public void Init(GameContent content, ProfileStorage storage, PlayerProfile profile, BattleController ctl, BattleStage stage)
        {
            Content = content;
            _storage = storage;
            Profile = profile;
            Ctl = ctl;
            _stage = stage;
            ctl.EventEmitted += OnBattleEvent;
            if (profile.TotalStars > 0) ctl.SkipGuidance(); // le guidage du premier geste ne sert qu'aux débutants
        }

        private void OnDestroy()
        {
            if (Ctl != null) Ctl.EventEmitted -= OnBattleEvent;
        }

        // ---- Navigation ----

        public void OpenLevels()
        {
            if (Nav.OpenLevelSelect()) Debug.Log("[Healer] écran : choix du niveau");
        }

        public void OpenWorkshop()
        {
            if (Nav.OpenWorkshop()) Debug.Log("[Healer] écran : atelier");
        }

        private void SetNotice(string text, bool error)
        {
            Notice = text;
            NoticeIsError = error;
            NoticeUntil = Time.realtimeSinceStartup + 3f;
        }

        public void BuyEquipment(string trackId)
        {
            var track = Content.Upgrades.Track(trackId);
            var result = Workshop.BuyEquipment(Profile, Content, trackId);
            if (result == PurchaseResult.Ok)
            {
                int level = Profile.Loadout.LevelOf(trackId);
                Debug.Log($"[Healer] atelier : achat {trackId} niveau {level}");
                SetNotice($"Acheté : {track?.Name} (niveau {level})", false);
                _storage.Save(Profile);
            }
            else
            {
                Debug.Log($"[Healer] atelier : refus {trackId} ({result})");
                SetNotice(Explain(result), true);
            }
        }

        public void PickTalent(int tier, string optionId)
        {
            bool alreadyBought = Profile.Loadout.Talents.ContainsKey(tier);
            var result = Workshop.PickTalent(Profile, Content, tier, optionId);
            if (result == PurchaseResult.Ok)
            {
                var name = Content.Upgrades.Tier(tier)?.Options.Find(o => o.Id == optionId)?.Name;
                Debug.Log($"[Healer] atelier : talent {tier} {optionId}");
                SetNotice((alreadyBought ? "Talent changé : " : "Talent acquis : ") + name, false);
                _storage.Save(Profile);
            }
            else
            {
                Debug.Log($"[Healer] atelier : refus talent {tier} {optionId} ({result})");
                SetNotice(Explain(result), true);
            }
        }

        private static string Explain(PurchaseResult r)
        {
            switch (r)
            {
                case PurchaseResult.NotEnoughGold: return "Or insuffisant";
                case PurchaseResult.MaxLevel: return "Niveau maximum atteint";
                case PurchaseResult.NeedPreviousTier: return "Choisissez d'abord le palier précédent";
                case PurchaseResult.Locked: return "Pas encore disponible";
                default: return "Achat impossible";
            }
        }

        public void BackToMenu()
        {
            if (Nav.BackToMenu()) Debug.Log("[Healer] écran : menu principal");
        }

        public bool StartLevel(string levelId)
        {
            var level = Content.LevelById(levelId);
            if (!Nav.StartLevel(levelId, Profile.IsUnlocked(level))) return false;
            Begin(level);
            return true;
        }

        /// <summary>Démarrage direct dans un niveau (options de ligne de commande : captures et tests).</summary>
        public void StartAt(string levelId)
        {
            Nav.StartAt(levelId);
            Begin(Content.LevelById(levelId));
        }

        private void Begin(LevelDef level)
        {
            LastReward = null;
            Ctl.StartLevel(Content, level, Profile.OwnedCharacters, Profile.Loadout, (uint)(Environment.TickCount & 0x7fffffff));
            var (team, skills) = LoadoutApplier.Apply(Content.Upgrades, Profile.Loadout, Content.Characters, Content.Skills);
            var tank = team.Find(c => c.Id == "tank");
            var heal = skills.Find(s => s.Id == "heal_single");
            Debug.Log($"[Healer] équipe : tank atk {tank?.Atk} PV {tank?.MaxHp} ; Soin {heal?.ManaCost} mana {heal?.HealAmount} PV");
            _stage.SetBoss(level.BossId);
            Debug.Log($"[Healer] niveau : {level.Id} ({level.Name})");
        }

        public void Restart()
        {
            LastReward = null;
            Ctl.Restart();
        }

        /// <summary>Niveau proposé après une victoire (null s'il n'y en a pas ou s'il est verrouillé).</summary>
        public LevelDef? NextLevel => CurrentLevel == null ? null : Progression.NextLevel(Profile, Content, CurrentLevel.Id);

        public void NextLevelNow()
        {
            var next = NextLevel;
            if (next != null) StartLevel(next.Id);
        }

        public void LeaveBattle()
        {
            if (Nav.LeaveBattle()) Debug.Log("[Healer] écran : choix du niveau");
        }

        public void ToggleMute()
        {
            Profile.Settings.Muted = !Profile.Settings.Muted;
            _storage.Save(Profile);
        }

        public void Quit() => Application.Quit();

        // ---- Fin de combat ----

        private void OnBattleEvent(BattleEvent e)
        {
            if (e.Type != "battleEnded" || Ctl.Level == null || Nav.Screen != AppScreen.Battle) return;
            LastReward = Progression.Complete(Profile, Content, Ctl.Level, Ctl.Stats);
            if (!LastReward.Victory) return;
            Debug.Log($"[Healer] progression : {Ctl.Level.Id} {LastReward.Stars} étoile(s), +{LastReward.GoldGained} or" + (LastReward.UnlockedLevelIds.Count > 0 ? ", débloque " + string.Join(",", LastReward.UnlockedLevelIds) : ""));
            _storage.Save(Profile);
        }
    }
}
