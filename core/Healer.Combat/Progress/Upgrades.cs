using System;
using System.Collections.Generic;
using System.Linq;

namespace Healer.Combat.Progress
{
    /// <summary>
    /// Un effet d'amélioration, exprimé en données : soit une statistique d'un personnage (stat = maxHp, atk, def,
    /// maxMana, manaRegen), soit un champ d'un sort (skill = id ou « * », field = healAmount, shieldAmount, manaCost,
    /// cooldownMs). Pct en pourcentage entier (15 = +15 %, -15 = -15 %). Aucun sort ni personnage n'est codé en dur.
    /// </summary>
    public class UpgradeEffect
    {
        public string? Stat { get; set; }
        public string? Skill { get; set; }
        public string? Field { get; set; }
        public int Pct { get; set; }
    }

    /// <summary>Piste d'équipement d'un personnage (« Épée du Garde », niveaux 1 à MaxLevel, achetés avec de l'or).</summary>
    public class EquipmentTrackDef
    {
        public string Id { get; set; } = "";
        public string CharacterId { get; set; } = "";
        public string Name { get; set; } = "";
        public int MaxLevel { get; set; }
        /// <summary>Coût du niveau n+1 = Costs[n].</summary>
        public List<int> Costs { get; set; } = new List<int>();
        /// <summary>Effets ajoutés à CHAQUE niveau.</summary>
        public List<UpgradeEffect> PerLevel { get; set; } = new List<UpgradeEffect>();
    }

