//------------------------------------------------------------------------------
datablock EffectProfile(VehicleAppearEffect)
{
   effectname = "vehicles/inventory_pad_appear";
   minDistance = 5;
   maxDistance = 10;
};

datablock EffectProfile(ActivateVehiclePadEffect)
{
   effectname = "powered/vehicle_pad_on";
   minDistance = 20;
   maxDistance = 30;
};

datablock AudioProfile(VehicleAppearSound)
{
   filename    = "fx/vehicles/inventory_pad_appear.wav";
   description = AudioClosest3d;
   preload = true;
   effect = VehicleAppearEffect;
};

datablock AudioProfile(ActivateVehiclePadSound)
{
   filename = 	"fx/powered/vehicle_pad_on.wav";
   description = AudioClose3d;
   preload = true;
   effect = ActivateVehiclePadEffect;
};

datablock StationFXVehicleData( VehicleInvFX )
{
   lifetime = 6.0;

   glowTopHeight = 1.5;
   glowBottomHeight = 0.1;
   glowTopRadius = 12.5;
   glowBottomRadius = 12.0;
   numGlowSegments = 26;
   glowFadeTime = 3.25;
   
   armLightDelay = 2.3;
   armLightLifetime = 3.0;
   armLightFadeTime = 1.5;
   numArcSegments = 10.0;

   sphereColor = "0.1 0.1 0.5";
   spherePhiSegments = 13;
   sphereThetaSegments = 8;
   sphereRadius = 12.0;
   sphereScale = "1.05 1.05 0.85";
   
   glowNodeName = "GLOWFX";

   leftNodeName[0]   = "LFX1";
   leftNodeName[1]   = "LFX2";
   leftNodeName[2]   = "LFX3";
   leftNodeName[3]   = "LFX4";

   rightNodeName[0]  = "RFX1";
   rightNodeName[1]  = "RFX2";
   rightNodeName[2]  = "RFX3";
   rightNodeName[3]  = "RFX4";


   texture[0] = "special/stationGlow";
   texture[1] = "special/stationLight2";
};

//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
function serverCmdBuyVehicle(%client, %blockName)
{
   %team = %client.getSensorGroup();
   if(vehicleCheck(%blockName, %team))     
   {
      %station = %client.player.station.pad;
      if( (%station.ready) && (%station.station.vehicle[%blockName]) )
      {
         %trans = %station.getTransform();
         %pos = getWords(%trans, 0, 2);
         %matrix = VectorOrthoBasis(getWords(%trans, 3, 6));
         %yrot = getWords(%matrix, 3, 5);
         %p = vectorAdd(%pos,vectorScale(%yrot, -3));
         %p =  getWords(%p,0, 1) @ " " @ getWord(%p,2) + 4;

//         error(%blockName);
//         error(%blockName.spawnOffset);

         %p = vectorAdd(%p, %blockName.spawnOffset);
         %rot = getWords(%trans, 3, 5);
         %angle = getWord(%trans, 6) + 3.14;
         %mask = $TypeMasks::VehicleObjectType | $TypeMasks::PlayerObjectType | 
                 $TypeMasks::StationObjectType | $TypeMasks::TurretObjectType;
	      InitContainerRadiusSearch(%p, %blockName.checkRadius, %mask);

	      %clear = 1;
         for (%x = 0; (%obj = containerSearchNext()) != 0; %x++)
         {
            if((%obj.getType() & $TypeMasks::VehicleObjectType) && (%obj.getDataBlock().checkIfPlayersMounted(%obj)))
            {
               %clear = 0;
               break;
            }
            else
               %removeObjects[%x] = %obj;
         }
         if(%clear)
         {
            %fadeTime = 0;
            for(%i = 0; %i < %x; %i++)
            {
               if(%removeObjects[%i].getType() & $TypeMasks::PlayerObjectType)
               {
                  %pData = %removeObjects[%i].getDataBlock();
                  %pData.damageObject(%removeObjects[%i], 0, "0 0 0", 1000, $DamageType::VehicleSpawn);
               }
               else
               {
                  %removeObjects[%i].mountable = 0;
                  %removeObjects[%i].startFade( 1000, 0, true );
                  %removeObjects[%i].schedule(1001, "delete");
                  %fadeTime = 1500;
               }
            }
            schedule(%fadeTime, 0, "createVehicle", %client, %station, %blockName, %team , %p, %rot, %angle);
         }
         else
            MessageClient(%client, "", 'Can\'t create vehicle. A player is on the creation pad.'); 
      }
      //--------------------------------------------------------------------------------------
      // z0dd - ZOD, 4/25/02. client tried to quick purchase a vehicle that isn't on this map 
      else
      {
      	 messageClient(%client, "", "~wfx/misc/misc.error.wav");
      }
      //--------------------------------------------------------------------------------------
   }
   //--------------------------------------------------------------------------------------------------------------
   // z0dd - ZOD, 4/25/02. client tried to quick purchase vehicle when max vehicles of this type are already in use
   else
   {
      messageClient(%client, "", "~wfx/misc/misc.error.wav");
   }
   //--------------------------------------------------------------------------------------------------------------
}

