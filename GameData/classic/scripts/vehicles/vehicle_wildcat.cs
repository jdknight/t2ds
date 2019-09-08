//**************************************************************
// WILDCAT GRAV CYCLE
//**************************************************************
//**************************************************************
// SOUNDS
//**************************************************************
datablock EffectProfile(ScoutEngineEffect)
{
   effectname = "vehicles/outrider_engine";
   minDistance = 5.0;
   maxDistance = 10.0;
};

datablock EffectProfile(ScoutThrustEffect)
{
   effectname = "vehicles/outrider_boost";
   minDistance = 5.0;
   maxDistance = 10.0;
};

datablock AudioProfile(ScoutSqueelSound)
{
   filename    = "fx/vehicles/outrider_skid.wav";
   description = ClosestLooping3d;
   preload = true;
};

// Scout
datablock AudioProfile(ScoutEngineSound)
{
   filename    = "fx/vehicles/outrider_engine.wav";
   description = AudioDefaultLooping3d;
   preload = true;
   effect = ScoutEngineEffect;
};

datablock AudioProfile(ScoutThrustSound)
{
   filename    = "fx/vehicles/outrider_boost.wav";
   description = AudioDefaultLooping3d;
   preload = true;
   effect = ScoutThrustEffect;
};

//**************************************************************
// LIGHTS
//**************************************************************
datablock RunningLightData(WildcatLight1)
{
   radius = 1.0;
   color = "1.0 1.0 1.0 0.3";
   nodeName = "Headlight_node01";
   direction = "-1.0 1.0 0.0";
   texture = "special/headlight4";
};

datablock RunningLightData(WildcatLight2)
{
   radius = 1.0;
   color = "1.0 1.0 1.0 0.3";
   nodeName = "Headlight_node02";
   direction = "1.0 1.0 0.0";
   texture = "special/headlight4";
};

datablock RunningLightData(WildcatLight3)
{
   type = 2;
   radius = 100.0;
   color = "1.0 1.0 1.0 1.0";
   offset = "0.0 0.0 0.0";
   direction = "0.0 1.0 0.0";
   texture = "special/projheadlight";
};

//**************************************************************
// VEHICLE CHARACTERISTICS
//**************************************************************

