using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.Components;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Sandbox.Game.AI.Pathfinding.Obsolete;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Utils;
using VRageMath;
using System.Security;

// Common tweaks for all GV Servers (no damage or combat changes)
// Code is based on Gauge's Balanced Deformation code, but heavily modified for more control. 
namespace GVTweaks.BlockTweaks
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class BlockTweaks : MySessionComponentBase
    {

        public const double hydroTankH2Density = 15000000 / (2.5 * 2.5 * 2.5 * 27); // LG Large hydro tank capacity divided by its volume in meters

        private readonly MyComponentDefinition steelPlateComponent = MyDefinitionManager.Static.GetComponentDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Component), "SteelPlate"));

        private void DoWork()
        {
            foreach (var blockDef in MyDefinitionManager.Static.GetDefinitionsOfType<MyCubeBlockDefinition>())
            {
                var turretDef = blockDef as MyLargeTurretBaseDefinition;
                var weaponDef = blockDef as MyWeaponBlockDefinition;
                var sorterDef = blockDef as MyConveyorSorterDefinition;
                var drillDef = blockDef as MyShipDrillDefinition;
                var pistonBaseDef = blockDef as MyPistonBaseDefinition;
                var beaconDef = blockDef as MyBeaconDefinition;
                var suspensionDef = blockDef as MyMotorSuspensionDefinition;
                var statorDef = blockDef as MyMotorStatorDefinition; //Motor stator is the base
                var advStatorDef = blockDef as MyMotorAdvancedStatorDefinition; //Motor stator is the base
                var thrustDef = blockDef as MyThrustDefinition;
                var gyroDef = blockDef as MyGyroDefinition;
                var upgradeModuleDef = blockDef as MyUpgradeModuleDefinition;
                var cockpitDef = blockDef as MyCockpitDefinition;
                var remoteControlDef = blockDef as MyRemoteControlDefinition;
                var timerBlockDef = blockDef as MyTimerBlockDefinition;
                var hydroTankDef = blockDef as MyGasTankDefinition;
                var welderDef = blockDef as MyShipWelderDefinition;
                var batteryDef = blockDef as MyBatteryBlockDefinition;
                var laserAntennaDef = blockDef as MyLaserAntennaDefinition;
                var cargoDef = blockDef as MyCargoContainerDefinition;
				var reactorDef = blockDef as MyReactorDefinition;
				var solarDef = blockDef as MySolarPanelDefinition;

                //light armor resistance and deformation
                if (blockDef.EdgeType == "Light" && blockDef.BlockTopology != MyBlockTopology.TriangleMesh)
                {
                    if (blockDef.CubeSize == MyCubeSize.Large)
                    {
                        blockDef.GeneralDamageMultiplier = 1.0f;
                        blockDef.DeformationRatio = 0.4f; //this also affects impact resistance
                    }

                    if (blockDef.CubeSize == MyCubeSize.Small)
                    {
                        blockDef.GeneralDamageMultiplier = 1.0f;
                        blockDef.DeformationRatio = 0.4f;
                    }
                    //blockDef.PCU = lightArmorPCU;
                }

                //heavy armor resistance and deformation, and functional component order flip
                if (blockDef.EdgeType == "Heavy")
                {
                    if (blockDef.CubeSize == MyCubeSize.Large)
                    {
                        blockDef.GeneralDamageMultiplier = 1.0f; //vanilla is all over the place
                        blockDef.DeformationRatio = 0.2f;
                    }

                    if (blockDef.CubeSize == MyCubeSize.Small)
                    {
                        blockDef.GeneralDamageMultiplier = 1.0f;
                        blockDef.DeformationRatio = 0.2f;
                    }

                    var lastCompIdx = blockDef.Components.Length - 1;
                    if (blockDef.Components[0].Count > blockDef.Components[lastCompIdx].Count && blockDef.Components[0].Definition.Id == blockDef.Components[lastCompIdx].Definition.Id)
                    {
                        var temp = blockDef.Components[0];
                        blockDef.Components[0] = blockDef.Components[lastCompIdx];
                        blockDef.Components[lastCompIdx] = temp;
                    }

                    // If no AwwScrap uncomment SetRatios
                    SetRatios(blockDef, blockDef.CriticalGroup);
                    // If we're using awwscrap, comment out the SetRatios above and uncomment SortAndSplitArmor below
                    //SortAndSplitArmor(blockDef);

                    //blockDef.PCU = blastDoorPCU;
                }

                //rotors (includes hinges)
                if (statorDef != null)
                {
                    statorDef.GeneralDamageMultiplier = 0.5f;
                }

                //adv rotors
                if (advStatorDef != null)
                {
                    advStatorDef.GeneralDamageMultiplier = 0.5f;
                }

                //rotor and hinge top parts
                if (blockDef.Id.SubtypeName.Contains("Rotor") || blockDef.Id.SubtypeName.Contains("HingeHead"))
                {
                    blockDef.GeneralDamageMultiplier = 0.5f;
                }

				//Standardize H2 tank capacity to scale linearly with block volume
                if (hydroTankDef != null && hydroTankDef.StoredGasId.SubtypeName == "Hydrogen")
                {
                    hydroTankDef.LeakPercent = 0.025f;
                    hydroTankDef.Capacity = (float)Math.Ceiling(hydroTankDef.Size.Volume() * Math.Pow(hydroTankDef.CubeSize == MyCubeSize.Large ? 2.5 : 0.5, 3) * hydroTankH2Density);
                    hydroTankDef.GasExplosionMaxRadius = hydroTankDef.Size.Length() * (hydroTankDef.CubeSize == MyCubeSize.Large ? 2.5f : 0.5f);
                    hydroTankDef.GasExplosionDamageMultiplier = 0.00015f;
                    if (string.IsNullOrEmpty(hydroTankDef.GasExplosionSound))
                    {
                        hydroTankDef.GasExplosionSound = "HydrogenExplosion";
                    }
                    hydroTankDef.GasExplosionNeededVolumeToReachMaxRadius = hydroTankDef.Capacity;
                }
				
				//adjusting output of all reactors
                if (reactorDef != null)
				{
					if (reactorDef.CubeSize == MyCubeSize.Large)
					{
						if (reactorDef.Size.Volume() <= 1f)
						{
							reactorDef.MaxPowerOutput = 20f; // 2:1 power output density to batteries
						}
						else
						{
							reactorDef.MaxPowerOutput = 600f; // Bonus for large variant
						}
					}
					else
					{
						if (reactorDef.Size.Volume() <= 1f)
						{
							reactorDef.MaxPowerOutput = 1.0f; // 4:1 power output density to batteries
						}
						else
						{
							reactorDef.MaxPowerOutput = 30f; // Bonus for large variant
						}
					}
					//buffing output of NPC Proprietary reactors, and making them not require fuel
					if (reactorDef.Id.SubtypeName.Contains("Proprietary"))
					{
						reactorDef.MaxPowerOutput *= 5f;
						reactorDef.FuelInfos = new MyReactorDefinition.FuelInfo[0];
						//reactorDef.FuelInfos[0].Ratio = 100f; //this is readonly and doesnt work, same for H2 engines
					}
                }

				//buffing output of solar to compensate for banned solar tracking scripts
                if (solarDef != null)
                {
                    solarDef.MaxPowerOutput *= 2f;
                }

				//remove LOS check for laser antenna
                if (laserAntennaDef != null)
                {
                    laserAntennaDef.RequireLineOfSight = false;
                }
								
				//Adjust container components to be proportional to block volume
                if (cargoDef != null && cargoDef.CubeSize == MyCubeSize.Large && cargoDef.Id.SubtypeName.Contains("Container"))
                {
                    ReplaceComponent(cargoDef, cargoDef.Components.Length - 1, steelPlateComponent, cargoDef.Size.Volume() > 1 ? 120 : 40);
                }
								
				//Make all 5x5 XL blocks have light edge type, and no deformation, and increase weld time
                if (blockDef.CubeSize == MyCubeSize.Large && blockDef.Id.SubtypeName.Contains("XL_") && blockDef.BlockTopology == MyBlockTopology.TriangleMesh)
                {
					blockDef.GeneralDamageMultiplier = 1.0f;
					blockDef.UsesDeformation = false;
					blockDef.DeformationRatio = 0.45f; //this seems to be a sweet spot between completely immune to collision, and popping with more than a light bump.
					blockDef.EdgeType = "Heavy";
					blockDef.IntegrityPointsPerSec = 2500;
                }				
            }
        }

        public override void LoadData()
        {
            DoWork();
        }

        // Method to replace components in a block construction list
		private static void ReplaceComponent(MyCubeBlockDefinition blockDef, int index, MyComponentDefinition newComp, int newCount, MyPhysicalItemDefinition deconstructItem = null)
        {
            var comp = blockDef.Components[index];
            var oldCount = comp.Count;
            float intDiff;
            float massDiff;
            if (newCount > 0)
            {
                intDiff = newComp.MaxIntegrity * newCount - comp.Definition.MaxIntegrity * oldCount;
                massDiff = newComp.Mass * newCount - comp.Definition.Mass * oldCount;

                blockDef.Components[index].Count = newCount;
            }
            else
            {
                intDiff = (newComp.MaxIntegrity - comp.Definition.MaxIntegrity) * oldCount;
                massDiff = (newComp.Mass - comp.Definition.Mass) * oldCount;
            }

            comp.Definition = newComp;
            comp.DeconstructItem = deconstructItem ?? newComp;

            blockDef.MaxIntegrity += intDiff;
            blockDef.Mass += massDiff;

            SetRatios(blockDef, blockDef.CriticalGroup);
        }

        // Method to insert components into block construction list
        private static void InsertComponent(MyCubeBlockDefinition blockDef, int componentIndex, MyComponentDefinition comp, int count, MyPhysicalItemDefinition deconstructItem = null, bool makeCritical = false)
        {
            var intDiff = comp.MaxIntegrity * count;
            var massDiff = comp.Mass * count;

            if (makeCritical)
            {
                blockDef.CriticalGroup = (ushort)componentIndex;
            }
            else
            if (componentIndex <= blockDef.CriticalGroup)
            {
                blockDef.CriticalGroup += 1;
            }

            blockDef.MaxIntegrity += intDiff;
            blockDef.Mass += massDiff;

            var newComps = new MyCubeBlockDefinition.Component[blockDef.Components.Length + 1];

            if (componentIndex == 0)
            {
                newComps[0] = new MyCubeBlockDefinition.Component
                {
                    Definition = comp,
                    DeconstructItem = deconstructItem ?? comp,
                    Count = count
                };
                blockDef.Components.CopyTo(newComps, 1);
            }
            else if (componentIndex == blockDef.Components.Length)
            {
                newComps[blockDef.Components.Length] = new MyCubeBlockDefinition.Component
                {
                    Definition = comp,
                    DeconstructItem = comp,
                    Count = count
                };
                blockDef.Components.CopyTo(newComps, 0);
            }
            else
            {
                for (var index = 0; index < newComps.Length; index++)
                {
                    if (index < componentIndex)
                    {
                        newComps[index] = blockDef.Components[index];
                    }
                    else if (index == componentIndex)
                    {
                        newComps[index] = new MyCubeBlockDefinition.Component
                        {
                            Definition = comp,
                            DeconstructItem = comp,
                            Count = count
                        };
                    }
                    else
                    {
                        newComps[index] = blockDef.Components[index - 1];
                    }
                }
            }

            blockDef.Components = newComps;

            SetRatios(blockDef, blockDef.CriticalGroup);
        }

        private static void ChangeComponentCount(MyCubeBlockDefinition blockDef, int index, int newCount)
        {
            var comp = blockDef.Components[index];
            var oldCount = comp.Count;
            var intDiff = comp.Definition.MaxIntegrity * (newCount - oldCount);
            var massDiff = comp.Definition.Mass * (newCount - oldCount);

            comp.Count = newCount;

            blockDef.MaxIntegrity += intDiff;
            blockDef.Mass += massDiff;

            SetRatios(blockDef, blockDef.CriticalGroup);
        }

        private void SortAndSplitArmor(MyCubeBlockDefinition blockDef)
        {
            if (blockDef.Components.Length <= 1 || blockDef.CriticalGroup == blockDef.Components.Length - 1)
            {
                return;
            }
            var nextCompIndex = MathHelper.Clamp(blockDef.CriticalGroup + 1, 0, blockDef.Components.Length - 1);
            var nextCompLow = (int)Math.Floor(blockDef.Components[nextCompIndex].Count / 2f);
            var nextCompHigh = (int)Math.Ceiling(blockDef.Components[nextCompIndex].Count / 2f);
            blockDef.Components[nextCompIndex].Count = nextCompLow;
            InsertComponent(blockDef, nextCompIndex, blockDef.Components[nextCompIndex].Definition, nextCompHigh, makeCritical: true);
        }

		// Method to set ratio of critical component and ownership of a block
        private static void SetRatios(MyCubeBlockDefinition blockDef, int criticalIndex)
        {
            var criticalIntegrity = 0f;
            var ownershipIntegrity = 0f;
            for (var index = 0; index <= criticalIndex; index++)
            {
                var component = blockDef.Components[index];
                if (ownershipIntegrity == 0f && component.Definition.Id.SubtypeName == "Computer")
                {
                    ownershipIntegrity = criticalIntegrity + component.Definition.MaxIntegrity;
                }

                criticalIntegrity += component.Count * component.Definition.MaxIntegrity;
                if (index == criticalIndex)
                {
                    criticalIntegrity -= component.Definition.MaxIntegrity;
                }
            }

            blockDef.CriticalIntegrityRatio = criticalIntegrity / blockDef.MaxIntegrity;
            blockDef.OwnershipIntegrityRatio = ownershipIntegrity / blockDef.MaxIntegrity;

            var count = blockDef.BuildProgressModels.Length;
            for (var index = 0; index < count; index++)
            {
                var buildPercent = (index + 1f) / count;
                blockDef.BuildProgressModels[index].BuildRatioUpperBound = buildPercent * blockDef.CriticalIntegrityRatio;
            }
        }
    }
}