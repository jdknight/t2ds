//------------------------------------------------------------------------------
// Object control
//------------------------------------------------------------------------------
function getControlObjectType(%obj)
{
   // turrets (camera is a turret)
    if (%obj.getType() & $TypeMasks::TurretObjectType)
    {
        %barrel = %obj.getMountedImage(0);
        if (isObject(%barrel))
            return addTaggedString(%barrel.getName());
    }

    // unknown
    return 'Unknown';
}

function serverCmdControlObject(%client, %targetId)
{
    // match started:
    if (!$MatchStarted)
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "mission has not started.");
        return;
    }

    // object:
    %obj = getTargetObject(%targetId);
    if (%obj == -1)
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "failed to find target object.");
        return;
    }

    // shapebase:
    if (!(%obj.getType() & $TypeMasks::ShapeBaseObjectType))
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "object cannot be controlled.");
        return;
    }

    // can control:
    if (!%obj.getDataBlock().canControl || %obj.getMountedImage(0).getName() $= "MissileBarrelLarge") // z0dd - ZOD 4/18/02. Prevent missile barrels from being controlled
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "object cannot be controlled.");
        return;
    }

    // check damage:
    if (%obj.getDamageState() !$= "Enabled")
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "object is " @ %obj.getDamageState());
        return;
    }

    // powered:
    if (!%obj.isPowered())
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "object is not powered.");
        return;
    }

    // controlled already:
    %control = %obj.getControllingClient();
    if (%control)
    {
        if (%control == %client)
            commandToClient(%client, 'ControlObjectResponse', false,
                "you are already controlling that object.");
        else
            commandToClient(%client, 'ControlObjectResponse', false,
                "someone is already controlling that object.");
        return;
    }

    // same team?
    if (getTargetSensorGroup(%targetId) != %client.getSensorGroup())
    {
        commandToClient(%client, 'ControlObjectResonse', false,
            "cannot control enemy objects.");
        return;
    }

    // dead?
    if (%client.player == 0)
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "dead people cannot control objects.");
        return;
    }

    //mounted in a vehicle?
    if (%client.player.isMounted())
    {
        commandToClient(%client, 'ControlObjectResponse', false,
            "can't control objects while mounted in a vehicle.");
        return;
    }

    %client.setControlObject(%obj);
    commandToClient(%client, 'ControlObjectResponse', true, getControlObjectType(%obj));

    // --------------------------------------------------------------------------------------------------------
    // z0dd - ZOD, 5/12/02. Change turrets name to controllers name.
    if ((%obj.getType() & $TypeMasks::TurretObjectType) && (!%client.isAIControlled()))
    {
        // Set this variable on the client so we can reset turret nameTag when client is done.
        %client.TurretControl = %obj;

        if (%obj.nameTag !$= "") // Get the name tag for storage, this is created in the *.mis file.
        {
            %obj.oldTag = getTaggedString(%obj.nameTag); // Store this nameTag in a var on the turret.
            removeTaggedString(%obj.nameTag); // Reset the turrets nameTag.
            %obj.nameTag = "";
        }
        else // This is either a deployed turret or the *.mis file has no nameTag for it.
        {
            %obj.oldTag = ""; // No nameTag to store on turret (paranoia).

            // Reset the turrets targetNameTag. This may cause problems - ZOD
            //removeTaggedString(%obj.getDataBlock().targetNameTag);
            //%obj.getDataBlock().targetNameTag = "";
        }

        // Reset the turrets target
        freeTarget(%obj.getTarget());

        // Set the turrets target and new nameTag.
        %obj.nameTag = addTaggedString(%client.nameBase @ " controlling ");
        %obj.target = createTarget(%obj, %obj.nameTag, "", "",
            %obj.getDatablock().targetTypeTag, %obj.team, 0);
        setTargetSensorGroup(%obj.target, %obj.team);
    }
    // --------------------------------------------------------------------------------------------------------
}