datablock HoverVehicleData(ScoutVehicle) : WildcatDamageProfile
{
   spawnOffset = "0 0 1";

   floatingGravMag = 3.5;

   catagory = "Vehicles";
   shapeFile = "vehicle_grav_scout.dts";
   computeCRC = true;

   debrisShapeName = "vehicle_grav_scout_debris.dts";
   debris = ShapeDebris;
   renderWhenDestroyed = false;

   drag = 0.0;
   density = 0.9;

   mountPose[0] = scoutRoot;
   cameraMaxDist = 5.0;
   cameraOffset = 0.7;
   cameraLag = 0.5;
   numMountPoints = 1;
   isProtectedMountPoint[0] = true;
   explosion = VehicleExplosion;
	explosionDamage = 0.5;
	explosionRadius = 5.0;

   lightOnly = 1;

   maxDamage = 0.60;
   destroyedLevel = 0.60;

   isShielded = true;
   rechargeRate = 0.7;
   energyPerDamagePoint = 95; // z0dd - ZOD, 3/30/02. Bike shield is less protective. was 75
   maxEnergy = 150;
   minJetEnergy = 15;
   jetEnergyDrain = 1.3;

   // Rigid Body
   mass = 400;
   bodyFriction = 0.1;
   bodyRestitution = 0.5;  
   softImpactSpeed = 20;       // Play SoftImpact Sound
   hardImpactSpeed = 28;      // Play HardImpact Sound

   // Ground Impact Damage (uses DamageType::Ground)
   minImpactSpeed = 29;
   speedDamageScale = 0.010;

   // Object Impact Damage (uses DamageType::Impact)
   collDamageThresholdVel = 23;
   collDamageMultiplier   = 0.030;

   dragForce            = 25 / 45.0;
   vertFactor           = 0.0;
   floatingThrustFactor = 0.35;

   mainThrustForce    = 35; // z0dd - ZOD, 3/30/02. Bike main thruster more powerful. was 30
   reverseThrustForce = 10;
   strafeThrustForce  = 8;
   turboFactor        = 1.80; // z0dd - ZOD, 3/30/02. Bike turbo thruster more powerful. was 1.5

   brakingForce = 25;
   brakingActivationSpeed = 4;

   stabLenMin = 2.25;
   stabLenMax = 3.75;
   stabSpringConstant  = 30;
   stabDampingConstant = 16;

   gyroDrag = 16;
   normalForce = 30;
   restorativeForce = 20;
   steeringForce = 30;
   rollForce  = 15;
   pitchForce = 7;

   dustEmitter = VehicleLiftoffDustEmitter;
   triggerDustHeight = 2.5;
   dustHeight = 1.0;
   dustTrailEmitter = TireEmitter;
   dustTrailOffset = "0.0 -1.0 0.5";
   triggerTrailHeight = 3.6;
   dustTrailFreqMod = 15.0;

   jetSound         = ScoutSqueelSound;
   engineSound      = ScoutEngineSound;
   floatSound       = ScoutThrustSound;
   softImpactSound  = GravSoftImpactSound;
   hardImpactSound  = HardImpactSound;
   //wheelImpactSound = WheelImpactSound;

   //
   softSplashSoundVelocity = 10.0; 
   mediumSplashSoundVelocity = 20.0;   
   hardSplashSoundVelocity = 30.0;   
   exitSplashSoundVelocity = 10.0;
   
   exitingWater      = VehicleExitWaterSoftSound;
   impactWaterEasy   = VehicleImpactWaterSoftSound;
   impactWaterMedium = VehicleImpactWaterSoftSound;
   impactWaterHard   = VehicleImpactWaterMediumSound;
   waterWakeSound    = VehicleWakeSoftSplashSound; 

   minMountDist = 4;

   damageEmitter[0] = SmallLightDamageSmoke;
   damageEmitter[1] = SmallHeavyDamageSmoke;
   damageEmitter[2] = DamageBubbles;
   damageEmitterOffset[0] = "0.0 -1.5 0.5 ";
   damageLevelTolerance[0] = 0.3;
   damageLevelTolerance[1] = 0.7;
   numDmgEmitterAreas = 1;

   splashEmitter[0] = VehicleFoamDropletsEmitter;
   splashEmitter[1] = VehicleFoamEmitter;

   shieldImpact = VehicleShieldImpact;
   
   forwardJetEmitter = WildcatJetEmitter;

   cmdCategory = Tactical;
   cmdIcon = CMDHoverScoutIcon;
   cmdMiniIconName = "commander/MiniIcons/com_landscout_grey";
   targetNameTag = 'WildCat';
   targetTypeTag = 'Grav Cycle';
   sensorData = VehiclePulseSensor;
   sensorRadius = VehiclePulseSensor.detectRadius; // z0dd - ZOD, 3/30/02. Allows sensor to be shown on CC

   checkRadius = 1.7785;
   observeParameters = "1 10 10";

   runningLight[0] = WildcatLight1;
   runningLight[1] = WildcatLight2;
   runningLight[2] = WildcatLight3;

   shieldEffectScale = "0.9375 1.125 0.6";
};

//**************************************************************
// WEAPONS: z0dd - ZOD, 5/14/02
//**************************************************************

$DeathMessageCTurretTeamKill[$DamageType::Bullet, 0] = '\c0%4 TEAMKILLED %1 by strafing from a Wildcat.';

$DeathMessageCTurretKill[$DamageType::Bullet, 0] = '\c0%4 turns %1 into swiss cheese with %6 Wildcat.';
$DeathMessageCTurretKill[$DamageType::Bullet, 1] = '\c0The lead from %4\'s Wildcat turns %1 into finely shredded meat.';
$DeathMessageCTurretKill[$DamageType::Bullet, 2] = '\c0%4 drills %1 full of holes with %6 Wildcat.';

datablock EffectProfile(GravChaingunFireEffect)
{
   effectname = "weapons/chaingun_fire";
   minDistance = 5.0;
   maxDistance = 10.0;
};

datablock AudioProfile(GravChaingunFireSound)
{
   filename    = "fx/vehicles/tank_chaingun.wav";
   description = AudioDefaultLooping3d;
   preload = true;
   effect = GravChaingunFireEffect;
};

datablock AudioProfile(GravChaingunDryFireSound)
{
   filename    = "fx/weapons/chaingun_off.wav";
   description = AudioClose3d;
   preload = true;
};

datablock AudioProfile(GravChaingunIdleSound)
{
   filename    = "fx/misc/diagnostic_on.wav";
   description = ClosestLooping3d;
   preload = true;
};

datablock TracerProjectileData(GravBullet) : ChaingunBullet
{
   projectileShapeName = "chaingun_shot.dts";
   directDamage = 0.135;
};

