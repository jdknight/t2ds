// DisplayName = Capture the Flag (Practice)

//--- GAME RULES BEGIN ---
//Prevent enemy from capturing your flag
//Score one point for grabbing the enemy's flag
//To capture, your flag must be at its stand
//Score 100 points each time enemy flag is captured
//--- GAME RULES END ---

//exec the AI scripts
exec("scripts/aiPracticeCtf.cs");

//-- tracking  ---
function PracticeCTFGame::initGameVars(%game)
{
   // z0dd - ZOD: Zero out Practice mode options
   $TeamDeployableMin[TurretIndoorDeployable] = 4;
   $TeamDeployableMin[TurretOutdoorDeployable] = 4;

   // z0dd - ZOD: We need only zero these when server comes up for first time.
   if(!$pctfLoaded)
   {
      $PracticeCtf::SpawnFavs = 0;
      $PracticeCtf::SpawnOnly = 0;
      $PracticeCtf::AutoFlagReturn = 0;
      $PracticeCtf::NoScoreLimit = 0;
      $PracticeCtf::ProtectStatics = 0;
      $PracticeCtf::UnlimAmmo = 0;
      $pctfLoaded = 1;
   }
   %game.SCORE_PER_SUICIDE             = 0; // z0dd - ZOD, 8/19/02. No penalty for suicide! Was -10
   %game.SCORE_PER_TEAMKILL            = -10;
   %game.SCORE_PER_DEATH               = 0;  
   %game.SCORE_PER_TK_DESTROY          = -10; // z0dd - ZOD, 10/03/02. Penalty for TKing equiptment.

   %game.SCORE_PER_KILL                = 10; 
   %game.SCORE_PER_PLYR_FLAG_CAP       = 30;
   %game.SCORE_PER_PLYR_FLAG_TOUCH     = 20;
   %game.SCORE_PER_TEAM_FLAG_CAP       = 100;
   %game.SCORE_PER_TEAM_FLAG_TOUCH     = 1;
   %game.SCORE_PER_ESCORT_ASSIST       = 5;
   %game.SCORE_PER_HEADSHOT            = 1;
   %game.SCORE_PER_REARSHOT            = 1; // z0dd - ZOD, 8/25/02. Rear Lance hits

   %game.SCORE_PER_TURRET_KILL         = 10;   // controlled
   %game.SCORE_PER_TURRET_KILL_AUTO    = 3;   // uncontrolled
   %game.SCORE_PER_FLAG_DEFEND         = 5; 
   %game.SCORE_PER_CARRIER_KILL        = 5;
   %game.SCORE_PER_FLAG_RETURN         = 10;
   %game.SCORE_PER_STALEMATE_RETURN    = 15;
   %game.SCORE_PER_GEN_DEFEND          = 5;
       
   %game.SCORE_PER_DESTROY_GEN         = 10;
   %game.SCORE_PER_DESTROY_SENSOR      = 4;
   %game.SCORE_PER_DESTROY_TURRET      = 5;
   %game.SCORE_PER_DESTROY_ISTATION    = 2;
   %game.SCORE_PER_DESTROY_VSTATION    = 5;
   %game.SCORE_PER_DESTROY_MPBTSTATION = 5; // z0dd - ZOD, 4/24/02. MPB Teleporter
   %game.SCORE_PER_DESTROY_SOLAR       = 5;
   %game.SCORE_PER_DESTROY_SENTRY      = 4;
   %game.SCORE_PER_DESTROY_DEP_SENSOR  = 1;
   %game.SCORE_PER_DESTROY_DEP_INV     = 2;
   %game.SCORE_PER_DESTROY_DEP_TUR     = 3;
       
   %game.SCORE_PER_DESTROY_SHRIKE      = 5;
   %game.SCORE_PER_DESTROY_BOMBER      = 8;
   %game.SCORE_PER_DESTROY_TRANSPORT   = 5;
   %game.SCORE_PER_DESTROY_WILDCAT     = 5;
   %game.SCORE_PER_DESTROY_TANK        = 8;
   %game.SCORE_PER_DESTROY_MPB         = 12;
   %game.SCORE_PER_PASSENGER           = 2;
       
   %game.SCORE_PER_REPAIR_GEN          = 8;
   %game.SCORE_PER_REPAIR_SENSOR       = 1;
   %game.SCORE_PER_REPAIR_TURRET       = 4;
   %game.SCORE_PER_REPAIR_ISTATION     = 2;
   %game.SCORE_PER_REPAIR_VSTATION     = 4;
   %game.SCORE_PER_REPAIR_MPBTSTATION  = 4; // z0dd - ZOD, 4/24/02. MPB Teleporter
   %game.SCORE_PER_REPAIR_SOLAR        = 4;
   %game.SCORE_PER_REPAIR_SENTRY       = 2;
   %game.SCORE_PER_REPAIR_DEP_TUR      = 3;
   %game.SCORE_PER_REPAIR_DEP_INV      = 2;
       
   %game.FLAG_RETURN_DELAY = 45 * 1000; //45 seconds

   %game.TIME_CONSIDERED_FLAGCARRIER_THREAT = 3 * 1000;  //after damaging enemy flag carrier
   %game.RADIUS_GEN_DEFENSE = 20;  //meters
   %game.RADIUS_FLAG_DEFENSE = 20;  //meters 

   %game.TOUCH_DELAY_MS = 20000;  //20 secs

   %game.fadeTimeMS = 2000;

   %game.notifyMineDist = 7.5;

   %game.stalemate = false;
   %game.stalemateObjsVisible = false;
   %game.stalemateTimeMS = 60000;
   %game.stalemateFreqMS = 15000;
   %game.stalemateDurationMS = 6000;
}

