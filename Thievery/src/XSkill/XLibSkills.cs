using Vintagestory.API.Common;
using XLib.XLeveling;

namespace Thievery.XSkill
{
    public class XLibSkills
    {
        private ICoreAPI _api;

        public void Initialize(ICoreAPI api)
        {
            _api = api;
            /*XLeveling xleveling = api.ModLoader.GetModSystem<XLeveling>();
            Skill skill = xleveling.GetSkill("thievery", false);
            if (skill == null)
            {
                skill = new Skill("thievery", "Thievery", "Survival", 200, 1.33f, 25)
                {
                    DisplayName = "Thievery",
                    Group = "Survival"
                };
                xleveling.RegisterSkill(skill);
            }

            skill.ClassExpMultipliers["thief"] = 1.4f;

            Ability thiefAbility = new Ability(
                "thief", 
                "thievery:ability-thief", 
                "thievery:abilitydesc-thief", 
                1, 
                1, 
                new int[] { 0 }, 
                false
            );
            thiefAbility.OnPlayerAbilityTierChanged += OnThief;
            skill.AddAbility(thiefAbility);

            Ability speedyPickerAbility = new Ability(
                "speedypicker", 
                "thievery:ability-speedypicker", 
                "thievery:abilitydesc-speedypicker", 
                1, 
                4, 
                new int[] { 10, 15, 30, 50 }, 
                false
            );
            speedyPickerAbility.OnPlayerAbilityTierChanged += OnSpeedyPicker;
            skill.AddAbility(speedyPickerAbility);

            Ability carefulPickerAbility = new Ability(
                "carefulpicker", 
                "thievery:ability-carefulpicker", 
                "thievery:abilitydesc-carefulpicker", 
                1, 
                4, 
                new int[] { 25, 50, 50, 50 }, 
                false
            );
            carefulPickerAbility.OnPlayerAbilityTierChanged += OnCarefulPicker;
            skill.AddAbility(carefulPickerAbility);

            // Add Silent Picker
            Ability silentPickerAbility = new Ability(
                "silentpicker", 
                "thievery:ability-silentpicker", 
                "thievery:abilitydesc-silentpicker", 
                1, 
                4, 
                new int[] { 20, 40, 60, 80 }, 
                false
            );
            silentPickerAbility.OnPlayerAbilityTierChanged += OnSilentPicker;
            skill.AddAbility(silentPickerAbility);

            // Add Master Key
            Ability masterKeyAbility = new Ability(
                "masterkey", 
                "thievery:ability-masterkey", 
                "thievery:abilitydesc-masterkey", 
                1, 
                4, 
                new int[] { 0, 0, 0, 2 }, // 2% chance unlocks at tier 4
                false
            );
            masterKeyAbility.OnPlayerAbilityTierChanged += OnMasterKey;
            skill.AddAbility(masterKeyAbility);
            
            Ability locksmithLuckAbility = new Ability(
                "locksmithluck",
                "thievery:ability-locksmithluck",
                "thievery:abilitydesc-locksmithluck",
                1,
                4,
                new int[] { 10, 20, 35, 50 },
                false
            );
            locksmithLuckAbility.OnPlayerAbilityTierChanged += OnLocksmithLuck;
            skill.AddAbility(locksmithLuckAbility);
            
            Ability trapDisarmerAbility = new Ability(
                "trapdisarmer",
                "thievery:ability-trapdisarmer",
                "thievery:abilitydesc-trapdisarmer",
                1,
                4,
                new int[] { 10, 25, 50, 100 },
                false
            );
            trapDisarmerAbility.OnPlayerAbilityTierChanged += OnTrapDisarmer;
            skill.AddAbility(trapDisarmerAbility);
            
            Ability tripperAbility = new Ability(
                "tripper",
                "thievery:ability-tripper",
                "thievery:abilitydesc-tripper",
                1,
                4,
                new int[] { 10, 25, 50, 100 },
                false
            );
            tripperAbility.OnPlayerAbilityTierChanged += OnTripper;
            skill.AddAbility(tripperAbility);

            Ability burglarsEyeAbility = new Ability(
                "burglarseye",
                "thievery:ability-burglarseye",
                "thievery:abilitydesc-burglarseye",
                1,
                4,
                new int[] { 10, 25, 50, 100 },
                false
            );
            burglarsEyeAbility.OnPlayerAbilityTierChanged += OnBurglarsEye;
            skill.AddAbility(burglarsEyeAbility);*/
        }

        private void OnThief(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;
        }

        private void OnSpeedyPicker(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int reductionPercent = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("lockpickTimeReduction", reductionPercent);
        }

        private void OnCarefulPicker(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int damageReductionPercent = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("lockpickDamageReduction", damageReductionPercent);

            if (newTier >= 3)
            {
                int additionalReduction = newTier == 3 ? 25 : 50;
                entity.WatchedAttributes.SetInt("additionalDamageReduction", additionalReduction);
            }
        }

        private void OnSilentPicker(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int noiseReductionPercent = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("lockpickNoiseReduction", noiseReductionPercent);
        }

        private void OnMasterKey(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int unlockchance = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("unlockchance", unlockchance);
        }

        private void OnLocksmithLuck(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int picksavechance = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("picksavechance", picksavechance);
        }

        private void OnTrapDisarmer(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int trapdisarmchance = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("trapdisarmchance", trapdisarmchance);
        }

        private void OnTripper(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int trapavoidchance = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("trapavoidchance", trapavoidchance);
        }

        private void OnBurglarsEye(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            EntityPlayer entity = player.Entity;
            if (entity == null) return;

            int newTier = playerAbility.Tier;
            if (newTier <= 0 || playerAbility.Ability.Values == null || newTier > playerAbility.Ability.Values.Length)
            {
                return;
            }

            int lockview = playerAbility.Ability.Values[newTier - 1];
            entity.WatchedAttributes.SetInt("lockview", lockview);
        }

    }
}