function createVehicle(%client, %station, %blockName, %team , %pos, %rot, %angle)
{
   %obj = %blockName.create(%team);   
   if(%obj)
   {
      //-----------------------------------------------
      // z0dd - ZOD, 4/25/02. MPB Teleporter.
      if ( %blockName $= "MobileBaseVehicle" )
      {
         %station.station.teleporter.MPB = %obj;
         %obj.teleporter = %station.station.teleporter;
      }
      //-----------------------------------------------
      %station.ready = false;
      %obj.team = %team;
      %obj.useCreateHeight(true);
      %obj.schedule(5500, "useCreateHeight", false);
      %obj.getDataBlock().isMountable(%obj, false);
      %obj.getDataBlock().schedule(6500, "isMountable", %obj, true);
      
      %station.playThread($ActivateThread,"activate2");
      %station.playAudio($ActivateSound, ActivateVehiclePadSound);

      vehicleListAdd(%blockName, %obj);
      MissionCleanup.add(%obj);
                                  
      %turret = %obj.getMountNodeObject(10);
      if(%turret > 0)
      {
         %turret.setCloaked(true);
         %turret.schedule(4800, "setCloaked", false);
      }
      
      %obj.setCloaked(true);
      %obj.setTransform(%pos @ " " @ %rot @ " " @ %angle);
   
      %obj.schedule(3700, "playAudio", 0, VehicleAppearSound);
      %obj.schedule(4800, "setCloaked", false);
      
      if(%client.player.lastVehicle)
      {
         %client.player.lastVehicle.lastPilot = "";
         vehicleAbandonTimeOut(%client.player.lastVehicle);
         %client.player.lastVehicle = "";
      }   
      %client.player.lastVehicle = %obj;
      %obj.lastPilot = %client.player;

      // play the FX
      %fx = new StationFXVehicle()
      {
         dataBlock = VehicleInvFX;
         stationObject = %station;
      };

      if ( %client.isVehicleTeleportEnabled() )
         %obj.getDataBlock().schedule(5000, "mountDriver", %obj, %client.player);
   }
   if(%obj.getTarget() != -1)
      setTargetSensorGroup(%obj.getTarget(), %client.getSensorGroup());
   // We are now closing the vehicle hud when you buy a vehicle, making the following call
   // unnecessary (and it breaks stuff, too!)
   //VehicleHud.updateHud(%client, 'vehicleHud');
}

//------------------------------------------------------------------------------
function VehicleData::mountDriver(%data, %obj, %player)
{
   if(isObject(%obj) && %obj.getDamageState() !$= "Destroyed")
   {
      %player.startFade(1000, 0, true);
      schedule(1000, 0, "testVehicleForMount", %player, %obj);
      %player.schedule(1500,"startFade",1000, 0, false);
   }
}

function testVehicleForMount(%player, %obj)
{
   // z0dd - ZOD, 4/25/02. Do not auto mount players who are teleporting, bug fix.
   // z0dd - ZOD, 7/10/02. Added check to see if player is in inv. Prevents switching
   // to illegal armor for a vehicle by purchasing a vehicle and buying a illegal
   // armor durig the tele phase period. 
   if(isObject(%obj) && %obj.getDamageState() !$= "Destroyed" && !%player.teleporting && !%player.client.inInv)
      %player.getDataBlock().onCollision(%player, %obj, 0);
}


