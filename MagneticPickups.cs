using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using System.Collections.ObjectModel;
using System.Collections;

namespace MagneticPickups
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.blappole.magneticpickups", "Magnetic Pickups", "1.0.0")]
    public class MagneticPickups : BaseUnityPlugin
    {
        public static ConfigEntry<float> PickupSpeed { get; set; }
        public static ConfigEntry<float> PickupMoveUpwardsSpeed { get; set; }
        public static ConfigFile MagneticPickupsConfigFile { get; set; }

        public void Awake()
        {
            // Set default config values, or load them.
            MagneticPickupsConfigFile = new ConfigFile(Paths.ConfigPath + @"\MagneticPickups.cfg", true);

            PickupSpeed = MagneticPickupsConfigFile.Bind(new ConfigDefinition("Pickups", "PickupSpeed"), 75f,
                new ConfigDescription("The speed with which the pickups will move towards players"));
            PickupMoveUpwardsSpeed = MagneticPickupsConfigFile.Bind(new ConfigDefinition("Pickups", "PickupMoveUpwardsSpeed"), 1.25f,
                new ConfigDescription("The upwards velocity to add to the pickups before they start moving towards a player"));

            Logger.LogMessage("Loaded Magnetic Pickups!");

            On.RoR2.GravitatePickup.FixedUpdate += (orig, self) =>
            {
                orig.Invoke(self);

                // Move the pickup upwards.
                self.rigidbody.velocity += Vector3.up * PickupMoveUpwardsSpeed.Value;

                // Retrieve players and get the closest one.
                var players = TeamComponent.GetTeamMembers(TeamIndex.Player);
                var location = GetClosestPlayerLocation(players, self.transform.position);

                var speed = Vector3.MoveTowards(self.rigidbody.velocity, (self.transform.position - location).normalized * 100f, PickupSpeed.Value);
                self.rigidbody.velocity += -speed;
            };
        }

        private Vector3 GetClosestPlayerLocation(ReadOnlyCollection<TeamComponent> players, Vector3 location)
        {
            var closestPosition = Vector3.positiveInfinity;
            var lowestDistance = float.MaxValue;
            foreach (TeamComponent teamComponent in players)
            {
                NetworkUser networkBody = Util.LookUpBodyNetworkUser(teamComponent.gameObject);
                if (!networkBody)
                {
                    continue;
                }

                var distance = Vector3.Distance(teamComponent.body.footPosition, location);
                if (distance < lowestDistance)
                {
                    closestPosition = teamComponent.body.footPosition;
                    lowestDistance = distance;
                }
            }
            return closestPosition;
        }
    }
}
