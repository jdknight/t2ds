//$MissionName is the file name of the mission
//$MapName is the displayed name(no underscore,spaces)
//$GameType (CTF,Hunters)

function DefaultGame::activatePackages(%game)
{
    // activate the default package for the game type
    activatePackage(DefaultGame);
    if (isPackage(%game.class) && %game.class !$= DefaultGame)
        activatePackage(%game.class);
}

function DefaultGame::deactivatePackages(%game)
{
    if (isPackage(%game.class) && %game.class !$= DefaultGame)
        deactivatePackage(%game.class);
    deactivatePackage(DefaultGame);
}

package DefaultGame {

function FlipFlop::objectiveInit(%data, %flipflop)
{
    // add this flipflop to missioncleanup
    %flipflopSet = nameToID("MissionCleanup/FlipFlops");
    if (%flipflopSet <= 0)
    {
        %flipflopSet = new SimSet("FlipFlops");
        MissionCleanup.add(%flipflopSet);
    }
    %flipflopSet.add(%flipflop);

    // see if there's a holo projector associated with this flipflop
    // search the flipflop's folder for a holo projector
    // if one exists, associate it with the flipflop
    %flipflop.projector = 0;
    %folder = %flipflop.getGroup();
    for (%i = 0; %i < %folder.getCount(); %i++)
    {
        %proj = %folder.getObject(%i);
        // weird, but line below prevents console error
        if (%proj.getClassName() !$= "SimGroup" &&
                %proj.getClassName() !$= "InteriorInstance")
            if (%proj.getDatablock().getName() $= "LogoProjector")
            {
                %flipflop.projector = %proj;
                %flipflop.projector.holo = 0;
                break;
            }
    }

    // may have been hidden
    %target = %flipFlop.getTarget();
    if (%target != -1)
    {
        // set flipflop to base skin
        setTargetSkin(%target, $teamSkin[0]);

        // make this always visible in the commander map
        setTargetAlwaysVisMask(%target, 0xffffffff);

        // make this always visible in the commander list
        setTargetRenderMask(%target, getTargetRenderMask(%target) |
            $TargetInfo::CommanderListRender);
    }
}

function FlipFlop::playerTouch(%data, %flipflop, %player)
{
    %client = %player.client;
    %flipTeam = %flipflop.team;

    if (%flipTeam == %client.team)
        return false;

    %teamName = game.getTeamName(%client.team);
    // Let the observers know:
    messageTeam(0, 'MsgClaimFlipFlop',
        '\c2%1 claimed %2 for %3.~wfx/misc/flipflop_taken.wav',
        %client.name, Game.cleanWord(%flipflop.name), %teamName);
    // Let the teammates know:
    messageTeam(%client.team, 'MsgClaimFlipFlop',
        '\c2%1 claimed %2 for %3.~wfx/misc/flipflop_taken.wav',
        %client.name, Game.cleanWord(%flipflop.name), %teamName);
    // Let the other team know:
    %losers = %client.team == 1 ? 2 : 1;
    messageTeam(%losers, 'MsgClaimFlipFlop',
        '\c2%1 claimed %2 for %3.~wfx/packs/shield_hit.wav',
        %client.name, Game.cleanWord(%flipflop.name), %teamName); // z0dd - ZOD, 10/30/02. Change flipflop lost sound

    // change the skin on the switch to claiming team's logo
    setTargetSkin(%flipflop.getTarget(), game.getTeamSkin(%player.team));
    setTargetSensorGroup(%flipflop.getTarget(), %player.team);

    // if there is a "projector" associated with this flipflop, put the claiming
    // team's logo there
    if (%flipflop.projector > 0)
    {
        %projector = %flipflop.projector;
        // axe the old projected holo, if one exists
        if (%projector.holo > 0)
            %projector.holo.delete();

        %newHolo = getTaggedString(game.getTeamSkin(%client.team)) @ "Logo";

        %projTransform = %projector.getTransform();
        // below two functions are from deployables.cs
        %projRot = rotFromTransform(%projTransform);
        %projPos = posFromTransform(%projTransform);
        // place the holo above the projector (default 10 meters)
        %hHeight = %projector.holoHeight;
        if (%hHeight $= "")
            %hHeight = 10;
        %holoZ = getWord(%projPos, 2) + %hHeight;
        %holoPos = firstWord(%projPos) SPC getWord(%projPos,1) SPC %holoZ;

        %holo = new StaticShape()
        {
            rotation = %projRot;
            position = %holoPos;
            dataBlock = %newHolo;
        };
        // dump the hologram into MissionCleanup
        MissionCleanup.add(%holo);
        // associate the holo with the projector
        %projector.holo = %holo;
    }

    // convert the resources associated with the flipflop
    Game.claimFlipflopResources(%flipflop, %client.team);

    if (Game.countFlips())
        for (%i = 1; %i <= Game.numTeams; %i++)
        {
            %teamHeld = Game.countFlipsHeld(%i);
            messageAll('MsgFlipFlopsHeld', "", %i, %teamHeld);
        }

    // call the ai function
    Game.AIplayerCaptureFlipFlop(%player, %flipflop);
    return true;
}

};

//--------- DEFAULT SCORING, SUPERCEDE IN GAMETYPE FILE ------------------

function DefaultGame::initGameVars(%game)
{
    %game.SCORE_PER_SUICIDE = 0;
    %game.SCORE_PER_TEAMKILL = 0;
    %game.SCORE_PER_DEATH = 0;

    %game.SCORE_PER_KILL = 0;

    %game.SCORE_PER_TURRET_KILL = 0;
}

//-- tracking  ---
// .deaths .kills .suicides .teamKills .turretKills

function DefaultGame::claimFlipflopResources(%game, %flipflop, %team)
{
    %group = %flipflop.getGroup();
    %group.setTeam(%team);

    // make this always visible in the commander map (gets reset when sensor
    // group gets changed)
    setTargetAlwaysVisMask(%flipflop.getTarget(), 0xffffffff);
}

//------------------------------------------------------------------------------
function DefaultGame::selectSpawnSphere(%game, %team)
{
    // - walks the objects in the 'teamdrops' group for this team
    // - find a random spawn point which has a running sum less more than
    //   0->total sphere weight

    %teamDropsGroup = "MissionCleanup/TeamDrops" @ %team;

    %group = nameToID(%teamDropsGroup);
    if (%group != -1)
    {
        %count = %group.getCount();
        if (%count != 0)
        {
            // Get total weight of those spheres not filtered by mission types list-
            %overallWeight = 0;
            for (%i = 0; %i < %count; %i++)
            {
                %sphereObj = %group.getObject(%i);
                if (!%sphereObj.isHidden())
                    %overallWeight += %sphereObj.sphereWeight;
            }

            if (%overallWeight > 0)
            {
                // Subtract a little from this as hedge against any rounding
                // offness
                %randSum = getRandom(%overallWeight) - 0.05;

                for (%i = 0; %i < %count; %i++)
                {
                    %sphereObj = %group.getObject(%i);
                    if (!%sphereObj.isHidden())
                    {
                        %randSum -= %sphereObj.sphereWeight;
                        if (%randSum <= 0)
                        {
                            return %group.getObject(%i); // Found our sphere
                        }
                    }
                }

                error("Random spawn sphere selection didn't work");
            }
            else
                error("No non-hidden spawnspheres were found in " @ %teamDropsGroup);
        }
        else
            error("No spawnspheres found in " @ %teamDropsGroup);
    }
    else
        error(%teamDropsGroup @ " not found in selectSpawnSphere().");

    return -1;
}

function DefaultGame::selectSpawnZone(%game, %sphere)
{
    // determines if this should spawn inside or outside
    %overallWeight = %sphere.indoorWeight + %sphere.outdoorWeight;
    %index = mFloor(getRandom() * (%overallWeight - 0.1)) + 1;
    if ((%index - %sphere.indoorWeight) > 0)
        return false; //do not pick an indoor spawn
    else
        return true;  //pick an indoor spawn
}

function DefaultGame::selectSpawnFacing(%game, %src, %target, %zone)
{
    // this used only when spawn loc is not on an interior. This points
    // spawning player to the ctr of spawnshpere
    %target = setWord(%target, 2, 0);
    %src = setWord(%src, 2, 0);

    if (VectorDist(%target, %src) == 0)
        return " 0 0 1 0  ";
    %vec = VectorNormalize(VectorSub(%target, %src));
    %angle = mAcos(getWord(%vec, 1));

    if (%src < %target)
        return (" 0 0 1 " @ %angle);
    else
        return (" 0 0 1 " @ -%angle);
}

function DefaultGame::pickTeamSpawn(%game, %team)
{
    // early exit if no nav graph
    if (!navGraphExists())
    {
        error("no navigation graph is present (" @ $currentMission @ ")");
        return -1;
    }

    for (%attempt = 0; %attempt < 20; %attempt++)
    {
        // finds a random spawn sphere
        // selects inside/outside on this random sphere
        // if the navgraph exists, then uses it to grab a random node as spawn
        // location/rotation
        %sphere = %game.selectSpawnSphere(%team);
        if (%sphere == -1)
        {
            error("no spawn spheres found for team " @ %team);
            return -1;
        }

        %zone = %game.selectSpawnZone(%sphere);
        %useIndoor = %zone;
        %useOutdoor = !%zone;
        if (%zone)
            %area = "indoor";
        else
            %area = "outdoor";

        %radius = %sphere.radius;
        %sphereTrans = %sphere.getTransform();
        // don't need full transform here, just x, y, z
        %sphereCtr = getWord(%sphereTrans, 0) @ " " @ getWord(%sphereTrans, 1) @
            " " @ getWord(%sphereTrans, 2);

        %avoidThese = $TypeMasks::VehicleObjectType |
            $TypeMasks::MoveableObjectType |
            $TypeMasks::PlayerObjectType |
            $TypeMasks::TurretObjectType;

        for (%tries = 0; %tries < 10; %tries++)
        {
            %nodeIndex = navGraph.randNode(
                %sphereCtr, %radius, %useIndoor, %useOutdoor);
            if (%nodeIndex >= 0)
            {
                %loc = navGraph.randNodeLoc(%nodeIndex);
                %adjUp = VectorAdd(%loc, "0 0 1.0"); // don't go much below

                if (ContainerBoxEmpty(%avoidThese, %adjUp, 2.0))
                    break;
            }
        }

        if (%nodeIndex >= 0)
        {
            %loc = navGraph.randNodeLoc(%nodeIndex);
            if (%zone)
            {
                %trns = %loc @ " 0 0 1 0";
                %spawnLoc = whereToLook(%trns);
            }
            else
            {
                %rot = %game.selectSpawnFacing(%loc, %sphereCtr, %zone);
                %spawnLoc = %loc @ %rot;
            }

            return %spawnLoc;
        }
    }
}

//------------------------------------------------------------

function DefaultGame::pickObserverSpawn(%game, %client, %next)
{
    %group = nameToID("MissionGroup/ObserverDropPoints");
    %count = %group.getCount();

    if (!%count || %group == -1)
    {
        error("no observer spawn points found");
        return -1;
    }

    if (%client.lastObserverSpawn == -1)
    {
        %client.lastObserverSpawn = 0;
        return %group.getObject(%client.lastObserverSpawn);
    }

    if (%next == true)
        %spawnIdx = %client.lastObserverSpawn + 1;
    else
        %spawnIdx = %client.lastObserverSpawn - 1;

    if (%spawnIdx < 0)
        %spawnIdx = %count - 1;
    else if (%spawnIdx >= %count)
        %spawnIdx = 0;

    %client.lastObserverSpawn = %spawnIdx;
    return %group.getObject(%spawnIdx);
}

//------------------------------------------------------------
function DefaultGame::spawnPlayer(%game, %client, %respawn)
{
    %client.lastSpawnPoint = %game.pickPlayerSpawn(%client, false);
    %client.suicidePickRespawnTime = getSimTime() + 20000;
    %game.createPlayer(%client, %client.lastSpawnPoint, %respawn);
}

//------------------------------------------------------------
function DefaultGame::playerSpawned(%game, %player)
{
    if (%player.client.respawnTimer)
        cancel(%player.client.respawnTimer);

    %player.client.observerStartTime = "";
    %game.equip(%player);

    // set the spawn time (for use by the AI system)
    %player.client.spawnTime = getSimTime();

    // jff: this should probably be checking the team of the client
    // update anyone observing this client
    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %cl = ClientGroup.getObject(%i);
        if (%cl.camera.mode $= "observerFollow" &&
                %cl.observeClient == %player.client)
        {
            %transform = %player.getTransform();
            %cl.camera.setOrbitMode(%player, %transform, 0.5, 4.5, 4.5);
            %cl.camera.targetObj = %player;
        }
    }
}

function DefaultGame::equip(%game, %player)
{
    for (%i =0; %i<$InventoryHudCount; %i++)
        %player.client.setInventoryHudItem(
            $InventoryHudData[%i, itemDataName], 0, 1);
    %player.client.clearBackpackIcon();

    //%player.setArmor("Light");
    %player.setInventory(RepairKit, 1);
    %player.setInventory(Grenade, 6);
    %player.setInventory(Blaster, 1);
    %player.setInventory(Disc, 1);
    %player.setInventory(Chaingun, 1);
    %player.setInventory(ChaingunAmmo, 100);
    %player.setInventory(DiscAmmo, 20);
    %player.setInventory(Beacon, 3);
    %player.setInventory(TargetingLaser, 1);
    %player.weaponCount = 3;

    %player.use("Blaster");
}

//------------------------------------------------------------
function DefaultGame::pickPlayerSpawn(%game, %client, %respawn)
{
    // place this client on his own team, '%respawn' does not ever seem to be
    // used we no longer care whether it is a respawn since all spawns use same
    // points.
    return %game.pickTeamSpawn(%client.team);
}

//------------------------------------------------------------
function DefaultGame::createPlayer(%game, %client, %spawnLoc, %respawn)
{
    // do not allow a new player if there is one (not destroyed) on this client
    if (isObject(%client.player) && %client.player.getState() !$= "Dead")
        return;

    // clients and cameras can exist in team 0, but players should not
    if (%client.team == 0)
        error("Players should not be added to team0!");

    // defaultplayerarmor is in 'players.cs'
    if (%spawnLoc == -1)
        %spawnLoc = "0 0 300 1 0 0 0";

    // copied from player.cs
    if (%client.race $= "Bioderm")
        // Only have male bioderms.
        %armor = $DefaultPlayerArmor @ "Male" @ %client.race @ Armor;
    else
        %armor = $DefaultPlayerArmor @ %client.sex @ %client.race @ Armor;
    %client.armor = $DefaultPlayerArmor;

    %player = new Player()
    {
        //dataBlock = $DefaultPlayerArmor;
        dataBlock = %armor;
    };

    if (%respawn)
    {
        %player.setInvincible(true);
        //%player.setCloaked(true); // z0dd - ZOD, 8/6/02. Don't spawn players cloaked
        %player.setInvincibleMode($InvincibleTime,0.02);
        //%player.respawnCloakThread = %player.schedule($InvincibleTime * 1000, "setRespawnCloakOff"); // z0dd - ZOD, 8/6/02. Don't spawn players cloaked
        %player.schedule($InvincibleTime * 1000, "setInvincible", false);
    }

    %player.setTransform(%spawnLoc);
    MissionCleanup.add(%player);

    // setup some info
    %player.setOwnerClient(%client);
    %player.team = %client.team;
    %client.outOfBounds = false;
    %player.setEnergyLevel(60);
    %client.player = %player;

    // updates client's target info for this player
    %player.setTarget(%client.target);
    setTargetDataBlock(%client.target, %player.getDatablock());
    setTargetSensorData(%client.target, PlayerSensor);
    setTargetSensorGroup(%client.target, %client.team);
    %client.setSensorGroup(%client.team);

    // make sure the player has been added to the team rank array...
    %game.populateTeamRankArray(%client);

    %game.playerSpawned(%client.player);
}

