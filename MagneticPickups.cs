using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using System.Collections.ObjectModel;

namespace MagneticPickups
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.blappole.magneticpickups", "Magnetic Pickups", "1.1.0")]
    public class MagneticPickups : BaseUnityPlugin
    {
        internal static MagneticPickups Instance;
        public static ConfigEntry<float> PickupSpeed { get; set; }
        public static ConfigEntry<float> PickupMoveUpwardsSpeed { get; set; }
        public static ConfigEntry<float> PickupRadius { get; set; }
        public static ConfigFile MagneticPickupsConfigFile { get; set; }

        public void Awake()
        {
            Instance = this;

            // Set default config values, or load them.
            MagneticPickupsConfigFile = new ConfigFile(Paths.ConfigPath + @"\MagneticPickups.cfg", true);

            PickupSpeed = MagneticPickupsConfigFile.Bind(new ConfigDefinition("Pickups", "PickupSpeed"), 25f,
                new ConfigDescription("The speed with which the pickups will move towards players"));
            PickupMoveUpwardsSpeed = MagneticPickupsConfigFile.Bind(new ConfigDefinition("Pickups", "PickupMoveUpwardsSpeed"), 1.25f,
                new ConfigDescription("The upwards velocity to add to the pickups before they start moving towards a player"));
            PickupRadius = MagneticPickupsConfigFile.Bind(new ConfigDefinition("Pickups", "PickupRadius"), 30f,
                new ConfigDescription("How close a pickup must be before it will begin to move towards a player"));

            Logger.LogMessage("Loaded Magnetic Pickups!");

            On.RoR2.GravitatePickup.FixedUpdate += (orig, self) =>
            {
                orig.Invoke(self);
                // Retrieve players and get the closest one.
                var players = TeamComponent.GetTeamMembers(TeamIndex.Player);
                var location = GetClosestPlayerLocation(players, self.transform.position);

                // Move the pickup towards the player's location if they are within the radius.
                if (Vector3.Distance(self.rigidbody.position, location) < PickupRadius.Value)
                {
                    // Set the velocity to 0 to begin with to remove any gravity.
                    self.rigidbody.velocity = Vector3.zero;

                    // Move the pickup upwards, if it is not already high above the ground and there's nothing directly above it.
                    bool didHitUp = Physics.Raycast(self.rigidbody.position, self.rigidbody.transform.up, out RaycastHit upwardsHit);
                    bool didHitDown = Physics.Raycast(self.rigidbody.position, -self.rigidbody.transform.up, out RaycastHit downwardsHit);
                    if ((!didHitUp || (didHitUp && upwardsHit.distance > 10f)) && didHitDown && downwardsHit.distance < 250f)
                    {
                        self.rigidbody.velocity += Vector3.up * PickupMoveUpwardsSpeed.Value;
                    }

                    var speed = Vector3.MoveTowards(self.rigidbody.velocity, (self.transform.position - location).normalized * 100f, PickupSpeed.Value);
                    self.rigidbody.velocity += -speed;
                }
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
