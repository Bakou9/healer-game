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
        /// <summary>Budget de puissance déclaré (E02-T09) : sert à COMPARER des talents entre eux, pas à changer le jeu.
        /// Les deux options d'un même palier doivent avoir un budget proche (voir CreditPolicy-like : TalentPolicy).</summary>
        public int Power { get; set; }
        /// <summary>Capstone (E02-T05) : id d'un sort du catalogue (skills.json, marqué "capstone": true) ajouté à la
        /// barre du soigneur SEULEMENT si cette option est choisie. Absent = ce talent ne débloque aucun sort.</summary>
        public string? UnlocksSkill { get; set; }
    }

    /// <summary>Palier de talent du soigneur : on choisit UNE option parmi deux (le choix peut être changé gratuitement).
    /// Appartient à une voie de spécialisation (E02-T02) : Tier est un numéro GLOBAL unique au catalogue (pas remis à
    /// zéro par voie), pour que Loadout.Talents (palier -> choix) n'ait pas besoin de connaître la voie.</summary>
    public class TalentTierDef
    {
        public int Tier { get; set; }
        /// <summary>Voie de spécialisation : "lumiere", "egide" ou "purification" (E02-T02). Un palier appartient à une seule voie.</summary>
        public string Voie { get; set; } = "";
        /// <summary>Palier dans SA voie (1 à 4), pour l'affichage et pour repérer le capstone (PalierDansVoie == 4).</summary>
        public int PalierDansVoie { get; set; }
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
        /// <summary>Les 4 paliers d'une voie, dans l'ordre (E02-T02).</summary>
        public List<TalentTierDef> Voie(string voie) => TalentTiers.Where(t => t.Voie == voie).OrderBy(t => t.PalierDansVoie).ToList();
        /// <summary>Les voies présentes dans le catalogue, dans leur ordre d'apparition (E02-T02).</summary>
        public List<string> Voies => TalentTiers.Select(t => t.Voie).Distinct().ToList();
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
            var allSkills = skills.ToList();
            // Un capstone (E02-T05) n'est dans la barre du soigneur QUE si le talent qui le débloque est choisi.
            var skillList = allSkills.Where(s => !s.Capstone).Select(Clone).ToList();
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
                // Capstone (E02-T05) : le sort qu'il débloque rejoint la barre, cherché dans TOUT le catalogue de
                // sorts (y compris les capstones qu'on a exclus plus haut), jamais codé en dur ici.
                if (option.UnlocksSkill != null && !skillList.Any(s => s.Id == option.UnlocksSkill))
                {
                    var unlocked = allSkills.FirstOrDefault(s => s.Id == option.UnlocksSkill);
                    if (unlocked != null) skillList.Add(Clone(unlocked));
                }
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
                if (s.CastMs > 0 && skillPct.TryGetValue((s.Id, "castMs"), out var cast)) s.CastMs = Math.Max(1, Scale(s.CastMs, cast));
            }
            return (characters, skillList);
        }

        private static CharacterDef Clone(CharacterDef c) => new CharacterDef
        {
            Id = c.Id, Name = c.Name, Role = c.Role, MaxHp = c.MaxHp, Atk = c.Atk, Def = c.Def, MaxMana = c.MaxMana, ManaRegenPerSec = c.ManaRegenPerSec,
            AttackIntervalMs = c.AttackIntervalMs,
            ArmorPct = c.ArmorPct, DodgePct = c.DodgePct, CritPct = c.CritPct, CritMultPct = c.CritMultPct, ThreatMod = c.ThreatMod, DamageType = c.DamageType,
            Resist = c.Resist == null ? null : new Dictionary<string, int>(c.Resist),
        };

        private static SkillDef Clone(SkillDef s) => new SkillDef
        {
            Id = s.Id, Name = s.Name, Description = s.Description, ManaCost = s.ManaCost, CooldownMs = s.CooldownMs, CastMs = s.CastMs, Target = s.Target,
            HealAmount = s.HealAmount, ShieldAmount = s.ShieldAmount, Cleanse = s.Cleanse, Capstone = s.Capstone,
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

        // ---- Mode développeur (D-063) : contourne l'économie pour tester ; jamais appelé par le jeu normal ----

        /// <summary>Or affiché par le bouton du mode développeur.</summary>
        public const int DevGold = 99_999;

        /// <summary>Fixe le niveau d'une piste d'équipement (borné à 0..maximum) sans payer. Renvoie le niveau obtenu, ou -1 si la piste est inconnue.</summary>
        public static int DevSetEquipmentLevel(PlayerProfile profile, GameContent content, string trackId, int level)
        {
            var track = content.Upgrades.Track(trackId);
            if (track == null) return -1;
            level = System.Math.Max(0, System.Math.Min(track.MaxLevel, level));
            if (level == 0) profile.Loadout.Equipment.Remove(trackId);
            else profile.Loadout.Equipment[trackId] = level;
            return level;
        }

        /// <summary>Amène l'or exactement à `amount` (par le portefeuille, donc inscrit au journal « mode développeur »).</summary>
        public static void DevSetGold(PlayerProfile profile, int amount)
        {
            int balance = profile.Wallet.Balance(Wallet.Gold);
            if (balance < amount) profile.Wallet.Grant(Wallet.Gold, amount - balance, "mode développeur");
            else if (balance > amount) profile.Wallet.TrySpend(Wallet.Gold, balance - amount, "mode développeur");
        }

        /// <summary>Un palier est ouvert s'il y a assez d'étoiles ET si le palier précédent DE SA VOIE est acheté
        /// (E02-T02 : trois voies parallèles, chacune a son propre fil — le tier GLOBAL n'indique pas d'ordre entre voies).</summary>
        public static PurchaseResult TierAvailability(PlayerProfile profile, GameContent content, int tier)
        {
            var def = content.Upgrades.Tier(tier);
            if (def == null) return PurchaseResult.UnknownItem;
            if (def.PalierDansVoie > 1)
            {
                var precedent = content.Upgrades.Voie(def.Voie).FirstOrDefault(t => t.PalierDansVoie == def.PalierDansVoie - 1);
                if (precedent == null || !profile.Loadout.Talents.ContainsKey(precedent.Tier)) return PurchaseResult.NeedPreviousTier;
            }
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