package PracticeCTFGame
{
   /////////////////////////////////////////////////////////////////////////////////////////
   // Practice Mode overloads //////////////////////////////////////////////////////////////
   /////////////////////////////////////////////////////////////////////////////////////////

   function ShapeBase::cleanNonType(%this, %type)
   {
      // z0dd - ZOD: If the map is multi mission type, need to make sure the CTF flags stay put
      if(%type $= PracticeCTF)
      {
         for(%h = 0; (%typeList = getWord(%this.missionTypesList, %h)) !$= ""; %h++)
            if(%typeList $= CTF)
               return;
      }
      Parent::cleanNonType(%this, %type);
   }

   //function ShapeBase::maxInventory(%this,%data)
   //{
      // z0dd - ZOD, 6/04/02: Allow virtually unlimited ammo in practice mode
   //   if(($PracticeCtf::UnlimAmmo == 1) && (%data.getName() !$= "RepairKit" && $CurrentMissionType $= Practice))
   //      return 999;
   //   else
   //      return %this.getDatablock().max[%data.getName()];
   //}

   function Player::maxInventory(%this, %data)
   {
      // z0dd - ZOD, 6/17/02: Allow virtually unlimited ammo in practice mode
      if(($PracticeCtf::UnlimAmmo == 1) && (%data.getName() !$= "RepairKit" && Game.class $= "PracticeCTFGame"))
      {
         return 999;
      }
      else
      {
         %max = ShapeBase::maxInventory(%this,%data);
         if (%this.getInventory(AmmoPack))
            %max += AmmoPack.max[%data.getName()];

         return %max;
      }
   }

   function StaticShapeData::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType)
   {
      // z0dd - ZOD, 5/04/02. Check to see if damage should be applied to all static team assets.
      if($PracticeCtf::ProtectStatics == 1 && %targetObject.getDataBlock().getClassName() !$= "TurretData" && !%targetObject.getDataBlock().deployedObject)
         return;
      else
         parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);
   }

   function Observer::onTrigger(%data,%obj,%trigger,%state)
   {
      if (%state == 0 || %trigger >= 4)
         return;

      %client = %obj.getControllingClient();
      if (%client == 0)
        return;
      // z0dd - ZOD: Check for client projectile observation
      switch$ (%obj.mode)
      {
         case "observerFollow":
            if(%obj.projectile !$= "")
            {
               if(%trigger == 3)
               {
                  clearBottomPrint(%client);
                  %client.camera.setFlyMode();
                  %client.setControlObject(%client.player);
                  %obj.projectile = ""; // Clear the projectile
               }
               else
                  return;
            }
            else
                Parent::onTrigger(%data,%obj,%trigger,%state);

         case "observerFly":
            if(%obj.projectile !$= "")
            {
               if(%trigger == 3)
               {
                  clearBottomPrint(%client);
                  %client.setControlObject(%client.player);
                  %obj.projectile = ""; // Clear the projectile
               }
               else
                  return;
            }
            else
               Parent::onTrigger(%data,%obj,%trigger,%state);

         default:
            Parent::onTrigger(%data,%obj,%trigger,%state);
      }
   }

   function Observer::setMode(%data, %obj, %mode, %targetObj)
   {
      if(%mode $= "")
         return;

      %client = %obj.getControllingClient();
      // z0dd - ZOD: Check for client projectile observation
      switch$ (%mode)
      {
         case "observerFollow":
            if(%obj.projectile !$= "")
            {
               bottomPrint(%targetObj.sourceObject.client, "Observing Projectile.\nPress your jet key to resume to player control.", 0, 2);
               %transform = %targetObj.initialPosition;
               %obj.setOrbitMode(%targetObj, %transform, 0.2, 12.0, 4.5);
            }
            else
               Parent::setMode(%data, %obj, %mode, %targetObj);

         case "observerFly":
            if(%obj.projectile !$= "")
            {
               %obj.setTransform(%obj.getTransform());
               %obj.setFlyMode();
               bottomPrint(%client, "Free Fly Mode.\nPress your jet key to resume player control.", 0, 2);
            }
            else
               Parent::setMode(%data, %obj, %mode, %targetObj);

         default:
            Parent::setMode(%data, %obj, %mode, %targetObj);
      }
      %obj.mode = %mode;
   }

   function MortarImage::onFire(%data, %obj, %slot)
   {
      // ---------------------------------------------------------------------------
      // z0dd - ZOD, 10/14/02. Anti rapid fire mortar/missile fix.
      if (%obj.cantFire !$= "")
      {
         return 0;
      }

      %wpnName = %data.getName();
      %obj.cantFire = 1;
      %preventTime = %data.stateTimeoutValue[4];
      %obj.reloadSchedule = schedule(%preventTime * 1000, %obj, resetFire, %obj);
      // ---------------------------------------------------------------------------

      // z0dd - ZOD: Check for client projectile observation
      %data.lightStart = getSimTime();
      if(%obj.client.mortarObs)
         %data.projectile = ObsMortarShot;

      %p = new (%data.projectileType)() {
         dataBlock        = %data.projectile;
         initialDirection = %obj.getMuzzleVector(%slot);
         initialPosition  = %obj.getMuzzlePoint(%slot);
         sourceObject     = %obj;
         sourceSlot       = %slot;
      };
      MissionCleanup.add(%p);
      if(%obj.client)
         %obj.client.projectile = %p;

      %obj.decInventory(%data.ammo,1);
      AIGrenadeThrown(%p);

      if(%obj.client.mortarObs)
      {
         %obj.client.camera.projectile = %p;
         %obj.client.camera.getDataBlock().setMode(%obj.client.camera, "observerFollow", %p);
         %obj.client.setControlObject(%obj.client.camera);
      }
      return %p;
   }

   function AssaultMortarTurretBarrel::onFire(%data, %obj, %slot)
   {
      // z0dd - ZOD: Check for client projectile observation
      %client = %obj.getControllingClient();
      if(%client)
         %obj.client = %client;

      %data.lightStart = getSimTime();
      %useEnergyObj = %obj.getObjectMount();
      if(!%useEnergyObj)
         %useEnergyObj = %obj;

      %energy = %useEnergyObj.getEnergyLevel();
      %vehicle = %useEnergyObj;
      if( %useEnergyObj.turretObject.getCapacitorLevel() < %data.minEnergy )
         return;

      if(%client.mortarObs)
         %data.projectile = ObsAssaultMortar;

      %p = new (%data.projectileType)() {
         dataBlock        = %data.projectile;
         initialDirection = %obj.getMuzzleVector(%slot);
         initialPosition  = %obj.getMuzzlePoint(%slot);
         sourceObject     = %obj;
         sourceSlot       = %slot;
         vehicleObject    = %vehicle;
      };
      MissionCleanup.add(%p);
      if(%client)
         %client.projectile = %p;

      %vehicle.turretObject.setCapacitorLevel( %vehicle.turretObject.getCapacitorLevel() - %data.fireEnergy );
      AIGrenadeThrown(%p);
      if(%client.mortarObs)
      {
         %client.camera.projectile = %p;
         %client.camera.getDataBlock().setMode(%client.camera, "observerFollow", %p);
         %client.setControlObject(%client.camera);
      }
      // Stop the onFire from looping
      %obj.setImageTrigger(4, false);
      return %p;
   }

   function AssaultPlasmaTurretBarrel::onFire(%data, %obj, %slot)
   {
      // Function used to kill console spam because of client
      // client value added to AssaultMortarTurretBarrel::onFire
      %obj.client = "";
      parent::onFire(%data, %obj, %slot);
   }

   function MissileLauncherImage::onFire(%data,%obj,%slot)
   {
      // ---------------------------------------------------------------------------
      // z0dd - ZOD, 10/14/02. Anti rapid fire mortar/missile fix.
      if (%obj.cantFire !$= "")
      {
         return 0;
      }

      %wpnName = %data.getName();
      %obj.cantFire = 1;
      %preventTime = %data.stateTimeoutValue[4];
      %obj.reloadSchedule = schedule(%preventTime * 1000, %obj, resetFire, %obj);
      // ---------------------------------------------------------------------------

      // z0dd - ZOD: Check for client projectile observation
      %data.lightStart = getSimTime();

      if(%obj.client.missileObs)
         %data.projectile = ObsShoulderMissile;

      %p = new (%data.projectileType)() {
         dataBlock        = %data.projectile;
         initialDirection = %obj.getMuzzleVector(%slot);
         initialPosition  = %obj.getMuzzlePoint(%slot);
         sourceObject     = %obj;
         sourceSlot       = %slot;
      };
      MissionCleanup.add(%p);
      MissileSet.add(%p);
      if(%obj.client)
         %obj.client.projectile = %p;

      %obj.decInventory(%data.ammo,1);
      if(%obj.client.missileObs)
      {
         %obj.client.camera.projectile = %p;
         %obj.client.camera.getDataBlock().setMode(%obj.client.camera, "observerFollow", %p);
         %obj.client.setControlObject(%obj.client.camera);
      }

      %target = %obj.getLockedTarget();
      if(%target)
         %p.setObjectTarget(%target);
      else if(%obj.isLocked())
         %p.setPositionTarget(%obj.getLockedPosition());
      else
         %p.setNoTarget();
   }

   function MissileLauncherImage::onWetFire(%data, %obj, %slot)
   {
      // z0dd - ZOD: Check for client projectile observation
      %data.lightStart = getSimTime();

      if(%obj.client.missileObs)
         %data.projectile = ObsShoulderMissile;

      %p = new (%data.projectileType)() {
         dataBlock        = %data.projectile;
         initialDirection = %obj.getMuzzleVector(%slot);
         initialPosition  = %obj.getMuzzlePoint(%slot);
         sourceObject     = %obj;
         sourceSlot       = %slot;
      };
      MissionCleanup.add(%p);
      MissileSet.add(%p);
      if(%obj.client)
         %obj.client.projectile = %p;

      %obj.decInventory(%data.ammo,1);
      if(%obj.client.missileObs)
      {
         %obj.client.camera.projectile = %p;
         %obj.client.camera.getDataBlock().setMode(%obj.client.camera, "observerFollow", %p);
         %obj.client.setControlObject(%obj.client.camera);
      }
      %p.setObjectTarget(0);
   }

   function GrenadeLauncherImage::onFire(%data, %obj, %slot)
   {
      // z0dd - ZOD: Check for client projectile observation
      %data.lightStart = getSimTime();
      if( %obj.station $= "" && %obj.isCloaked() )
      {
         if( %obj.respawnCloakThread !$= "" )
         {
            Cancel(%obj.respawnCloakThread);
            %obj.setCloaked( false );
            %obj.respawnCloakThread = "";
         }
         else
         {
            if( %obj.getEnergyLevel() > 20 )
            {
               %obj.setCloaked( false );
               %obj.reCloak = %obj.schedule( 500, "setCloaked", true ); 
            }
         }
      }
      if( %obj.client > 0 )
      {
         %obj.setInvincibleMode(0 ,0.00);
         %obj.setInvincible( false );  
      }
      if(%obj.client.grenadeObs)
         %data.projectile = ObsGrenade;

      %p = new (%data.projectileType)() {
         dataBlock        = %data.projectile;
         initialDirection = %obj.getMuzzleVector(%slot);
         initialPosition  = %obj.getMuzzlePoint(%slot);
         sourceObject     = %obj;
         sourceSlot       = %slot;
      };
      MissionCleanup.add(%p);
      if(%obj.client)
         %obj.client.projectile = %p;

      %obj.decInventory(%data.ammo,1);
      AIGrenadeThrown(%p);
      if(%obj.client.grenadeObs)
      {
         %obj.client.camera.projectile = %p;
         %obj.client.camera.getDataBlock().setMode(%obj.client.camera, "observerFollow", %p);
         %obj.client.setControlObject(%obj.client.camera);
      }
      return %p;
   }

   function DiscImage::onFire(%data, %obj, %slot)
   {
      // z0dd - ZOD: Check for client projectile observation
      %data.lightStart = getSimTime();
      if( %obj.station $= "" && %obj.isCloaked() )
      {
         if( %obj.respawnCloakThread !$= "" )
         {
            Cancel(%obj.respawnCloakThread);
            %obj.setCloaked( false );
            %obj.respawnCloakThread = "";
         }
         else
         {
            if( %obj.getEnergyLevel() > 20 )
            {
               %obj.setCloaked( false );
               %obj.reCloak = %obj.schedule( 500, "setCloaked", true ); 
            }
         }
      }
      if( %obj.client > 0 )
      {
         %obj.setInvincibleMode(0 ,0.00);
         %obj.setInvincible( false );  
      }
      if(%obj.client.discObs)
         %data.projectile = ObsDiscProjectile;

      %p = new (%data.projectileType)() {
         dataBlock        = %data.projectile;
         initialDirection = %obj.getMuzzleVector(%slot);
         initialPosition  = %obj.getMuzzlePoint(%slot);
         sourceObject     = %obj;
         sourceSlot       = %slot;
      };
      MissionCleanup.add(%p);
      if(%obj.client)
         %obj.client.projectile = %p;

      %obj.decInventory(%data.ammo,1);
      if(%obj.client.discObs)
      {
         %obj.client.camera.projectile = %p;
         %obj.client.camera.getDataBlock().setMode(%obj.client.camera, "observerFollow", %p);
         %obj.client.setControlObject(%obj.client.camera);
      }
      return %p;
   }

   function ProjectileData::onExplode(%data, %proj, %pos, %mod)
   {
      // z0dd - ZOD: If client is observing this projectile, let client camera free fly
      if(%proj.sourceObject.client !$="" && isObject(%proj.sourceObject))
      {
         %client = %proj.sourceObject.client;
         %camera = %proj.sourceObject.client.getControlObject();
         if(%camera == %client.camera && %proj == %camera.projectile)
         {
            clearBottomPrint(%client);
            %client.camera.getDataBlock().setMode(%client.camera, "observerFly");
         }
      }
      Parent::onExplode(%data, %proj, %pos, %mod);
   }

   function Beacon::onUse(%data, %obj)
   {
      if(%obj.client.transMode)
         deployTransferPad(%data, %obj);
      else
         Parent::onUse(%data, %obj);
   }

   function StationInventory::stationReady(%data, %obj)
   {
      //Display the Inventory Station GUI here
      %obj.notReady = 1;
      %obj.inUse = "Down";
      %obj.schedule(500,"playThread",$ActivateThread,"activate1");
      %player = %obj.triggeredBy;
      %energy = %player.getEnergyLevel();
      %max = %player.getDatablock().maxEnergy; // z0dd - ZOD, 4/20/02. Inv energy bug fix
      %player.setCloaked(true);
      %player.schedule(500, "setCloaked", false);              
	if (!%player.client.isAIControlled())
      {
         if($PracticeCtf::SpawnOnly) // z0dd - ZOD: Test for spawn only
	      getAmmoStationLovin(%player.client);
         else
            buyFavorites(%player.client);
      }
      %player.setEnergyLevel(mFloor(%player.getDatablock().maxEnergy * %energy / %max)); // z0dd - ZOD, 4/20/02. Inv energy bug fix
      %data.schedule( 500, "beginPersonalInvEffect", %obj );
   }

   function MobileInvStation::stationReady(%data, %obj)
   {
      //Display the Inventory Station GUI here
      %obj.notReady = 1;
      %obj.inUse = "Down";
      %obj.schedule(200,"playThread",$ActivateThread,"activate1");
      %obj.getObjectMount().playThread($ActivateThread,"Activate");
      %player = %obj.triggeredBy;
      %energy = %player.getEnergyLevel();
      %player.setCloaked(true);
      %player.schedule(900, "setCloaked", false);
	if (!%player.client.isAIControlled())
      {
         if($PracticeCtf::SpawnOnly) // z0dd - ZOD: Test for spawn only
	      getAmmoStationLovin(%player.client);
         else
            buyFavorites(%player.client);
      }
      %player.setEnergyLevel(%energy);
   }

   function DeployedStationInventory::stationReady(%data, %obj)
   {
      %obj.notReady = 1;
      %player = %obj.triggeredBy;
      %obj.playThread($ActivateThread,"activate1");
      if (!%player.client.isAIControlled())
      {
         if($PracticeCtf::SpawnOnly) // z0dd - ZOD: Test for spawn only
	      getAmmoStationLovin(%player.client);
         else
            buyDeployableFavorites(%player.client);
      }
   }

   function Flag::onEnterLiquid(%data, %obj, %coverage, %type)
   {
      if(%type > 3)  // 1-3 are water, 4+ is lava and quicksand(?)
      {
         //error("flag("@%obj@") is in liquid type" SPC %type);
         if($PracticeCtf::AutoFlagReturn) // z0dd - ZOD: Test automatic flag returns
            Game.flagReturn(%obj);
         else
            Game.schedule(3000, flagReturn, %obj);
      }
   }
   /////////////////////////////////////////////////////////////////////////////////////////

   function ShapeBaseData::onDestroyed(%data, %obj)
   {
      %scorer = %obj.lastDamagedBy;
      if(!isObject(%scorer))
         return;
    
      if( (%scorer.getType() & $TypeMasks::GameBaseObjectType) && %scorer.getDataBlock().catagory $= "Vehicles" )
      {
         // z0dd - ZOD, 6/18/02. %name was never defined.
         %name = %scorer.getDatablock().getName();
         if(%name $= "BomberFlyer" || %name $= "AssaultVehicle")
            %gunnerNode = 1;
         else
            %gunnerNode = 0;
        
         if(%scorer.getMountNodeObject(%gunnerNode))
         {
            %destroyer = %scorer.getMountNodeObject(%gunnerNode).client;
            %scorer = %destroyer;
            %damagingTeam = %scorer.team;
         }
      }
      else if(%scorer.getClassName() $= "Turret")
      {
         if(%scorer.getControllingClient())
         {
            //manned turret
            %destroyer = %scorer.getControllingClient();
            %scorer = %destroyer;
            %damagingTeam = %scorer.team;
         }
         else
         {
            return;  //unmanned turret
         }
      }

      if(!%damagingTeam)
         %damagingTeam = %scorer.team;

      // z0dd - ZOD, 10/03/02. Total re-write from here down.
      if(%damagingTeam == %obj.team)
      {
         if(!%obj.getDataBlock().deployedObject)
         {
            teamDestroyMessage(%scorer, 'msgTkDes', '\c5Teammate %1 destroyed your team\'s %2!', %scorer.name, Game.cleanWord(%obj.getDataBlock().targetTypeTag));
            Game.awardScoreTkDestroy(%scorer);
         }
         return;
      }
      
      %objType = %obj.getDataBlock().getName();
      switch$ ( %objType )
      {
         case "GeneratorLarge":
            teamDestroyMessage(%scorer, 'msgGenDestroyed', '\c5%1 destroyed a %2 Generator!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreGenDestroy(%scorer);
   
         case "SolarPanel":
            teamDestroyMessage(%scorer, 'msgSolarDestroyed', '\c5%1 destroyed a %2 Solar Panel!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreSolarDestroy(%scorer);
            game.shareScore(%score, %score);
   
         case "SensorLargePulse":
            teamDestroyMessage(%scorer, 'msgSensorDestroyed', '\c5%1 destroyed a %2 Sensor!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreSensorDestroy(%scorer);
   
         case "SensorMediumPulse":
            teamDestroyMessage(%scorer, 'msgSensorDestroyed', '\c5%1 destroyed a %2 Sensor!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreSensorDestroy(%scorer);
   
         case "DeployedMotionSensor":
            teamDestroyMessage(%scorer, 'msgDepSenDestroyed', '\c5%1 destroyed a Deployable Sensor!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreDepSensorDestroy(%scorer);
   
         case "DeployedPulseSensor":
            teamDestroyMessage(%scorer, 'msgDepSenDestroyed', '\c5%1 destroyed a Deployable Sensor!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreDepSensorDestroy(%scorer);
   
         case "StationInventory":
            teamDestroyMessage(%scorer, 'msgInvDestroyed', '\c5%1 destroyed a %2 Inventory Station!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreInvDestroy(%scorer);
   
         case "StationAmmo":
            teamDestroyMessage(%scorer, 'msgInvDestroyed', '\c5%1 destroyed a %2 Ammo Station!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreInvDestroy(%scorer);
   
         case "StationVehicle":
            teamDestroyMessage(%scorer, 'msgVehStationDestroyed', '\c5%1 destroyed a Vehicle Station!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreVehicleStationDestroy(%scorer);
   
         case "MPBTeleporter": // z0dd - ZOD, 4/24/02. MPB teleporter
            teamDestroyMessage(%scorer, 'msgTeleporterDestroyed', '\c5%1 destroyed a MPB Teleport Station!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreMPBTeleporterDestroy(%scorer);
   
         case "DeployedStationInventory":
            teamDestroyMessage(%scorer, 'msgDepInvDestroyed', '\c5%1 destroyed a Deployable Inventory!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreDepStationDestroy(%scorer);
   
         case "TurretBaseLarge":
            teamDestroyMessage(%scorer, 'msgTurDestroyed', '\c5%1 destroyed a %2 Turret!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreTurretDestroy(%scorer);
   
         case "SentryTurret":
            teamDestroyMessage(%scorer, 'msgSentryDestroyed', '\c5%1 destroyed a %2 Sentry Turret!', %scorer.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreSentryDestroy(%scorer);
   
         case "TurretDeployedFloorIndoor":
            teamDestroyMessage(%scorer, 'msgDepTurDestroyed', '\c5%1 destroyed a Spider Clamp Turret!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreDepTurretDestroy(%scorer);
   
         case "TurretDeployedWallIndoor":
            teamDestroyMessage(%scorer, 'msgDepTurDestroyed', '\c5%1 destroyed a Spider Clamp Turret!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreDepTurretDestroy(%scorer);
   
         case "TurretDeployedCeilingIndoor":
            teamDestroyMessage(%scorer, 'msgDepTurDestroyed', '\c5%1 destroyed a Spider Clamp Turret!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreDepTurretDestroy(%scorer);
   
         case "TurretDeployedOutdoor":
            teamDestroyMessage(%scorer, 'msgDepTurDestroyed', '\c5%1 destroyed a Landspike Turret!', %scorer.name); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
            %score = game.awardScoreDepTurretDestroy(%scorer);

         default:
            return;
      }

      if(!%obj.soiledByEnemyRepair)
      {
         game.shareScore(%scorer, %score);
      }
   }

   function ShapeBaseData::onDisabled(%data, %obj)
   {
      %obj.wasDisabled = true;
      Parent::onDisabled(%data, %obj);
   }

   function RepairGunImage::onRepair(%this, %obj, %slot)
   {
      Parent::onRepair(%this, %obj, %slot);
      %target = %obj.repairing;
      if(%target && %target.team != %obj.team)
      {
         //error("Enemy stuff("@%obj@") is being repaired (by "@%target@")");
         %target.soiledByEnemyRepair = true;
      }
   }

   function Flag::objectiveInit(%data, %flag)
   {
      if (!%flag.isTeamSkinned)
      {
         %pos = %flag.getTransform();
         %group = %flag.getGroup();
      }
      %flag.originalPosition = %flag.getTransform();
      $flagPos[%flag.team] = %flag.originalPosition;
      %flag.isHome = true;
      %flag.carrier = "";
      %flag.grabber = "";
      setTargetSkin(%flag.getTarget(), PracticeCTFGame::getTeamSkin(PracticeCTFGame, %flag.team));
      setTargetSensorGroup(%flag.getTarget(), %flag.team);
      setTargetAlwaysVisMask(%flag.getTarget(), 0x7);
      setTargetRenderMask(%flag.getTarget(), getTargetRenderMask(%flag.getTarget()) | 0x2);
      %flag.scopeWhenSensorVisible(true);
      $flagStatus[%flag.team] = "<At Base>";

      //Point the flag and stand at each other
      %group = %flag.getGroup();
      %count = %group.getCount();
      %flag.stand = "";
      for(%i = 0; %i < %count; %i++)
      {
         %this = %group.getObject(%i);
         //---------------------------------------------------------------------------------------------------------------------------
         // z0dd - ZOD: Added TSStatic, console spam fix
         if(%this.getClassName() !$= "InteriorInstance" && %this.getClassName() !$= "SimGroup" && %this.getClassName() !$= "TSStatic")
         {
            if(%this.getDataBlock().getName() $= "ExteriorFlagStand")
            {
               %flag.stand = %this;
               %this.flag = %flag;
            }
         }
      }
      // set the nametag on the target
      setTargetName(%flag.getTarget(), PracticeCTFGame::getTeamName(PracticeCTFGame, %flag.team));

      // create a marker on this guy
      %flag.waypoint = new MissionMarker() {
         position = %flag.getTransform();
         dataBlock = "FlagMarker";
      };
      MissionCleanup.add(%flag.waypoint);

      // create a target for this (there is no MissionMarker::onAdd script call)
      %target = createTarget(%flag.waypoint, PracticeCTFGame::getTeamName( PracticeCTFGame, %flag.team), "", "", 'Base', %flag.team, 0);
      setTargetAlwaysVisMask(%target, 0xffffffff);

      //store the flag in an array
      $TeamFlag[%flag.team] = %flag;

      // --------------------------------------------------------
      // z0dd - ZOD, 5/26/02. Don't let flag hover over defenders
      %flag.static = true;
  
      // -------------------------------------------------------------------------------------------
      // z0dd - ZOD, 10/03/02. Use triggers for flags that are at home, hack for flag collision bug.
      %flag.trigger = new Trigger()
      {
         dataBlock = flagTrigger;
         polyhedron = "-0.6 0.6 0.1 1.2 0.0 0.0 0.0 -1.2 0.0 0.0 0.0 2.5";
         position = %flag.position;
         rotation = %flag.rotation;
      };
      MissionCleanup.add(%flag.trigger);
      %flag.trigger.flag = %flag;
      // -------------------------------------------------------------------------------------------
   }
};

//--------------------------------------------------------------------------

function PracticeCTFGame::getTeamSkin(%game, %team)
{
   CTFGame::getTeamSkin(%game, %team);
}

function PracticeCTFGame::getTeamName(%game, %team)
{
   CTFGame::getTeamName(%game, %team);
}

//--------------------------------------------------------------------------
function PracticeCTFGame::missionLoadDone(%game)
{
   //default version sets up teams - must be called first...
   DefaultGame::missionLoadDone(%game);

   for(%i = 1; %i < (%game.numTeams + 1); %i++)
      $teamScore[%i] = 0;

   // remove 
   MissionGroup.clearFlagWaypoints();

   //reset some globals, just in case...
	$dontScoreTimer[1] = false;
	$dontScoreTimer[2] = false;

   echo( "starting camp thread..." );
   %game.campThread_1 = schedule( 1000, 0, "checkVehicleCamping", 1 );
   %game.campThread_2 = schedule( 1000, 0, "checkVehicleCamping", 2 );

   // z0dd - ZOD: Need to save off turret barrels for practice mode reset
   // Function allready exists in siege no need to duplicate.
   SiegeGame::checkTurretBases();
}

function PracticeCTFGame::playerTouchFlag(%game, %player, %flag)
{
   %client = %player.client;
   
   if ((%flag.carrier $= "") && (%player.getState() !$= "Dead"))
   {
      //flag isn't held and has been touched by a live player
      if (%client.team == %flag.team)
         %game.playerTouchOwnFlag(%player, %flag);
      else
         %game.playerTouchEnemyFlag(%player, %flag);

      // SquirrelOfDeath, 10/02/02. moved searchSchedule cancel from here to playerTouchEnemyFlag.
   }

   // toggle visibility of the flag
   setTargetRenderMask(%flag.waypoint.getTarget(), %flag.isHome ? 0 : 1);
}

function PracticeCTFGame::playerTouchOwnFlag(%game, %player, %flag)
{   
   if(%flag.isHome)
   {
      if (%player.holdingFlag !$= "")      
         %game.flagCap(%player);
   }
   else      
      %game.flagReturn(%flag, %player);
            
   //call the AI function
   %game.AIplayerTouchOwnFlag(%player, %flag);            
}

function PracticeCTFGame::playerTouchEnemyFlag(%game, %player, %flag)
{
   // ---------------------------------------------------------------
   // z0dd, ZOD - 9/27/02. Player must wait to grab after throwing it
   if((%player.flagTossWait !$= "") && %player.flagTossWait)
      return;
   // ---------------------------------------------------------------
   
   cancel(%flag.searchSchedule); // z0dd - ZOD, 9/28/02. Hack for flag collision bug.  SquirrelOfDeath, 10/02/02: Moved from PlayerTouchFlag

   cancel(%game.updateFlagThread[%flag]); // z0dd - ZOD, 8/4/02. Cancel this flag's thread to KineticPoet's flag updater
   %game.flagHeldTime[%flag] = getSimTime(); // z0dd - ZOD, 8/15/02. Store time player grabbed flag.

   %client = %player.client;
   %player.holdingFlag = %flag;  //%player has this flag
   %flag.carrier = %player;  //this %flag is carried by %player

    %player.mountImage(FlagImage, $FlagSlot, true, %game.getTeamSkin(%flag.team));

   %game.playerGotFlagTarget(%player);
   //only cancel the return timer if the player is in bounds...
   if (!%client.outOfBounds)
   {
      cancel($FlagReturnTimer[%flag]);
      $FlagReturnTimer[%flag] = "";
   }

   //if this flag was "at home", see if both flags have now been taken
   if (%flag.isHome)
   {
      // tiebreaker score
      game.awardScoreFlagTouch( %client, %flag );

      %startStalemate = false;
      if ($TeamFlag[1] == %flag)
         %startStalemate = !$TeamFlag[2].isHome;
      else
         %startStalemate = !$TeamFlag[1].isHome;

      if (%startStalemate)
         %game.stalemateSchedule = %game.schedule(%game.stalemateTimeMS, beginStalemate);
   
   }

   %flag.hide(true);
   %flag.startFade(0, 0, false);         
   %flag.isHome = false;
   if(%flag.stand)
      %flag.stand.getDataBlock().onFlagTaken(%flag.stand);//animate, if exterior stand

   $flagStatus[%flag.team] = %client.nameBase;
   %teamName = %game.getTeamName(%flag.team);
   messageTeamExcept(%client, 'MsgCTFFlagTaken', '\c2Teammate %1 took the %2 flag.~wfx/misc/flag_snatch.wav', %client.name, %teamName, %flag.team, %client.nameBase);
   messageTeam(%flag.team, 'MsgCTFFlagTaken', '\c2Your flag has been taken by %1!~wfx/misc/flag_taken.wav',%client.name, 0, %flag.team, %client.nameBase);
   messageTeam(0, 'MsgCTFFlagTaken', '\c2%1 took the %2 flag.~wfx/misc/flag_snatch.wav', %client.name, %teamName, %flag.team, %client.nameBase);
   messageClient(%client, 'MsgCTFFlagTaken', '\c2You took the %2 flag.~wfx/misc/flag_snatch.wav', %client.name, %teamName, %flag.team, %client.nameBase);     
   logEcho(%client.nameBase@" (pl "@%player@"/cl "@%client@") took team "@%flag.team@" flag");
   
   //call the AI function
   %game.AIplayerTouchEnemyFlag(%player, %flag);

   //if the player is out of bounds, then in 3 seconds, it should be thrown back towards the in bounds area...
   if (%client.outOfBounds)
      %game.schedule(3000, "boundaryLoseFlag", %player);
}

function PracticeCTFGame::playerGotFlagTarget(%game, %player)
{
   %player.scopeWhenSensorVisible(true);
   %target = %player.getTarget();
   setTargetRenderMask(%target, getTargetRenderMask(%target) | 0x2);
   if(%game.stalemateObjsVisible)
      setTargetAlwaysVisMask(%target, 0x7);
}

function PracticeCTFGame::playerLostFlagTarget(%game, %player)
{
   %player.scopeWhenSensorVisible(false);
   %target = %player.getTarget();
   setTargetRenderMask(%target, getTargetRenderMask(%target) & ~0x2);
   // clear his always vis target mask
   setTargetAlwaysVisMask(%target, (1 << getTargetSensorGroup(%target)));
}

//----------------------------------------------------------------------------------------
// z0dd - ZOD, 8/4/02: KineticPoet's flag updater code
function PracticeCTFGame::updateFlagTransform(%game, %flag)
{
   %flag.setTransform(%flag.getTransform());
   %game.updateFlagThread[%flag] = %game.schedule(100, "updateFlagTransform", %flag);
}
//----------------------------------------------------------------------------------------

function PracticeCTFGame::playerDroppedFlag(%game, %player)
{
   %client = %player.client;
   %flag = %player.holdingFlag;
   %game.updateFlagTransform(%flag); // z0dd - ZOD, 8/4/02, Call to KineticPoet's flag updater
   %held = %game.formatTime(getSimTime() - %game.flagHeldTime[%flag], false); // z0dd - ZOD, 8/15/02. How long did player hold flag?
   
   %game.playerLostFlagTarget(%player);

   %player.holdingFlag = ""; //player isn't holding a flag anymore
   %flag.carrier = "";  //flag isn't held anymore 
   $flagStatus[%flag.team] = "<In the Field>";
   
   %player.unMountImage($FlagSlot);
   %flag.hide(false); //Does the throwItem function handle this?   

   %teamName = %game.getTeamName(%flag.team);
   messageTeamExcept(%client, 'MsgCTFFlagDropped', '\c2Teammate %1 dropped the %2 flag. (Held: %4)~wfx/misc/flag_drop.wav', %client.name, %teamName, %flag.team, %held); // z0dd - ZOD, 8/15/02. How long flag was held
   messageTeam(%flag.team, 'MsgCTFFlagDropped', '\c2Your flag has been dropped by %1! (Held: %4)~wfx/misc/flag_drop.wav', %client.name, 0, %flag.team, %held); // z0dd - ZOD, 8/15/02. How long flag was held
   messageTeam(0, 'MsgCTFFlagDropped', '\c2%1 dropped the %2 flag. (Held: %4)~wfx/misc/flag_drop.wav', %client.name, %teamName, %flag.team, %held); // z0dd - ZOD, 8/15/02. How long flag was held
   if(!%player.client.outOfBounds)
      messageClient(%client, 'MsgCTFFlagDropped', '\c2You dropped the %2 flag. (Held: %4)~wfx/misc/flag_drop.wav', %client.name, %teamName, %flag.team, %held); // z0dd - ZOD, 8/15/02. How long flag was held
                                                                                                                                                                // Yogi, 8/18/02. 3rd param changed 0 -> %client.name
   logEcho(%client.nameBase@" (pl "@%player@"/cl "@%client@") dropped team "@%flag.team@" flag"@" (Held: "@%held@")");

   // z0dd - ZOD: Check for auto flag returns
   if($PracticeCtf::AutoFlagReturn)
   {
      %game.flagReturnFade(%flag);
   }
   else
   {
      //don't duplicate the schedule if there's already one in progress...
      if($FlagReturnTimer[%flag] <= 0)
         $FlagReturnTimer[%flag] = %game.schedule(%game.FLAG_RETURN_DELAY - %game.fadeTimeMS, "flagReturnFade", %flag);
   }

   //call the AI function
   %game.AIplayerDroppedFlag(%player, %flag);
}

function PracticeCTFGame::flagCap(%game, %player)
{
   %client = %player.client;
   %flag = %player.holdingFlag;
   %flag.carrier = "";

   %held = %game.formatTime(getSimTime() - %game.flagHeldTime[%flag], false); // z0dd - ZOD, 8/15/02. How long did player hold flag?

   %game.playerLostFlagTarget(%player);
   //award points to player and team
   %teamName = %game.getTeamName(%flag.team);
   messageTeamExcept(%client, 'MsgCTFFlagCapped', '\c2%1 captured the %2 flag! (Held: %5)~wfx/misc/flag_capture.wav', %client.name, %teamName, %flag.team, %client.team, %held);
   messageTeam(%flag.team, 'MsgCTFFlagCapped', '\c2Your flag was captured by %1. (Held: %5)~wfx/misc/flag_lost.wav', %client.name, 0, %flag.team, %client.team, %held); 
   messageTeam(0, 'MsgCTFFlagCapped', '\c2%1 captured the %2 flag! (Held: %5)~wfx/misc/flag_capture.wav', %client.name, %teamName, %flag.team, %client.team, %held); 
   messageClient(%client, 'MsgCTFFlagCapped', '\c2You captured the %2 flag! (Held: %5)~wfx/misc/flag_capture.wav', %client.name, %teamName, %flag.team, %client.team, %held); // z0dd - ZOD, 8/19/02. Yogi. 3rd param changed from 0 to %client.name

   logEcho(%client.nameBase@" (pl "@%player@"/cl "@%client@") capped team "@%client.team@" flag"@" (Held: "@%held@")");
   %player.holdingFlag = ""; //no longer holding it.
   %player.unMountImage($FlagSlot);
   %game.awardScoreFlagCap(%client, %flag);   
   %game.flagReset(%flag);
     
   //call the AI function
   %game.AIflagCap(%player, %flag);

   //if this cap didn't end the game, play the announcer...
   if ($missionRunning)
   {
      if (%game.getTeamName(%client.team) $= 'Inferno')
         messageAll("", '~wvoice/announcer/ann.infscores.wav');
      else if (%game.getTeamName(%client.team) $= 'Storm')
         messageAll("", '~wvoice/announcer/ann.stoscores.wav');
      else if (%game.getTeamName(%client.team) $= 'Phoenix')
         messageAll("", '~wvoice/announcer/ann.pxscore.wav');
      else if (%game.getTeamName(%client.team) $= 'Blood Eagle')
         messageAll("", '~wvoice/announcer/ann.bescore.wav');
      else if (%game.getTeamName(%client.team) $= 'Diamond Sword')
         messageAll("", '~wvoice/announcer/ann.dsscore.wav');
      else if (%game.getTeamName(%client.team) $= 'Starwolf')
         messageAll("", '~wvoice/announcer/ann.swscore.wav');
   }
}

function PracticeCTFGame::flagReturnFade(%game, %flag)
{
   $FlagReturnTimer[%flag] = %game.schedule(%game.fadeTimeMS, "flagReturn", %flag);
   %flag.startFade(%game.fadeTimeMS, 0, true);
}

function PracticeCTFGame::flagReturn(%game, %flag, %player)
{
   // z0dd - ZOD: Bug fix related to practice mode
   if($FlagReturnTimer[%flag] !$= "")
   {
      cancel($FlagReturnTimer[%flag]);
      $FlagReturnTimer[%flag] = "";
   }

   if(%flag.team == 1)
      %otherTeam = 2;
   else
      %otherTeam = 1;
   %teamName = %game.getTeamName(%flag.team);
   if (%player !$= "")
   {
      //a player returned it
      %client = %player.client;
      messageTeamExcept(%client, 'MsgCTFFlagReturned', '\c2Teammate %1 returned your flag to base.~wfx/misc/flag_return.wav', %client.name, 0, %flag.team);
      messageTeam(%otherTeam, 'MsgCTFFlagReturned', '\c2Enemy %1 returned the %2 flag.~wfx/misc/flag_return.wav', %client.name, %teamName, %flag.team);
      messageTeam(0, 'MsgCTFFlagReturned', '\c2%1 returned the %2 flag.~wfx/misc/flag_return.wav', %client.name, %teamName, %flag.team);
      messageClient(%client, 'MsgCTFFlagReturned', '\c2You returned your flag.~wfx/misc/flag_return.wav', %client.name, %teamName, %flag.team); // z0dd - ZOD, 8/19/02. Yogi. 3rd param changed from 0 to %client.name
      logEcho(%client.nameBase@" (pl "@%player@"/cl "@%client@") returned team "@%flag.team@" flag");
      
      // find out what type of return it is
      // stalemate return?
      
      // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
      if(%game.stalemate)
      {
         //error("Stalemate return!!!");
         %game.awardScoreStalemateReturn(%player.client);
      }
      else // regular return
      {
         %enemyFlagDist = vectorDist($flagPos[%flag.team], $flagPos[%otherTeam]);
         %dist = vectorDist(%flag.position, %flag.originalPosition);
        
         %rawRatio = %dist/%enemyFlagDist;
         %ratio = %rawRatio < 1 ? %rawRatio : 1;
         %percentage = mFloor( (%ratio) * 10 ) * 10;
         %game.awardScoreFlagReturn(%player.client, %percentage); 
      }
      // ---------------------------------------------------
   }      
   else
   {
      //returned due to timer
      messageTeam(%otherTeam, 'MsgCTFFlagReturned', '\c2The %2 flag was returned to base.~wfx/misc/flag_return.wav', 0, %teamName, %flag.team);  //because it was dropped for too long
      messageTeam(%flag.team, 'MsgCTFFlagReturned', '\c2Your flag was returned.~wfx/misc/flag_return.wav', 0, 0, %flag.team);
      messageTeam(0, 'MsgCTFFlagReturned', '\c2The %2 flag was returned to base.~wfx/misc/flag_return.wav', 0, %teamName, %flag.team);
      logEcho("team "@%flag.team@" flag returned (timeout)");
   }

   %game.flagReset(%flag);
}

function PracticeCTFGame::showStalemateTargets(%game)
{
   cancel(%game.stalemateSchedule);

   //show the targets
   for (%i = 1; %i <= 2; %i++)
   {
      %flag = $TeamFlag[%i];

      //find the object to scope/waypoint....
      //render the target hud icon for slot 1 (a centermass flag)
      //if we just set him as always sensor vis, it'll work fine.
      if (isObject(%flag.carrier))
         setTargetAlwaysVisMask(%flag.carrier.getTarget(), 0x7);
   }

   //schedule the targets to hide
   %game.stalemateObjsVisible = true;
   %game.stalemateSchedule = %game.schedule(%game.stalemateDurationMS, hideStalemateTargets);
}

function PracticeCTFGame::hideStalemateTargets(%game)
{
   cancel(%game.stalemateSchedule);

   //hide the targets
   for (%i = 1; %i <= 2; %i++)
   {
      %flag = $TeamFlag[%i];
      if (isObject(%flag.carrier))
      {
         %target = %flag.carrier.getTarget();
         setTargetAlwaysVisMask(%target, (1 << getTargetSensorGroup(%target)));
      }
   }

   //schedule the targets to show again
   %game.stalemateObjsVisible = false;
   %game.stalemateSchedule = %game.schedule(%game.stalemateFreqMS, showStalemateTargets);
}

function PracticeCTFGame::beginStalemate(%game)
{
   %game.stalemate = true;
   %game.showStalemateTargets();
}

function PracticeCTFGame::endStalemate(%game)
{
   %game.stalemate = false;
   %game.hideStalemateTargets();
   cancel(%game.stalemateSchedule);
}

function PracticeCTFGame::flagReset(%game, %flag)
{
   cancel(%game.updateFlagThread[%flag]); // z0dd - ZOD, 8/4/02. Cancel this flag's thread to KineticPoet's flag updater
   
   //any time a flag is reset, kill the stalemate schedule
   %game.endStalemate();

   //make sure if there's a player carrying it (probably one out of bounds...), it is stripped first
   if (isObject(%flag.carrier))
   {
      //hide the target hud icon for slot 2 (a centermass flag - visible only as part of a teams sensor network)
      %game.playerLostFlagTarget(%flag.carrier);
      %flag.carrier.holdingFlag = ""; //no longer holding it.
      %flag.carrier.unMountImage($FlagSlot);
   }

   //fades, restore default position, home, velocity, general status, etc.
   %flag.setVelocity("0 0 0");
   %flag.setTransform(%flag.originalPosition);
   %flag.isHome = true;
   %flag.carrier = "";
   %flag.grabber = "";
   $flagStatus[%flag.team] = "<At Base>";
   %flag.hide(false);
   if(%flag.stand)
      %flag.stand.getDataBlock().onFlagReturn(%flag.stand);//animate, if exterior stand

   //fade the flag in...
   %flag.startFade(%game.fadeTimeMS, 0, false);         

   // dont render base target
   setTargetRenderMask(%flag.waypoint.getTarget(), 0);

   //call the AI function
   %game.AIflagReset(%flag);

   // --------------------------------------------------------
   // z0dd - ZOD, 5/26/02. Don't let flag hover over defenders
   %flag.static = true;

   // --------------------------------------------------------------------------
   // z0dd - ZOD, 9/28/02. Hack for flag collision bug.
   if(%flag.searchSchedule !$="")
   {
      cancel(%flag.searchSchedule);
   }
   // --------------------------------------------------------------------------   
}

function PracticeCTFGame::timeLimitReached(%game)
{
   logEcho("game over (timelimit)");
   %game.gameOver();
   cycleMissions();
}

function PracticeCTFGame::scoreLimitReached(%game)
{
   logEcho("game over (scorelimit)");
   %game.gameOver();
   cycleMissions();
}

function PracticeCTFGame::notifyMineDeployed(%game, %mine)
{
   //see if the mine is within 5 meters of the flag stand...
   %mineTeam = %mine.sourceObject.team;
   %homeFlag = $TeamFlag[%mineTeam];
   if (isObject(%homeFlag))
   {
      %dist = VectorDist(%homeFlag.originalPosition, %mine.position);
      if (%dist <= %game.notifyMineDist)
      {
         messageTeam(%mineTeam, 'MsgCTFFlagMined', "The flag has been mined.~wvoice/announcer/flag_minedFem.wav" );
      }
   }
}

function PracticeCTFGame::gameOver(%game)
{
   // z0dd - ZOD, 9/28/02. Hack for flag collision bug.
   for(%f = 1; %f <= %game.numTeams; %f++)
   {
      cancel($TeamFlag[%f].searchSchedule);
   }

   // -------------------------------------------
   // z0dd - ZOD, 9/28/02. Cancel camp schedules.
   if( Game.campThread_1 !$= "" )
      cancel(Game.campThread_1);

   if( Game.campThread_2 !$= "" )
      cancel(Game.campThread_2);
   // -------------------------------------------

   //call the default
   DefaultGame::gameOver(%game);

   //send the winner message
   %winner = "";
   if ($teamScore[1] > $teamScore[2])
      %winner = %game.getTeamName(1);
   else if ($teamScore[2] > $teamScore[1])
      %winner = %game.getTeamName(2);

   if (%winner $= 'Storm')
      messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.stowins.wav" );
   else if (%winner $= 'Inferno')
      messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.infwins.wav" );
   else if (%winner $= 'Starwolf')
      messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.swwin.wav" );
   else if (%winner $= 'Blood Eagle')
      messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.bewin.wav" );
   else if (%winner $= 'Diamond Sword')
      messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.dswin.wav" );
   else if (%winner $= 'Phoenix')
      messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.pxwin.wav" );
   else
      messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.gameover.wav" );

   messageAll('MsgClearObjHud', "");
   for(%i = 0; %i < ClientGroup.getCount(); %i ++) 
   {
      %client = ClientGroup.getObject(%i);
      %game.resetScore(%client);
   }
   for(%j = 1; %j <= %game.numTeams; %j++)
      $TeamScore[%j] = 0;
}

function PracticeCTFGame::onClientDamaged(%game, %clVictim, %clAttacker, %damageType, %implement, %damageLoc)
{ 
   if(%clVictim.headshot && %damageType == $DamageType::Laser && %clVictim.team != %clAttacker.team)
   {
        %clAttacker.scoreHeadshot++;
       if (%game.SCORE_PER_HEADSHOT != 0)
      {
         messageClient(%clAttacker, 'msgHeadshot', '\c0You received a %1 point bonus for a successful headshot.', %game.SCORE_PER_HEADSHOT);
         messageTeamExcept(%clAttacker, 'msgHeadshot', '\c5%1 hit a sniper rifle headshot.', %clAttacker.name); // z0dd - ZOD, 8/15/02. Tell team
      }
      %game.recalcScore(%clAttacker);
   }

   // -----------------------------------------------
   // z0dd - ZOD, 8/25/02. Rear Lance hits
   if(%clVictim.rearshot && %damageType == $DamageType::ShockLance && %clVictim.team != %clAttacker.team)
   {
      %clAttacker.scoreRearshot++;
      if (%game.SCORE_PER_REARSHOT != 0)
      {
         messageClient(%clAttacker, 'msgRearshot', '\c0You received a %1 point bonus for a successful rearshot.', %game.SCORE_PER_REARSHOT);
         messageTeamExcept(%clAttacker, 'msgRearshot', '\c5%1 hit a shocklance rearshot.', %clAttacker.name);
      }
      %game.recalcScore(%clAttacker);
   }
   // -----------------------------------------------
   
   //the DefaultGame will set some vars
   DefaultGame::onClientDamaged(%game, %clVictim, %clAttacker, %damageType, %implement, %damageLoc);
    
   //if victim is carrying a flag and is not on the attackers team, mark the attacker as a threat for x seconds(for scoring purposes)
   if ((%clVictim.holdingFlag !$= "") && (%clVictim.team != %clAttacker.team))
   {
      %clAttacker.dmgdFlagCarrier = true;
      cancel(%clAttacker.threatTimer);  //restart timer    
      %clAttacker.threatTimer = schedule(%game.TIME_CONSIDERED_FLAGCARRIER_THREAT, %clAttacker.dmgdFlagCarrier = false);
   }
}

////////////////////////////////////////////////////////////////////////////////////////
function PracticeCTFGame::clientMissionDropReady(%game, %client)
{
   messageClient(%client, 'MsgClientReady',"", %game.class);
   %game.resetScore(%client);
   for(%i = 1; %i <= %game.numTeams; %i++)
   {
      $Teams[%i].score = 0;
      messageClient(%client, 'MsgCTFAddTeam', "", %i, %game.getTeamName(%i), $flagStatus[%i], $TeamScore[%i]);
   }
   //%game.populateTeamRankArray(%client);

   //messageClient(%client, 'MsgYourRankIs', "", -1);
      
   messageClient(%client, 'MsgMissionDropInfo', '\c0You are in mission %1 (%2).', $MissionDisplayName, $MissionTypeDisplayName, $ServerName ); 

   DefaultGame::clientMissionDropReady(%game, %client);
}

function PracticeCTFGame::assignClientTeam(%game, %client, %respawn)
{
   DefaultGame::assignClientTeam(%game, %client, %respawn);
   // if player's team is not on top of objective hud, switch lines
   messageClient(%client, 'MsgCheckTeamLines', "", %client.team);
}

function PracticeCTFGame::recalcScore(%game, %cl)
{
   %killValue = %cl.kills * %game.SCORE_PER_KILL;
   %deathValue = %cl.deaths * %game.SCORE_PER_DEATH;

   if (%killValue - %deathValue == 0)
      %killPoints = 0;
   else
      %killPoints = (%killValue * %killValue) / (%killValue - %deathValue);

   %cl.offenseScore = %killPoints +
                      %cl.suicides            * %game.SCORE_PER_SUICIDE + 
                      %cl.escortAssists       * %game.SCORE_PER_ESCORT_ASSIST +
                      %cl.teamKills           * %game.SCORE_PER_TEAMKILL + 
                      %cl.tkDestroys          * %game.SCORE_PER_TK_DESTROY + // z0dd - ZOD, 10/03/02. Penalty for tking equiptment.
                      %cl.scoreHeadshot       * %game.SCORE_PER_HEADSHOT + 
                      %cl.scoreRearshot       * %game.SCORE_PER_REARSHOT + // z0dd - ZOD, 8/25/02. Added Lance rear shot messages
                      %cl.flagCaps            * %game.SCORE_PER_PLYR_FLAG_CAP + 
                      %cl.flagGrabs           * %game.SCORE_PER_PLYR_FLAG_TOUCH + 
                      %cl.genDestroys         * %game.SCORE_PER_DESTROY_GEN + 
                      %cl.sensorDestroys      * %game.SCORE_PER_DESTROY_SENSOR + 
                      %cl.turretDestroys      * %game.SCORE_PER_DESTROY_TURRET + 
                      %cl.iStationDestroys    * %game.SCORE_PER_DESTROY_ISTATION + 
                      %cl.vstationDestroys    * %game.SCORE_PER_DESTROY_VSTATION + 
                      %cl.mpbtstationDestroys * %game.SCORE_PER_DESTROY_MPBTSTATION + // z0dd - ZOD 3/30/02. MPB Teleporter
                      %cl.solarDestroys       * %game.SCORE_PER_DESTROY_SOLAR + 
                      %cl.sentryDestroys      * %game.SCORE_PER_DESTROY_SENTRY + 
                      %cl.depSensorDestroys   * %game.SCORE_PER_DESTROY_DEP_SENSOR + 
                      %cl.depTurretDestroys   * %game.SCORE_PER_DESTROY_DEP_TUR + 
                      %cl.depStationDestroys  * %game.SCORE_PER_DESTROY_DEP_INV +
                      %cl.vehicleScore + %cl.vehicleBonus; 

   %cl.defenseScore = %cl.genDefends         * %game.SCORE_PER_GEN_DEFEND +   
                      %cl.flagDefends        * %game.SCORE_PER_FLAG_DEFEND +
                      %cl.carrierKills       * %game.SCORE_PER_CARRIER_KILL +  
                      %cl.escortAssists      * %game.SCORE_PER_ESCORT_ASSIST + 
                      %cl.turretKills        * %game.SCORE_PER_TURRET_KILL_AUTO +  
                      %cl.mannedturretKills  * %game.SCORE_PER_TURRET_KILL +  
                      %cl.genRepairs         * %game.SCORE_PER_REPAIR_GEN +
                      %cl.SensorRepairs      * %game.SCORE_PER_REPAIR_SENSOR +
                      %cl.TurretRepairs      * %game.SCORE_PER_REPAIR_TURRET +
                      %cl.StationRepairs     * %game.SCORE_PER_REPAIR_ISTATION +
                      %cl.VStationRepairs    * %game.SCORE_PER_REPAIR_VSTATION +
                      %cl.mpbtstationRepairs * %game.SCORE_PER_REPAIR_MPBTSTATION + // z0dd - ZOD 3/30/02. MPB Teleporter
                      %cl.solarRepairs       * %game.SCORE_PER_REPAIR_SOLAR +
                      %cl.sentryRepairs      * %game.SCORE_PER_REPAIR_SENTRY +
                      %cl.depInvRepairs      * %game.SCORE_PER_REPAIR_DEP_INV +
                      %cl.depTurretRepairs   * %game.SCORE_PER_REPAIR_DEP_TUR +
                      %cl.returnPts; 

   %cl.score = mFloor(%cl.offenseScore + %cl.defenseScore);
   %game.recalcTeamRanks(%cl);
}

function PracticeCTFGame::updateKillScores(%game, %clVictim, %clKiller, %damageType, %implement)
{
// is this a vehicle kill rather than a player kill
    
   // console error message suppression
   if( isObject( %implement ) )
   {
      if( %implement.getDataBlock().getName() $= "AssaultPlasmaTurret" ||  %implement.getDataBlock().getName() $= "BomberTurret" ) // gunner
           %clKiller = %implement.vehicleMounted.getMountNodeObject(1).client;
      else if(%implement.getDataBlock().catagory $= "Vehicles") // pilot
           %clKiller = %implement.getMountNodeObject(0).client;             
   }

   if(%game.testTurretKill(%implement))   //check for turretkill before awarded a non client points for a kill
      %game.awardScoreTurretKill(%clVictim, %implement);
   else if (%game.testKill(%clVictim, %clKiller)) //verify victim was an enemy
   {
      %value = %game.awardScoreKill(%clKiller);
      %game.shareScore(%clKiller, %value);
      %game.awardScoreDeath(%clVictim);

      if (%game.testGenDefend(%clVictim, %clKiller))
         %game.awardScoreGenDefend(%clKiller);

      if(%game.testCarrierKill(%clVictim, %clKiller))    
         %game.awardScoreCarrierKill(%clKiller);
      else
      {
         if (%game.testFlagDefend(%clVictim, %clKiller))
            %game.awardScoreFlagDefend(%clKiller);
      }
      if (%game.testEscortAssist(%clVictim, %clKiller))
         %game.awardScoreEscortAssist(%clKiller);     
   }       
   else
   {        
      if (%game.testSuicide(%clVictim, %clKiller, %damageType))  //otherwise test for suicide
      {
         %game.awardScoreSuicide(%clVictim);     
      }
      else
      {
         if (%game.testTeamKill(%clVictim, %clKiller)) //otherwise test for a teamkill
            %game.awardScoreTeamKill(%clVictim, %clKiller);
      }
   }
}

function PracticeCTFGame::testFlagDefend(%game, %victimID, %killerID)
{
   InitContainerRadiusSearch(%victimID.plyrPointOfDeath, %game.RADIUS_FLAG_DEFENSE, $TypeMasks::ItemObjectType);
   %objID = containerSearchNext();   
   while(%objID != 0) 
   {
     %objType = %objID.getDataBlock().getName();
     if ((%objType $= "Flag") && (%objID.team == %killerID.team)) 
          return true;  //found the(a) killer's flag near the victim's point of death
     else
        %objID = containerSearchNext();     
   }
   return false; //didn't find a qualifying flag within required radius of victims point of death  
}

function PracticeCTFGame::testGenDefend(%game, %victimID, %killerID)
{
   InitContainerRadiusSearch(%victimID.plyrPointOfDeath, %game.RADIUS_GEN_DEFENSE, $TypeMasks::StaticShapeObjectType);
   %objID = containerSearchNext();
   while(%objID != 0)
   {
      %objType = %objID.getDataBlock().ClassName;
     if ((%objType $= "generator") && (%objID.team == %killerID.team)) 
        return true;  //found a killer's generator within required radius of victim's death
     else
        %objID = containerSearchNext();
   }
   return false;  //didn't find a qualifying gen within required radius of victim's point of death 
}

function PracticeCTFGame::testCarrierKill(%game, %victimID, %killerID)
{
   %flag = %victimID.plyrDiedHoldingFlag;
   return ((%flag !$= "") && (%flag.team == %killerID.team));  
}

function PracticeCTFGame::testEscortAssist(%game, %victimID, %killerID)
{
   return (%victimID.dmgdFlagCarrier); 
}

function PracticeCTFGame::testValidRepair(%game, %obj)
{
    if(!%obj.wasDisabled)
    {
        //error(%obj SPC "was never disabled");
        return false;
    }
    else if(%obj.lastDamagedByTeam == %obj.team)
    {
        //error(%obj SPC "was last damaged by a friendly");
        return false;
    }
    else if(%obj.team != %obj.repairedBy.team)
    {
        //error(%obj SPC "was repaired by an enemy");
        return false;
    }
    else 
    {
        if(%obj.soiledByEnemyRepair)
            %obj.soiledByEnemyRepair = false;
        return true;
    }
}  

function PracticeCTFGame::awardScoreFlagCap(%game, %cl, %flag)
{
    %cl.flagCaps++;
    $TeamScore[%cl.team] += %game.SCORE_PER_TEAM_FLAG_CAP;
    messageAll('MsgTeamScoreIs', "", %cl.team, $TeamScore[%cl.team]);

    %flag.grabber.flagGrabs++;

    if (%game.SCORE_PER_TEAM_FLAG_CAP > 0)
    {
        %plural = (%game.SCORE_PER_PLYR_FLAG_CAP != 1 ? 's' : "");
        %plural2 = (%game.SCORE_PER_PLYR_FLAG_TOUCH != 1 ? 's' : "");

        // z0dd - ZOD, 9/29/02. Removed T2 demo code from here

        // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
        if(%cl == %flag.grabber)
        {
           messageClient(%cl, 'msgCTFFriendCap', '\c0You receive %1 point%2 for stealing and capturing the enemy flag!', %game.SCORE_PER_PLYR_FLAG_CAP+%game.SCORE_PER_PLYR_FLAG_TOUCH, %plural);
           messageTeam(%flag.team, 'msgCTFEnemyCap', '\c0Enemy %1 received %2 point%3 for capturing your flag!', %cl.name, %game.SCORE_PER_PLYR_FLAG_CAP+%game.SCORE_PER_PLYR_FLAG_TOUCH, %plural);
           //messageTeamExcept(%cl, 'msgCTFFriendCap', '\c0Teammate %1 receives %2 point%3 for capturing the enemy flag!', %cl.name, %game.SCORE_PER_PLYR_FLAG_CAP+%game.SCORE_PER_PLYR_FLAG_TOUCH, %plural);  // z0dd - ZOD, 8/15/02. Message is pointless
        }
        else
        {
           if(isObject(%flag.grabber))  // is the grabber still here?
           {
              messageClient(%cl, 'msgCTFFriendCap', '\c0You receive %1 point%2 for capturing the enemy flag!  %3 gets %4 point%5 for the steal assist.', %game.SCORE_PER_PLYR_FLAG_CAP, %plural, %flag.grabber.name, %game.SCORE_PER_PLYR_FLAG_TOUCH, %plural2);
              messageClient(%flag.grabber, 'msgCTFFriendCap', '\c0You receive %1 point%2 for stealing a flag that was subsequently capped by %3.', %game.SCORE_PER_PLYR_FLAG_TOUCH, %plural2, %cl.name);
           }
           else
              messageClient(%cl, 'msgCTFFriendCap', '\c0You receive %1 point%2 for capturing the enemy flag!', %game.SCORE_PER_PLYR_FLAG_CAP, %plural);

           //messageTeamExcept(%cl, 'msgCTFFriendCap', '\c0Teammate %1 receives %2 point%3 for capturing the enemy flag!', %cl.name, %game.SCORE_PER_PLYR_FLAG_CAP, %plural);  // z0dd - ZOD, 8/15/02. Message is pointless
           //messageTeam(%flag.team, 'msgCTFEnemyCap', '\c0Enemy %1 received %2 point%3 for capturing your flag!', %cl.name, %game.SCORE_PER_PLYR_FLAG_CAP, %plural); // z0dd - ZOD, 8/15/02. Message is pointless
        }
        // ---------------------------------------------------
    }

    %game.recalcScore(%cl);

    if(isObject(%flag.grabber))
        %game.recalcScore(%flag.grabber);

    %game.checkScoreLimit(%cl.team);
}


function PracticeCTFGame::awardScoreFlagTouch(%game, %cl, %flag)
{
 
    %flag.grabber = %cl;
    %team = %cl.team;
	if( $DontScoreTimer[%team] )
		return;

	$dontScoreTimer[%team] = true;
   //tinman - needed to remove all game calls to "eval" for the PURE server...
   %game.schedule(%game.TOUCH_DELAY_MS, resetDontScoreTimer, %team);
	//schedule(%game.TOUCH_DELAY_MS, 0, eval, "$dontScoreTimer["@%team@"] = false;");
	schedule(%game.TOUCH_DELAY_MS, 0, eval, "$dontScoreTimer["@%team@"] = false;");
   $TeamScore[%team] += %game.SCORE_PER_TEAM_FLAG_TOUCH;
   messageAll('MsgTeamScoreIs', "", %team, $TeamScore[%team]);

   if (%game.SCORE_PER_TEAM_FLAG_TOUCH > 0)
   {
      %plural = (%game.SCORE_PER_TEAM_FLAG_TOUCH != 1 ? 's' : "");
      messageTeam(%team, 'msgCTFFriendFlagTouch', '\c0Your team receives %1 point%2 for grabbing the enemy flag!', %game.SCORE_PER_TEAM_FLAG_TOUCH, %plural);
      messageTeam(%flag.team, 'msgCTFEnemyFlagTouch', '\c0Enemy %1 receives %2 point%3 for grabbing your flag!', %cl.name, %game.SCORE_PER_TEAM_FLAG_TOUCH, %plural);
   }
   %game.recalcScore(%cl);
   %game.checkScoreLimit(%team);
}

function PracticeCTFGame::resetDontScoreTimer(%game, %team)
{
   $dontScoreTimer[%team] = false;
}

function PracticeCTFGame::checkScoreLimit(%game, %team)
{
   // z0dd - ZOD, 5/12/02. Check for no score limit
   if(!$PracticeCtf::NoScoreLimit)
   {
      %scoreLimit = MissionGroup.CTF_scoreLimit * %game.SCORE_PER_TEAM_FLAG_CAP;
      // default of 5 if scoreLimit not defined
      if(%scoreLimit $= "")
         %scoreLimit = 5 * %game.SCORE_PER_TEAM_FLAG_CAP;
      if($TeamScore[%team] >= %scoreLimit) 
         %game.scoreLimitReached();
   }
   else
      %scoreLimit = %scoreLimit = 999;
}

function PracticeCTFGame::awardScoreFlagReturn(%game, %cl, %perc)
{
   // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
   if (%game.SCORE_PER_FLAG_RETURN != 0)
   {
      %pts = mfloor( %game.SCORE_PER_FLAG_RETURN * (%perc/100) );
      if(%perc  == 100)
         messageClient(%cl, 'scoreFlaRetMsg', 'Flag return - exceeded capping distance - %1 point bonus.', %pts, %perc);
      else if(%perc  == 0)
         messageClient(%cl, 'scoreFlaRetMsg', 'You gently place the flag back on the stand.', %pts, %perc);
      else 
         messageClient(%cl, 'scoreFlaRetMsg', '\c0Flag return from %2%% of capping distance - %1 point bonus.', %pts, %perc);
      %cl.returnPts += %pts;
   }
   %game.recalcScore(%cl);
   return %game.SCORE_PER_FLAG_RETURN;
   // ---------------------------------------------------
}

function PracticeCTFGame::awardScoreStalemateReturn(%game, %cl)
{
   if (%game.SCORE_PER_STALEMATE_RETURN != 0)
   {
      messageClient(%cl, 'scoreStaleRetMsg', '\c0You received a %1 point bonus for a stalemate-breaking, flag return.', %game.SCORE_PER_STALEMATE_RETURN);
        %cl.returnPts += %game.SCORE_PER_STALEMATE_RETURN;
   }
   %game.recalcScore(%cl);
    return %game.SCORE_PER_STALEMATE_RETURN;
}

// Asset Destruction scoring
function PracticeCTFGame::awardScoreGenDestroy(%game,%cl)
{
   %cl.genDestroys++;
   if (%game.SCORE_PER_DESTROY_GEN != 0)
      {
         messageClient(%cl, 'msgGenDes', '\c0You received a %1 point bonus for destroying an enemy generator.', %game.SCORE_PER_DESTROY_GEN);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_GEN;
}

function PracticeCTFGame::awardScoreSensorDestroy(%game,%cl)
{
   %cl.sensorDestroys++;
   if (%game.SCORE_PER_DESTROY_SENSOR != 0)
      {
         messageClient(%cl, 'msgSensorDes', '\c0You received a %1 point bonus for destroying an enemy sensor.', %game.SCORE_PER_DESTROY_SENSOR);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_SENSOR;
}

function PracticeCTFGame::awardScoreTurretDestroy(%game,%cl)
{
   %cl.turretDestroys++;
   if (%game.SCORE_PER_DESTROY_TURRET != 0)
      {
         messageClient(%cl, 'msgTurretDes', '\c0You received a %1 point bonus for destroying an enemy turret.', %game.SCORE_PER_DESTROY_TURRET);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_TURRET;
}

function PracticeCTFGame::awardScoreInvDestroy(%game,%cl)
{
   %cl.IStationDestroys++;
   if (%game.SCORE_PER_DESTROY_ISTATION != 0)
      {
         messageClient(%cl, 'msgInvDes', '\c0You received a %1 point bonus for destroying an enemy inventory station.', %game.SCORE_PER_DESTROY_ISTATION);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_ISTATION;
}

function PracticeCTFGame::awardScoreVehicleStationDestroy(%game,%cl)
{
   %cl.VStationDestroys++;
   if (%game.SCORE_PER_DESTROY_VSTATION != 0)
      {
         messageClient(%cl, 'msgVSDes', '\c0You received a %1 point bonus for destroying an enemy vehicle station.', %game.SCORE_PER_DESTROY_VSTATION);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_VSTATION;
}

// ---------------------------------------------------------
// z0dd - ZOD 3/30/02. MBP Teleporter
function PracticeCTFGame::awardScoreMPBTeleporterDestroy(%game,%cl)
{
   %cl.mpbtstationDestroys++;
   if (%game.SCORE_PER_DESTROY_MPBTSTATION != 0)
   {
      messageClient(%cl, 'msgMPBTeleDes', '\c0You received a %1 point bonus for destroying an enemy MPB teleport station.', %game.SCORE_PER_DESTROY_MPBTSTATION);
      //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
   }
   %game.recalcScore(%cl);
   return %game.SCORE_PER_DESTROY_MPBTSTATION;
}
// ---------------------------------------------------------

function PracticeCTFGame::awardScoreSolarDestroy(%game,%cl)
{
   %cl.SolarDestroys++;
   if (%game.SCORE_PER_DESTROY_SOLAR != 0)
      {
         messageClient(%cl, 'msgSolarDes', '\c0You received a %1 point bonus for destroying an enemy solar panel.', %game.SCORE_PER_DESTROY_SOLAR);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_SOLAR;
}

function PracticeCTFGame::awardScoreSentryDestroy(%game,%cl)
{
   %cl.sentryDestroys++;
   if (%game.SCORE_PER_DESTROY_SENTRY != 0)
      {
         messageClient(%cl, 'msgSentryDes', '\c0You received a %1 point bonus for destroying an enemy sentry turret.', %game.SCORE_PER_DESTROY_SENTRY);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_SENTRY;
}

function PracticeCTFGame::awardScoreDepSensorDestroy(%game,%cl)
{
   %cl.depSensorDestroys++;
   if (%game.SCORE_PER_DESTROY_DEP_SENSOR != 0)
      {
         messageClient(%cl, 'msgDepSensorDes', '\c0You received a %1 point bonus for destroying an enemy deployable sensor.', %game.SCORE_PER_DESTROY_DEP_SENSOR); // z0dd - ZOD, 8/15/02. Added "sensor"
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_DEP_SENSOR;
}

function PracticeCTFGame::awardScoreDepTurretDestroy(%game,%cl)
{
   %cl.depTurretDestroys++;
   if (%game.SCORE_PER_DESTROY_DEP_TUR != 0)
      {
         messageClient(%cl, 'msgDepTurDes', '\c0You received a %1 point bonus for destroying an enemy deployed turret.', %game.SCORE_PER_DESTROY_DEP_TUR);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_DEP_TUR;
}

function PracticeCTFGame::awardScoreDepStationDestroy(%game,%cl)
{
   %cl.depStationDestroys++;
   if (%game.SCORE_PER_DESTROY_DEP_INV != 0)
      {
         messageClient(%cl, 'msgDepInvDes', '\c0You received a %1 point bonus for destroying an enemy deployed station.', %game.SCORE_PER_DESTROY_DEP_INV);
         //messageTeamExcept(%cl, 'msgGenDes', '\c0Teammate %1 received a %2 point bonus for destroying an enemy generator.', %cl.name, %game.SCORE_PER_GEN_DESTROY);
      }
      %game.recalcScore(%cl);
    return %game.SCORE_PER_DESTROY_DEP_INV;
}

// ---------------------------------------------------------
// z0dd - ZOD, 10/03/02. Penalty for TKing equiptment.
function PracticeCTFGame::awardScoreTkDestroy(%game, %cl)
{
   %cl.tkDestroys++;
   if (%game.SCORE_PER_TK_DESTROY != 0)
   {
      messageClient(%cl, 'msgTkDes', '\c0You have been penalized %1 points for destroying your teams equiptment.', %game.SCORE_PER_TK_DESTROY);
   }
   %game.recalcScore(%cl);
   return %game.SCORE_PER_TK_DESTROY;
}
// ---------------------------------------------------------

function PracticeCTFGame::awardScoreGenDefend(%game, %killerID)
{
   %killerID.genDefends++;
   if (%game.SCORE_PER_GEN_DEFEND != 0)
   {
      messageClient(%killerID, 'msgGenDef', '\c0You received a %1 point bonus for defending a generator.', %game.SCORE_PER_GEN_DEFEND);
      messageTeamExcept(%killerID, 'msgGenDef', '\c2%1 defended our generator from an attack.', %killerID.name); // z0dd - ZOD, 8/15/02. Tell team
      //messageTeamExcept(%killerID, 'msgGenDef', '\c0Teammate %1 received a %2 point bonus for defending a generator.', %killerID.name, %game.SCORE_PER_GEN_DEFEND);
   }
   %game.recalcScore(%cl);
    return %game.SCORE_PER_GEN_DEFEND;
}

function PracticeCTFGame::awardScoreCarrierKill(%game, %killerID)
{
   %killerID.carrierKills++;
   if (%game.SCORE_PER_CARRIER_KILL != 0)
   {
      messageClient(%killerID, 'msgCarKill', '\c0You received a %1 point bonus for stopping the enemy flag carrier!', %game.SCORE_PER_CARRIER_KILL);
      messageTeamExcept(%killerID, 'msgCarKill', '\c2%1 stopped the enemy flag carrier.', %killerID.name); // z0dd - ZOD, 8/15/02. Tell team
      //messageTeamExcept(%killerID, 'msgCarKill', '\c0Teammate %1 received a %2 point bonus for stopping the enemy flag carrier!', %killerID.name, %game.SCORE_PER_CARRIER_KILL);
   }
   %game.recalcScore(%killerID);   
    return %game.SCORE_PER_CARRIER_KILL;
}

function PracticeCTFGame::awardScoreFlagDefend(%game, %killerID)
{
   %killerID.flagDefends++;
   if (%game.SCORE_PER_FLAG_DEFEND != 0)
   {
      messageClient(%killerID, 'msgFlagDef', '\c0You received a %1 point bonus for defending your flag!', %game.SCORE_PER_FLAG_DEFEND);
      messageTeamExcept(%killerID, 'msgFlagDef', '\c2%1 defended our flag.', %killerID.name); // z0dd - ZOD, 8/15/02. Tell team
      //messageTeamExcept(%killerID, 'msgFlagDef', '\c0Teammate %1 received a %2 point bonus for defending your flag!', %killerID.name, %game.SCORE_PER_FLAG_DEFEND);
   }      
   %game.recalcScore(%killerID);
    return %game.SCORE_PER_FLAG_DEFEND;
}

function PracticeCTFGame::awardScoreEscortAssist(%game, %killerID)
{
   %killerID.escortAssists++;
   if (%game.SCORE_PER_ESCORT_ASSIST != 0)
   {
      messageClient(%killerID, 'msgEscAsst', '\c0You received a %1 point bonus for protecting the flag carrier!', %game.SCORE_PER_ESCORT_ASSIST);
      messageTeamExcept(%killerID, 'msgEscAsst', '\c2%1 protected our flag carrier.', %killerID.name); // z0dd - ZOD, 8/15/02. Tell team
      //messageTeamExcept(%killerID, 'msgEscAsst', '\c0Teammate %1 received a %2 point bonus for protecting the flag carrier!', %killerID.name, %game.SCORE_PER_ESCORT_ASSIST);
   }
   %game.recalcScore(%killerID);
    return %game.SCORE_PER_ESCORT_ASSIST;
}

function PracticeCTFGame::resetScore(%game, %client)
{
   %client.offenseScore = 0;
   %client.kills = 0;
   %client.deaths = 0;
   %client.suicides = 0;
   %client.escortAssists = 0;
   %client.teamKills = 0;
   %client.tkDestroys = 0; // z0dd - ZOD, 10/03/02. Penalty for tking equiptment.
   %client.flagCaps = 0;
   %client.flagGrabs = 0;
   %client.genDestroys = 0;
   %client.sensorDestroys = 0;
   %client.turretDestroys = 0;
   %client.iStationDestroys = 0;
   %client.vstationDestroys = 0;
   %client.mpbtstationDestroys = 0; // z0dd - ZOD 3/30/02. MPB Teleporter
   %client.solarDestroys = 0;
   %client.sentryDestroys = 0;
   %client.depSensorDestroys = 0;
   %client.depTurretDestroys = 0;
   %client.depStationDestroys = 0;
   %client.vehicleScore = 0; 
   %client.vehicleBonus = 0; 

   %client.flagDefends = 0;
   %client.defenseScore = 0;
   %client.genDefends = 0;
   %client.carrierKills = 0;
   %client.escortAssists = 0;
   %client.turretKills = 0;
   %client.mannedTurretKills = 0;
   %client.flagReturns = 0;
   %client.genRepairs = 0;
   %client.SensorRepairs = 0;
   %client.TurretRepairs = 0;
   %client.StationRepairs = 0;
   %client.VStationRepairs = 0;
   %client.mpbtstationRepairs = 0; // z0dd - ZOD 3/30/02. MPB Teleporter
   %client.solarRepairs = 0;
   %client.sentryRepairs = 0;
   %client.depInvRepairs = 0;
   %client.depTurretRepairs = 0;
   %client.returnPts = 0;
   %client.score = 0;

   // z0dd - ZOD: Reset practice mode varibles
   for(%i=0; %i < $Host::ClassicMaxTelepads; %i++)
   {
      %client.spawnPad[%i] = "";
   }
   %client.transMode = "";
   %client.transPad = 1;
   %client.camera.projectile = "";
   %client.mortarObs = "";
   %client.missileObs = "";
   %client.grenadeObs = "";
   %client.discObs = "";
}

function PracticeCTFGame::objectRepaired(%game, %obj, %objName)
{     
   %item = %obj.getDataBlock().getName();
   switch$ (%item)
   {
      case generatorLarge :
         %game.genOnRepaired(%obj, %objName);
      case sensorMediumPulse : 
         %game.sensorOnRepaired(%obj, %objName);
      case sensorLargePulse : 
         %game.sensorOnRepaired(%obj, %objName);
      case stationInventory :
         %game.stationOnRepaired(%obj, %objName);
      case turretBaseLarge : 
         %game.turretOnRepaired(%obj, %objName);
      case stationVehicle :
        %game.vStationOnRepaired(%obj, %objName);
      case MPBTeleporter : // z0dd - ZOD 3/30/02. MPB Teleporter
        %game.mpbTStationOnRepaired(%obj, %objName);
      case solarPanel :
        %game.solarPanelOnRepaired(%obj, %objName);
      case sentryTurret :
        %game.sentryTurretOnRepaired(%obj, %objName);
      case TurretDeployedWallIndoor:
        %game.depTurretOnRepaired(%obj, %objName);
      case TurretDeployedFloorIndoor:
        %game.depTurretOnRepaired(%obj, %objName);
      case TurretDeployedCeilingIndoor:
        %game.depTurretOnRepaired(%obj, %objName);
      case TurretDeployedOutdoor:
        %game.depTurretOnRepaired(%obj, %objName);
   }
   %obj.wasDisabled = false;
}

function PracticeCTFGame::genOnRepaired(%game, %obj, %objName)
{
      
   if (%game.testValidRepair(%obj))
   {
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgGenRepaired', '\c0%1 repaired the %2 Generator!', %repairman.name, %obj.nameTag);
      %game.awardScoreGenRepair(%obj.repairedBy);
   }           
}

function PracticeCTFGame::stationOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgStationRepaired', '\c0%1 repaired the %2 Inventory Station!', %repairman.name, %obj.nameTag);
      %game.awardScoreStationRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::sensorOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgSensorRepaired', '\c0%1 repaired the %2 Pulse Sensor!', %repairman.name, %obj.nameTag);
      %game.awardScoreSensorRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::turretOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgTurretRepaired', '\c0%1 repaired the %2 Turret!', %repairman.name, %obj.nameTag);
      %game.awardScoreTurretRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::vStationOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgvstationRepaired', '\c0%1 repaired the Vehicle Station!', %repairman.name);
      %game.awardScoreVStationRepair(%obj.repairedBy);
   }
}

// z0dd - ZOD 3/17/02. Score for repairing MPB Teleporter station.
function PracticeCTFGame::mpbTStationOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgTeleporterRepaired', '\c0%1 repaired the MPB Teleporter Station!', %repairman.name);
      %game.awardScoreMpbTStationRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::solarPanelOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgsolarRepaired', '\c0%1 repaired the %2 Solar Panel!', %repairman.name, %obj.nameTag);
      %game.awardScoreSolarRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::sentryTurretOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgsentryTurretRepaired', '\c0%1 repaired the %2 Sentry Turret!', %repairman.name, %obj.nameTag);
      %game.awardScoreSentryRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::depTurretOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgdepTurretRepaired', '\c4%1 repaired a %2 Deployable Turret!', %repairman.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Added this line
      %game.awardScoreDepTurretRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::depInvOnRepaired(%game, %obj, %objName)
{
   if (%game.testValidRepair(%obj)) 
   {     
      %repairman = %obj.repairedBy;
      teamRepairMessage(%repairman, 'msgdepInvRepaired', '\c4%1 repaired a %2 Deployable Inventory!', %repairman.name, %obj.nameTag); // z0dd - ZOD, 8/20/02. Added this line
      %game.awardScoreDepInvRepair(%obj.repairedBy);
   }
}

function PracticeCTFGame::awardScoreGenRepair(%game, %cl)
{
   %cl.genRepairs++;
   if (%game.SCORE_PER_REPAIR_GEN != 0)
   {
      messageClient(%cl, 'msgGenRep', '\c0You received a %1 point bonus for repairing a generator.', %game.SCORE_PER_REPAIR_GEN);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreStationRepair(%game, %cl)
{
   %cl.stationRepairs++;
   if (%game.SCORE_PER_REPAIR_ISTATION != 0)
   {
      messageClient(%cl, 'msgIStationRep', '\c0You received a %1 point bonus for repairing a inventory station.', %game.SCORE_PER_REPAIR_ISTATION);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreSensorRepair(%game, %cl)
{
   %cl.sensorRepairs++;
   if (%game.SCORE_PER_REPAIR_SENSOR != 0)
   {
      messageClient(%cl, 'msgSensorRep', '\c0You received a %1 point bonus for repairing a sensor.', %game.SCORE_PER_REPAIR_SENSOR);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreTurretRepair(%game, %cl)
{
   %cl.TurretRepairs++;
   if (%game.SCORE_PER_REPAIR_TURRET != 0)
   {
      messageClient(%cl, 'msgTurretRep', '\c0You received a %1 point bonus for repairing a base turret.', %game.SCORE_PER_REPAIR_TURRET);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreVStationRepair(%game, %cl)
{
   %cl.VStationRepairs++;
   if (%game.SCORE_PER_REPAIR_VSTATION != 0)
   {
      messageClient(%cl, 'msgVStationRep', '\c0You received a %1 point bonus for repairing a vehicle station.', %game.SCORE_PER_REPAIR_VSTATION);
   }
   %game.recalcScore(%cl);
}

// z0dd - ZOD 3/17/02. Score for repairing MPB Teleporter station.
function PracticeCTFGame::awardScoreMpbTStationRepair(%game, %cl)
{
   %cl.mpbtstationRepairs++;
   if (%game.SCORE_PER_REPAIR_MPBTSTATION != 0)
   {
      messageClient(%cl, 'msgVStationRep', '\c0You received a %1 point bonus for repairing a mpb teleporter station.', %game.SCORE_PER_REPAIR_TSTATION);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreSolarRepair(%game, %cl)
{
   %cl.solarRepairs++;
   if (%game.SCORE_PER_REPAIR_SOLAR != 0)
   {
      messageClient(%cl, 'msgsolarRep', '\c0You received a %1 point bonus for repairing a solar panel.', %game.SCORE_PER_REPAIR_SOLAR);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreSentryRepair(%game, %cl)
{
   %cl.sentryRepairs++;
   if (%game.SCORE_PER_REPAIR_SENTRY != 0)
   {
      messageClient(%cl, 'msgSentryRep', '\c0You received a %1 point bonus for repairing a sentry turret.', %game.SCORE_PER_REPAIR_SENTRY);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreDepTurretRepair(%game, %cl)
{
   %cl.depTurretRepairs++;
   if (%game.SCORE_PER_REPAIR_DEP_TUR != 0)
   {
      messageClient(%cl, 'msgDepTurretRep', '\c0You received a %1 point bonus for repairing a deployed turret.', %game.SCORE_PER_REPAIR_DEP_TUR);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::awardScoreDepInvRepair(%game, %cl)
{
   %cl.depInvRepairs++;
   if (%game.SCORE_PER_REPAIR_DEP_INV != 0)
   {
      messageClient(%cl, 'msgDepInvRep', '\c0You received a %1 point bonus for repairing a deployed station.', %game.SCORE_PER_REPAIR_DEP_INV);
   }
   %game.recalcScore(%cl);
}

function PracticeCTFGame::enterMissionArea(%game, %playerData, %player)
{
    if(%player.getState() $= "Dead")
    return;
    %player.client.outOfBounds = false; 
   messageClient(%player.client, 'EnterMissionArea', '\c1You are back in the mission area.');
   logEcho(%player.client.nameBase@" (pl "@%player@"/cl "@%player.client@") entered mission area");

   //the instant a player leaves the mission boundary, the flag is dropped, and the return is scheduled...
   if (%player.holdingFlag > 0)
   {
      cancel($FlagReturnTimer[%player.holdingFlag]);
      $FlagReturnTimer[%player.holdingFlag] = "";
   }
}

function PracticeCTFGame::leaveMissionArea(%game, %playerData, %player)
{
    if(%player.getState() $= "Dead")
    return;
   // maybe we'll do this just in case
   %player.client.outOfBounds = true;
   // if the player is holding a flag, strip it and throw it back into the mission area
   // otherwise, just print a message
   if(%player.holdingFlag > 0)
      %game.boundaryLoseFlag(%player);
   else
      messageClient(%player.client, 'MsgLeaveMissionArea', '\c1You have left the mission area.~wfx/misc/warning_beep.wav');
   logEcho(%player.client.nameBase@" (pl "@%player@"/cl "@%player.client@") left mission area");
}

function PracticeCTFGame::boundaryLoseFlag(%game, %player)
{
   // this is called when a player goes out of the mission area while holding
   // the enemy flag. - make sure the player is still out of bounds
   if (!%player.client.outOfBounds || !isObject(%player.holdingFlag))
      return;

   // ------------------------------------------------------------------------------
   // z0dd - ZOD - SquirrelOfDeath, 9/27/02. Delay on grabbing flag after tossing it
   %player.flagTossWait = true;
   %player.schedule(1000, resetFlagTossWait);
   // -------------------------------------------------------------------------------

   %client = %player.client;
   %flag = %player.holdingFlag;
   %flag.setVelocity("0 0 0");
   %flag.setTransform(%player.getWorldBoxCenter());
   %flag.setCollisionTimeout(%player);

   %held = %game.formatTime(getSimTime() - %game.flagHeldTime[%flag], false); // z0dd - ZOD, 8/15/02. How long did player hold flag?

   %game.playerDroppedFlag(%player);

   // now for the tricky part -- throwing the flag back into the mission area
   // let's try throwing it back towards its "home"
   %home = %flag.originalPosition;
   %vecx =  firstWord(%home) - firstWord(%player.getWorldBoxCenter());
   %vecy = getWord(%home, 1) - getWord(%player.getWorldBoxCenter(), 1);
   %vecz = getWord(%home, 2) - getWord(%player.getWorldBoxCenter(), 2);
   %vec = %vecx SPC %vecy SPC %vecz;

   // normalize the vector, scale it, and add an extra "upwards" component
   %vecNorm = VectorNormalize(%vec);
   %vec = VectorScale(%vecNorm, 1500);
   %vec = vectorAdd(%vec, "0 0 500");

   // z0dd - ZOD, 6/09/02. Remove anti-hover so flag can be thrown properly
   %flag.static = false;

   // z0dd - ZOD, 10/02/02. Hack for flag collision bug.
   %flag.searchSchedule = %game.schedule(10, "startFlagCollisionSearch", %flag);

   // apply the impulse to the flag object
   %flag.applyImpulse(%player.getWorldBoxCenter(), %vec);

   //don't forget to send the message
   //messageClient(%player.client, 'MsgCTFFlagDropped', '\c1You have left the mission area and lost the flag.~wfx/misc/flag_drop.wav', 0, 0, %player.holdingFlag.team);

   // z0dd - ZOD 3/30/02. Above message was sending the wrong varible to objective hud.
   messageClient(%player.client, 'MsgCTFFlagDropped', '\c1You have left the mission area and lost the flag. (Held: %4)~wfx/misc/flag_drop.wav', %client.name, 0, %flag.team, %held); // z0dd - ZOD, 8/19/02. yogi. 3rd param changed from 0 to %client.name
   logEcho(%player.client.nameBase@" (pl "@%player@"/cl "@%player.client@") lost flag (out of bounds)"@" (Held: "@%held@")");
}

function PracticeCTFGame::dropFlag(%game, %player)
{
   if(%player.holdingFlag > 0)
   {
      if (!%player.client.outOfBounds)
         %player.throwObject(%player.holdingFlag);
      else
         %game.boundaryLoseFlag(%player);
   }
}

function PracticeCTFGame::applyConcussion(%game, %player)
{
   %game.dropFlag( %player );
}

function PracticeCTFGame::vehicleDestroyed(%game, %vehicle, %destroyer)
{
    //vehicle name
    %data = %vehicle.getDataBlock();
    //%vehicleType = getTaggedString(%data.targetNameTag) SPC getTaggedString(%data.targetTypeTag);
    %vehicleType = getTaggedString(%data.targetTypeTag);
    if(%vehicleType !$= "MPB")
        %vehicleType = strlwr(%vehicleType);
    
    %enemyTeam = ( %destroyer.team == 1 ) ? 2 : 1;
    
    %scorer = 0;
    %multiplier = 1;
    
    %passengers = 0;
    for(%i = 0; %i < %data.numMountPoints; %i++)
        if(%vehicle.getMountNodeObject(%i))
            %passengers++;
    
    //what destroyed this vehicle
    if(%destroyer.client)
    {
        //it was a player, or his mine, satchel, whatever...
        %destroyer = %destroyer.client;
        %scorer = %destroyer;
        
        // determine if the object used was a mine
        if(%vehicle.lastDamageType == $DamageType::Mine)
            %multiplier = 2;
    }    
    else if(%destroyer.getClassName() $= "Turret")
    {
        if(%destroyer.getControllingClient())
        {
            //manned turret
            %destroyer = %destroyer.getControllingClient();
            %scorer = %destroyer;
        }
        else 
        {
            %destroyerName = "A turret";
            %multiplier = 0;
        }
    }    
    else if(%destroyer.getDataBlock().catagory $= "Vehicles")
    {
        // Vehicle vs vehicle kill!
        if(%name $= "BomberFlyer" || %name $= "AssaultVehicle")
            %gunnerNode = 1;
        else
            %gunnerNode = 0;
        
        if(%destroyer.getMountNodeObject(%gunnerNode))
        {
            %destroyer = %destroyer.getMountNodeObject(%gunnerNode).client;
            %scorer = %destroyer;
        }
        %multiplier = 3;
    }
    else  // Is there anything else we care about?
        return;

    
    if(%destroyerName $= "")
        %destroyerName = %destroyer.name;
        
    if(%vehicle.team == %destroyer.team) // team kill
    {
        %pref = (%vehicleType $= "Assault Tank") ? "an" : "a";
        messageAll( 'msgVehicleTeamDestroy', '\c0%1 TEAMKILLED %3 %2!', %destroyerName, %vehicleType, %pref);
    }
        
    else // legit kill
    {
        //messageTeamExcept(%destroyer, 'msgVehicleDestroy', '\c0%1 destroyed an enemy %2.', %destroyerName, %vehicleType); // z0dd - ZOD, 8/20/02. not needed with new messenger on line below
        teamDestroyMessage(%destroyer, 'msgVehDestroyed', '\c5%1 destroyed an enemy %2!', %destroyerName, %vehicleType); // z0dd - ZOD, 8/20/02. Send teammates a destroy message
        messageTeam(%enemyTeam, 'msgVehicleDestroy', '\c0%1 destroyed your team\'s %2.', %destroyerName, %vehicleType);
        //messageClient(%destroyer, 'msgVehicleDestroy', '\c0You destroyed an enemy %1.', %vehicleType);
    
        if(%scorer)
        {
            %value = %game.awardScoreVehicleDestroyed(%scorer, %vehicleType, %multiplier, %passengers);
            %game.shareScore(%value);
        }
    }
}

function PracticeCTFGame::awardScoreVehicleDestroyed(%game, %client, %vehicleType, %mult, %passengers)
{
    // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
    
    if(%vehicleType $= "Grav Cycle")
        %base = %game.SCORE_PER_DESTROY_WILDCAT;
    else if(%vehicleType $= "Assault Tank")
        %base = %game.SCORE_PER_DESTROY_TANK;
    else if(%vehicleType $= "MPB")
        %base = %game.SCORE_PER_DESTROY_MPB;
    else if(%vehicleType $= "Turbograv")
        %base = %game.SCORE_PER_DESTROY_SHRIKE;
    else if(%vehicleType $= "Bomber")
        %base = %game.SCORE_PER_DESTROY_BOMBER;
    else if(%vehicleType $= "Heavy Transport")
        %base = %game.SCORE_PER_DESTROY_TRANSPORT;
    
    %total = ( %base * %mult ) + ( %passengers * %game.SCORE_PER_PASSENGER ); 

    %client.vehicleScore += %total;
    
    messageClient(%client, 'msgVehicleScore', '\c0You received a %1 point bonus for destroying an enemy %2.', %total, %vehicleType);
    %game.recalcScore(%client);
    return %total;
}

function PracticeCTFGame::shareScore(%game, %client, %amount)
{
    // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
    
    //error("share score of"SPC %amount SPC "from client:" SPC %client); 
    // all of the player in the bomber and tank share the points
    // gained from any of the others
    %vehicle = %client.vehicleMounted;
    if(!%vehicle)
        return 0;
    %vehicleType = getTaggedString(%vehicle.getDataBlock().targetTypeTag);
    if(%vehicleType $= "Bomber" || %vehicleType $= "Assault Tank")
    {
        for(%i = 0; %i < %vehicle.getDataBlock().numMountPoints; %i++)
        {
            %occupant = %vehicle.getMountNodeObject(%i);
            if(%occupant)
            {
                %occCl = %occupant.client;
                if(%occCl != %client && %occCl.team == %client.team)
                {
                    // the vehicle has a valid teammate at this node
                    // share the score with them
                    %occCl.vehicleBonus += %amount;
                    %game.recalcScore(%occCl);
                }
            }        
        }
    }
}

function PracticeCTFGame::awardScoreTurretKill(%game, %victimID, %implement)
{
    if ((%killer = %implement.getControllingClient()) != 0) //award whoever might be controlling the turret
    {
        if (%killer == %victimID)
            %game.awardScoreSuicide(%victimID);
        else if (%killer.team == %victimID.team) //player controlling a turret killed a teammate     
        {
            %killer.teamKills++;
            %game.awardScoreTurretTeamKill(%victimID, %killer);
            %game.awardScoreDeath(%victimID);
        }
        else
        {
            %killer.mannedturretKills++;
            %game.recalcScore(%killer);
            %game.awardScoreDeath(%victimID);
        }     
    }   
    else if ((%killer = %implement.owner) != 0) //if it isn't controlled, award score to whoever deployed it
    {
        if (%killer.team == %victimID.team)       
        {
            %game.awardScoreDeath(%victimID);
        }
        else       
        {
            %killer.turretKills++;
            %game.recalcScore(%killer);
            %game.awardScoreDeath(%victimID);
        }
    }   
    //default is, no one was controlling it, no one owned it.  No score given.   
}

function PracticeCTFGame::testKill(%game, %victimID, %killerID)
{
   return ((%killerID !=0) && (%victimID.team != %killerID.team));
}

function PracticeCTFGame::awardScoreKill(%game, %killerID)
{
   %killerID.kills++;   
   %game.recalcScore(%killerID);    
    return %game.SCORE_PER_KILL;
}

/////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: Practice Mode functions //////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

function PracticeCTFGame::onClientLeaveGame(%game, %client)
{
   // Clear out this clients transfer pads
   for( %i = 0; %i < $Host::ClassicMaxTelepads; %i++ )
   {
      if(isObject(%client.spawnPad[%i]))
         %client.spawnPad[%i].setDamageState(Destroyed);
   }

   // Call the default
   DefaultGame::onClientLeaveGame(%game, %client);
}

function PracticeCTFGame::clientChangeTeam(%game, %client, %team, %fromObs)
{
   // Clear out this clients transfer pads
   for( %i = 0; %i < $Host::ClassicMaxTelepads; %i++ ) {
      if(isObject(%client.spawnPad[%i]))
         %client.spawnPad[%i].setDamageState(Destroyed);
   }

   // Call the default
   DefaultGame::clientChangeTeam(%game, %client, %team, %fromObs);
}

function PracticeCTFGame::equip(%game, %player)
{
   for(%i =0; %i<$InventoryHudCount; %i++)
      %player.client.setInventoryHudItem($InventoryHudData[%i, itemDataName], 0, 1);
   %player.client.clearBackpackIcon();

   if(!%player.client.isAIControlled() && $PracticeCtf::SpawnFavs && !$PracticeCtf::SpawnOnly)
   {
      buyFavorites(%player.client);
      %player.setEnergyLevel(%player.getDataBlock().maxEnergy);
      %player.selectWeaponSlot( 0 );
   }
   else
   {
      %player.setInventory(Blaster,1);
      %player.setInventory(Disc,1);
      %player.setInventory(DiscAmmo, 999);
      %player.setInventory(Chaingun, 1);
      %player.setInventory(ChaingunAmmo, 999);
      %player.setInventory(RepairKit,1);
      %player.setInventory(Grenade, 30);
      %player.setInventory(Beacon, 20);
      %player.setInventory(TargetingLaser, 1);
      %player.weaponCount = 3;

      %player.use("Blaster");
   }
}

/////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: Map Reset functions///////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

function PracticeCTFGame::resetMission(%game)
{
   Cancel( %game.timeCheck );
   CancelCountdown();
   CancelEndCountdown();

   //setup the second half...
   // MES -- per MarkF, scheduling for 0 seconds will prevent player deletion-related crashes
   %game.schedule(0, "refreshAll");
}

function PracticeCTFGame::refreshAll(%game)
{
   //stop the game and the bots
   $MatchStarted = false;
   $CountdownStarted = false;
   AISystemEnabled(false);

   // Let everyone know what is going on
   bottomPrintAll("\nYou are in observation mode while mission is resetting.\n", $Host::warmupTime, 3);

   // Return all flags that aren't home
   for(%f = 1; %f <= %game.numTeams; %f++)
   {
      if(!$TeamFlag[%f].isHome)
      {
         %game.flagReturn($TeamFlag[%f]);
      }
   }

   //reset stations and vehicles that players were using
   // Function exists in Siege, use it, no need to duplicate
   SiegeGame::resetPlayers(%game);

   // zero out the counts for deployable items (found in defaultGame.cs)
   %game.clearDeployableMaxes();

   // Clean up deployables triggers
   cleanTriggers(nameToID("MissionCleanup/Deployables"));

   // clean up the MissionCleanup group - note, this includes deleting all the player objects
   %clean = nameToID("MissionCleanup");
   %clean.schedule(800, "housekeeping");
   //%clean.housekeeping(); // Function exists in Siege no need to duplicate

   // Vehicle objects placed in original position
   // This apprently switches the vehicles team as well, not a good idea.
   //resetNonStaticObjPositions();

   // Restore static objects to their original condition
   // Function exists in defaultGame.cs, in turn calls PracticeCTFGame::groupObjectRestore
   %group = nameToID("MissionGroup/Teams");
   %group.objectRestore();

   %count = ClientGroup.getCount();
   echo("-----------count=" @ %count);
   for(%cl = 0; %cl < %count; %cl++)
   {
      %client = ClientGroup.getObject(%cl);
      if( !%client.isAIControlled() )
      {
         Game.forceObserver( %client, "playerChoose" );

         // old code. wasn't working correctly for some reason
         // Put everybody in observer mode:
         //%client.camera.getDataBlock().setMode( %client.camera, "observerStaticNoNext" );
         //%client.setControlObject( %client.camera );

         // This sets certain keybinds, usefull, lets keep it?
         //commandToClient( %client, 'setHudMode', 'Standard' );
         //commandToClient( %client, 'ControlObjectReset' );
            
         //clientResetTargets(%client, true);
         //%client.notReady = true;
      }
   }
   %game.schedule( $Host::warmupTime * 1000, "resetOver" );
}

function PracticeCTFGame::groupObjectRestore(%game, %this)
{
   for(%i = 0; %i < %this.getCount(); %i++)
      %this.getObject(%i).objectRestore();
}

function PracticeCTFGame::shapeObjectRestore(%game, %object)
{
   if(%object.getDamageLevel())
   {
      %object.setDamageLevel(0.0);
      %object.setDamageState(Enabled);
   }
   if(%object.getDatablock().getName() $= "TurretBaseLarge")
   {
      // check to see if the turret base still has the same type of barrel it had
      // at the beginning of the mission
      if(%object.getMountedImage(0))
      {
         if(%object.getMountedImage(0).getName() !$= %object.originalBarrel)
         {
            // pop the "new" barrel
            %object.unmountImage(0);
            // mount the original barrel
            %object.mountImage(%object.initialBarrel, 0, false);
         }
      }
   }
}

function PracticeCTFGame::resetOver( %game )
{
   // Reset the team scores
   for(%j = 1; %j <= %game.numTeams; %j++)
      $TeamScore[%j] = 0;

   // drop all players into mission
   %game.dropPlayers();

   //setup the AI for the second half
   %game.aiHalfTime();
   
   // start the mission again (release players)
   %game.startupCountDown( $Host::warmupTime );
}

function PracticeCTFGame::dropPlayers( %game )
{
   %count = ClientGroup.getCount();
   for(%cl = 0; %cl < %count; %cl++)
   {
      %client = ClientGroup.getObject(%cl);

      // Reset client score
      %game.resetScore(%client);

      // Resend the scores and flag status
      for(%i = 1; %i <= %game.numTeams; %i++)
         messageClient(%client, 'MsgCTFAddTeam', "", %i, %game.getTeamName(%i), $flagStatus[%i], $TeamScore[%i]);

      if( !%client.isAIControlled() )
      {
         // keep observers in observer mode
         if(%client.team == 0)
            %client.camera.getDataBlock().setMode(%client.camera, "justJoined");
         else
         {
            %game.spawnPlayer( %client, false );
            
            %client.camera.getDataBlock().setMode( %client.camera, "pre-game", %client.player );
            %client.setControlObject( %client.camera );
            //%client.notReady = false;
         }
      }
   }
}

function PracticeCTFGame::startupCountDown(%game, %time)
{
   %game.startupCountDown = true;
   $MatchStarted = false;

   %timeMS = %time * 1000;
   %game.schedule(%timeMS, "startMap");
   notifyMatchStart(%timeMS);

   if(%timeMS > 30000)
      schedule(%timeMS - 30000, 0, "notifyMatchStart", 30000);
   if(%timeMS > 20000)
      schedule(%timeMS - 20000, 0, "notifyMatchStart", 20000);
   if(%timeMS > 10000)
      schedule(%timeMS - 10000, 0, "notifyMatchStart", 10000);
   if(%timeMS > 5000)
      schedule(%timeMS - 5000, 0, "notifyMatchStart", 5000);
   if(%timeMS > 4000)
      schedule(%timeMS - 4000, 0, "notifyMatchStart", 4000);
   if(%timeMS > 3000)
      schedule(%timeMS - 3000, 0, "notifyMatchStart", 3000);
   if(%timeMS > 2000)
      schedule(%timeMS - 2000, 0, "notifyMatchStart", 2000);
   if(%timeMS > 1000)
      schedule(%timeMS - 1000, 0, "notifyMatchStart", 1000);
}

function PracticeCTFGame::startMap(%game)
{
   $MatchStarted = true;
   $CountdownStarted = true;
   %game.startupCountDown = false;

   MessageAll('MsgMissionStart', "\c2Match started");

   // reset the timelimit
   %curTimeLeftMS = $Host::TimeLimit * 60 * 1000;
   $missionStartTime = getSimTime();
   %game.timeCheck = %game.schedule(20000, "checkTimeLimit");

   //schedule the end of match countdown
   EndCountdown($Host::TimeLimit * 60 * 1000);

   // set all clients control to their player
   %count = ClientGroup.getCount();
   for( %i = 0; %i < %count; %i++ )
   {
      %cl = ClientGroup.getObject(%i);

      // Send clients the new clock
      messageClient(%cl, 'MsgSystemClock', "", $Host::TimeLimit, %curTimeLeftMS);

      if (!isObject(%cl.player))
         commandToClient(%cl, 'setHudMode', 'Observer');
      else
      {
         %cl.observerMode = "";
         %cl.setControlObject( %cl.player );
         commandToClient(%cl, 'setHudMode', 'Standard');
         %client.notReady = false;
      }
   }

   //now synchronize everyone's clock
   updateClientTimes(%curTimeLeftMS);

   //start the bots up again...
   AISystemEnabled(true);
}

/////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: Hud related Practice functions ///////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

function PracticeCTFGame::initPracticeHud(%game, %client, %val)
{
   commandToClient(%client, 'initializePracHud', "PracticeCTF");
   commandToClient(%client, 'practiceHudHead', "CTF Practice Config", "Server Settings", "Player Settings", "Projectile Observation", "Telepad Options", "Spawn Vehicle at Pad");

   // Send admins full set of options. Edit, this was causing problems when client requires update.
   //if(%client.isAdmin)
   //{
      commandToClient(%client, 'practiceHudPopulate', "Minimum Turrets", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16");
      commandToClient(%client, 'practiceHudPopulate', "Return Flag", getTaggedString(Game.getTeamName(1)), getTaggedString(Game.getTeamName(2)));
      //if(%client.isSuperAdmin)
      //{
         commandToClient(%client, 'practiceHudPopulate', "Save Deployables", $MissionDisplayName @ " File 1", $MissionDisplayName @ " File 2", $MissionDisplayName @ " File 3", $MissionDisplayName @ " File 4", $MissionDisplayName @ " File 5", $MissionDisplayName @ " File 6");
         commandToClient(%client, 'practiceHudPopulate', "Load Deployables", $MissionDisplayName @ " File 1", $MissionDisplayName @ " File 2", $MissionDisplayName @ " File 3", $MissionDisplayName @ " File 4", $MissionDisplayName @ " File 5", $MissionDisplayName @ " File 6");
      //}
   //}
   commandToClient(%client, 'practiceHudDone');
}

function PracticeCTFGame::sendPracHudUpdate(%game, %client, %msg)
{
   %serverOptMask = 0;
   if ($PracticeCtf::UnlimAmmo == 1)      %serverOptMask += 1;
   if ($PracticeCtf::AutoFlagReturn == 1) %serverOptMask += 2;
   if ($PracticeCtf::SpawnFavs == 1)      %serverOptMask += 4;
   if ($PracticeCtf::SpawnOnly == 1)      %serverOptMask += 8;
   if ($PracticeCtf::NoScoreLimit == 1)   %serverOptMask += 16;
   if ($PracticeCtf::ProtectStatics == 1) %serverOptMask += 32;
                             
   %clientOptMask = 0;
   if (%client.discObs == 1)    %clientOptMask += 1;
   if (%client.grenadeObs == 1) %clientOptMask += 2;
   if (%client.mortarObs == 1)  %clientOptMask += 4;
   if (%client.missileObs == 1) %clientOptMask += 8;
   if (%client.transMode == 0)  %clientOptMask += 16;
   if (%client.transMode == 1)  %clientOptMask += 32;

   messageClient(%client, 'UpdatePracHud', %msg, %serverOptMask, %clientOptMask, %client.isAdmin + %client.isSuperAdmin);
}

function PracticeCTFGame::practiceBtnCmd(%game, %client, %btn, %val)
{
   // dont let non admins change practice mode server settings
   if((mFloor(%btn/10) == 1) && (!%client.isAdmin) && (!%client.isSuperAdmin))
   {
      messageClient(%client, 'MsgPracMode', '\c2Only admins can change that Server Setting.~wfx/misc/misc.error.wav');
      return;
   }
   if(!$MatchStarted)
   {
      messageClient( %client, 'MsgPracMode', '\c2You must wait for the match to start before using practice commands.~wfx/misc/misc.error.wav');
      return;
   }

   %name = %client.nameBase;
   %snd = "~wfx/misc/warning_beep.wav";
 
   switch$ (%btn)
   {
      // GUI CONTROLS
      
      // SERVER BUTTONS
      case 10:
         $PracticeCtf::UnlimAmmo = !$PracticeCtf::UnlimAmmo;
         sendAllPracHudUpdate(%game, "\c3" @ %name @ "\c2: 999 AMMO turned \c3" @ ($PracticeCtf::UnlimAmmo ? "ON" : "OFF") @ %snd);

      case 11:
         $PracticeCtf::AutoFlagReturn = !$PracticeCtf::AutoFlagReturn;
         sendAllPracHudUpdate(%game, "\c3" @ %name @ "\c2: AUTO-RETURN FLAGS turned \c3" @ ($PracticeCtf::AutoFlagReturn ? "ON" : "OFF") @ %snd);

      case 12:
         $PracticeCtf::SpawnFavs = !$PracticeCtf::SpawnFavs;
         sendAllPracHudUpdate(%game, "\c3" @ %name @ "\c2: SPAWN IN FAVORITE turned \c3" @ ($PracticeCtf::SpawnFavs ? "ON" : "OFF") @ %snd);

      case 13:
         $PracticeCtf::SpawnOnly = !$PracticeCtf::SpawnOnly;
         sendAllPracHudUpdate(%game, "\c3" @ %name @ "\c2: SPAWN ONLY turned \c3" @ ($PracticeCtf::SpawnOnly ? "ON" : "OFF") @ %snd);

      case 14:
         $PracticeCtf::NoScoreLimit = !$PracticeCtf::NoScoreLimit;
         sendAllPracHudUpdate(%game, "\c3" @ %name @ "\c2: NO SCORE LIMIT turned \c3" @ ($PracticeCtf::NoScoreLimit ? "ON" : "OFF") @ %snd);
         for ( %team = 1; %team <= %game.numTeams; %team++ )
            %game.checkScoreLimit(%team);

      case 15:
         $PracticeCtf::ProtectStatics = !$PracticeCtf::ProtectStatics;
         sendAllPracHudUpdate(%game, "\c3" @ %name @ "\c2: PROTECT ASSESTS turned \c3" @ ($PracticeCtf::ProtectStatics ? "ON" : "OFF") @ %snd);

      case 16:
         messageAll( 'MsgPracMode', '\c3%1\c2: Resetting map.%2', %client.name, %snd);
         %game.resetMission();
      
      // TELEPORT OPTIONS
      case 20:
         %client.transMode = 0; // beacon
         //messageClient( %client, 'MsgPracMode', '\c2Beacon deploy mode: \c3Beacon\c2.%1', %snd);

      case 21:
         %client.transMode = 1; // telepad
         //messageClient( %client, 'MsgPracMode', '\c2Beacon deploy mode: \c3Transfer Pad\c2.%1', %snd);

      case 22:
         selectPad(%client);

      case 23:
         destroyPad(%client, 0);

      case 24:
         teleportToPad(%client);
         
      // SPAWN VEHICLES
      case 30:
         spawnVehAtPad(%client, "ScoutVehicle");

      case 31:
         spawnVehAtPad(%client, "AssaultVehicle");

      case 32:
         spawnVehAtPad(%client, "MobileBaseVehicle");

      case 33:
         spawnVehAtPad(%client, "ScoutFlyer");

      case 34:
         spawnVehAtPad(%client, "BomberFlyer");

      case 35:
         spawnVehAtPad(%client, "HAPCFlyer");
      
      // PROJECTILE OBSERVATION
      case 40:
         %client.discObs = %val;

      case 41:
         %client.grenadeObs = %val;

      case 42:
         %client.mortarObs = %val;

      case 43:
         %client.missileObs = %val;

      default:
         messageClient( %client, 'MsgError', '\c2Unknown values.%1', %snd);
   }
}

// Menu Submit handler
function PracticeCTFGame::updatePracticeHudSet(%game, %client, %opt, %val)
{
   // dont let non admins change practice mode server settings
   if((%opt < 3) && ((!%client.isAdmin) || (!%client.isSuperAdmin)))
   {
      messageClient(%client, 'MsgError', '\c2Only admins can change that Server Setting.~wfx/misc/misc.error.wav');
      return;
   } 
   else if((!%client.isSuperAdmin))    // dont let non SuperAdmins change SuperAdmin only practice mode server settings
   {
      messageClient(%client, 'MsgError', '\c2Only SuperAdmins can change that Server Setting.~wfx/misc/misc.error.wav');
      return;
   }
   if(!$MatchStarted)
   {
      messageClient( %client, 'MsgError', '\c2You must wait for the match to start before using practice commands.~wfx/misc/misc.error.wav');
      return;
   }

   %snd = '~wfx/misc/warning_beep.wav';
   %adj = %val == 1 ? 1 : 0; // Convert index to only 1 or 0 to help keep function smaller
   %detail = (%adj ? "On" : "Off");
   %name = %client.name;

   switch$ ( %opt )
   {
      case 1:
         %min = %val+3;
         $TeamDeployableMin[TurretIndoorDeployable] = %min;
         $TeamDeployableMin[TurretOutdoorDeployable] = %min;
         %detail = %min;
         messageAll( 'MsgPracMode', '\c3%5\c2: \"Minimum Turrets\" set to: \c3%4\c2.%1', %snd, %val, %adj, %detail, %name, $CurrentMission );

      case 2:
         if(!$TeamFlag[%val].isHome)
         {
            %game.flagReturn($TeamFlag[%val]);
            messageAll( 'MsgPracMode', '\c3%5\c2: returned the flag.%1', %snd, %val, %adj, %detail, %name, $CurrentMission );
         }
         else
            messageClient( %client, 'MsgPracMode', '\c2Unknown values.', %snd, %val, %adj, %detail, %name, $CurrentMission );

      case 3:
         messageClient( %client, 'MsgPracMode', '\c2Attempting to save deployables to file: %6 %2.', %snd, %val, %adj, %detail, %name, $CurrentMission );
         %game.saveDeployables(%client, %val);

      case 4:
         messageClient( %client, 'MsgPracMode', '\c2Attempting to load deployables from file: %6 %2.', %snd, %val, %adj, %detail, %name, $CurrentMission );
         %game.loadDeployables(%client, %val);

      default:
         messageClient( %client, 'MsgError', '\c2Unknown values.', %snd, %val, %adj, %detail, %name, $CurrentMission );
   }
}

// default to 0
$DeployablesCount = 0;

function PracticeCTFGame::loadDeployables(%game, %client, %val)
{
   if(%client.isSuperAdmin)
   {
      %filename = "prefs/" @ "DepSav" @ $CurrentMission @ %val @ ".cs";
      if(!isFile(%filename))
      {
         messageClient(%client, 'MsgPracMode', '\c2File %6 %2 does not exist!', 0, %val, 0, 0, 0, $CurrentMission );
         return;
      }
      else
      {
         %group = nameToID("MissionCleanup/Deployables");
         %count = %group.getCount();
         for(%i = 0; %i < %count; %i++)
         {
            %obj = %group.getObject(%i);
            %obj.setDamageState(Destroyed);
         }      	
      	      	
         %file = new FileObject();
         if(%file.openForRead(%filename))
         {
            exec(%filename);
            for(%i = 0; %i < $DeployablesCount; %i++)
            {
               %class = $DeployableClassName[%i];
               if(%class $= "TurretData")
                  %className = "Turret";
               else
                  %className = "StaticShape";

               %deplObj = new (%className)() {
                  dataBlock = $DeployableDataBlockName[%i];
               };
               %deplObj.setTransform($DeployableTrans[%i]);
               if(%deplObj.getDatablock().rechargeRate)
                  %deplObj.setRechargeRate(%deplObj.getDatablock().rechargeRate);

               %deplObj.team = $DeployableTeam[%i];
               %deplObj.owner = 0;
               if(%deplObj.getTarget() != -1)
                  setTargetSensorGroup(%deplObj.getTarget(), $DeployableTeam[%i]);

               addToDeployGroup(%deplObj);
               AIDeployObject(0, %deplObj);
               $TeamDeployedCount[$DeployableTeam[%i], $DeployableItem[%i]]++;
               %deplObj.deploy();
             }
         }
         %file.delete();
         messageClient(%client, 'MsgPracMode', '\c2Load file %6 %2 completed.', 0, %val, 0, 0, 0, $CurrentMission );
      }
   }
   else
      messageClient(%client, 'MsgError', '\c2Only SuperAdmins can use this setting.~wfx/misc/misc.error.wav');
}

function PracticeCTFGame::saveDeployables(%game, %client, %val)
{
   if(%client.isSuperAdmin)
   {
      %filename = "prefs/" @ "DepSav" @ $CurrentMission @ %val @ ".cs";

      // Check to see if theres anything to write first
      %group = nameToID("MissionCleanup/Deployables");
      if (%group > 0)
         %depCount = %group.getCount();
      else
         return;

      if(isFile(%filename))
         deleteFile(%filename);

      %file = new fileObject();
      %file.openForWrite(%filename);
      %count = 0;
      for(%i = 0; %i < %depCount; %i++)
      {
         %deplObj = %group.getObject(%i);
         if(isObject(%deplObj))
         {
            %name = %deplObj.getDataBlock().getName();
            if(%name $= "DeployedStationInventory")
               %item = "InventoryDeployable";
            else if(%name $= "DeployedMotionSensor")
               %item = "MotionSensorDeployable";
            else if(%name $= "DeployedPulseSensor")
               %item = "PulseSensorDeployable";
            else if(%name $= "TurretDeployedOutdoor")
               %item = "TurretOutdoorDeployable";
            else if(%name $= "TurretDeployedFloorIndoor" || %name $= "TurretDeployedWallIndoor" || %name $= "TurretDeployedCeilingIndoor")
               %item = "TurretIndoorDeployable";
            else if(%name $= "TurretDeployedCamera")
               %item = "DeployedCamera";
            else
               %item = "";

            %file.writeLine("$DeployableDataBlockName[" @ %count @"] = " @ %name @ ";");
            %file.writeLine("$DeployableClassName[" @ %count @"] = " @ %deplObj.getDataBlock().getClassName() @ ";");
            %file.writeLine("$DeployableItem[" @ %count @"] = " @ %item @ ";");
            %file.writeLine("$DeployableTrans[" @ %count @"] = " @ "\"" @ %deplObj.getTransform() @ "\"" @ ";");
            %file.writeLine("$DeployableTeam[" @ %count @"] = " @ %deplObj.team @ ";");
            %count++;
         }
      }
      %file.writeLine("$DeployablesCount = " @ %count @ ";");
      %file.close();
      %file.delete();
      messageClient(%client, 'MsgPracMode', '\c2Save file %6 %2 completed.', 0, %val, 0, 0, 0, $CurrentMission );
   }
   else
      messageClient(%client, 'MsgError', '\c2Only SuperAdmins can use this setting.~wfx/misc/misc.error.wav');
}

// z0dd - ZOD, 10/02/02. Hack for flag collision bug.
function CTFGame::startFlagCollisionSearch(%game, %flag)
{
   %flag.searchSchedule = %game.schedule(10, "startFlagCollisionSearch", %flag); // SquirrelOfDeath, 10/02/02. Moved from after the while loop
   %pos = %flag.getWorldBoxCenter();
   InitContainerRadiusSearch( %pos, 1.0, $TypeMasks::VehicleObjectType | $TypeMasks::CorpseObjectType | $TypeMasks::PlayerObjectType );
   while((%found = containerSearchNext()) != 0)
   {
      %flag.getDataBlock().onCollision(%flag, %found);
      // SquirrelOfDeath, 10/02/02. Removed break to catch all players possibly intersecting with flag
   }
}