//------------------------------------------------------------------------------
function VehicleData::checkIfPlayersMounted(%data, %obj)
{
   for(%i = 0; %i < %obj.getDatablock().numMountPoints; %i++)
      if (%obj.getMountNodeObject(%i))
         return true; 
   return false;
}

//------------------------------------------------------------------------------
function VehicleData::isMountable(%data, %obj, %val)
{
   %obj.mountable = %val;
}

//------------------------------------------------------------------------------
function vehicleCheck(%blockName, %team)
{
   if(($VehicleMax[%blockName] - $VehicleTotalCount[%team, %blockName]) > 0)
      return true;   
//   else
//   {
//      for(%i = 0; %i < $VehicleMax[%blockName]; %i++)
//      {
//         %obj = $VehicleInField[%blockName, %i];      
//         if(%obj.abandon)
//         {
//            vehicleListRemove(%blockName, %obj);
//            %obj.delete();
//            return true;
//         }
//      }
//   }
   return false;
}
	

//--------------------------------------------------------------------------------------------------------------------------------------
// z0dd - ZOD, 4/24/02. rewrote this function
function VehicleHud::updateHud( %obj, %client, %tag )
{
   %station = %client.player.station;
   %team = %client.getSensorGroup();
   %count = 0;
   %none = "-";

   %vehmsg = %station.vehicle[ScoutVehicle] ? "( 1 )   GRAV CYCLE                        " : %none;
   %numleft = %station.vehicle[ScoutVehicle] ? ($VehicleMax[ScoutVehicle] - $VehicleTotalCount[%team, ScoutVehicle]) : 0;
   messageClient( %client, 'SetLineHud', "", %tag, %count, %vehmsg, "", ScoutVehicle, %numleft );
   %count++;

   %vehmsg = %station.vehicle[AssaultVehicle] ? "( 2 )   ASSAULT TANK                    " : %none;
   %numleft = %station.vehicle[AssaultVehicle] ? ($VehicleMax[AssaultVehicle] - $VehicleTotalCount[%team, AssaultVehicle]) : 0;   
   messageClient( %client, 'SetLineHud', "", %tag, %count, %vehmsg, "", AssaultVehicle, %numleft );
   %count++;

   %vehmsg = %station.vehicle[mobileBaseVehicle] ? "( 3 )   MOBILE POINT BASE               " : %none;
   %numleft = %station.vehicle[MobileBaseVehicle] ? ($VehicleMax[MobileBaseVehicle] - $VehicleTotalCount[%team, MobileBaseVehicle]) : 0;      
   messageClient( %client, 'SetLineHud', "", %tag, %count, %vehmsg, "", MobileBaseVehicle, %numleft );
   %count++;

   %vehmsg = %station.vehicle[scoutFlyer] ? "( 4 )   SCOUT FLIER                       " : %none;
   %numleft = %station.vehicle[ScoutFlyer] ? ($VehicleMax[ScoutFlyer] - $VehicleTotalCount[%team, ScoutFlyer]) : 0;         
   messageClient( %client, 'SetLineHud', "", %tag, %count, %vehmsg, "", ScoutFlyer, %numleft );
   %count++;

   %vehmsg = %station.vehicle[bomberFlyer] ? "( 5 )   BOMBER                             " : %none;
   %numleft = %station.vehicle[BomberFlyer] ? ($VehicleMax[BomberFlyer] - $VehicleTotalCount[%team, BomberFlyer]) : 0;
   messageClient( %client, 'SetLineHud', "", %tag, %count, %vehmsg, "", BomberFlyer, %numleft );
   %count++;

   %vehmsg = %station.vehicle[hapcFlyer] ? "( 6 )   TRANSPORT                        " : %none;
   %numleft = %station.vehicle[HAPCFlyer] ? ($VehicleMax[HAPCFlyer] - $VehicleTotalCount[%team, HAPCFlyer]) : 0;   
   messageClient( %client, 'SetLineHud', "", %tag, %count, %vehmsg, "", HAPCFlyer, %numleft ); 
}
//--------------------------------------------------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------
function VehicleHud::clearHud( %obj, %client, %tag, %count )
{
   for ( %i = 0; %i < %count; %i++ )
      messageClient( %client, 'RemoveLineHud', "", %tag, %i );
}

//------------------------------------------------------------------------------
function serverCmdEnableVehicleTeleport( %client, %enabled )
{
   %client.setVehicleTeleportEnabled( %enabled );
}																							  
