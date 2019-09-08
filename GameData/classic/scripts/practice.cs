////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: PRACTICE HUD SERVER CALLS ///////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////

function serverCMDpracticeHudInitialize(%client, %val)
{
   Game.initPracticeHud(%client, %val);
}

function serverCmdpracticeBtnCall(%client, %btn, %val)
{
   Game.practiceBtnCmd(%client, %btn, %val);
}

function serverCmdpracticeUpdateSettings(%client, %opt, %val)
{
   // USAGE: commandToServer(%opt, %val);
   // %opt is the index # of the hud option
   // %val is the index # of the option setting

   Game.updatePracticeHudSet(%client, %opt, %val);
}

function serverCmdneedPracHudUpdate(%client)
{
   Game.sendPracHudUpdate(%client, "");
}

function sendAllPracHudUpdate(%game, %msg)
{
   %count = ClientGroup.getCount();
   for(%i = 0; %i < %count; %i++)
   {
      %cl = ClientGroup.getObject(%i);
      %game.sendPracHudUpdate(%cl, %msg);
   }
}

/////////////////////////////////////////////////////////////////////////////////////////
// Practice Mode projectile and effects datablocks //////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

//Mortar
datablock ExplosionData(UnderwaterObsMortarExp) : UnderwaterMortarExplosion
{
   shakeCamera = false;
};

datablock ExplosionData(ObsMortarExp) : MortarExplosion
{
   shakeCamera = false;
};

datablock GrenadeProjectileData(ObsMortarShot) : MortarShot
{
   explosion = "ObsMortarExp";
   underwaterExplosion = "UnderwaterObsMortarExp";
};

//Tank Mortar
datablock GrenadeProjectileData(ObsAssaultMortar) : AssaultMortar
{
   explosion = "ObsMortarExp";
};

//Missile
datablock ExplosionData(ObsMissileExp) : MissileExplosion
{
   shakeCamera = false;
};

datablock SeekerProjectileData(ObsShoulderMissile) : ShoulderMissile
{
   explosion = "ObsMissileExp";
};

//GrandeLauncher
datablock ExplosionData(UnderwaterObsGrenadeExp) : UnderwaterGrenadeExplosion
{ 
   shakeCamera = false;
};

datablock ExplosionData(ObsGrenadeExp) : GrenadeExplosion
{
   shakeCamera = false;
};

datablock GrenadeProjectileData(ObsGrenade) : BasicGrenade
{
   explosion           = "ObsGrenadeExp";
   underwaterExplosion = "UnderwaterObsGrenadeExp";
};

//Disc
datablock ExplosionData(UnderwaterObsDiscExp) : UnderwaterDiscExplosion
{
   shakeCamera = false;
};

datablock ExplosionData(ObsDiscExp) : DiscExplosion
{
   shakeCamera = false;
};

datablock LinearProjectileData(ObsDiscProjectile) : DiscProjectile
{
   explosion  = "ObsDiscExp";
   underwaterExplosion = "UnderwaterObsDiscExp";
};

/////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: Practice Mode transfer pad functions and datablocks //////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

datablock ItemData(TransferPad)
{
   className = HandInventory;
   catagory = "Misc";
   shapeFile = "Nexuscap.dts";
   mass = 1;
   elasticity = 0.2;
   friction = 0.8;
   pickupRadius = 1;
   pickUpName = "a deployable transfer pad";
   computeCRC = false;
};

datablock StaticShapeData(DeployedTransferPad) : StaticShapeDamageProfile
{
   shapeFile = "Nexuscap.dts";
   explosion = DeployablesExplosion;
   maxDamage = 100;
   disabledLevel = 100;
   destroyedLevel = 100;
   dynamicType = $TypeMasks::SensorObjectType;
   deployedObject = true;

   cmdCategory = "DSupport";
   cmdIcon = "CMDSwitchIcon";
   cmdMiniIconName = "commander/MiniIcons/com_switch_grey";
   targetNameTag = 'Deployed';
   targetTypeTag = 'transfer pad';

   debrisShapeName = "debris_generic_small.dts";
   debris = SmallShapeDebris;
};

// Are max telepads deployed?
function maxSpawnPads(%obj)
{
   for( %i = 0; %i < $Host::ClassicMaxTelepads; %i++ )
   {
      if(%obj.client.spawnPad[%i] $="")
         return 0;
   }
   return 1;
}