    public class TalentOptionDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<UpgradeEffect> Effects { get; set; } = new List<UpgradeEffect>();
    }

    /// <summary>Palier de talent du soigneur : on choisit UNE option parmi deux (le choix peut être changé gratuitement).</summary>
    public class TalentTierDef
    {
        public int Tier { get; set; }
        /// <summary>Total d'étoiles requis pour ouvrir ce palier.</summary>
        public int RequiresStars { get; set; }
        public int Cost { get; set; }
        public List<TalentOptionDef> Options { get; set; } = new List<TalentOptionDef>();
    }

    public sealed class UpgradeCatalog
    {
        public List<EquipmentTrackDef> Equipment { get; set; } = new List<EquipmentTrackDef>();
        public List<TalentTierDef> TalentTiers { get; set; } = new List<TalentTierDef>();

        public static readonly UpgradeCatalog Empty = new UpgradeCatalog();

        public EquipmentTrackDef? Track(string id) => Equipment.Find(t => t.Id == id);
        public TalentTierDef? Tier(int tier) => TalentTiers.Find(t => t.Tier == tier);
    }

    /// <summary>Ce que le joueur a acheté : niveaux d'équipement et options de talent choisies. Sérialisé avec le profil.</summary>
    public sealed class Loadout
    {
        public Dictionary<string, int> Equipment { get; } = new Dictionary<string, int>();
        /// <summary>Palier → identifiant de l'option choisie (un palier absent n'est pas acheté).</summary>
        public Dictionary<int, string> Talents { get; } = new Dictionary<int, string>();

        public int LevelOf(string trackId) => Equipment.TryGetValue(trackId, out var l) ? l : 0;

        public Loadout Clone()
        {
            var c = new Loadout();
            foreach (var kv in Equipment) c.Equipment[kv.Key] = kv.Value;
            foreach (var kv in Talents) c.Talents[kv.Key] = kv.Value;
            return c;
        }

        /// <summary>Tout au maximum (mesures d'équilibrage) avec les premières options de talent, ou celles données.</summary>
        public static Loadout Maxed(UpgradeCatalog catalog, IReadOnlyList<int>? optionIndexes = null)
        {
            var l = new Loadout();
            foreach (var t in catalog.Equipment) l.Equipment[t.Id] = t.MaxLevel;
            for (int i = 0; i < catalog.TalentTiers.Count; i++)
                l.Talents[catalog.TalentTiers[i].Tier] = catalog.TalentTiers[i].Options[optionIndexes == null ? 0 : optionIndexes[i]].Id;
            return l;
        }
    }

    /// <summary>
    /// Applique un équipement et des talents aux personnages et aux sorts d'un combat. Pur : renvoie des COPIES,
    /// ne modifie jamais le contenu. Les pourcentages de toutes les sources s'ADDITIONNENT puis s'appliquent une
    /// seule fois à la valeur de base (pas d'effet boule de neige), arrondie au plus proche (demi vers le haut).
    /// Sans amélioration, le résultat est strictement identique au contenu de base : les golden ne bougent pas.
    /// </summary>
    public static class LoadoutApplier
    {
        public static double Scale(double value, int pct) => Math.Floor(value * (100 + pct) / 100.0 + 0.5);

        public static (List<CharacterDef> characters, List<SkillDef> skills) Apply(
            UpgradeCatalog catalog, Loadout? loadout, IEnumerable<CharacterDef> team, IEnumerable<SkillDef> skills)
        {
            var characters = team.Select(Clone).ToList();
            var skillList = skills.Select(Clone).ToList();
            if (loadout == null) return (characters, skillList);

            var statPct = new Dictionary<(string character, string stat), int>();
            var skillPct = new Dictionary<(string skill, string field), int>();
            var healer = characters.FirstOrDefault(c => c.Role == "healer")?.Id ?? "";

            void Add(string owner, UpgradeEffect e, int times)
            {
                if (e.Stat != null)
                {
                    var key = (owner, e.Stat);
                    statPct[key] = (statPct.TryGetValue(key, out var v) ? v : 0) + e.Pct * times;
                }
                else if (e.Skill != null && e.Field != null)
                {
                    foreach (var s in skillList.Where(s => e.Skill == "*" || s.Id == e.Skill))
                    {
                        var key = (s.Id, e.Field);
                        skillPct[key] = (skillPct.TryGetValue(key, out var v) ? v : 0) + e.Pct * times;
                    }
                }
            }

            foreach (var track in catalog.Equipment)
            {
                int level = Math.Min(loadout.LevelOf(track.Id), track.MaxLevel);
                if (level <= 0 || !characters.Any(c => c.Id == track.CharacterId)) continue;
                foreach (var e in track.PerLevel) Add(track.CharacterId, e, level);
            }
            foreach (var kv in loadout.Talents)
            {
                var option = catalog.Tier(kv.Key)?.Options.Find(o => o.Id == kv.Value);
                if (option == null) continue;
                foreach (var e in option.Effects) Add(healer, e, 1);
            }

            foreach (var c in characters)
            {
                if (statPct.TryGetValue((c.Id, "maxHp"), out var hp)) c.MaxHp = Scale(c.MaxHp, hp);
                if (statPct.TryGetValue((c.Id, "atk"), out var atk)) c.Atk = Scale(c.Atk, atk);
                if (statPct.TryGetValue((c.Id, "def"), out var def)) c.Def = Scale(c.Def, def);
                if (c.MaxMana != null && statPct.TryGetValue((c.Id, "maxMana"), out var mana)) c.MaxMana = Scale(c.MaxMana.Value, mana);
                if (c.ManaRegenPerSec != null && statPct.TryGetValue((c.Id, "manaRegen"), out var regen)) c.ManaRegenPerSec = c.ManaRegenPerSec.Value * (100 + regen) / 100.0;
            }
            foreach (var s in skillList)
            {
                if (s.HealAmount != null && skillPct.TryGetValue((s.Id, "healAmount"), out var heal)) s.HealAmount = Scale(s.HealAmount.Value, heal);
                if (s.ShieldAmount != null && skillPct.TryGetValue((s.Id, "shieldAmount"), out var shield)) s.ShieldAmount = Scale(s.ShieldAmount.Value, shield);
                if (skillPct.TryGetValue((s.Id, "manaCost"), out var cost)) s.ManaCost = Math.Max(0, Scale(s.ManaCost, cost));
                if (skillPct.TryGetValue((s.Id, "cooldownMs"), out var cd)) s.CooldownMs = Math.Max(1, Scale(s.CooldownMs, cd));
            }
            return (characters, skillList);
        }

        private static CharacterDef Clone(CharacterDef c) => new CharacterDef
        {
            Id = c.Id, Name = c.Name, Role = c.Role, MaxHp = c.MaxHp, Atk = c.Atk, Def = c.Def, MaxMana = c.MaxMana, ManaRegenPerSec = c.ManaRegenPerSec,
        };

        private static SkillDef Clone(SkillDef s) => new SkillDef
        {
            Id = s.Id, Name = s.Name, Description = s.Description, ManaCost = s.ManaCost, CooldownMs = s.CooldownMs, Target = s.Target,
            HealAmount = s.HealAmount, ShieldAmount = s.ShieldAmount, Cleanse = s.Cleanse,
        };
    }

    public enum PurchaseResult
    {
        Ok,
        UnknownItem,
        MaxLevel,
        NotEnoughGold,
        /// <summary>Palier de talent pas encore ouvert (étoiles) ou personnage non possédé.</summary>
        Locked,
        /// <summary>Il faut d'abord acheter le palier précédent.</summary>
        NeedPreviousTier,
    }

    /// <summary>L'atelier : achats d'équipement et choix de talents. Toute dépense passe par le portefeuille, avec sa raison.</summary>
    public static class Workshop
    {
        /// <summary>Prix du prochain niveau d'une piste (0 si déjà au maximum ou inconnue).</summary>
        public static int NextCost(PlayerProfile profile, EquipmentTrackDef track)
        {
            int level = profile.Loadout.LevelOf(track.Id);
            return level >= track.MaxLevel || level >= track.Costs.Count ? 0 : track.Costs[level];
        }

        public static PurchaseResult BuyEquipment(PlayerProfile profile, GameContent content, string trackId)
        {
            var track = content.Upgrades.Track(trackId);
            if (track == null) return PurchaseResult.UnknownItem;
            if (!profile.Owns(track.CharacterId)) return PurchaseResult.Locked;
            int level = profile.Loadout.LevelOf(trackId);
            if (level >= track.MaxLevel) return PurchaseResult.MaxLevel;
            int cost = NextCost(profile, track);
            if (!profile.Wallet.TrySpend(Wallet.Gold, cost, $"atelier:{trackId}:niveau {level + 1}")) return PurchaseResult.NotEnoughGold;
            profile.Loadout.Equipment[trackId] = level + 1;
            return PurchaseResult.Ok;
        }

        /// <summary>Un palier est ouvert s'il y a assez d'étoiles ET si le palier précédent est acheté.</summary>
        public static PurchaseResult TierAvailability(PlayerProfile profile, GameContent content, int tier)
        {
            var def = content.Upgrades.Tier(tier);
            if (def == null) return PurchaseResult.UnknownItem;
            if (tier > 1 && !profile.Loadout.Talents.ContainsKey(tier - 1)) return PurchaseResult.NeedPreviousTier;
            if (profile.TotalStars < def.RequiresStars) return PurchaseResult.Locked;
            return PurchaseResult.Ok;
        }

        /// <summary>
        /// Choisit une option de talent. Premier choix d'un palier : payant. Changer d'option dans un palier déjà
        /// acheté : gratuit (on n'a pas peur de se tromper). Choisir l'option déjà active ne fait rien.
        /// </summary>
        public static PurchaseResult PickTalent(PlayerProfile profile, GameContent content, int tier, string optionId)
        {
            var def = content.Upgrades.Tier(tier);
            if (def == null || def.Options.All(o => o.Id != optionId)) return PurchaseResult.UnknownItem;
            if (profile.Loadout.Talents.ContainsKey(tier))
            {
                profile.Loadout.Talents[tier] = optionId;
                return PurchaseResult.Ok;
            }
            var availability = TierAvailability(profile, content, tier);
            if (availability != PurchaseResult.Ok) return availability;
            if (!profile.Wallet.TrySpend(Wallet.Gold, def.Cost, $"atelier:talent:{tier}:{optionId}")) return PurchaseResult.NotEnoughGold;
            profile.Loadout.Talents[tier] = optionId;
            return PurchaseResult.Ok;
        }
    }
}