function Player::setRespawnCloakOff(%player)
{
    %player.setCloaked(false);
    %player.respawnCloakThread = "";
}

//------------------------------------------------------------

function DefaultGame::startMatch(%game)
{
    echo("START MATCH");
    messageAll('MsgMissionStart', "\c2Match started!");

    // the match has been started, clear the team rank array, and
    // repopulate it...
    for (%i = 0; %i < 32; %i++)
        %game.clearTeamRankArray(%i);

    // used in BountyGame, prolly in a few others as well...
    $matchStarted = true;

    %game.clearDeployableMaxes();

    $missionStartTime = getSimTime();
    %curTimeLeftMS = ($Host::TimeLimit * 60 * 1000);

    // schedule first timeLimit check for 20 seconds
    if (%game.class !$= "SiegeGame")
        %game.timeCheck = %game.schedule(20000, "checkTimeLimit");

    // schedule the end of match countdown
    EndCountdown($Host::TimeLimit * 60 * 1000);

    // reset everyone's score and add them to the team rank array
    for (%i = 0; %i < ClientGroup.getCount(); %i++)
    {
        %cl = ClientGroup.getObject(%i);
        %game.resetScore(%cl);
        %game.populateTeamRankArray(%cl);
    }

    // set all clients control to their player
    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %cl = ClientGroup.getObject(%i);

        // Siege game will set the clock differently
        if (%game.class !$= "SiegeGame")
            messageClient(%cl, 'MsgSystemClock', "", $Host::TimeLimit, %curTimeLeftMS);

        if (!$Host::TournamentMode && %cl.matchStartReady && %cl.camera.mode $= "pre-game")
        {
            commandToClient(%cl, 'setHudMode', 'Standard');
            %cl.setControlObject(%cl.player);
        }
        else
        {
            if (%cl.matchStartReady)
            {
                if (%cl.camera.mode $= "pre-game")
                {
                    %cl.observerMode = "";
                    commandToClient(%cl, 'setHudMode', 'Standard');

                    if (isObject(%cl.player))
                        %cl.setControlObject(%cl.player);
                }
                else
                    %cl.observerMode = "observerFly";
            }
        }
    }

    // on with the show this is it!
    AISystemEnabled(true);
}

function DefaultGame::gameOver(%game)
{
    // set the bool
    $missionRunning = false;

    CancelCountdown();
    CancelEndCountdown();

    // loop through all the clients, and do any cleanup...
    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %client = ClientGroup.getObject(%i);
        %player = %client.player;
        %client.lastTeam = %client.team;

        // z0dd - ZOD, 6/13/02. Need to remove this for random teams by Founder (founder@mechina.com).
        if ($CurrentMissionType $= TR2) // z0dd - ZOD, 9/17/02. Check for Team Rabbit 2
            %client.lastTeam = %client.team;

        if (!%client.isAiControlled())
        {
            %client.endMission();
            messageClient(%client, 'MsgClearDebrief', "");
            %game.sendDebriefing(%client);
            if (%client.player.isBomber)
                commandToClient(%client, 'endBomberSight');

            // clear the score hud...
            messageClient(%client, 'SetScoreHudHeader', "", "");
            messageClient(%client, 'SetScoreHudSubheader', "", "");
            messageClient(%client, 'ClearHud', "", 'scoreScreen', 0);

            // clean up the players' HUDs:
            %client.setWeaponsHudClearAll();
            %client.setInventoryHudClearAll();
        }
    }

    // z0dd - ZOD, 6/22/02. Setup random teams by Founder (founder@mechina.com).
    if ($CurrentMissionType !$= TR2) // z0dd - ZOD, 9/17/02. Check for Team Rabbit 2
        %game.setupClientTeams();

    // Default game does nothing...  except lets the AI know the mission is over
    AIMissionEnd();
}

//------------------------------------------------------------------------------
function DefaultGame::sendDebriefing(%game, %client)
{
   if (%game.numTeams == 1)
   {
      // Mission result:
      %winner = $TeamRank[0, 0];
      if (%winner.score > 0)
         messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 wins!', $TeamRank[0, 0].name);
      else
         messageClient(%client, 'MsgDebriefResult', "", '<just:center>Nobody wins.');

      // Player scores:
      %count = $TeamRank[0, count];
      messageClient(%client, 'MsgDebriefAddLine', "", '<spush><color:00dc00><font:univers condensed:18>PLAYER<lmargin%%:60>SCORE<lmargin%%:80>KILLS<spop>');
      for (%i = 0; %i < %count; %i++)
      {
         %cl = $TeamRank[0, %i];
         if (%cl.score $= "")
            %score = 0;
         else
            %score = %cl.score;
         if (%cl.kills $= "")
            %kills = 0;
         else
            %kills = %cl.kills;
         messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:60> %1</clip><lmargin%%:60><clip%%:20> %2</clip><lmargin%%:80><clip%%:20> %3', %cl.name, %score, %kills);
      }
   }
   else
   {
      %topScore = "";
      %topCount = 0;
      for (%team = 1; %team <= %game.numTeams; %team++)
      {
         if (%topScore $= "" || $TeamScore[%team] > %topScore)
         {
            %topScore = $TeamScore[%team];
            %firstTeam = %team;
            %topCount = 1;
         }
         else if ($TeamScore[%team] == %topScore)
         {
            %secondTeam = %team;
            %topCount++;
         }
      }

      // Mission result:
      if (%topCount == 1)
         messageClient(%client, 'MsgDebriefResult', "", '<just:center>Team %1 wins!', %game.getTeamName(%firstTeam));
      else if (%topCount == 2)
         messageClient(%client, 'MsgDebriefResult', "", '<just:center>Team %1 and Team %2 tie!', %game.getTeamName(%firstTeam), %game.getTeamName(%secondTeam));
      else
         messageClient(%client, 'MsgDebriefResult', "", '<just:center>The mission ended in a tie.');

      // Team scores:
      messageClient(%client, 'MsgDebriefAddLine', "", '<spush><color:00dc00><font:univers condensed:18>TEAM<lmargin%%:60>SCORE<spop>');
      for (%team = 1; %team - 1 < %game.numTeams; %team++)
      {
         if ($TeamScore[%team] $= "")
            %score = 0;
         else
            %score = $TeamScore[%team];
         messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:60> %1</clip><lmargin%%:60><clip%%:40> %2</clip>', %game.getTeamName(%team), %score);
      }

      // Player scores:
      messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:00dc00><font:univers condensed:18>PLAYER<lmargin%%:40>TEAM<lmargin%%:70>SCORE<lmargin%%:87>KILLS<spop>');
      for (%team = 1; %team - 1 < %game.numTeams; %team++)
         %count[%team] = 0;

      %notDone = true;
      while (%notDone)
      {
         // Get the highest remaining score:
         %highScore = "";
         for (%team = 1; %team <= %game.numTeams; %team++)
         {
            if (%count[%team] < $TeamRank[%team, count] && (%highScore $= "" || $TeamRank[%team, %count[%team]].score > %highScore))
            {
               %highScore = $TeamRank[%team, %count[%team]].score;
               %highTeam = %team;
            }
         }

         // Send the debrief line:
         %cl = $TeamRank[%highTeam, %count[%highTeam]];
         %score = %cl.score $= "" ? 0 : %cl.score;
         %kills = %cl.kills $= "" ? 0 : %cl.kills;
         messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:40> %1</clip><lmargin%%:40><clip%%:30> %2</clip><lmargin%%:70><clip%%:17> %3</clip><lmargin%%:87><clip%%:13> %4</clip>', %cl.name, %game.getTeamName(%cl.team), %score, %kills);

         %count[%highTeam]++;
         %notDone = false;
         for (%team = 1; %team - 1 < %game.numTeams; %team++)
         {
            if (%count[%team] < $TeamRank[%team, count])
            {
               %notDone = true;
               break;
            }
         }
      }
   }

   // now go through an list all the observers:
   %count = ClientGroup.getCount();
   %printedHeader = false;
   for (%i = 0; %i < %count; %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if (%cl.team <= 0)
      {
         // print the header only if we actually find an observer
         if (!%printedHeader)
         {
            %printedHeader = true;
            messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:00dc00><font:univers condensed:18>OBSERVERS<lmargin%%:60>SCORE<spop>');
         }

         //print out the client
         %score = %cl.score $= "" ? 0 : %cl.score;
         messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:60> %1</clip><lmargin%%:60><clip%%:40> %2</clip>', %cl.name, %score);
      }
   }
}

//------------------------------------------------------------
function DefaultGame::clearDeployableMaxes(%game)
{
   for (%i = 0; %i <= %game.numTeams; %i++)
   {
      $TeamDeployedCount[%i, TurretIndoorDeployable] = 0;
      $TeamDeployedCount[%i, TurretOutdoorDeployable] = 0;
      $TeamDeployedCount[%i, PulseSensorDeployable] = 0;
      $TeamDeployedCount[%i, MotionSensorDeployable] = 0;
      $TeamDeployedCount[%i, InventoryDeployable] = 0;
      $TeamDeployedCount[%i, DeployedCamera] = 0;
      $TeamDeployedCount[%i, MineDeployed] = 0;
      $TeamDeployedCount[%i, TargetBeacon] = 0;
      $TeamDeployedCount[%i, MarkerBeacon] = 0;
   }
}

// called from player scripts
function DefaultGame::onClientDamaged(%game, %clVictim, %clAttacker, %damageType, %sourceObject)
{
    //set the vars if it was a turret
    if (isObject(%sourceObject))
    {
        %sourceClassType = %sourceObject.getDataBlock().getClassName();
        %sourceType = %sourceObject.getDataBlock().getName();
    }

    if (%sourceClassType $= "TurretData")
    {
        // jff: are there special turret types which makes this needed?
        // tinman:  yes, we don't want bots stopping to fire on the big outdoor turrets, which they
        // will just get mowed down.  deployables only.
        if (%sourceType $= "TurretDeployedFloorIndoor" ||
                %sourceType $= "TurretDeployedWallIndoor" ||
                %sourceType $= "TurretDeployedCeilingIndoor" ||
                %sourceType $= "TurretDeployedOutdoor")
        {
            %clVictim.lastDamageTurretTime = getSimTime();
            %clVictim.lastDamageTurret = %sourceObject;
        }

        %turretAttacker = %sourceObject.getControllingClient();

        //-------------------------------------------------------------------
        // z0dd - ZOD, 5/29/02. Play a sound to client when they hit a player
        if (%turretAttacker)
        {
            %client = %turretAttacker;
        }
        //-------------------------------------------------------------------

        // should get a damage message from friendly fire turrets also
        if (%turretAttacker && %turretAttacker != %clVictim &&
                %turretAttacker.team == %clVictim.team)
        {
            // is a team game & player just damaged a teammate
            if (%game.numTeams > 1 &&
                    %turretAttacker.player.causedRecentDamage != %clVictim.player)
            {
                %turretAttacker.player.causedRecentDamage = %clVictim.player;
                //allow friendly fire message every x ms
                %turretAttacker.player.schedule(1000, "causedRecentDamage", "");
                %game.friendlyFireMessage(%clVictim, %turretAttacker);
            }
        }
    }
    else if (%sourceClassType $= "PlayerData")
    {
        %client = %clAttacker; // z0dd - ZOD, 5/29/02. Play a sound to client when they hit a player

        // now see if both were on the same team
        if (%clAttacker && %clAttacker != %clVictim && %clVictim.team == %clAttacker.team)
        {
            // is a team game & player just damaged a teammate
            if (%game.numTeams > 1 && %clAttacker.player.causedRecentDamage != %clVictim.player)
            {
                %clAttacker.player.causedRecentDamage = %clVictim.player;
                // allow friendly fire message every x ms
                %clAttacker.player.schedule(1000, "causedRecentDamage", "");
                %game.friendlyFireMessage(%clVictim, %clAttacker);
            }
        }

        if (%clAttacker && %clAttacker != %clVictim)
        {
            %clVictim.lastDamageTime = getSimTime();
            %clVictim.lastDamageClient = %clAttacker;
            if (%clVictim.isAIControlled())
                %clVictim.clientDetected(%clAttacker);
        }
    }
    // ------------------------------------------------------------------
    // z0dd - ZOD, 5/29/02. Play a sound to client when they hit a player
    else if (%sourceClassType $= "WheeledVehicleData" ||
            %sourceClassType $= "FlyingVehicleData" ||
            %sourceClassType $= "HoverVehicleData")
    {
        if (%sourceObject.getControllingClient())
        {
            %client = %sourceObject.getControllingClient();
        }
    }

    if (%client && %client.playerHitSound && $CurrentMissionType !$= TR2)
    {
        // 1)  Blaster
        // 2)  Plasma Gun
        // 3)  Chaingun
        // 4)  Disc
        // 5)  Grenades (GL and hand)
        // 6)  Laser
        // 8)  Mortar
        // 9)  Missile
        // 10) ShockLance

        // 13) Impact (object to object)

        // 16) Plasma Turret
        // 17) AA Turret
        // 18) ELF Turret
        // 19) Mortar Turret
        // 20) Missile Turret
        // 21) Indoor Deployable Turret
        // 22) Outdoor Deployable Turret
        // 23) Sentry Turret

        // 26) Shrike Blaster
        // 27) Bobmer Plasma
        // 28) Bomber Bomb
        // 29) Tank Chaingun
        // 30) Tank Mortar
        // 31) Satchel
        if (%client.team != %clVictim.team)
        {
            if ((%damageType > 0  && %damageType < 11)     ||
                    (%damageType == 13)                    ||
                    (%damageType > 15 && %damageType < 24) ||
                    (%damageType > 25 && %damageType < 32))
            {
                messageClient(%client, 'MsgClientHit', %client.playerHitWav);
            }
        }
    }
    // ------------------------------------------------------------------

    // call the game specific AI routines...
    if (isObject(%clVictim) && %clVictim.isAIControlled())
        %game.onAIDamaged(%clVictim, %clAttacker, %damageType, %sourceObject);
    if (isObject(%clAttacker) && %clAttacker.isAIControlled())
        %game.onAIFriendlyFire(%clVictim, %clAttacker, %damageType, %sourceObject);
}

function DefaultGame::friendlyFireMessage(%game, %damaged, %damager)
{
    messageClient(%damaged, 'MsgDamagedByTeam',
        '\c1You were harmed by teammate %1', %damager.name);
    messageClient(%damager, 'MsgDamagedTeam',
        '\c1You just harmed teammate %1.', %damaged.name);
}

function DefaultGame::clearWaitRespawn(%game, %client)
{
    %client.waitRespawn = 0;
}