// Deploy a transfer telepad
function deployTransferPad(%data, %obj)
{
   if(%obj.inv[%data.getName()] <= 0)
   {
      return 0;   
   }
   %eyePos = posFromTransform(%obj.getEyeTransform());
   %scEyeVec = VectorScale(VectorNormalize(%obj.getEyeVector()), 4.25);
   %eyeEnd = VectorAdd(%eyePos, %scEyeVec);
   %mask = $TypeMasks::TerrainObjectType | $TypeMasks::InteriorObjectType;
   %searchResult = containerRayCast(%eyePos, %eyeEnd, %mask, 0);
   if(!%searchResult)
   {
      if(%obj.inv[%data.getName()] > 0)
         messageClient(%obj.client, 'MsgBeaconNoSurface', '\c2Cannot place pad. Too far from surface.');

      return 0;
   }
   %test = ($TypeMasks::VehicleObjectType | $TypeMasks::MoveableObjectType |
            $TypeMasks::StaticShapeObjectType | $TypeMasks::ForceFieldObjectType |
            $TypeMasks::ItemObjectType | $TypeMasks::PlayerObjectType |
            $TypeMasks::TurretObjectType | $TypeMasks::StaticTSObjectType);

   InitContainerRadiusSearch( %eyeEnd, 2.5, %test );
   %res = containerSearchNext();
   if(%res)
   {
      messageClient(%obj.client, 'MsgBeaconNoSurface', '\c2You cannot place this item so close to another object.');
      return 0;
   }
   else if(maxSpawnPads(%obj))
   {
      messageClient(%obj.client, 'MsgDeployFailed', '\c2You do not have a vacant telepad slot.~wfx/misc/misc.error.wav');
      return 0;
   }
   else
   {
      %terrPt = posFromRaycast(%searchResult);
      %terrNrm = normalFromRaycast(%searchResult);

      // getTerrainAngle() function found in staticShape.cs
      %intAngle = getTerrainAngle(%terrNrm);
      if(%intAngle > 30)
      {
         messageClient(%obj.client, 'MsgDeployFailed', '\c2Surface is too steep for this item.~wfx/misc/misc.error.wav');
         return;
      }
      %rotAxis = vectorNormalize(vectorCross(%terrNrm, "0 0 1"));
      if (getWord(%terrNrm, 2) == 1 || getWord(%terrNrm, 2) == -1)
         %rotAxis = vectorNormalize(vectorCross(%terrNrm, "0 1 0"));

      %rotation = %rotAxis @ " " @ %intAngle;
      %obj.decInventory(%data, 1);
      %deplObj = new StaticShape() {
         dataBlock = "DeployedTransferPad";
         position = %terrPt; // VectorAdd(%terrPt, VectorScale(%terrNrm, 0.05));
         rotation = %rotation;
      };
      //%deplObj.playThread($AmbientThread, "ambient");
      %deplObj.team = %obj.client.team;
      %deplObj.sourceObject = %obj;
      %deplObj.owner = %obj.client;
      if(%deplObj.getTarget() != -1)
         setTargetSensorGroup(%deplObj.getTarget(), %obj.client.team);

      MissionCleanup.add(%deplObj);

      // Stuff this transfer pad into an array so we can have multi pads.
      // Help with this code provided by Founder (founder@mechina.com)
      for( %i = 0; %i < $Host::ClassicMaxTelepads; %i++ ) {
         if(%obj.client.spawnPad[%i] $= "")
         {
            %obj.client.spawnPad[%i] = %deplObj;
            messageClient(%obj.client, 'MsgPracMode', '\c2Deployed Telepad: %1', %i+1);
            break;
         }
      }
      %deplObj.deploy();
      return %deplObj;
   }
}

function DeployedTransferPad::onDestroyed(%data, %obj, %prevState)
{
   for( %i = 0; %i < $Host::ClassicMaxTelepads; %i++ )
   {
      if(%obj.owner.spawnPad[%i] $= %obj)
      {
         %obj.owner.spawnPad[%i] = "";
         break;
      }   
   }

   %obj.schedule(800, delete);
}

// Select telepad
function selectPad(%client)
{
   if(%client.transPad $="")
      %client.transPad = 1;
   else
      %client.transPad = (%client.transPad % $Host::ClassicMaxTelepads) + 1;

   messageClient( %client, 'MsgPracMode', '\c2Transfer Pad selected: \c3%1', %client.transPad );
}

// Destroy a telepad
function destroyPad(%client)
{
   if(isObject(%client.spawnPad[%client.transPad-1]))
      %client.spawnPad[%client.transPad-1].setDamageState(Destroyed);
   else
      messageClient(%client, 'MsgError', '\c2Transfer Pad %1 does not exist!~wfx/misc/misc.error.wav', %client.transPad);
} 

