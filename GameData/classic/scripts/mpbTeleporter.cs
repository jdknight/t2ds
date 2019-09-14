////////////////////////////////////////////////////////////////////////////////
//                    Mobile Point Base Teleporter                            //
//                       z0dd - ZOD, 4/24/02                                  //
////////////////////////////////////////////////////////////////////////////////

datablock StaticShapeData(MPBTeleporter) : StaticShapeDamageProfile
{
    className = Station;
    catagory = "Stations";
    shapeFile = "station_teleport.dts";
    maxDamage = 1.20;
    destroyedLevel = 1.20;
    disabledLevel = 0.84;
    explosion = ShapeExplosion;
    expDmgRadius = 10.0;
    expDamage = 0.4;
    expImpulse = 1500.0;
    dynamicType = $TypeMasks::StationObjectType;
    isShielded = true;
    energyPerDamagePoint = 33;
    maxEnergy = 250;
    rechargeRate = 0.31;
    humSound = StationVehicleHumSound;
    // don't let these be damaged in Siege missions
    noDamageInSiege = true;
    cmdCategory = "Support";
    cmdIcon = CMDVehicleStationIcon;
    cmdMiniIconName = "commander/MiniIcons/com_vehicle_pad_inventory";
    targetNameTag = 'MPB';
    targetTypeTag = 'Teleport Station';
    teleporter = 1;
};

datablock ParticleData(mpbteleportparticle)
{
    dragCoefficient = 1.5;
    gravityCoefficient = 0.2;
    inheritedVelFactor = 0.2;
    constantAcceleration = 0.0;
    lifetimeMS = 1000;
    lifetimeVarianceMS = 0;
    textureName = "particleTest";

    colors[0] = "0.06 0.06 0.36 1.0";
    colors[1] = "0.06 0.06 0.36 0.0";
    sizes[0] = 0.65;
    sizes[1] = 0.30;
};

datablock ParticleEmitterData(MPBTeleportEmitter)
{
    ejectionPeriodMS = 5;
    periodVarianceMS = 0;
    ejectionVelocity = 1.1;
    velocityVariance = 1.0;
    ejectionOffset = 2.0;
    thetaMin = 0.0;
    thetaMax = 10.0;
    phiReferenceVel = 0.0;
    phiVariance = 360.0;
    overrideAdvances = false;
    particles = "mpbteleportparticle";
};

function StationVehicle::createTeleporter(%data, %obj, %group)
{
    // %obj = Teleport Object
    // %data = Teleport Datablock
    // %group = Vehicle Pads Mission Group

    %Teleporter = new StaticShape()
    {
        scale = "1 1 1";
        dataBlock = "MPBTeleporter";
        lockCount = "0";
        homingCount = "0";
        team = %obj.team;
    };
    %obj.teleporter = %Teleporter;
    %Teleporter.vStation = %obj.pad;

    %trans = %obj.pad.getSlotTransform(0);
    %vSPos = getWords(%trans,0,2);
    %vRot =  getWords(%trans,3,5);
    %vAngle = getWord(%trans,6);
    %matrix = VectorOrthoBasis(%vRot @ " " @ %vAngle + 0.36);
    %yRot = getWords(%matrix, 3, 5);
    %pos = vectorAdd(%vSPos, vectorScale(%yRot, -31.5));
    %Teleporter.setTransform(%pos @ " " @ %vRot @ " " @ %vAngle);

    // Add the teleporter to the v-pads mission group for cleanup and power.
    %group.add(%Teleporter);
    %Teleporter.setPersistent(false); // set the teleporter to not save in the editor.

    // Apparently called to early on mission load done, call it now.
    %Teleporter.getDataBlock().gainPower(%Teleporter);

    // Set the sensor group.
    if (%Teleporter.getTarget() != -1)
        setTargetSensorGroup(%Teleporter.getTarget(), %obj.team);

    // Allow players to use it.
    %Teleporter.disabled = 0;
}

function MPBTeleporter::onCollision(%data, %obj, %col)
{
    if (%col.getDataBlock().className !$= "Armor" || %col.getState() $= "Dead" || %col.teleporting)
        return;

    if (isObject(%col))
    {
        if (%obj.team == %col.client.team)
        {
            if (!%obj.isDisabled())
            {
                if (%obj.isPowered())
                {
                    if (isObject(%obj.MPB) && %obj.MPB.fullyDeployed)
                    {
                        if (%obj.disabled == 0)
                        {
                            %col.lastWeapon = (%col.getMountedImage($WeaponSlot) == 0) ? "" : %col.getMountedImage($WeaponSlot).getName().item;
                            %col.unmountImage($WeaponSlot);
                            %pos = %obj.position;
                            %col.setvelocity("0 0 0");
                            %col.setMoveState(true);
                            %rot = getWords(%col.getTransform(), 3, 6);
                            %col.setTransform(getWord(%pos,0) @ " " @ getWord(%pos,1) @ " " @ getWord(%pos,2) + 0.6 @ " " @ %rot);
                            %col.teleporting = 1;
                            %col.startFade(1000, 0, true);
                            %col.playAudio($PlaySound, StationVehicleAcitvateSound);

                            %obj.disabled = 1; // Disable the teleporter to more then one person at a time for a time.
                            %obj.setThreadDir($ActivateThread, TRUE);
                            %obj.playThread($ActivateThread, "activate");

                            %data.sparkEmitter(%obj);
                            %data.schedule(2000, "teleportout", %obj, %col);
                            %data.schedule(4000, "teleportingDone", %obj, %col);
                        }
                        else
                            messageClient(%col.client, 'MsgTeleportRecharging', '\c2Teleporter is recharging please stand by. ~wfx/powered/nexus_deny.wav');
                    }
                    else
                        messageClient(%col.client, "MsgNoMPB", 'MPB is not deployed.');
                }
                else
                    messageClient(%col.client, 'MsgStationNoPower', '\c2Teleporter is not powered.');
            }
            else
                messageClient(%col.client, 'MsgStationDisabled', '\c2Teleporter is disabled.');
        }
        else
            messageClient(%col.client, 'MsgStationDenied', '\c2Access Denied -- Wrong team.~wfx/powered/station_denied.wav');
    }
    else
        return;
}