// called from player scripts
function DefaultGame::onClientKilled(%game, %clVictim, %clKiller, %damageType,
        %implement, %damageLocation)
{
    %plVictim = %clVictim.player;
    %plKiller = %clKiller.player;
    %clVictim.plyrPointOfDeath = %plVictim.position;
    %clVictim.plyrDiedHoldingFlag = %plVictim.holdingFlag;
    %clVictim.waitRespawn = 1;

    cancel(%obj.reloadSchedule); // z0dd - ZOD, 9/3/02. Cancel rapid wpn fire fix scheduler
    cancel(%plVictim.reCloak);
    cancel(%clVictim.respawnTimer);
    %clVictim.respawnTimer = %game.schedule(
        ($Host::PlayerRespawnTimeout * 1000), "forceObserver",
        %clVictim, "spawnTimeout");

    // reset the alarm for out of bounds
    if (%clVictim.outOfBounds)
        messageClient(%clVictim, 'EnterMissionArea', "");

    if (%damageType == $DamageType::suicide)
        %respawnDelay = 2; // z0dd - ZOD, 4/24/02. Was 10
    else
        %respawnDelay = 2;

    %game.schedule(%respawnDelay * 1000, "clearWaitRespawn", %clVictim);

    // if victim had an undetonated satchel charge pack, get rid of it
    if (%plVictim.thrownChargeId != 0)
        if (!%plVictim.thrownChargeId.kaboom)
            %plVictim.thrownChargeId.delete();

    if (%plVictim.lastVehicle !$= "")
    {
        schedule(15000, %plVictim.lastVehicle, "vehicleAbandonTimeOut",
            %plVictim.lastVehicle);
        %plVictim.lastVehicle.lastPilot = "";
    }

    // unmount pilot or remove sight from bomber
    if (%plVictim.isMounted())
    {
        if (%plVictim.vehicleTurret)
            %plVictim.vehicleTurret.getDataBlock().playerDismount(
                %plVictim.vehicleTurret);
        else
        {
            %plVictim.getDataBlock().doDismount(%plVictim, true);
            %plVictim.mountVehicle = false;
        }
    }

    if (%plVictim.inStation)
        commandToClient(%plVictim.client, 'setStationKeys', false);
    %clVictim.camera.mode = "playerDeath";

    // reset who triggered this station and cancel outstanding armor switch
    // thread
    if (%plVictim.station)
    {
        %plVictim.station.triggeredBy = "";
        %plVictim.station.getDataBlock().stationTriggered(%plVictim.station,0);
        if (%plVictim.armorSwitchSchedule)
            cancel(%plVictim.armorSwitchSchedule);
    }

    // Close huds if player dies...
    messageClient(%clVictim, 'CloseHud', "", 'inventoryScreen');
    messageClient(%clVictim, 'CloseHud', "", 'vehicleHud');
    commandToClient(%clVictim, 'setHudMode', 'Standard', "", 0);

    // $weaponslot from item.cs
    %plVictim.setRepairRate(0);
    %plVictim.setImageTrigger($WeaponSlot, false);

    playDeathAnimation(%plVictim, %damageLocation, %damageType);
    playDeathCry(%plVictim);

    %victimName = %clVictim.name;

    %game.displayDeathMessages(%clVictim, %clKiller, %damageType, %implement);
    %game.updateKillScores(%clVictim, %clKiller, %damageType, %implement);

    // toss whatever is being carried, '$flagslot' from item.cs
    // MES - had to move this to after death message display because of Rabbit
    // game type
    for (%index = 0 ; %index < 8; %index++)
    {
        %image = %plVictim.getMountedImage(%index);
        if (%image)
        {
            if (%index == $FlagSlot)
                %plVictim.throwObject(%plVictim.holdingFlag);
            else
                %plVictim.throw(%image.item);
        }
    }

    // target manager update
    setTargetDataBlock(%clVictim.target, 0);
    setTargetSensorData(%clVictim.target, 0);

    // clear the hud
    %clVictim.SetWeaponsHudClearAll();
    %clVictim.SetInventoryHudClearAll();
    %clVictim.setAmmoHudCount(-1);

    // clear out weapons, inventory and pack huds
    // - make sure the deploy hud gets shut off
    // - clear the pack icon
    messageClient(%clVictim, 'msgDeploySensorOff', "");
    messageClient(%clVictim, 'msgPackIconOff', "");

    //clear the deployable HUD
    %plVictim.client.deployPack = false;
    cancel(%plVictim.deployCheckThread);
    deactivateDeploySensor(%plVictim);

    //if the killer was an AI...
    if (isObject(%clKiller) && %clKiller.isAIControlled())
        %game.onAIKilledClient(%clVictim, %clKiller, %damageType, %implement);

    // reset control object on this player: also sets 'playgui' as content
    serverCmdResetControlObject(%clVictim);

    // set control object to the camera
    %clVictim.player = 0;
    %transform = %plVictim.getTransform();

    // note, AI's don't have a camera...
    if (isObject(%clVictim.camera))
    {
        %clVictim.camera.setTransform(%transform);
        %clVictim.camera.setOrbitMode(%plVictim, %plVictim.getTransform(), 0.5, 4.5, 4.5);
        %clVictim.setControlObject(%clVictim.camera);
    }

    // hook in the AI specific code for when a client dies
    if (%clVictim.isAIControlled())
    {
        aiReleaseHumanControl(%clVictim.controlByHuman, %clVictim);
        %game.onAIKilled(%clVictim, %clKiller, %damageType, %implement);
    }
    else
        aiReleaseHumanControl(%clVictim, %clVictim.controlAI);

    // used to track corpses so the AI can get ammo, etc...
    AICorpseAdded(%plVictim);

    // if the death was a suicide, prevent respawning for 5 seconds...
    %clVictim.lastDeathSuicide = false;
    if (%damageType == $DamageType::Suicide)
    {
        %clVictim.lastDeathSuicide = true;
        %clVictim.suicideRespawnTime = getSimTime() + 1000; // z0dd - ZOD, 4/24/02. Was 5000
    }
}

function DefaultGame::forceObserver(%game, %client, %reason)
{
    // make sure we have a valid client...
    if (%client <= 0)
        return;

    // first kill this player
    if (%client.player)
        %client.player.scriptKill(0);

    if (%client.respawnTimer)
        cancel(%client.respawnTimer);

    %client.respawnTimer = "";

    // remove them from the team rank array
    %game.removeFromTeamRankArray(%client);

    // place them in observer mode
    %client.lastObserverSpawn = -1;
    %client.observerStartTime = getSimTime();
    %adminForce = 0;

    switch$ (%reason)
    {
    case "playerChoose":
        %client.camera.getDataBlock().setMode(%client.camera, "observerFly");
        messageClient(%client, 'MsgClientJoinTeam',
            '\c2You have become an observer.',
            %client.name, %game.getTeamName(0), %client, 0);
        %client.lastTeam = %client.team;

    case "AdminForce":
        %client.camera.getDataBlock().setMode(%client.camera, "observerFly");
        messageClient(%client, 'MsgClientJoinTeam',
            '\c2You have been forced into observer mode by the admin.',
            %client.name, %game.getTeamName(0), %client, 0);
        %client.lastTeam = %client.team;
        %adminForce = 1;

        if ($Host::TournamentMode)
        {
            if (!$matchStarted)
            {
                if (%client.camera.Mode $= "pickingTeam")
                {
                    commandToClient(%client, 'processPickTeam');
                    clearBottomPrint(%client);
                }
                else
                {
                    clearCenterPrint(%client);
                    %client.notReady = true;
                }
            }
        }

    case "spawnTimeout":
        %client.camera.getDataBlock().setMode(%client.camera, "observerTimeout");
        messageClient(%client, 'MsgClientJoinTeam',
            '\c2You have been placed in observer mode due to delay in respawning.',
            %client.name, %game.getTeamName(0), %client, 0);
        // save the team the player was on - only if this was a delay in respawning
        %client.lastTeam = %client.team;
    }

    // switch client to team 0 (observer)
    %client.team = 0;
    %client.player.team = 0;
    setTargetSensorGroup(%client.target, %client.team);
    %client.setSensorGroup(%client.team);

    // set their control to the obs. cam
    %client.setControlObject(%client.camera);
    commandToClient(%client, 'setHudMode', 'Observer');

    // display the hud
    //displayObserverHud(%client, 0);
    updateObserverFlyHud(%client);

    // message everyone about this event
    if (!%adminForce)
        messageAllExcept(%client, -1, 'MsgClientJoinTeam',
            '\c2%1 has become an observer.',
            %client.name, %game.getTeamName(0), %client, 0);
    else
        messageAllExcept(%client, -1, 'MsgClientJoinTeam',
            '\c2The admin has forced %1 to become an observer.',
            %client.name, %game.getTeamName(0), %client, 0);

    updateCanListenState(%client);

    // call the onEvent for this game type
    // - Bounty uses this to remove this client from others' hit lists
    %game.onClientEnterObserverMode(%client);
}