// Teleport to pad
function teleportToPad(%client)
{
   %player = %client.player;
   if(!isObject(%player))
      return;

   %pad = %client.spawnPad[%client.transPad-1];
   if(isObject(%pad))
   {
      if(%player.holdingFlag)
      {
         messageClient(%client, 'MsgError', '\c2You cannot teleport with the flag!~wfx/misc/misc.error.wav');
         %player.throwObject( %player.holdingFlag );
      }
      %pos = getWord(%pad.position, 0) @ " " @ getWord(%pad.position, 1) @ " " @ (getWord(%pad.position, 2)+0.5);
      %player.setTransform(%pos @ " " @ rotFromTransform(%pad.getTransform()));
      %player.teleporting = 0;

      %player.setVelocity("0 0 0");
      if($PracticeCtf::SpawnFavs)
      {
         buyFavorites(%client);
      }
      %player.setDamageLevel(0);
      %player.setEnergyLevel(%player.getDataBlock().maxEnergy);
      %player.selectWeaponSlot( 0 );
   }
   else
   {
      messageClient(%client, 'MsgError', '\c2Transfer Pad %1 does not exist!~wfx/misc/misc.error.wav', %client.transPad);
   }
}

// Spawn Vehicle at telepad
function spawnVehAtPad(%client, %blockname)
{
   %pad = %client.spawnPad[%client.transPad-1];
   %team = %client.getSensorGroup();
   if(isObject(%pad))
   {
      // Check for obstructions above the pad
      %pos = getBoxCenter(%pad.getWorldBox());
      %height = firstWord(%pos) SPC getWord(%pos, 1) SPC (getWord(%pos, 2)+30);
      %mask = $TypeMasks::ShapeBaseObjectType | $TypeMasks::InteriorObjectType;
      %obstruction = ContainerRayCast(%pos, %height, %mask);
      if(%obstruction)
      {
         messageClient(%client, 'MsgError', '\c2Cannot spawn vehicle. Area above pad is blocked.');
         return;
      }
      if(%blockName !$="")
      {
         if(vehicleCheck(%blockName, %team))
         {
            %pos = posFromTransform(%pad.getTransform());
            %rotation = rotFromTransform(%pad.getTransform());
            %position = vectorAdd(%pos, %blockName.spawnOffset);

            // Check for obstructions
            %mask = $TypeMasks::TerrainObjectType | $TypeMasks::InteriorObjectType;
            InitContainerRadiusSearch(%position, %blockName.checkRadius, %mask);
            while(%found = containerSearchNext() != 0)
            {
               %position = vectorAdd(%position, %blockName.spawnOffset);
            }
            %veh = %blockName.create(%team);
            if(%veh)
            {
               %veh.team = %team;
               %veh.useCreateHeight(true);
               %veh.schedule(4000, "useCreateHeight", false);
               %veh.getDataBlock().isMountable(%obj, false);
               %veh.getDataBlock().schedule(1000, "isMountable", %obj, true);
               vehicleListAdd(%blockName, %veh);
               MissionCleanup.add(%veh);
               %veh.setTransform(%position @ " " @ %rotation);

               if(%client.player.lastVehicle)
               {
                  %client.player.lastVehicle.lastPilot = "";
                  vehicleAbandonTimeOut(%client.player.lastVehicle);
                  %client.player.lastVehicle = "";
               }
               %client.player.lastVehicle = %veh;
               %veh.lastPilot = %obj.player;

               if(%client.isVehicleTeleportEnabled())
               {
                  if(%client.player.holdingFlag)
                  {
                     messageClient(%client, 'MsgError', '\c2You cannot teleport into your vehicle with the flag!~wfx/misc/misc.error.wav');
                     %client.player.throwObject( %client.player.holdingFlag );
                  }
                  %veh.getDataBlock().schedule(3000, "mountDriver", %veh, %client.player);
               }
               if(%veh.getTarget() != -1)
                  setTargetSensorGroup(%veh.getTarget(), %client.getSensorGroup());
            }
         }
         else
            messageClient(%client, 'MsgError', '\c2Your team\'s control network has reached its capacity for this vehicle.~wfx/misc/misc.error.wav');
      }
      else
         messageClient(%client, 'MsgError', '\c2Unknown vehicle type.~wfx/misc/misc.error.wav');
   }
   else
      messageClient(%client, 'MsgError', '\c2Transfer Pad %1 does not exist!~wfx/misc/misc.error.wav', %client.transPad);
}

/////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: Practice Mission types Console Spam fixes ////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

function DefaultGame::initPracticeHud(%game, %client, %val)
{
   commandToClient(%client, 'initializePracHud', "Default");
   commandToClient(%client, 'practiceHudHead', "CTF Practice Config", "Server Settings", "Player Settings", "Projectile Observation", "Telepad Options", "Spawn Vehicle at Pad");
   commandToClient(%client, 'practiceHudPopulate', "Disabled", "Empty");
   commandToClient(%client, 'practiceHudDone');
}

function DefaultGame::practiceBtnCmd(%game, %client, %btn, %val)
{
   // Do nothing
}

function DefaultGame::updatePracticeHudSet(%game, %client, %opt, %val)
{
   // Do nothing
}

function DefaultGame::sendPracHudUpdate(%game, %client)
{
   // Do nothing
}