datablock ShapeBaseImageData(GravChaingunImage)
{
   className = WeaponImage;
   shapeFile = "weapon_chaingun.dts";
   item      = Chaingun;
   offset = "-0.1 0.68 +0.26";
   //rotation = "0 0 0 90";
   projectile = GravBullet; //ChaingunBullet;
   projectileType = TracerProjectile;
   projectileSpread = 18.0 / 1000.0; // 10
   emap = true;
   mountPoint = 10;
   usesEnergy = true;
   useMountEnergy = true;
   // DAVEG -- balancing numbers below!
   minEnergy = 8;
   fireEnergy = 1.55;

   stateName[0] = "Activate";
   stateSequence[0] = "Activate";
   stateSound[0] = GravChaingunIdleSound;
   stateAllowImageChange[0] = false;
   stateTimeoutValue[0] = 0.1;
   stateTransitionOnTimeout[0] = "Ready";
   stateTransitionOnNoAmmo[0] = "NoAmmo";

   stateName[1] = "Ready";
   stateSpinThread[1] = Stop;
   stateTransitionOnTriggerDown[1] = "Spinup";
   stateTransitionOnNoAmmo[1] = "NoAmmo";

   stateName[2] = "NoAmmo";
   stateTransitionOnAmmo[2] = "Ready";
   stateSpinThread[2] = Stop;
   stateTransitionOnTriggerDown[2] = "DryFire";

   stateName[3] = "Spinup";
   stateSpinThread[3] = SpinUp;
   //stateSound[3] = ChaingunSpinupSound;
   stateTimeoutValue[3] = 0.05;
   stateWaitForTimeout[3] = false;
   stateTransitionOnTimeout[3] = "Fire";
   stateTransitionOnTriggerUp[3] = "Spindown";

   stateName[4] = "Fire";
   stateSequence[4] = "Fire";
   stateSequenceRandomFlash[4] = true;
   stateSpinThread[4] = FullSpeed;
   stateSound[4] = GravChaingunFireSound;
   //stateRecoil[4] = LightRecoil;
   stateAllowImageChange[4] = false;
   stateScript[4] = "onFire";
   stateFire[4] = true;
   stateEjectShell[4] = true;
   stateTimeoutValue[4] = 0.1;
   stateTransitionOnTimeout[4] = "Fire";
   stateTransitionOnTriggerUp[4] = "Spindown";
   stateTransitionOnNoAmmo[4] = "EmptySpindown";

   stateName[5] = "Spindown";
   //stateSound[5] = ChaingunSpinDownSound;
   stateSpinThread[5] = SpinDown;
   stateTimeoutValue[5] = 0.05;
   stateWaitForTimeout[5] = true;
   stateTransitionOnTimeout[5] = "Ready";
   stateTransitionOnTriggerDown[5] = "Spinup";

   stateName[6] = "EmptySpindown";
   //stateSound[6] = ChaingunSpinDownSound;
   stateSpinThread[6] = SpinDown;
   stateTimeoutValue[6] = 0.5;
   stateTransitionOnTimeout[6] = "NoAmmo";

   stateName[7] = "DryFire";
   stateSound[7] = GravChaingunDryFireSound;
   stateTimeoutValue[7] = 1.0;
   stateTransitionOnTimeout[7] = "NoAmmo";
};

package wildcat
{
function ScoutVehicle::onAdd(%this, %obj)
{
   Parent::onAdd(%this, %obj);
   %obj.mountImage(GravChaingunImage, 0);
   %obj.setImageTrigger(0, false);
   %obj.schedule(5500, "playThread", $ActivateThread, "activate");
}

function ScoutVehicle::playerMounted(%data, %obj, %player, %node)
{
   // scout vehicle == SUV (single-user vehicle)
   commandToClient(%player.client, 'setHudMode', 'Pilot', "Hoverbike", %node);

   // z0dd - ZOD, 5/14/02. Create a weapon hud and reticle
   commandToClient(%player.client, 'ShowVehicleWeapons', "Hoverbike");

   // update observers who are following this guy...
   if( %player.client.observeCount > 0 )
      resetObserveFollow( %player.client, false );
}

function ScoutVehicle::playerDismounted(%data, %obj, %player)
{
   %obj.setImageTrigger(0, false);
   setTargetSensorGroup(%obj.getTarget(), %obj.team);
   if( %player.client.observeCount > 0 )
      resetObserveFollow( %player.client, true );
}
};

activatePackage(wildcat);