function DefaultGame::displayDeathMessages(%game, %clVictim, %clKiller,
        %damageType, %implement)
{
    // ----------------------------------------------------------------------------------
    // z0dd - ZOD, 6/18/02. From Panama Jack, send the damageTypeText as the last variable
    // in each death message so client knows what weapon it was that killed them.

    %victimGender = (%clVictim.sex $= "Male" ? 'him' : 'her');
    %victimPoss = (%clVictim.sex $= "Male" ? 'his' : 'her');
    %killerGender = (%clKiller.sex $= "Male" ? 'him' : 'her');
    %killerPoss = (%clKiller.sex $= "Male" ? 'his' : 'her');
    %victimName = %clVictim.name;
    %killerName = %clKiller.name;
    //error("DamageType = " @ %damageType @ ", implement = " @ %implement @ ", implement class = " @ %implement.getClassName() @ ", is controlled = " @ %implement.getControllingClient());

    if (%damageType == $DamageType::Explosion)
    {
        messageAll('msgExplosionKill',
            $DeathMessageExplosion[mFloor(getRandom() * $DeathMessageExplosionCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    // player presses ctrl-k
    else if (%damageType == $DamageType::Suicide)
    {
        messageAll('msgSuicide',
            $DeathMessageSuicide[mFloor(getRandom() * $DeathMessageSuicideCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    else if (%damageType == $DamageType::VehicleSpawn)
    {
        messageAll('msgVehicleSpawnKill',
            $DeathMessageVehPad[mFloor(getRandom() * $DeathMessageVehPadCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    else if (%damageType == $DamageType::ForceFieldPowerup)
    {
        messageAll('msgVehicleSpawnKill',
            $DeathMessageFFPowerup[mFloor(getRandom() * $DeathMessageFFPowerupCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    else if (%damageType == $DamageType::Crash)
    {
        messageAll('msgVehicleCrash',
            $DeathMessageVehicleCrash[%damageType,
                mFloor(getRandom() * $DeathMessageVehicleCrashCount)],
                %victimName, %victimGender, %victimPoss,
                %killerName, %killerGender, %killerPoss,
                %damageType, $DamageTypeText[%damageType]);
    }
    // run down by vehicle
    else if (%damageType == $DamageType::Impact)
    {
        if ((%controller = %implement.getControllingClient()) > 0)
        {
            %killerGender = (%controller.sex $= "Male" ? 'him' : 'her');
            %killerPoss = (%controller.sex $= "Male" ? 'his' : 'her');
            %killerName = %controller.name;
            messageAll('msgVehicleKill',
                $DeathMessageVehicle[mFloor(getRandom() * $DeathMessageVehicleCount)],
                %victimName, %victimGender, %victimPoss,
                %killerName, %killerGender, %killerPoss,
                %damageType, $DamageTypeText[%damageType]);
        }
        else
        {
            messageAll('msgVehicleKill',
                $DeathMessageVehicleUnmanned[mFloor(getRandom() * $DeathMessageVehicleUnmannedCount)],
                %victimName, %victimGender, %victimPoss,
                %killerName, %killerGender, %killerPoss,
                %damageType, $DamageTypeText[%damageType]);
        }
    }
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    // z0dd - ZOD, 5/15/02. Added Hover Vehicle so we get proper
    // death messages when killed with Wildcat chaingun
    //else if (isObject(%implement) && (%implement.getClassName() $= "Turret" || %implement.getClassName() $= "VehicleTurret" || %implement.getClassName() $= "FlyingVehicle"))   //player killed by a turret
    else if (isObject(%implement) && (%implement.getClassName() $= "Turret" ||
            %implement.getClassName() $= "VehicleTurret" ||
            %implement.getClassName() $= "FlyingVehicle" ||
            %implement.getClassName() $= "HoverVehicle"))
    {
        // is turret being controlled?
        if (%implement.getControllingClient() != 0)
        {
            %controller = %implement.getControllingClient();
            %killerGender = (%controller.sex $= "Male" ? 'him' : 'her');
            %killerPoss = (%controller.sex $= "Male" ? 'his' : 'her');
            %killerName = %controller.name;

            if (%controller == %clVictim)
                messageAll('msgTurretSelfKill',
                    $DeathMessageTurretSelfKill[mFloor(getRandom() * $DeathMessageTurretSelfKillCount)],
                    %victimName, %victimGender, %victimPoss,
                    %killerName, %killerGender, %killerPoss,
                    %damageType, $DamageTypeText[%damageType]);
            // controller TK'd a friendly
            else if (%controller.team == %clVictim.team)
                messageAll('msgCTurretKill',
                    $DeathMessageCTurretTeamKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretTeamKillCount)],
                    %victimName, %victimGender, %victimPoss,
                    %killerName, %killerGender, %killerPoss,
                    %damageType, $DamageTypeText[%damageType]);
            // controller killed an enemy
            else
                messageAll('msgCTurretKill',
                    $DeathMessageCTurretKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretKillCount)],
                    %victimName, %victimGender, %victimPoss,
                    %killerName, %killerGender, %killerPoss,
                    %damageType, $DamageTypeText[%damageType]);
        }
        // use the handle associated with the deployed object to verify
        // valid owner
        else if (isObject(%implement.owner))
        {
            %owner = %implement.owner;
            // turret is uncontrolled, but is owned - treat the same as
            // controlled.
            %killerGender = (%owner.sex $= "Male" ? 'him' : 'her');
            %killerPoss = (%owner.sex $= "Male" ? 'his' : 'her');
            %killerName = %owner.name;

            // player got in the way of a teammates deployed but
            // uncontrolled turret.
            if (%owner.team == %clVictim.team)
                messageAll('msgCTurretKill',
                    $DeathMessageCTurretAccdtlKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretAccdtlKillCount)],
                    %victimName, %victimGender, %victimPoss,
                    %killerName, %killerGender, %killerPoss,
                    %damageType, $DamageTypeText[%damageType]);
            else // deployed, uncontrolled turret killed an enemy
                messageAll('msgCTurretKill',
                    $DeathMessageCTurretKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretKillCount)],
                    %victimName, %victimGender, %victimPoss,
                    %killerName, %killerGender, %killerPoss,
                    %damageType, $DamageTypeText[%damageType]);
        }
        // turret is not a placed (owned) turret (or owner is no longer on it's
        // team), and is not being controlled
        else
        {
            messageAll('msgTurretKill',
                $DeathMessageTurretKill[%damageType, mFloor(getRandom() * $DeathMessageTurretKillCount)],
                %victimName, %victimGender, %victimPoss,
                %killerName, %killerGender, %killerPoss,
                %damageType, $DamageTypeText[%damageType]);
        }
    }
    // player killed himself or fell to death
    else if (%clKiller == %clVictim || %damageType == $DamageType::Ground)
    {
        messageAll('msgSelfKill',
            $DeathMessageSelfKill[%damageType, mFloor(getRandom() * $DeathMessageSelfKillCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    // killer died due to Out-of-Bounds damage
    else if (%damageType == $DamageType::OutOfBounds)
    {
        messageAll('msgOOBKill',
            $DeathMessageOOB[mFloor(getRandom() * $DeathMessageOOBCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    // victim died from camping near the nexus...
    else if (%damageType == $DamageType::NexusCamping)
    {
        messageAll('msgCampKill',
            $DeathMessageCamping[mFloor(getRandom() * $DeathMessageCampingCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    // was a TK
    else if (%clKiller.team == %clVictim.team)
    {
        messageAll('msgTeamKill',
            $DeathMessageTeamKill[%damageType, mFloor(getRandom() * $DeathMessageTeamKillCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    // player died by falling in lava
    else if (%damageType == $DamageType::Lava)
    {
        messageAll('msgLavaKill',
            $DeathMessageLava[mFloor(getRandom() * $DeathMessageLavaCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    // player was struck by lightning
    else if (%damageType == $DamageType::Lightning)
    {
        messageAll('msgLightningKill',
            $DeathMessageLightning[mFloor(getRandom() * $DeathMessageLightningCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    else if (%damageType == $DamageType::Mine && !isObject(%clKiller))
    {
        error("Mine kill w/o source");
        messageAll('MsgRogueMineKill',
            $DeathMessageRogueMine[%damageType, mFloor(getRandom() * $DeathMessageRogueMineCount)],
            %victimName, %victimGender, %victimPoss,
            %killerName, %killerGender, %killerPoss,
            %damageType, $DamageTypeText[%damageType]);
    }
    else // was a legitimate enemy kill
    {
        if (%damageType == 6 && %clVictim.headShot)
        {
            // laser headshot just occurred
            messageAll('MsgHeadshotKill',
                $DeathMessageHeadshot[%damageType, mFloor(getRandom() * $DeathMessageHeadshotCount)],
                %victimName, %victimGender, %victimPoss,
                %killerName, %killerGender, %killerPoss,
                %damageType, $DamageTypeText[%damageType]);
        }
        // ----------------------------------------------------
        // z0dd - ZOD, 8/25/02. Rear Lance hits
        else if (%damageType == 10 && %clVictim.rearshot)
        {
            // shocklance rearshot just occurred
            messageAll('MsgRearshotKill',
                $DeathMessageRearshot[%damageType, mFloor(getRandom() * $DeathMessageRearshotCount)],
                %victimName, %victimGender, %victimPoss,
                %killerName, %killerGender, %killerPoss,
                %damageType, $DamageTypeText[%damageType]);
        }
        // ----------------------------------------------------
        else
            messageAll('MsgLegitKill',
                $DeathMessage[%damageType, mFloor(getRandom() * $DeathMessageCount)],
                %victimName, %victimGender, %victimPoss,
                %killerName, %killerGender, %killerPoss,
                %damageType, $DamageTypeText[%damageType]);
    }
}

function DefaultGame::assignClientTeam(%game, %client, %respawn)
{
    // this function is overwritten in non-team mission types (e.g. DM)
    // so these lines won't do anything
    //if (!%game.numTeams)
    //{
    //   setTargetSkin(%client.target, %client.skin);
    //   return;
    //}

    //  camera is responsible for creating a player
    //  - counts the number of players per team
    //  - puts this player on the least player count team
    //  - sets the client's skin to the servers default

    %numPlayers = ClientGroup.getCount();
    for (%i = 0; %i <= %game.numTeams; %i++)
        %numTeamPlayers[%i] = 0;

    for (%i = 0; %i < %numPlayers; %i = %i + 1)
    {
        %cl = ClientGroup.getObject(%i);
        if (%cl != %client)
            %numTeamPlayers[%cl.team]++;
    }

    %leastPlayers = %numTeamPlayers[1];
    %leastTeam = 1;
    for (%i = 2; %i <= %game.numTeams; %i++)
    {
        if (%numTeamPlayers[%i] < %leastPlayers ||
                (%numTeamPlayers[%i] == %leastPlayers &&
                $teamScore[%i] < $teamScore[%leastTeam]))
        {
            %leastTeam = %i;
            %leastPlayers = %numTeamPlayers[%i];
        }
    }

    %client.team = %leastTeam;
    %client.lastTeam = %team;

    // Assign the team skin:
    if (%client.isAIControlled())
    {
        if (%leastTeam & 1)
        {
            %client.skin = addTaggedString("basebot");
            setTargetSkin(%client.target, 'basebot');
        }
        else
        {
            %client.skin = addTaggedString("basebbot");
            setTargetSkin(%client.target, 'basebbot');
        }
    }
    else
        setTargetSkin(%client.target, %game.getTeamSkin(%client.team));
        //setTargetSkin(%client.target, %client.skin);

    // might as well standardize the messages
    //messageAllExcept(%client, -1, 'MsgClientJoinTeam',
    //  '\c1%1 joined %2.', %client.name,
    //  $teamName[%leastTeam], %client, %leastTeam);
    //messageClient(%client, 'MsgClientJoinTeam',
    //  '\c1You joined the %2 team.', $client.name,
    //  $teamName[%client.team], %client, %client.team);
    messageAllExcept(%client, -1, 'MsgClientJoinTeam',
        '\c1%1 joined %2.', %client.name,
        %game.getTeamName(%client.team), %client, %client.team);
    messageClient(%client, 'MsgClientJoinTeam',
        '\c1You joined the %2 team.', %client.name,
        %game.getTeamName(%client.team), %client, %client.team);

    updateCanListenState(%client);
}

function DefaultGame::getTeamSkin(%game, %team)
{
    return $teamSkin[%team];
}

function DefaultGame::getTeamName(%game, %team)
{
    return $teamName[%team];
}

function DefaultGame::clientJoinTeam(%game, %client, %team, %respawn)
{
    if (%team < 1 || %team > %game.numTeams)
        return;

    if (%respawn $= "")
        %respawn = 1;

    %client.team = %team;
    %client.lastTeam = %team;
    setTargetSkin(%client.target, %game.getTeamSkin(%team));
    setTargetSensorGroup(%client.target, %team);
    %client.setSensorGroup(%team);

    // Spawn the player:
    %game.spawnPlayer(%client, %respawn);

    messageAllExcept(%client, -1, 'MsgClientJoinTeam',
        '\c1%1 joined %2.',
        %client.name, %game.getTeamName(%team), %client, %team);
    messageClient(%client, 'MsgClientJoinTeam',
        '\c1You joined the %2 team.',
        %client.name, %game.getTeamName(%client.team), %client, %client.team);

    updateCanListenState(%client);
}

function DefaultGame::AIHasJoined(%game, %client)
{
    //defined to prevent console spam
}

function DefaultGame::AIChangeTeam(%game, %client, %newTeam)
{
    // make sure we're trying to drop an AI
    if (!isObject(%client) || !%client.isAIControlled())
        return;

    // clear the ai from any objectives, etc...
    AIUnassignClient(%client);
    %client.stop();
    %client.clearTasks();
    %client.clearStep();
    %client.lastDamageClient = -1;
    %client.lastDamageTurret = -1;
    %client.shouldEngage = -1;
    %client.setEngageTarget(-1);
    %client.setTargetObject(-1);
    %client.pilotVehicle = false;
    %client.defaultTasksAdded = false;

    // kill the player, which should cause the Game object to perform
    // whatever cleanup is required.
    if (isObject(%client.player))
        %client.player.scriptKill(0);

    // clean up the team rank array
    %game.removeFromTeamRankArray(%client);

    // assign the new team
    %client.team = %newTeam;
    if (%newTeam < 0)
        Game.assignClientTeam(%client);
    else
    {
        if (%client.team & 1)
        {
            %client.skin = addTaggedString("basebot");
            setTargetSkin(%client.target, 'basebot');
        }
        else
        {
            %client.skin = addTaggedString("basebbot");
            setTargetSkin(%client.target, 'basebbot');
        }
    }

    messageAllExcept(%client, -1, 'MsgClientJoinTeam',
        '\c1bot %1 has switched to team %2.',
        %client.name, %game.getTeamName(%client.team), %client, %client.team);
}

function DefaultGame::clientChangeTeam(%game, %client, %team, %fromObs, %respawned) // z0dd - ZOD, 6/06/02. Don't send a message if player used respawn feature. Added %respawned
{
    // first, remove the client from the team rank array
    // the player will be added to the new team array as soon as he respawns...
    %game.removeFromTeamRankArray(%client);

    %pl = %client.player;
    if (isObject(%pl))
    {
        if (%pl.isMounted())
            %pl.getDataBlock().doDismount(%pl);
        %pl.scriptKill(0);
    }

    // reset the client's targets and tasks only
    clientResetTargets(%client, true);

    // give this client a new handle to disassociate ownership of
    // deployed objects
    if (%team $= "" && %team > 0 && %team <= %game.numTeams)
    {
        if (%client.team == 1)
            %client.team = 2;
        else
            %client.team = 1;
    }
    else
        %client.team = %team;

    // Set the client's skin:
    if (!%client.isAIControlled())
        setTargetSkin(%client.target, %game.getTeamSkin(%client.team));
    setTargetSensorGroup(%client.target, %client.team);
    %client.setSensorGroup(%client.team);

    // Spawn the player:
    %client.lastSpawnPoint = %game.pickPlayerSpawn(%client);

    %game.createPlayer(%client, %client.lastSpawnPoint, $MatchStarted);

    if ($MatchStarted)
        %client.setControlObject(%client.player);
    else
    {
        %client.camera.getDataBlock().setMode(
            %client.camera, "pre-game", %client.player);
        %client.setControlObject(%client.camera);
    }

    // call the onEvent for this game type
    // - Bounty uses this to remove this client from others' hit lists
    %game.onClientEnterObserverMode(%client);

    if (%fromObs $= "" || !%fromObs)
    {
        //-------------------------------------------------------------------------
        // z0dd - ZOD, 6/06/02. Don't send a message if player used respawn feature
        if (!%respawned)
        {
            messageAllExcept(%client, -1, 'MsgClientJoinTeam',
                '\c1%1 switched to team %2.',
                %client.name, %game.getTeamName(%client.team), %client, %client.team);
            messageClient(%client, 'MsgClientJoinTeam',
                '\c1You switched to team %2.',
                %client.name, %game.getTeamName(%client.team), %client, %client.team);
        }
        //-------------------------------------------------------------------------
    }
    else
    {
        messageAllExcept(%client, -1, 'MsgClientJoinTeam',
            '\c1%1 joined team %2.',
            %client.name, %game.getTeamName(%client.team), %client, %client.team);
        messageClient(%client, 'MsgClientJoinTeam',
            '\c1You joined team %2.',
            %client.name, %game.getTeamName(%client.team), %client, %client.team);
    }

    updateCanListenState(%client);

    // MES - switch objective hud lines when client switches teams
    messageClient(%client, 'MsgCheckTeamLines', "", %client.team);
}

// missioncleanup and missiongroup are checked prior to entering game code
function DefaultGame::missionLoadDone(%game)
{
    //  walks through the mission group and sets the power stuff up
    //   - groups get initialized with power count 0 then iterated to
    //     increment power count if an object within is powered
    //   - powers objects up/down
    //MissionGroup.objectiveInit();
    MissionGroup.clearPower();
    MissionGroup.powerInit(0);

    // set up scoring variables and other game specific globals
    %game.initGameVars();

    // make team0 visible/friendly to all
    setSensorGroupAlwaysVisMask(0, 0xffffffff);
    setSensorGroupFriendlyMask(0, 0xffffffff);

    // update colors:
    // - enemy teams are red
    // - same team is green
    // - team 0 is white
    for (%i = 0; %i < 32; %i++)
    {
        %team = (1 << %i);
        setSensorGroupColor(%i, %team, "0 255 0 255");
        setSensorGroupColor(%i, ~%team, "255 0 0 255");
        setSensorGroupColor(%i, 1, "255 255 255 255");

        // setup the team targets (always friendly and visible to same team)
        setTargetAlwaysVisMask(%i, %team);
        setTargetFriendlyMask(%i, %team);
    }

    // set up the teams
    %game.setUpTeams();

    // clear out the team rank array...
    for (%i = 0; %i < 32; %i++)
        $TeamRank[%i, count] = "";

    // objectiveInit has to take place after setupTeams -- objective HUD
    // relies on flags
    // having their team set
    MissionGroup.objectiveInit();

    // initialize the AI system
    %game.aiInit();

    // need to reset the teams if we switch from say, CTF to Bounty...
    // assign the bots team
    if ($currentMissionType !$= $previousMissionType)
    {
        $previousMissionType = $currentMissionType;
        for (%i = 0; %i < ClientGroup.getCount(); %i++)
        {
            %cl = ClientGroup.getObject(%i);
            if (%cl.isAIControlled())
                %game.assignClientTeam(%cl);
        }
    }

    // Save off respawn or Siege Team switch information...
    if (%game.class !$= "SiegeGame")
        MissionGroup.setupPositionMarkers(true);
}

function DefaultGame::onClientLeaveGame(%game, %client)
{
    // if there is a player attached to this client, kill it
    if (isObject(%client.player))
        %client.player.scriptKill(0);

    // cancel a scheduled call...
    cancel(%client.respawnTimer);
    %client.respawnTimer = "";

    // remove them from the team rank arrays
    %game.removeFromTeamRankArray(%client);
}

function DefaultGame::clientMissionDropReady(%game, %client)
{
    // synchronize the clock HUD
    messageClient(%client, 'MsgSystemClock', "", 0, 0);

    %game.sendClientTeamList(%client);
    %game.setupClientHuds(%client);

    %observer = false;
    if (!$Host::TournamentMode)
    {
        if (%client.camera.mode $= "observerFly" ||
                %client.camera.mode $= "justJoined")
        {
            %observer = true;
            %client.observerStartTime = getSimTime();
            commandToClient(%client, 'setHudMode', 'Observer');
            %client.setControlObject(%client.camera);
            //displayObserverHud(%client, 0);
            updateObserverFlyHud(%client);
        }

        if (!%observer)
        {
            // server has not started anything yet
            if (!$MatchStarted && !$CountdownStarted)
            {
                %client.setControlObject(%client.camera);
                commandToClient(%client, 'setHudMode', 'Observer');
            }
            // server has started the countdown
            else if (!$MatchStarted && $CountdownStarted)
            {
                commandToClient(%client, 'setHudMode', 'Observer');
                %client.setControlObject(%client.camera);
            }
            // the game has already started
            else
            {
                commandToClient(%client, 'setHudMode', 'Standard');
                %client.setControlObject(%client.player);
            }
        }
    }
    else
    {
        // set all players into obs mode. setting the control object will
        // handle further procedures...
        %client.camera.getDataBlock().setMode(%client.camera, "ObserverFly");
        commandToClient(%client, 'setHudMode', 'Observer');
        %client.setControlObject(%client.camera);
        messageAll('MsgClientJoinTeam', "",
            %client.name, $teamName[0], %client, 0);
        %client.team = 0;

        if (!$MatchStarted && !$CountdownStarted)
        {
            if ($TeamDamage)
                %damMess = "ENABLED";
            else
                %damMess = "DISABLED";

            if (%game.numTeams > 1)
                bottomPrint(%client,
                    "Server is Running in Tournament Mode.\nPick a Team\n" @
                    "Team Damage is " @ %damMess, 0, 3);
        }
        else
        {
            bottomPrint(%client, "\nServer is Running in Tournament Mode", 0, 3);
        }
    }

    // make sure the objective HUD indicates your team on top and in green...
    if (%client.team > 0)
        messageClient(%client, 'MsgCheckTeamLines', "", %client.team);

    // were ready to go.
    %client.matchStartReady = true;
}

function DefaultGame::sendClientTeamList(%game, %client)
{
    // Send the client the current team list:
    %teamCount = %game.numTeams;
    for (%i = 0; %i < %teamCount; %i++)
    {
        if (%i > 0)
            %teamList = %teamList @ "\n";

        %teamList = %teamList @ detag(getTaggedString(
            %game.getTeamName(%i + 1)));
    }
    messageClient(%client, 'MsgTeamList', "", %teamCount, %teamList);
}

function DefaultGame::setupClientHuds(%game, %client)
{
    // tell the client to setup the huds...
    for (%i =0; %i<$WeaponsHudCount; %i++)
        %client.setWeaponsHudBitmap(%i, $WeaponsHudData[%i, itemDataName],
            $WeaponsHudData[%i, bitmapName]);
    for (%i =0; %i<$InventoryHudCount; %i++)
    {
        if ($InventoryHudData[%i, slot] != 0)
            %client.setInventoryHudBitmap($InventoryHudData[%i, slot],
                $InventoryHudData[%i, itemDataName],
                $InventoryHudData[%i, bitmapName]);
    }
    %client.setInventoryHudBitmap(0, "", "gui/hud_handgren");

    %client.setWeaponsHudBackGroundBmp("gui/hud_new_panel");
    %client.setWeaponsHudHighLightBmp("gui/hud_new_weaponselect");
    %client.setWeaponsHudInfiniteAmmoBmp("gui/hud_infinity");
    %client.setInventoryHudBackGroundBmp("gui/hud_new_panel");

    // tell the client if we are protecting statics (so no health bar will
    // be displayed)
    commandToClient(%client, 'protectingStaticObjects',
        %game.allowsProtectedStatics());
    commandToClient(%client, 'setPowerAudioProfiles',
        sPowerUp.getId(), sPowerDown.getId());
}

function DefaultGame::testDrop(%game, %client)
{
    %game.clientJoinTeam(%client, 1, false);
    %client.camera.getDataBlock().setMode(
        %client.camera, "pre-game", %client.player);
    %client.setControlObject(%client.camera);
    CommandToClient(%client, 'setPlayContent');
}

function DefaultGame::onClientEnterObserverMode(%game, %client)
{
    // Default game doesn't care...
}

function DefaultGame::dropFlag(%game, %player)
{
    // allows implementers to override a "dropFlag" event
}

// from 'item.cs'
function DefaultGame::playerTouchFlag(%game, %player, %flag)
{
    messageAll('MsgPlayerTouchFlag',
        'Player %1 touched flag %2', %player, %flag);
}

// from 'item.cs'
function DefaultGame::playerDroppedFlag(%game, %player, %flag)
{
    messageAll('MsgPlayerDroppedFlag',
        'Player %1 dropped flag %2', %player, %flag);
}

// from 'staticShape.cs'
function DefaultGame::flagStandCollision(%game, %dataBlock, %obj, %colObj)
{
    // for retreiveGame
}

function DefaultGame::notifyMineDeployed(%game, %mine)
{
    // do nothing in the default game...
}

// from 'staticshape.cs'
function DefaultGame::findProjector(%game, %flipflop)
{
    // search the flipflop's folder for a holo projector
    // if one exists, associate it with the flipflop
    %flipflop.projector = 0;
    %folder = %flipflop.getGroup();
    for (%i = 0; %i < %folder.getCount(); %i++)
    {
        %proj = %folder.getObject(%i);
        if (%proj.getDatablock().getName() $= "LogoProjector")
        {
            %flipflop.projector = %proj;
            %flipflop.projector.holo = 0;
            break;
        }
    }
}

//******************************************************************************
//*   DefaultGame Trigger  -  Functions                                        *
//******************************************************************************

/// -Trigger- //////////////////////////////////////////////////////////////////
//Function -- onEnterTrigger (%game, %name, %data, %obj, %colObj)
//                %game = Current game type object
//                %name = Trigger name - defined when trigger is created
//                %data = Trigger Data Block
//                %obj = Trigger Object
//                %colObj = Object that collided with the trigger
//Decription -- Called when trigger has been triggered
////////////////////////////////////////////////////////////////////////////////
// from 'trigger.cs'
function DefaultGame::onEnterTrigger(%game, %triggerName, %data, %obj, %colobj)
{
    // Do Nothing
}

/// -Trigger- //////////////////////////////////////////////////////////////////
//Function -- onLeaveTrigger (%game, %name, %data, %obj, %colObj)
//                %game = Current game type object
//                %name = Trigger name - defined when trigger is created
//                %data = Trigger Data Block
//                %obj = Trigger Object
//                %colObj = Object that collided with the trigger
//Decription -- Called when trigger has been untriggered
////////////////////////////////////////////////////////////////////////////////
// from 'trigger.cs'
function DefaultGame::onLeaveTrigger(%game, %triggerName, %data, %obj, %colobj)
{
    // Do Nothing
}

/// -Trigger- //////////////////////////////////////////////////////////////////
//Function -- onTickTrigger(%game, %name, %data, %obj)
//                %game = Current game type object
//                %name = Trigger name - defined when trigger is created
//                %data = Trigger Data Block
//                %obj = Trigger Object
//Decription -- Called every tick if triggered
////////////////////////////////////////////////////////////////////////////////
// from 'trigger.cs'
function DefaultGame::onTickTrigger(%game, %triggerName, %data, %obj)
{
    // Do Nothing
}

function DefaultGame::setUpTeams(%game)
{
    %group = nameToID("MissionGroup/Teams");
    if (%group == -1)
        return;

    // create a team0 if it does not exist
    %team = nameToID("MissionGroup/Teams/team0");
    if (%team == -1)
    {
        %team = new SimGroup("team0");
        %group.add(%team);
    }

    // 'team0' is not counted as a team here
    %game.numTeams = 0;
    while (%team != -1)
    {
        // create drop set and add all spawnsphere objects into it
        %dropSet = new SimSet("TeamDrops" @ %game.numTeams);
        MissionCleanup.add(%dropSet);

        %spawns = nameToID(
            "MissionGroup/Teams/team" @ %game.numTeams @ "/SpawnSpheres");
        if (%spawns != -1)
        {
            %count = %spawns.getCount();
            for (%i = 0; %i < %count; %i++)
                %dropSet.add(%spawns.getObject(%i));
        }

        // set the 'team' field for all the objects in this team
        %team.setTeam(%game.numTeams);

        clearVehicleCount(%team+1);
        // get next group
        %team = nameToID("MissionGroup/Teams/team" @ %game.numTeams + 1);
        if (%team != -1)
            %game.numTeams++;
    }

    // set the number of sensor groups (including team0) that are processed
    setSensorGroupCount(%game.numTeams + 1);
}

function SimGroup::setTeam(%this, %team)
{
    for (%i = 0; %i < %this.getCount(); %i++)
    {
        %obj = %this.getObject(%i);
        switch$ (%obj.getClassName())
        {
        case SpawnSphere:
            if ($MatchStarted)
            {
                // find out what team the spawn sphere used to belong to
                %found = false;
                for (%l = 1; %l <= Game.numTeams; %l++)
                {
                    %drops = nameToId("MissionCleanup/TeamDrops" @ %l);
                    for (%j = 0; %j < %drops.getCount(); %j++)
                    {
                        %current = %drops.getObject(%j);
                        if (%current == %obj)
                            %found = %l;
                    }
                }

                if (%team != %found)
                    Game.claimSpawn(%obj, %team, %found);
                else
                    error("spawn "@%obj@" is already on team "@%team@"!");
            }
            else
                Game.claimSpawn(%obj, %team, "");
        case SimGroup:
            %obj.setTeam(%team);
        default:
            %obj.team = %team;
        }

        if (%obj.getType() & $TypeMasks::GameBaseObjectType)
        {
            // eeck.. please go away when scripts get cleaned...
            // -----------------------------------------------------------------
            // z0dd - ZOD, 5/8/02. Part of re-write of Vehicle
            // station creation. Do not need this code anymore.
            //if (%obj.getDataBlock().getName() $= "StationVehiclePad")
            //{
            //   %team = %obj.team;
            //   %obj = %obj.station;
            //   %obj.team = %team;
            //%obj.teleporter.team = %team;
            //}
            %target = %obj.getTarget();
            if (%target != -1)
                setTargetSensorGroup(%target, %team);
        }
    }
}

function DefaultGame::claimSpawn(%game, %obj, %newTeam, %oldTeam)
{
    if (%newTeam == %oldTeam)
        return;

    %newSpawnGroup = nameToId("MissionCleanup/TeamDrops" @ %newTeam);
    if (%oldTeam !$= "")
    {
        %oldSpawnGroup = nameToId("MissionCleanup/TeamDrops" @ %oldTeam);
        %oldSpawnGroup.remove(%obj);
    }
    %newSpawnGroup.add(%obj);
}

// recursive function to assign teams to all mission objects

function SimGroup::swapTeams(%this)
{
    // used in Siege only
    Game.groupSwapTeams(%this);
}

function ShapeBase::swapTeams(%this)
{
    // used in Siege only
    Game.objectSwapTeams(%this);
}

function GameBase::swapTeams(%this)
{
    // used in Siege only
    Game.objectSwapTeams(%this);
}

function TSStatic::swapTeams(%this)
{
    // used in Siege only
    // do nothing
}

function InteriorInstance::swapTeams(%this)
{
    // used in Siege only
    // do nothing -- interiors don't switch teams
}

function SimGroup::swapVehiclePads(%this)
{
    // used in Siege only
    Game.groupSwapVehiclePads(%this);
}

function ShapeBase::swapVehiclePads(%this)
{
    // used in Siege only
    Game.objectSwapVehiclePads(%this);
}

function GameBase::swapVehiclePads(%this)
{
    // used in Siege only
    // do nothing -- only searching for vehicle pads
}

function InteriorInstance::swapVehiclePads(%this)
{
    // used in Siege only
    // do nothing -- only searching for vehicle pads
}

function SimSet::swapVehiclePads(%this)
{
    // used in Siege only
    // do nothing -- only searching for vehicle pads
}

function PhysicalZone::swapVehiclePads(%this)
{
    // used in Siege only
    // do nothing -- only searching for vehicle pads
}

function SimGroup::objectRestore(%this)
{
    // used in Siege only
    Game.groupObjectRestore(%this);
}

function ShapeBase::objectRestore(%object)
{
    // only used for Siege
    Game.shapeObjectRestore(%object);
}

function Turret::objectRestore(%object)
{
    // only used for Siege
    Game.shapeObjectRestore(%object);
}

function AIObjective::objectRestore(%object)
{
    // only used for Siege
    // don't do anything for AI Objectives
}

function DefaultGame::checkObjectives(%game)
{
    // any special objectives that can be met by gametype
    // none for default game
}

//---------------------------------------------------

function DefaultGame::checkTimeLimit(%game, %forced)
{
    // Don't add extra checks:
    if (%forced)
        cancel(%game.timeCheck);

    // if there is no time limit, check back in a minute to see if it's been set
    if ($Host::TimeLimit $= "" || $Host::TimeLimit == 0)
    {
        %game.timeCheck = %game.schedule(20000, "checkTimeLimit");
        return;
    }

    %curTimeLeftMS = ($Host::TimeLimit * 60 * 1000) +
        $missionStartTime - getSimTime();

    if (%curTimeLeftMS <= 0)
    {
        // time's up, put down your pencils
        %game.timeLimitReached();
    }
    else
    {
        if (%curTimeLeftMS >= 20000)
            %game.timeCheck = %game.schedule(20000, "checkTimeLimit");
        else
            %game.timeCheck = %game.schedule(
                %curTimeLeftMS + 1, "checkTimeLimit");

        // now synchronize everyone's clock
        messageAll('MsgSystemClock', "", $Host::TimeLimit, %curTimeLeftMS);
    }
}

function listplayers()
{
    for (%i = 0; %i < ClientGroup.getCount(); %i++)
    {
        %cl = ClientGroup.getObject(%i);
        %status = "";
        if (%cl.isAiControlled())
            %status = "Bot ";
        if (%cl.isSmurf)
            %status = "Alias ";
        if (%cl.isAdmin)
            %status = %status @ "Admin ";
        if (%cl.isSuperAdmin)
            %status = %status @ "SuperAdmin ";
        if (%status $= "")
            %status = "<normal>";
        echo("client: " @ %cl @ " player: " @ %cl.player @ " name: " @
            %cl.nameBase @ " team: " @ %cl.team @ " status: " @ %status);
    }
}

function DefaultGame::clearTeamRankArray(%game, %team)
{
    %count = $TeamRank[%team, count];
    for (%i = 0; %i < %count; %i++)
        $TeamRank[%team, %i] = "";
    $TeamRank[%team, count] = 0;
}

function DefaultGame::populateTeamRankArray(%game, %client)
{
    // this function should be called *after* the client has been added
    // to a team...
    if (%client <= 0 || %client.team <= 0)
        return;

    // find the team
    if (%game.numTeams == 1)
        %team = 0;
    else
        %team = %client.team;

    // find the number of teammates already ranked...
    %count = $TeamRank[%team, count];
    if (%count $= "")
    {
        $TeamRank[%team, count] = 0;
        %count = 0;
    }

    // make sure we're not already in the array
    for (%i = 0; %i < %count; %i++)
    {
        if ($TeamRank[%team, %i] == %client)
            return;
    }

    // add the client in at the bottom of the list, and increment the count
    $TeamRank[%team, %count] = %client;
    $TeamRank[%team, count] = $TeamRank[%team, count] + 1;

    //now recalculate the team rank for this player
    %game.recalcTeamRanks(%client);
}

function DefaultGame::removeFromTeamRankArray(%game, %client)
{
    // note, this should be called *before* the client actually switches
    // teams or drops...
    if (%client <= 0 || %client.team <= 0)
        return;

    // find the correct team
    if (%game.numTeams == 1)
        %team = 0;
    else
        %team = %client.team;

    // now search through the team rank array, looking for this client
    %count = $TeamRank[%team, count];
    for (%i = 0; %i < %count; %i++)
    {
        if ($TeamRank[%team, %i] == %client)
        {
            // we've found the client in the array, now loop through, and
            // move everyone else up a rank
            for (%j = %i + 1; %j < %count; %j++)
            {
                %cl = $TeamRank[%team, %j];
                $TeamRank[%team, %j - 1] = %cl;
                messageClient(%cl, 'MsgYourRankIs', "", %j);
            }
            $TeamRank[%team, %count - 1] = "";

            // now decrement the team rank array count, and break
            $TeamRank[%team, count] = $TeamRank[%team, count] - 1;
            break;
        }
    }
}

function DefaultGame::recalcTeamRanks(%game, %client)
{
    if (%client <= 0 || %client.team <= 0)
        return;

    // this is a little confusing -- someone's actual numerical rank is always
    // one number higher than his index in the $TeamRank array
    // (e.g. person ranked 1st has index of 0)

    // TINMAN:  I'm going to remove the %client.teamRank field - the index
    // in the $TeamRank array already contains their rank - safer to search
    // the array than to maintain the information in a separate variable...

    // find the team, the client in the team array
    if (%game.numTeams == 1)
        %team = 0;
    else
        %team = %client.team;

    %count = $TeamRank[%team, count];
    %index = -1;
    for (%i = 0; %i < %count; %i++)
    {
        if ($TeamRank[%team, %i] == %client)
        {
            %index = %i;
            break;
        }
    }

    // if they weren't found in the array, return
    if (%index < 0)
        return;

    // make sure far down the array as they should be...
    %tempIndex = %index;
    %swapped = false;
    while (true)
    {
        if (%tempIndex <= 0)
            break;

        %tempIndex--;
        %tempClient = $TeamRank[%team, %tempIndex];

        // see if we should swap the two
        if (%client.score > %tempClient.score)
        {
            %swapped = true;
            %index = %tempIndex;
            $TeamRank[%team, %tempIndex] = %client;
            $TeamRank[%team, %tempIndex + 1] = %tempClient;
            messageClient(%tempClient, 'MsgYourRankIs', "", %tempIndex + 2);
        }
    }

    // if we've swapped up at least once, we obviously won't need to
    // swap down as well...
    if (%swapped)
    {
        messageClient(%client, 'MsgYourRankIs', "", %index + 1);
        return;
    }

    // since we didn't swap up, see if we need to swap down...
    %tempIndex = %index;
    %swapped = false;
    while (true)
    {
        if (%tempIndex >= %count - 1)
            break;

        %tempIndex++;
        %tempClient = $TeamRank[%team, %tempIndex];

        //see if we should swap the two
        if (%client.score < %tempClient.score)
        {
            %swapped = true;
            %index = %tempIndex;
            $TeamRank[%team, %tempIndex] = %client;
            $TeamRank[%team, %tempIndex - 1] = %tempClient;
            messageClient(%tempClient, 'MsgYourRankIs', "", %tempIndex);
        }
    }

    // send the message (regardless of whether a swap happened or not)
    messageClient(%client, 'MsgYourRankIs', "", %index + 1);
}

function DefaultGame::recalcScore(%game, %cl)
{
    %game.recalcTeamRanks(%cl);
}

function DefaultGame::testKill(%game, %victimID, %killerID)
{
    return (%killerID !=0 && %victimID.team != %killerID.team);
}

function DefaultGame::testSuicide(%game, %victimID, %killerID, %damageType)
{
    return (%victimID == %killerID || %damageType == $DamageType::Ground ||
        %damageType == $DamageType::Suicide);
}

function DefaultGame::testTeamKill(%game, %victimID, %killerID)
{
    return (%killerID.team == %victimID.team);
}

function DefaultGame::testTurretKill(%game, %implement)
{
    if (%implement == 0)
        return false;
    else
        return (%implement.getClassName() $= "Turret");
}

// function DefaultGame::awardScoreFlagCap(%game, %cl)
// {
//    %cl.flagCaps++;
//    $TeamScore[%cl.team] += %game.SCORE_PER_TEAM_FLAG_CAP;
//    messageAll('MsgCTFTeamScore', "", %cl.team, $TeamScore[%cl.team]);
//
//    if (%game.SCORE_PER_PLYR_FLAG_CAP > 1)
//      %plural = "s";
//    else
//      %plural = "";
//
//    if (%game.SCORE_PER_PLYR_FLAG_CAP != 0)
//         messageClient(%cl, 'scoreFlaCapMsg', 'You received %1 point%2 for capturing the flag.', %game.SCORE_PER_PLYR_FLAG_CAP, %plural);
//    %game.recalcScore(%cl);
// }

function DefaultGame::testOOBDeath(%game, %damageType)
{
    return (%damageType == $DamageType::OutOfBounds);
}

function DefaultGame::awardScoreTurretKill(%game, %victimID, %implement)
{
    // award whoever might be controlling the turret
    if ((%killer = %implement.getControllingClient()) != 0)
    {
        if (%killer == %victimID)
            %game.awardScoreSuicide(%victimID);
        // player controlling a turret killed a teammate
        else if (%killer.team == %victimID.team)
        {
            %killer.teamKills++;
            %game.awardScoreTurretTeamKill(%victimID, %killer);
            %game.awardScoreDeath(%victimID);
        }
        else
        {
            %killer.turretKills++;
            %game.recalcScore(%killer);
            %game.awardScoreDeath(%victimID);
        }
    }
    // if it isn't controlled, award score to whoever deployed it
    else if ((%killer = %implement.owner) != 0)
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

    //default is, no one was controlling it, no one owned it. No score given.
}

function DefaultGame::awardScoreDeath(%game, %victimID)
{
    %victimID.deaths++;
    if (%game.SCORE_PER_DEATH != 0)
    {
        // %plural = (abs(%game.SCORE_PER_DEATH) != 1 ? "s" : "");
        // messageClient(%victimID, 'MsgScoreDeath',
        //  '\c0You have been penalized %1 point%2 for dying.',
        //  abs(%game.SCORE_PER_DEATH), %plural);
        %game.recalcScore(%victimID);
    }
}

function DefaultGame::awardScoreKill(%game, %killerID)
{
    %killerID.kills++;
    %game.recalcScore(%killerID);
}

function DefaultGame::awardScoreSuicide(%game, %victimID)
{
    %victimID.suicides++;
    //if (%game.SCORE_PER_SUICIDE != 0)
    //  messageClient(%victimID, 'MsgScoreSuicide',
    //  '\c0You have been penalized for killing yourself.');
    %game.recalcScore(%victimID);
}

function DefaultGame::awardScoreTeamkill(%game, %victimID, %killerID)
{
    %killerID.teamKills++;
    if (%game.SCORE_PER_TEAMKILL != 0)
        messageClient(%killerID, 'MsgScoreTeamkill',
            '\c0You have been penalized for killing teammate %1.',
            %victimID.name);
    %game.recalcScore(%killerID);
}

function DefaultGame::awardScoreTurretTeamKill(%game, %victimID, %killerID)
{
    %killerID.teamKills++;
    if (%game.SCORE_PER_TEAMKILL != 0)
        messageClient(%killerID, 'MsgScoreTeamkill',
            '\c0You have been penalized for killing your teammate %1, with a turret.',
            %victimID.name);
    %game.recalcScore(%killerID);
}

function DefaultGame::objectRepaired(%game, %obj, %objName)
{
    %item = %obj.getDataBlock().getName();
    switch$ (%item)
    {
    case generatorLarge:
        %game.genOnRepaired(%obj, %objName);
    case stationInventory:
        %game.stationOnRepaired(%obj, %objName);
    case sensorMediumPulse:
        %game.sensorOnRepaired(%obj, %objName);
    case sensorLargePulse:
        %game.sensorOnRepaired(%obj, %objName);
    case turretBaseLarge:
        %game.turretOnRepaired(%obj, %objName);
    case stationVehicle:
        %game.vStationOnRepaired(%obj, %objName);
    default:
        // unused by current game types. Add more checks here if desired
    }
}

function DefaultGame::allowsProtectedStatics(%game)
{
    return false;
}

// jff: why is game object doing this?
// Return a simple string with no extras
function DefaultGame::cleanWord(%game, %this)
{
    %length = strlen(%this);
    for (%i = 0; %i < %length; %i++)
    {
        %char = getSubStr(%this, %i, 1);
        if (%char $= "_")
        {
            %next =  getSubStr(%this, (%i+1), 1);
            if (%next $= "_")
            {
                %char = "'"; // apostrophe (2 chars)
                %i++;
            }
            else
                %char = " "; // space
        }

        %clean = (%clean @ %char);
    }
}

function DefaultGame::stationOnEnterTrigger(%game, %data, %obj, %colObj)
{
    return true;
}

function DefaultGame::WeaponOnUse(%game, %data, %obj)
{
    return true;
}

function DefaultGame::HandInvOnUse(%game, %data, %obj)
{
    return true;
}

function DefaultGame::WeaponOnInventory(%game, %this, %obj, %amount)
{
    return true;
}

function DefaultGame::ObserverOnTrigger(%game, %data, %obj, %trigger, %state)
{
    return true;
}

// jff: why is the game being notified that a weapon is being thrown?
// hot potato gametype?
function DefaultGame::ShapeThrowWeapon(%game, %this)
{
    return true;
}

function DefaultGame::leaveMissionArea(%game, %playerData, %player)
{
    if (%player.getState() $= "Dead")
        return;

    %player.client.outOfBounds = true;
    messageClient(%player.client, 'LeaveMissionArea',
        '\c1You left the mission area.~wfx/misc/warning_beep.wav');
}

function DefaultGame::enterMissionArea(%game, %playerData, %player)
{
    if (%player.getState() $= "Dead")
        return;

    %player.client.outOfBounds = false;
    messageClient(%player.client, 'EnterMissionArea',
        '\c1You are back in the mission area.');
}

//------------------------------------------------------------------------------
// AI stubs:
//------------------------------------------------------------------------------

function DefaultGame::onAIDamaged(%game, %clVictim, %clAttacker,
        %damageType, %sourceObject)
{
}

function DefaultGame::onAIFriendlyFire(%game, %clVictim, %clAttacker,
        %damageType, %sourceObject)
{
}

function DefaultGame::onAIKilled(%game, %clVictim, %clKiller,
        %damageType, %implement)
{
    // unassign the client from any objectives
    AIUnassignClient(%clVictim);

    // break the link, if this ai is controlled
    aiReleaseHumanControl(%clVictim.controlByHuman, %clVictim);

    // and schedule the respawn
    %clVictim.respawnThread = schedule(5000,
        %clVictim, "onAIRespawn", %clVictim);
}

function DefaultGame::onAIKilledClient(%game, %clVictim, %clAttacker,
        %damageType, %implement)
{
    %clAttacker.setVictim(%clVictim, %clVictim.player);
}

//------------------------------------------------------------------------------
// Voting stuff:
//------------------------------------------------------------------------------
function DefaultGame::sendGamePlayerPopupMenu(%game, %client, %targetClient, %key)
{
    if (!%targetClient.matchStartReady)
        return;

    %isAdmin = (%client.isAdmin || %client.isSuperAdmin);

    %isTargetSelf = (%client == %targetClient);
    %isTargetAdmin = (%targetClient.isAdmin || %targetClient.isSuperAdmin);
    %isTargetBot = %targetClient.isAIControlled();
    %isTargetObserver = (%targetClient.team == 0);
    %outrankTarget = false;
    if (%client.isSuperAdmin)
        %outrankTarget = !%targetClient.isSuperAdmin;
    else if (%client.isAdmin)
        %outrankTarget = !%targetClient.isAdmin;

    if (%client.isSuperAdmin && %targetClient.guid != 0)
    {
        messageClient(%client, 'MsgPlayerPopupItem', "", %key, "addAdmin",
            "", 'Add to Server Admin List', 10);
        messageClient(%client, 'MsgPlayerPopupItem', "", %key, "addSuperAdmin",
            "", 'Add to Server SuperAdmin List', 11);
    }

    // mute options
    if (!%isTargetSelf)
    {
        if (%client.muted[%targetClient])
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "MutePlayer", "", 'Unmute Text Chat', 1);
        else
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "MutePlayer", "", 'Mute Text Chat', 1);

        if (!%isTargetBot && %client.canListenTo(%targetClient))
        {
            if (%client.getListenState(%targetClient))
                messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                    "ListenPlayer", "", 'Disable Voice Com', 9);
            else
                messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                    "ListenPlayer", "", 'Enable Voice Com', 9);
        }

        // ------------------------------------------
        // z0dd - ZOD 4/4/02. Observe a specific player
        if (%client.team == 0 && !%isTargetObserver)
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "ObservePlayer", "", 'Observe Player', 12);
    }

    if (!%client.canVote && !%isAdmin)
        return;

    // regular vote options on players
    if (Vote.scheduled $= "" && !%isAdmin && !%isTargetAdmin)
    {
        if ($Host::allowAdminPlayerVotes && !%isTargetBot)
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "AdminPlayer", "", 'Vote to Make Admin', 2);

        %teamSpecific = (Game.numTeams > 1 && %targetClient.team != 0);
        if (!%isTargetSelf &&
                (!%teamSpecific || %client.team == %targetClient.team))
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "KickPlayer", "", 'Vote to Kick', 3);
    }
    // Admin only options on players:
    else if (%isAdmin)
    {
        if (!%isTargetBot && !%isTargetAdmin)
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "AdminPlayer", "", 'Make Admin', 2);

        if (!%isTargetSelf && %outrankTarget)
        {
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "KickPlayer", "", 'Kick', 3);

            if (!%isTargetBot)
            {
                // ------------------------------------------------------------------------------------------------------
                // z0dd - ZOD 4/4/02. Warn player,  send private message and remove admin privileges
                messageClient(%client, 'MsgPlayerPopupItem', "", %key, "Warn", "", 'Warn player', 13);
                if (%isTargetAdmin)
                    messageClient(%client, 'MsgPlayerPopupItem', "", %key, "StripAdmin", "", 'Strip admin', 14);

                if (%client.isSuperAdmin)
                    messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                        "BanPlayer", "", 'Ban', 4);

                if (!%isTargetObserver)
                    messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                        "ToObserver", "", 'Force observer', 5);
            }
        }

        if (!%isTargetBot)
            messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                "SendMessage", "", 'Send Private Message', 15);

        if (%isTargetSelf || %outrankTarget)
        {
            if (%game.numTeams > 1)
            {
                if (%isTargetObserver)
                {
                    %action = (%isTargetSelf ? "Join " : "Change to ");
                    %str1 = %action @ getTaggedString(%game.getTeamName(1));
                    %str2 = %action @ getTaggedString(%game.getTeamName(2));

                    messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                        "ChangeTeam", "", %str1, 6);
                    messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                        "ChangeTeam", "", %str2, 7);
                }
                else
                {
                    %changeTo = (%targetClient.team == 1 ? 2 : 1);
                    %str = "Switch to " @ getTaggedString(
                        %game.getTeamName(%changeTo));
                    %caseId = 5 + %changeTo;

                    messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                        "ChangeTeam", "", %str, %caseId);
                }
            }
            else if (%isTargetObserver)
            {
                %str = (%isTargetSelf ? 'Join the Game' : 'Add to Game');
                messageClient(%client, 'MsgPlayerPopupItem', "", %key,
                    "JoinGame", "", %str, 8);
            }
        }
    }
}

//------------------------------------------------------------------------------
function DefaultGame::sendGameVoteMenu(%game, %client, %key)
{
    %isAdmin = (%client.isAdmin || %client.isSuperAdmin);
    %multipleTeams = (%game.numTeams > 1);

    if (!%game.voteTeamManagementOverrides)
    {
        if ($MatchStarted)
        {
            if (%client.team != 0)
            {
                if (%multipleTeams && !$Host::TournamentMode)
                    messageClient(%client, 'MsgVoteItem', "", %key,
                        'ChooseTeam', "", 'Change Teams');
                messageClient(%client, 'MsgVoteItem', "", %key,
                    'MakeObserver', "", 'Become an Observer');
            }
            else
            {
                if (!%multipleTeams && !$Host::TournamentMode)
                    messageClient(%client, 'MsgVoteItem', "", %key,
                        'JoinGame', "", 'Join the Game');
            }
        }
        else if (%multipleTeams)
        {
            if (%isAdmin)
            {
                messageClient(%client, 'MsgVoteItem', "", %key,
                    'ChooseTeam', "", 'Choose Team');
            }
            else if (!$Host::TournamentMode)
            {
                messageClient(%client, 'MsgVoteItem', "", %key,
                    'ChooseTeam', "", 'Change Teams');
            }
        }
    }

    if (%isAdmin)
    {
        if ($Host::TournamentMode && !$MatchStarted && !$CountdownStarted)
            messageClient(%client, 'MsgVoteItem', "", %key,
                'VoteMatchStart', '', 'Start Match');

        messageClient(%client, 'MsgVoteItem', "", %key,
            'VoteChangeMission', '', 'Change the Mission');

        messageClient(%client, 'MsgVoteItem', "", %key,
            'VoteChangeTimeLimit', '', 'Change the Time Limit');

        if (!$Host::TournamentMode)
            if ($TeamDamage)
                messageClient(%client, 'MsgVoteItem', "", %key,
                    'VoteTeamDamage', '', 'Disable Team Damage');
            else
                messageClient(%client, 'MsgVoteItem', "", %key,
                    'VoteTeamDamage', '', 'Enable Team Damage');

        if ($Host::TournamentMode)
            messageClient(%client, 'MsgVoteItem', "", %key,
                'VoteFFAMode', '', 'Free-for-all Mode');
        else
            messageClient(%client, 'MsgVoteItem', "", %key,
                'VoteTournamentMode', '', 'Tournament Mode');
    }
    // show options if we can vote and a vote is not already running
    else if (%client.canVote && Vote.scheduled $= "")
    {
        if ($Host::TournamentMode && !$MatchStarted && !$CountdownStarted)
            messageClient(%client, 'MsgVoteItem', "", %key,
                'VoteMatchStart', '', 'Vote to Start the Match');

        messageClient(%client, 'MsgVoteItem', "", %key,
            'VoteChangeMission', '', 'Vote to Change the Mission');

        if (!$Host::TournamentMode)
            if ($TeamDamage)
                messageClient(%client, 'MsgVoteItem', "", %key,
                    'VoteTeamDamage', '', 'Vote to Disable Team Damage');
            else
                messageClient(%client, 'MsgVoteItem', "", %key,
                    'VoteTeamDamage', '', 'Vote to Enable Team Damage');

        if ($Host::TournamentMode)
            messageClient(%client, 'MsgVoteItem', "", %key,
                'VoteFFAMode', '', 'Vote Free-for-all Mode');
    }
}

function DefaultGame::sendGameVoteMenuTail(%game, %client, %key)
{
    %isAdmin = (%client.isAdmin || %client.isSuperAdmin);
    if (!%isAdmin) return;

    %totalSlots = $Host::MaxPlayers - $HostGamePlayerCount + $HostGameBotCount;
    if ($HostGameBotCount > 0 && %totalSlots > 0)
        messageClient(%client, 'MsgVoteItem', "", %key,
            'Addbot', "", 'Add a Bot');

    if (%client.isSuperAdmin && $Host::AllowSuperAdminServerReboot)
    {
        messageClient(%client, 'MsgVoteItem', "", %key,
            'VoteRebootServer', '', 'Reboot the Server');
    }
}

//------------------------------------------------------------------------------
function DefaultGame::sendTimeLimitList(%game, %client, %key)
{
    messageClient(%client, 'MsgVoteItem', "", %key, 10, "", '10 minutes');
    messageClient(%client, 'MsgVoteItem', "", %key, 15, "", '15 minutes');
    messageClient(%client, 'MsgVoteItem', "", %key, 20, "", '20 minutes');
    messageClient(%client, 'MsgVoteItem', "", %key, 25, "", '25 minutes');
    messageClient(%client, 'MsgVoteItem', "", %key, 30, "", '30 minutes');
    messageClient(%client, 'MsgVoteItem', "", %key, 45, "", '45 minutes');
    messageClient(%client, 'MsgVoteItem', "", %key, 60, "", '60 minutes');
    messageClient(%client, 'MsgVoteItem', "", %key, 999, "", 'No time limit');
}

//------------------------------------------------------------------------------
function DefaultGame::isValidVote(%game, %client, %type,
        %arg1, %arg2, %arg3, %arg4)
{
    switch$ (%type)
    {
    case "VoteChangeMission":
        // verify mission and type pair is valid
        %missionId = %arg3;
        %missionType = %arg4;
        %found = false;
        for (%i = 0; %i < $HostMissionCount[%missionType]; %i++)
        {
            if ($HostMission[%missionType, %i] == %missionId)
            {
                %found = true;
                break;
            }
        }
        if (!%found) return false;

        // ensure client did not provide a map that does not support bots (if
        // we currently have bots)
        if ($HostGameBotCount > 0 && !$BotEnabled[%missionId])
            return false;

    case "VoteChangeTimeLimit":
        %newLimit = %arg1;
        if (%newLimit < 1)
            return false;

    case "VoteFfaMode":
        if (!$Host::TournamentMode)
            return false;

    case "VoteMatchStart":
        if (!$Host::TournamentMode || $MatchStarted || $CountdownStarted)
            return false;

    case "VoteTeamDamage":
        if ($Host::TournamentMode || %game.numTeams == 1)
            return false;

    case "VoteTournamentMode":
        if ($Host::TournamentMode || !(%client.isAdmin || %client.isSuperAdmin))
            return false;

    default:
        return false;
    }

    return true;
}

function DefaultGame::preprocessVote(%game, %client, %type,
        %arg1, %arg2, %arg3, %arg4)
{
    if (!%client.isAdmin && !%client.isSuperAdmin)
        return false;

    switch$ (%type)
    {
    case "VoteChangeMission":
        // abort any active vote on map change
        if (Vote.type !$= "")
            clearVotes();

        %missionDisplayName = %arg1;
        %typeDisplayName = %arg2;
        %mission = $HostMissionFile[%arg3];
        %missionType = $HostTypeName[%arg4];

        messageAll('MsgAdminChangeMission',
            '\c2An administrator has changed the mission to %1 (%2).',
            %missionDisplayName, %typeDisplayName);
        %game.gameOver();
        loadMission(%mission, %missionType, false);

    case "VoteChangeTimeLimit":
        // check if this request was pending a vote
        if (Vote.type $= "VoteChangeTimeLimit")
            clearVotes();

        %newLimit = %arg1;
        if (%newLimit >= 999)
            %display = "unlimited";
        else if (%newLimit != 1)
            %display = %newLimit SPC "minutes";
        else
            %display = %newLimit SPC "minute";

        messageAll('MsgAdminForce',
            '\c2An administrator has changed the mission time limit to %1.',
            %display);
        %game.handleNewTimeLimit(%newLimit);

    case "VoteFfaMode":
        // abort any active vote on map change
        if (Vote.type !$= "")
            clearVotes();

        messageAll('MsgAdminForce',
            '\c2An administrator has switched the server to free-for-all mode.');
        setModeFFA($CurrentMission, $CurrentMissionType);

    case "VoteMatchStart":
        // check if this request was pending a vote
        if (Vote.type $= "VoteMatchStart")
            clearVotes();

        %ready = forceTourneyMatchStart();
        if (%ready)
        {
            messageAll('MsgMissionStart',
                '\c2An administrator has forced the match to start.');
            startTourneyCountdown();
        }
        else
        {
            messageClient(%client, 'MsgClient', '\c2No players are ready yet.');
        }

    case "VoteTeamDamage":
        // check if this request was pending a vote
        if (Vote.type $= "VoteTeamDamage")
            clearVotes();

        if ($TeamDamage)
        {
            messageAll('MsgAdminForce',
                '\c2An administrator has disabled team damage.');
            $Host::TeamDamageOn = $TeamDamage = 0;
        }
        else
        {
            messageAll('MsgAdminForce',
                '\c2An administrator has enabled team damage.');
            $Host::TeamDamageOn = $TeamDamage = 1;
        }

    case "VoteTournamentMode":
        // abort any active vote on map change
        if (Vote.type !$= "")
            clearVotes();

        %missionDisplayName = %arg1;
        %typeDisplayName = %arg2;
        %mission = $HostMissionFile[%arg3];
        %missionType = $HostTypeName[%arg4];

        messageAll('MsgAdminChangeMission',
            '\c2An administrator has switched the server to tournament mode (%1; %2).',
            %missionDisplayName, %typeDisplayName);
        setModeTournament(%mission, %missionType);
    }

    return true;
}

function DefaultGame::notifyVote(%game, %client, %origin, %type,
        %arg1, %arg2, %arg3, %arg4)
{
    switch$ (%type)
    {
    case "VoteChangeMission":
        %missionDisplayName = %arg1;
        %typeDisplayName = %arg2;

        messageClient(%client, 'VoteStarted',
            '\c2%1 initiated a vote to change the mission to %2 (%3).',
            %origin.name, %missionDisplayName, %typeDisplayName);

    case "VoteChangeTimeLimit":
        %newLimit = %arg1;
        if (%newLimit >= 999)
            %display = "unlimited";
        else if (%newLimit != 1)
            %display = %newLimit SPC "minutes";
        else
            %display = %newLimit SPC "minute";

        messageClient(%client, 'VoteStarted',
            '\c2%1 initiated a vote to change the mission time limit to %1.',
            %origin.name, %display);

    case "VoteFfaMode":
        messageClient(%client, 'VoteStarted',
            '\c2%1 initiated a vote to switch to free-for-all mode.',
            %origin.name);

    case "VoteMatchStart":
        messageClient(%client, 'VoteStarted',
            '\c2%1 initiated a vote to start the match.',
            %origin.name);

    case "VoteTeamDamage":
        if ($TeamDamage)
            messageClient(%client, 'VoteStarted',
                '\c2%1 initiated a vote to disable team damage.',
                %origin.name);
        else
            messageClient(%client, 'VoteStarted',
                '\c2%1 initiated a vote to enable team damage.',
                %origin.name);

    case "VoteTournamentMode":
        %missionDisplayName = %arg1;
        %typeDisplayName = %arg2;

        messageClient(%client, 'VoteStarted',
            '\c2%1 initiated a vote to switch to tournament mode (%2; %3).',
            %origin.name, %missionDisplayName, %typeDisplayName);

    default:
        error("unhandled vote-type in DefaultGame::notifyVote");
    }
}

function DefaultGame::processVote(%game, %client, %type, %passed, %percentage,
            %arg1, %arg2, %arg3, %arg4)
{
    switch$ (%type)
    {
    case "VoteChangeMission":
        %missionDisplayName = %arg1;
        %typeDisplayName = %arg2;
        %mission = $HostMissionFile[%arg3];
        %missionType = $HostTypeName[%arg4];

        if (%passed)
        {
            messageAll('MsgVotePassed',
                '\c2The mission was changed to %1 (%2) by vote.',
                %missionDisplayName, %typeDisplayName);
            %game.gameOver();
            loadMission(%mission, %missionType, false);
        }
        else
            messageAll('MsgVoteFailed',
                "\c2Vote to change the mission did not pass (" @ %percentage @ "\%).");

    case "VoteChangeTimeLimit":
        %newLimit = %arg1;
        if (%newLimit >= 999)
            %display = "unlimited";
        else if (%newLimit != 1)
            %display = %newLimit SPC "minutes";
        else
            %display = %newLimit SPC "minute";

        if (%passed)
        {
            messageAll('MsgVotePassed',
                '\c2The mission time limit was set to %1 by vote.',
                %display);
            %game.handleNewTimeLimit(%newLimit);
        }
        else
            messageAll('MsgVoteFailed',
                "\c2Vote to change the mission time limit did not pass (" @ %percentage @ "\%).");

    case "VoteFfaMode":
        // drop vote if we somehow already moved out of tournament mode
        if (!$Host::TournamentMode)
            return false;

        if (%passed)
        {
            messageAll('MsgVotePassed',
                '\c2Server switched to free-for-all mode by vote.');
            setModeFFA($CurrentMission, $CurrentMissionType);
        }
        else
            messageAll('MsgVoteFailed',
                "\c2Vote to switch to free-for-all mode did not pass (" @ %percentage @ "\%).");

    case "VoteMatchStart":
        // drop vote if we somehow already moved out of tournament mode or the
        // match has already started/in-countdown
        if (!$Host::TournamentMode || $MatchStarted || $CountdownStarted)
            return false;

        if (%passed)
        {
            %ready = forceTourneyMatchStart();
            if (%ready)
            {
                messageAll('MsgMissionStart',
                    "\c2The match has been started by vote (" @ %percentage @ "\%).");
                startTourneyCountdown();
            }
            else
            {
                messageClient(%client, 'MsgClient',
                    '\c2Vote passed to start the match, but no players are ready yet.');
            }
        }
        else
            messageAll('MsgVoteFailed',
                "\c2Vote to start the match did not pass (" @ %percentage @ "\%).");

    case "VoteTeamDamage":
        if (%passed)
        {
            if ($TeamDamage)
            {
                messageAll('MsgVotePassed',
                    '\c2Team damage was disabled by vote.');
                $Host::TeamDamageOn = $TeamDamage = 0;
            }
            else
            {
                messageAll('MsgVotePassed',
                    '\c2Team damage was enabled by vote.');
                $Host::TeamDamageOn = $TeamDamage = 1;
            }
        }
        else
        {
            if ($TeamDamage)
            {
                messageAll('MsgVoteFailed',
                    "\c2Vote to disable team damage did not pass (" @ %percentage @ "\%).");
            }
            else
            {
                messageAll('MsgVoteFailed',
                    "\c2Vote to enable team damage did not pass (" @ %percentage @ "\%).");
            }
        }

    case "VoteTournamentMode":
        %mission = $HostMissionFile[%arg3];
        %missionType = $HostTypeName[%arg4];

        if (%passed)
        {
            messageAll('MsgVotePassed',
                '\c2Server switched to tournament mode by vote.');
            setModeTournament(%mission, %missionType);
        }
        else
            messageAll('MsgVoteFailed',
                "\c2Vote to change to tournament mode did not pass (" @ %percentage @ "\%).");
    }
}

//------------------------------------------------------------------------------
function DefaultGame::handleNewTimeLimit(%game, %newLimit)
{
    $Host::TimeLimit = %newLimit;

    // ignore if a new time limit occurred when the match has yet to be started
    if (!$MatchStarted)
        return;

    // schedule the end of match countdown
    %elapsedTimeMs = getSimTime() - $MissionStartTime;
    %curTimeLeftMs = ($Host::TimeLimit * 60 * 1000) - %elapsedTimeMs;
    cancelEndCountdown();
    endCountdown(%curTimeLeftMs);
    cancel(%game.timeSync);
    %game.checkTimeLimit(true);
}

//------------------------------------------------------------------------------
function DefaultGame::processGameLink(%game, %client, %arg1, %arg2, %arg3, %arg4, %arg5)
{
    // the default behavior when clicking on a game link is to start
    // observing that client
    %targetClient = %arg1;
    if (%client.team == 0 && isObject(%targetClient) && %targetClient.team != 0)
    {
        %prevObsClient = %client.observeClient;

        // update the observer list for this client
        observerFollowUpdate(%client, %targetClient, %prevObsClient !$= "");

        serverCmdObserveClient(%client, %targetClient);
        displayObserverHud(%client, %targetClient);

        if (%targetClient != %prevObsClient)
        {
            messageClient(%targetClient, 'Observer',
                '\c1%1 is now observing you.', %client.name);
            messageClient(%prevObsClient, 'ObserverEnd',
                '\c1%1 is no longer observing you.', %client.name);
        }
    }
}

//------------------------------------------------------------------------------
$ScoreHudMaxVisible = 19;
function DefaultGame::updateScoreHud(%game, %client, %tag)
{
    if (Game.numTeams > 1)
    {
        // Send header:
        messageClient(%client, 'SetScoreHudHeader', "",
            '<tab:15,315>\t%1<rmargin:260><just:right>%2<rmargin:560><just:left>\t%3<just:right>%4',
            %game.getTeamName(1), $TeamScore[1],
            %game.getTeamName(2), $TeamScore[2]);

        // Send subheader:
        messageClient(%client, 'SetScoreHudSubheader', "",
            '<tab:15,315>\tPLAYERS (%1)<rmargin:260><just:right>SCORE<rmargin:560><just:left>\tPLAYERS (%2)<just:right>SCORE',
            $TeamRank[1, count], $TeamRank[2, count]);

        %index = 0;
        while (true)
        {
            if (%index >= $TeamRank[1, count]+2 && %index >= $TeamRank[2, count]+2)
                break;

            // get the team1 client info
            %team1Client = "";
            %team1ClientScore = "";
            %col1Style = "";
            if (%index < $TeamRank[1, count])
            {
                %team1Client = $TeamRank[1, %index];
                %team1ClientScore = %team1Client.score $= "" ? 0 : %team1Client.score;
                %col1Style = %team1Client == %client ? "<color:dcdcdc>" : "";
                %team1playersTotalScore += %team1Client.score;
            }
            else if (%index == $teamRank[1, count] &&
                    $teamRank[1, count] != 0 && %game.class $= "CTFGame")
            {
                %team1ClientScore = "--------------";
            }
            else if (%index == $teamRank[1, count]+1 &&
                    $teamRank[1, count] != 0 && %game.class $= "CTFGame")
            {
                %team1ClientScore = %team1playersTotalScore != 0 ? %team1playersTotalScore : 0;
            }

            // get the team2 client info
            %team2Client = "";
            %team2ClientScore = "";
            %col2Style = "";
            if (%index < $TeamRank[2, count])
            {
                %team2Client = $TeamRank[2, %index];
                %team2ClientScore = %team2Client.score $= "" ? 0 : %team2Client.score;
                %col2Style = %team2Client == %client ? "<color:dcdcdc>" : "";
                %team2playersTotalScore += %team2Client.score;
            }
            else if (%index == $teamRank[2, count] &&
                    $teamRank[2, count] != 0 && %game.class $= "CTFGame")
            {
                %team2ClientScore = "--------------";
            }
            else if (%index == $teamRank[2, count]+1 &&
                    $teamRank[2, count] != 0 && %game.class $= "CTFGame")
            {
                %team2ClientScore = %team2playersTotalScore != 0 ? %team2playersTotalScore : 0;
            }

            // if the client is not an observer, send the message
            if (%client.team != 0)
            {
                messageClient(%client, 'SetLineHud', "", %tag, %index,
                    '<tab:20,320>\t<spush>%5<clip:200>%1</clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:200>%3</clip><just:right>%4',
                    %team1Client.name, %team1ClientScore,
                    %team2Client.name, %team2ClientScore,
                    %col1Style, %col2Style);
            }
            // else for observers, create an anchor around the player name
            // so they can be observed
            else
            {
                messageClient(%client, 'SetLineHud', "", %tag, %index,
                    '<tab:20,320>\t<spush>%5<clip:200><a:gamelink\t%7>%1</a></clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:200><a:gamelink\t%8>%3</a></clip><just:right>%4',
                    %team1Client.name, %team1ClientScore,
                    %team2Client.name, %team2ClientScore,
                    %col1Style, %col2Style, %team1Client, %team2Client);
            }

            %index++;
        }
    }
    else
    {
        // tricky stuff here... use two columns if we have more
        // than 15 clients...
        %numClients = $TeamRank[0, count];
        if (%numClients > $ScoreHudMaxVisible)
            %numColumns = 2;

        // Clear header:
        messageClient(%client, 'SetScoreHudHeader', "", "");

        // Send header:
        if (%numColumns == 2)
            messageClient(%client, 'SetScoreHudSubheader', "",
                '<tab:15,315>\tPLAYER<rmargin:270><just:right>SCORE<rmargin:570><just:left>\tPLAYER<just:right>SCORE');
        else
            messageClient(%client, 'SetScoreHudSubheader', "",
                '<tab:15>\tPLAYER<rmargin:270><just:right>SCORE');

        %countMax = %numClients;
        if (%countMax > (2 * $ScoreHudMaxVisible))
        {
            if (%countMax & 1)
                %countMax++;
            %countMax = %countMax / 2;
        }
        else if (%countMax > $ScoreHudMaxVisible)
            %countMax = $ScoreHudMaxVisible;

        for (%index = 0; %index < %countMax; %index++)
        {
            // get the client info
            %col1Client = $TeamRank[0, %index];
            %col1ClientScore = (%col1Client.score $= "" ? 0 : %col1Client.score);
            %col1Style = (%col1Client == %client ? "<color:dcdcdc>" : "");

            // see if we have two columns
            if (%numColumns == 2)
            {
                %col2Client = "";
                %col2ClientScore = "";
                %col2Style = "";

                // get the column 2 client info
                %col2Index = %index + %countMax;
                if (%col2Index < %numClients)
                {
                    %col2Client = $TeamRank[0, %col2Index];
                    %col2ClientScore = (%col2Client.score $= "" ? 0 : %col2Client.score);
                    %col2Style = (%col2Client == %client ? "<color:dcdcdc>" : "");
                }
            }

            // if the client is not an observer, send the message
            if (%client.team != 0)
            {
                if (%numColumns == 2)
                    messageClient(%client, 'SetLineHud', "", %tag, %index,
                        '<tab:25,325>\t<spush>%5<clip:195>%1</clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:195>%3</clip><just:right>%4',
                        %col1Client.name, %col1ClientScore, %col2Client.name, %col2ClientScore, %col1Style, %col2Style);
                else
                    messageClient(%client, 'SetLineHud', "", %tag, %index,
                        '<tab:25>\t%3<clip:195>%1</clip><rmargin:260><just:right>%2',
                        %col1Client.name, %col1ClientScore, %col1Style);
            }
            // else for observers, create an anchor around the player name
            // so they can be observed
            else
            {
                if (%numColumns == 2)
                    messageClient(%client, 'SetLineHud', "", %tag, %index,
                        '<tab:25,325>\t<spush>%5<clip:195><a:gamelink\t%7>%1</a></clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:195><a:gamelink\t%8>%3</a></clip><just:right>%4',
                        %col1Client.name, %col1ClientScore,
                        %col2Client.name, %col2ClientScore,
                        %col1Style, %col2Style, %col1Client, %col2Client);
                else
                    messageClient(%client, 'SetLineHud', "", %tag, %index,
                        '<tab:25>\t%3<clip:195><a:gamelink\t%4>%1</a></clip><rmargin:260><just:right>%2',
                        %col1Client.name, %col1ClientScore,
                        %col1Style, %col1Client);
            }
        }
    }

    // Tack on the list of observers:
    %observerCount = 0;
    for (%i = 0; %i < ClientGroup.getCount(); %i++)
    {
        %cl = ClientGroup.getObject(%i);
        if (%cl.team == 0)
            %observerCount++;
    }

    if (%observerCount > 0)
    {
        messageClient(%client, 'SetLineHud', "", %tag, %index, "");
        %index++;
        messageClient(%client, 'SetLineHud', "", %tag, %index,
            '<tab:10, 310><spush><font:Univers Condensed:22>\tOBSERVERS (%1)<rmargin:260><just:right>TIME<spop>',
            %observerCount);
        %index++;
        for (%i = 0; %i < ClientGroup.getCount(); %i++)
        {
            %cl = ClientGroup.getObject(%i);
            //if this is an observer
            if (%cl.team == 0)
            {
                %obsTime = getSimTime() - %cl.observerStartTime;
                %obsTimeStr = %game.formatTime(%obsTime, false);
                messageClient(%client, 'SetLineHud', "", %tag, %index,
                    '<tab:20, 310>\t<clip:150>%1</clip><rmargin:260><just:right>%2',
                    %cl.name, %obsTimeStr);
                %index++;
            }
        }
    }

    //clear the rest of Hud so we don't get old lines hanging around...
    messageClient(%client, 'ClearHud', "", %tag, %index);
}

//------------------------------------------------------------------------------
function UpdateClientTimes(%time)
{
    %secondsLeft = %time / 1000;
    messageAll('MsgSystemClock', "", (%secondsLeft / 60), %time);
}

//------------------------------------------------------------------------------
function notifyMatchStart(%time)
{
    %seconds = mFloor(%time / 1000);
    if (%seconds > 2)
        messageAll('MsgMissionStart',
            '\c2Match starts in %1 seconds.~wfx/misc/hunters_%1.wav', %seconds);
    else if (%seconds == 2)
        messageAll('MsgMissionStart',
            '\c2Match starts in 2 seconds.~wvoice/announcer/ann.match_begins.wav');
    else if (%seconds == 1)
        messageAll('MsgMissionStart', '\c2Match starts in 1 second.');
    UpdateClientTimes(%time);
}

//------------------------------------------------------------------------------
function notifyMatchEnd(%time)
{
    %seconds = mFloor(%time / 1000);
    if (%seconds > 1)
        messageAll('MsgMissionEnd',
            '\c2Match ends in %1 seconds.~wfx/misc/hunters_%1.wav', %seconds);
    else if (%seconds == 1)
        messageAll('MsgMissionEnd',
            '\c2Match ends in 1 second.~wfx/misc/hunters_1.wav');
    UpdateClientTimes(%time);
}

function DefaultGame::formatTime(%game, %tStr, %includeHundredths)
{
    %timeInSeconds = %tStr / 1000;
    %mins = mFloor(%timeInSeconds / 60);
    if (%mins < 1)
        %timeString = "00:";
    else if (%mins < 10)
        %timeString = "0" @ %mins @ ":";
    else
        %timeString = %mins @ ":";

    %timeInSeconds -= (%mins * 60);
    %secs = mFloor(%timeInSeconds);
    if (%secs < 1)
        %timeString = %timeString @ "00";
    else if (%secs < 10)
        %timeString = %timeString @ "0" @ %secs;
    else
        %timeString = %timeString @ %secs;

    if (%includeHundredths)
    {
        %timeString = %timeString @ ".";
        %timeInSeconds -= %secs;
        %hSecs = mFloor(%timeInSeconds * 100); // will be between 0 and 999
        if (%hSecs < 1)
            %timeString = %timeString @ "00";
        else if (%hSecs < 10)
            %timeString = %timeString @ "0" @ %hSecs;
        else
            %timeString = %timeString @ %hSecs;
    }

    return %timeString;
}

//------------------------------------------------------------------------------
//AI FUNCTIONS
function DefaultGame::AIChooseGameObjective(%game, %client)
{
    AIChooseObjective(%client);
}

//------------------------------------------------------------------------------
function DefaultGame::getServerStatusString(%game)
{
    %status = %game.numTeams;
    for (%team = 1; %team - 1 < %game.numTeams; %team++)
    {
        %score = isObject($teamScore[%team]) ? $teamScore[%team] : 0;
        %teamStr = getTaggedString(%game.getTeamName(%team)) TAB %score;
        %status = %status NL %teamStr;
    }

    %status = %status NL ClientGroup.getCount();
    for (%i = 0; %i < ClientGroup.getCount(); %i++)
    {
        %cl = ClientGroup.getObject(%i);
        %score = %cl.score $= "" ? 0 : %cl.score;
        %playerStr = getTaggedString(%cl.name) TAB
            getTaggedString(%game.getTeamName(%cl.team)) TAB %score;
        %status = %status NL %playerStr;
    }

    return %status;
}

//------------------------------------------------------------------------------
function DefaultGame::endMission(%game)
{
}

//------------------------------------------------------------------------------
// z0dd - ZOD. Console spam fix
function DefaultGame::countFlips(%game)
{
    return false;
}

////////////////////////////////////////////////////////////////////////////////
// Random Teams code by Founder (founder@mechina.com) 6/22/02 //////////////////
////////////////////////////////////////////////////////////////////////////////

function DefaultGame::setupClientTeams(%game)
{
    if (!$Host::ClassicRandomizeTeams || %game.numTeams == 1)
    {
        %count = ClientGroup.getCount();
        for (%i = 0; %i < %count; %i++)
        {
            %client = ClientGroup.getObject(%i);
            %client.lastTeam = %client.team;
            %client.setupTeam = 0;
        }

        return;
    }
    else
    {
        %numTeamPlayers = 0;
        %totalNumPlayers = ClientGroup.getCount();
        for (%i = 0; %i < %totalNumPlayers; %i++)
        {
            %cl = ClientGroup.getObject(%i);
            if (%cl.team == 0)
                %cl.lastTeam = %cl.team;
            else
            {
                %teamPlayer[%numTeamPlayers] = %cl;
                %numTeamPlayers++;
            }
        }
        %numPlayersLeft = %numTeamPlayers - 1;
        for (%j = 0; %j < %numTeamPlayers; %j++)
        {
            if (%numPlayersLeft > 0)
            {
                %r = 0;
                %val = mFloor(getRandom(0, %numPlayersLeft));
                if (%val > %numPlayersLeft)
                    %val = %numPlayersLeft;

                %client = %teamPlayer[%val];
                %shuffledPlayersArray[%j] = %client;
                for (%y = 0; %y <= %numPlayersLeft; %y++)
                {
                    %clplyr = %teamPlayer[%y];
                    if (%clplyr != %client)
                    {
                        %teamPlayer[%r] = %clplyr;
                        %r++;
                    }
                }
                %numPlayersLeft--;
            }
            else
                %shuffledPlayersArray[%j] = %teamPlayer[%numPlayersLeft];
        }
        %thisTeam = 1;
        for (%k = 0; %k <= %numTeamPlayers; %k++)
        {
            if (%thisTeam == 1)
            {
                %shuffledPlayersArray[%k].lastTeam = 1;
                %thisTeam = 0;
            }
            else
            {
                %shuffledPlayersArray[%k].lastTeam = 2;
                %thisTeam = 1;
            }
        }
    }
}