function MPBTeleporter::teleportOut(%data, %obj, %player)
{
    if (isObject(%obj.MPB))
    {
        %index = -1;
        for (%x=0; %x < %obj.MPB.spawnPosCount; %x++)
        {
            %index = mFloor(getRandom() * %obj.MPB.spawnPosCount);
            InitContainerRadiusSearch(%obj.MPB.spawnPos[%index], 2, $TypeMasks::MoveableObjectType);
            if (ContainerSearchNext() == 0)
                break;
            else
                %index = -1;
        }
        if (%index >= 0)
        {
            %player.setTransform(%obj.MPB.spawnPos[%index] @ " " @ getWords(%obj.MPB.getTransform(), 3, 6));
        }
        else
        {
            messageClient(%player.client, 'MsgTeleFailed', 'No Valid teleporting positions.');
            %player.teleporting = 0;
        }
    }
    else
    {
        messageClient(%player.client, 'MsgTeleFailed', 'No Valid teleporting positions because MPB was destroyed');
        %player.teleporting = 0;
    }
    %data.schedule(1000, "teleportIn", %player);
}

function MPBTeleporter::teleportIn(%data, %player)
{
    %data.sparkEmitter(%player); // z0dd - ZOD, 4/24/02. teleport sparkles
    %player.startFade(1000, 0, false);
    %player.playAudio($PlaySound, StationVehicleDeactivateSound);
}

function MPBTeleporter::reEnable(%data, %obj)
{
    %obj.disabled = 0;
}

function MPBTeleporter::sparkEmitter(%data, %obj)
{
    if (isObject(%obj.teleportEmitter))
        %obj.teleportEmitter.delete();

    %obj.teleportEmitter = new ParticleEmissionDummy()
    {
        position = %obj.position;
        rotation = "1 0 0 0";
        scale = "1 1 1";
        dataBlock = defaultEmissionDummy;
        emitter = "MPBTeleportEmitter";
        velocity = "1";
    };
    MissionCleanup.add(%obj.teleportEmitter);
    %obj.teleportEmitter.schedule(800, "delete");

    if (isObject(%obj.teleEmitter))
        %obj.teleEmitter.delete();

    %obj.teleEmitter = new ParticleEmissionDummy()
    {
        position = %obj.position;
        rotation = "1 0 0 0";
        scale = "1 1 1";
        dataBlock = defaultEmissionDummy;
        emitter = "FlyerJetEmitter";
        velocity = "1";
    };
    MissionCleanup.add(%obj.teleEmitter);
    %obj.teleEmitter.schedule(700, "delete");
}

function MPBTeleporter::teleportingDone(%data, %obj, %player)
{
    %player.setMoveState(false);
    %player.teleporting = 0;
    %player.station = "";
    %data.reEnable(%obj);
    if (%player.getMountedImage($WeaponSlot) == 0)
    {
        if (%player.inv[%player.lastWeapon])
            %player.use(%player.lastWeapon);
        else
            %player.selectWeaponSlot(0);
    }
}

//------------------------------------------------------------------------------------------
// Gets called from function MobileBaseVehicle::vehicleDeploy(%data, %obj, %player, %force).
// Passes this information to the MPBTeleporter::teleportOut function.
//------------------------------------------------------------------------------------------

function checkSpawnPos(%MPB, %radius)
{
    for (%y = -1; %y < 1; %y += 0.25)
    {
        %xCount=0;
        for (%x = -1; %x < 1; %x += 0.25)
        {
            $MPBSpawnPos[(%yCount * 8) + %xCount] = %x @ " " @ %y;
            %xCount++;
        }
        %yCount++;
    }
    %count = -1;

    for (%x = 0; %x < 64; %x++)
    {
        %pPos = getWords(%MPB.getTransform(), 0, 2);
        %pPosX = getWord(%pPos, 0);
        %pPosY = getWord(%pPos, 1);
        %pPosZ = getWord(%pPos, 2);

        %posX = %pPosX + (getWord($MPBSpawnPos[%x],0) * %radius);
        %posY = %pPosY + (getWord($MPBSpawnPos[%x],1) * %radius);

        %terrHeight = getTerrainHeight(%posX @ " " @ %posY);

        if (mAbs(%terrHeight - %pPosZ) < %radius)
        {
            %mask = $TypeMasks::VehicleObjectType |
                $TypeMasks::MoveableObjectType    |
                $TypeMasks::StaticShapeObjectType |
                $TypeMasks::StaticTSObjectType    |
                $TypeMasks::ForceFieldObjectType  |
                $TypeMasks::ItemObjectType        |
                $TypeMasks::PlayerObjectType      |
                $TypeMasks::TurretObjectType      |
                $TypeMasks::InteriorObjectType;

            InitContainerRadiusSearch(%posX @ " " @ %posY @ " " @ %terrHeight, 2, %mask);
            if (ContainerSearchNext() == 0)
                %MPB.spawnPos[%count++] = %posX @ " " @ %posY @ " " @ %terrHeight;
        }
    }

    %MPB.spawnPosCount = %count;
}