//------------------------------------------------------------------------------
// TV Functions
//------------------------------------------------------------------------------
function resetControlObject(%client)
{
    if (isObject(%client.comCam))
        %client.comCam.delete();

    if (isObject(%client.player) && !%client.player.isDestroyed() && $MatchStarted)
        %client.setControlObject(%client.player);
    else
        %client.setControlObject(%client.camera);

    // -------------------------------------------------------------------------
    // z0dd - ZOD, 5/12/02. Reset the turrets nameTag back to its original.
    if (%client.TurretControl !$= "")
        %turret = %client.TurretControl;
    else
        return;

    if (isObject(%turret))
    {
        // Reset the turrets target and nameTag
        removeTaggedString(%turret.nameTag);
        %turret.nameTag = "";
        freeTarget(%turret.getTarget());

        // Set the turrets target and new nameTag
        if (%turret.oldTag !$= "")
            %turret.nameTag = addTaggedString(%turret.oldTag);
        else
            //%turret.nameTag = addTaggedString(getTaggedString(%turret.getDataBlock().targetNameTag));
            %turret.nameTag = %turret.getDataBlock().targetNameTag; // This should already be a tagged string

        %turret.target = createTarget(%turret, %turret.nameTag, "", "",
            %turret.getDatablock().targetTypeTag, %turret.team, 0);
        setTargetSensorGroup(%turret.target, %turret.team);

        // Reset the variable set on the client and turret
        %turret.oldTag = "";
        %client.TurretControl = "";
    }
    // -------------------------------------------------------------------------
}

function serverCmdResetControlObject(%client)
{
    resetControlObject(%client);
    commandToClient(%client, 'ControlObjectReset');
    // --------------------------------------------------------
    // z0dd - ZOD 4/18/02. Vehicle reticle disappearance fix.
    // commandToClient(%client, 'RemoveReticle');
    //if (isObject(%client.player))
    //{
    //   %weapon = %client.player.getMountedImage($WeaponSlot);
    //   %client.setWeaponsHudActive(%weapon.item);
    //}
    if (isObject(%client.player))
    {
        if (%client.player.isPilot() || %client.player.isWeaponOperator())
        {
            return;
        }
        else
        {
            commandToClient(%client, 'RemoveReticle');
            %weapon = %client.player.getMountedImage($WeaponSlot);
            %client.setWeaponsHudActive(%weapon.item);
        }
    }
    // End z0dd - ZOD
    // --------------------------------------------------------
}

function serverCmdAttachCommanderCamera(%client, %target)
{
    // dont allow observing until match has started
    if (!$MatchStarted)
    {
        commandToClient(%client, 'CameraAttachResponse', false);
        return;
    }

    %obj = getTargetObject(%target);
    if ((%obj == -1) || (%target == -1))
    {
        commandToClient(%client, 'CameraAttachResponse', false);
        return;
    }

    // shape base object?
    if (!(%obj.getType() & $TypeMasks::ShapeBaseObjectType))
    {
        commandToClient(%client, 'CameraAttachResponse', false);
        return;
    }

    // can be observed?
    if (!%obj.getDataBlock() || !%obj.getDataBlock().canObserve)
    {
        commandToClient(%client, 'CameraAttachResponse', false);
        return;
    }

    // same team?
    if (getTargetSensorGroup(%target) != %client.getSensorGroup())
    {
        commandToClient(%client, 'CameraAttachResponse', false);
        return;
    }

    // powered?
    if (!%obj.isPowered())
    {
        commandToClient(%client, 'CameraAttachResponse', false);
        return;
    }

    // client connection?
    if (%obj.getClassName() $= "GameConnection")
    {
        %player = %obj.player;
        if (%obj == %client)
        {
            if (isObject(%player) && !%player.isDestroyed())
            {
                %client.setControlObject(%player);
                commandToClient(%client, 'CameraAttachResponse', true);
                return;
            }
        }

        %obj = %player;
    }

    if (!isObject(%obj) || %obj.isDestroyed())
    {
        commandToClient(%client, 'CameraAttachResponse', false);
        return;
    }

    %data = %obj.getDataBlock();
    %obsData = %data.observeParameters;
    %obsX = firstWord(%obsData);
    %obsY = getWord(%obsData, 1);
    %obsZ = getWord(%obsData, 2);

    // don't set the camera mode so that it does not interfere with spawning
    %transform = %obj.getTransform();

    // create a fresh camera to observe through... (could add to a list on
    // the observed camera to be removed when that object dies/...)
    if (!isObject(%client.comCam))
    {
        %client.comCam = new Camera()
        {
            dataBlock = CommanderCamera;
        };
        MissionCleanup.add(%client.comCam);
    }

    %client.comCam.setTransform(%transform);
    %client.comCam.setOrbitMode(%obj, %transform, %obsX, %obsY, %obsZ);

    %client.setControlObject(%client.comCam);
    commandToClient(%client, 'CameraAttachResponse', true);
}

//------------------------------------------------------------------------------
// Scoping
function serverCmdScopeCommanderMap(%client, %scope)
{
    if (%scope)
        resetControlObject(%client);
    %client.scopeCommanderMap(%scope);

    commandToClient(%client, 'ScopeCommanderMap', %scope);
}