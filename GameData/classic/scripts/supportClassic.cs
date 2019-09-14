/////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: Generic Console Spam fixes ///////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

function Projectile::isMounted(%this)
{
    return 0;
}

function VehicleBlocker::getDataBlock(%this)
{
    return %this;
}

function VehicleBlocker::getName(%this)
{
    return %this;
}

function WaterBlock::damage(%this)
{
    // Do nothing
}

function InteriorInstance::getDataBlock(%this)
{
    return %this;
}

function InteriorInstance::getName(%this)
{
    return "InteriorInstance";
}

function TerrainBlock::getDataBlock(%this)
{
    return %this;
}

function TerrainBlock::getName(%this)
{
    return "Terrain";
}

function AIConnection::isMounted(%client)
{
    %vehicle = %client.getControlObject();
    %className = %vehicle.getDataBlock().className;
    if (%className $= WheeledVehicleData || %className $= FlyingVehicleData || %className $= HoverVehicleData)
        return true;
    else
        return false;
}

function ForceFieldBareData::isMounted(%obj)
{
    // created to prevent console errors
}

function ForceFieldBareData::damageObject(%data, %targetObject, %position, %sourceObject, %amount, %damageType)
{
    // created to prevent console errors
}

function ItemData::onPickup(%this, %obj, %player, %amount)
{
    // %this = Object datablock
    // %obj = Object ID number
    // %player = player
    // %amount = amount picked up (1)

    // Created to prevent console errors related to picking up items
}

/////////////////////////////////////////////////////////////////////////////////////////
// Random Teams code by Founder (founder@mechina.com) 6/13/02 ///////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

// Couple other files edited for Random Teams.
// Hud.cs and DefaultGame.cs
function AIConnection::startMission(%client)
{
    // assign the team
    if (%client.team <= 0)
        Game.assignClientTeam(%client);

    if (%client.lastTeam !$= "")
    {
        if (%client.team != %client.lastTeam)
            Game.AIChangeTeam(%client, %client.lastTeam);
    }

    // set the client's sensor group...
    setTargetSensorGroup(%client.target, %client.team);
    %client.setSensorGroup(%client.team);

    // sends a message so everyone know the bot is in the game...
    Game.AIHasJoined(%client);
    %client.matchStartReady = true;

    // spawn the bot...
    onAIRespawn(%client);
}

/////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD: Universal functions //////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////

function stripTaggedVar(%var)
{
    return stripChars(detag(getTaggedString(%var)), "\cp\co\c6\c7\c8\c9");
}

// Removes triggers from Siege when players switch sides, also used in practiceCTF
function cleanTriggers(%group)
{
    if (%group > 0)
        %depCount = %group.getCount();
    else
        return;

    for (%i = 0; %i < %depCount; %i++)
    {
        %deplObj = %group.getObject(%i);
        if (isObject(%deplObj))
        {
            if (%deplObj.trigger !$= "")
                %deplObj.trigger.schedule(0, "delete");
        }
    }
}

// -----------------------------------------------------
// z0dd - ZOD, 6/22/02. Hack to eliminate texture cheats
package cloaking
{
    function ShapeBase::setCloaked(%obj, %bool)
    {
        Parent::setCloaked(%obj, %bool);

        if (%bool)
            %obj.startFade(0, 800, true);
        else
            %obj.startFade(0, 0, false);
    }
};
activatePackage(cloaking);

function VehicleData::onCollision(%data, %obj, %col)
{
    // Keep vehicle ghost from harming players?
    if (%obj.getDamageState() $= "Destroyed")
        return;

    if (!isObject(%obj) || !isObject(%col))
        return;

    // Will cause vehicles to take damage when they collide with armors
    //if (%col.getDataBlock().className $= "Armor")
    //{
    //   %vel = %obj.getVelocity();
    //   %vecLen = vectorDot(%vel, vectorNormalize(%vel));

    //   if (%vecLen > %data.collDamageThresholdVel)
    //      %data.damageObject(%obj, 0, VectorAdd(%vec, %obj.getPosition()),
    //                         %vecLen * %data.collDamageMultiplier, $DamageType::Impact);

    //   %obj.playAudio(0, %data.hardImpactSound);
}

function serverCmdPrivateMessageSent(%client, %target, %text)
{
    // Client side:
    //commandToServer('PrivateMessageSent', %target, %text);

    if (%text $= "" || spamAlert(%client))
        return;

    if (%client.isAdmin)
    {
        %snd = '~wfx/misc/diagnostic_on.wav';
        if (strlen(%text) >= $Host::MaxMessageLen)
            %text = getSubStr(%text, 0, $Host::MaxMessageLen);

        messageClient(%target, 'MsgPrivate', '\c5Message from %1: \c3%2%3', %client.name, %text, %snd);
    }
    else
        messageClient(%client, 'MsgError', '\c4Only admins can send private messages');
}

//////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD, 10/03/02. Part of flag collision bug hack.
//////////////////////////////////////////////////////////////////////////////

datablock TriggerData(flagTrigger)
{
    tickPeriodMS = 10;
};

function flagTrigger::onEnterTrigger(%data, %obj, %colObj)
{
    %flag = %obj.flag;
    if ($flagStatus[%flag.team] $= "<At Base>")
        %flag.getDataBlock().onCollision(%flag, %colObj);
    else
        return;
}

function flagTrigger::onLeaveTrigger(%data, %obj, %colObj)
{
    // Thou shalt not spam
}

function flagTrigger::onTickTrigger(%data, %obj)
{
    // Thou shalt not spam
}
