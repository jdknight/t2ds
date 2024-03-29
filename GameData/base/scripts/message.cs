
$SPAM_PROTECTION_PERIOD = 10000;
$SPAM_MESSAGE_THRESHOLD = 4;
$SPAM_PENALTY_PERIOD    = 10000;
$SPAM_MESSAGE           = '\c3FLOOD PROTECTION:\cr You must wait another %1 seconds.';

function GameConnection::spamMessageTimeout(%this)
{
    if (%this.spamMessageCount > 0)
        %this.spamMessageCount--;
}

function GameConnection::spamReset(%this)
{
    %this.isSpamming = false;
}

function spamAlert(%client)
{
    if ($Host::FloodProtectionEnabled != true)
        return(false);

    if (!%client.isSpamming && %client.spamMessageCount >= $SPAM_MESSAGE_THRESHOLD)
    {
        %client.spamProtectStart = getSimTime();
        %client.isSpamming = true;
        %client.schedule($SPAM_PENALTY_PERIOD, spamReset);
    }

    if (%client.isSpamming)
    {
        %wait = mFloor(($SPAM_PENALTY_PERIOD -
            (getSimTime() - %client.spamProtectStart)) / 1000);
        messageClient(%client, "", $SPAM_MESSAGE, %wait);
        return true;
    }

    %client.spamMessageCount++;
    %client.schedule($SPAM_PROTECTION_PERIOD, spamMessageTimeout);
    return false;
}

function chatMessageClient(%client, %sender, %voiceTag, %voicePitch, %msgString,
        %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10)
{
    // see if the client has muted the sender
    if (!%client.muted[%sender])
        commandToClient(%client, 'ChatMessage', %sender, %voiceTag, %voicePitch,
            %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10);
}

function cannedChatMessageClient(%client, %sender, %msgString, %name,
        %string, %keys)
{
    if (!%client.muted[%sender])
        commandToClient(%client, 'CannedChatMessage', %sender, %msgString,
            %name, %string, %keys, %sender.voiceTag, %sender.voicePitch);
}

function chatMessageTeam(%sender, %team, %msgString, %a1, %a2, %a3, %a4, %a5,
        %a6, %a7, %a8, %a9, %a10)
{
    if (%msgString $= "" || spamAlert(%sender))
        return;

    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %obj = ClientGroup.getObject(%i);
        if (%obj.team == %sender.team)
            chatMessageClient(%obj, %sender, %sender.voiceTag,
                %sender.voicePitch, %msgString, %a1, %a2, %a3, %a4, %a5, %a6,
                %a7, %a8, %a9, %a10);
    }
}

function cannedChatMessageTeam(%sender, %team, %msgString, %name,
        %string, %keys)
{
    if (%msgString $= "" || spamAlert(%sender))
        return;

    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %obj = ClientGroup.getObject(%i);
        if (%obj.team == %sender.team)
            cannedChatMessageClient(%obj, %sender, %msgString, %name,
                %string, %keys);
    }
}

function chatMessageAll(%sender, %msgString, %a1, %a2, %a3, %a4, %a5, %a6,
        %a7, %a8, %a9, %a10)
{
    if (%msgString $= "" || spamAlert(%sender))
        return;

    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %obj = ClientGroup.getObject(%i);
        chatMessageClient(%obj, %sender, %sender.voiceTag, %sender.voicePitch,
            %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10);
    }

    // relay chat to console/telnet if server administrators desire it
    if ($Host::EchoChat)
    {
        echo(stripTaggedVar(%sender.name), ": ", stripAudioStr(%a2));
    }
}

function cannedChatMessageAll(%sender, %msgString, %name, %string, %keys)
{
    if (%msgString $= "" || spamAlert(%sender))
        return;

    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
        cannedChatMessageClient(ClientGroup.getObject(%i), %sender, %msgString,
            %name, %string, %keys);

    // relay chat to console/telnet if server administrators desire it
    if ($Host::EchoChat)
    {
        echo(stripTaggedVar(%sender.name), ": ", stripAudioStr(%string));
    }
}

//---------------------------------------------------------------------------
function messageClient(%client, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5,
        %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
    commandToClient(%client, 'ServerMessage', %msgType, %msgString, %a1, %a2,
        %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
}

function messageTeam(%team, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6,
        %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
    %count = ClientGroup.getCount();
    for (%cl= 0; %cl < %count; %cl++)
    {
        %recipient = ClientGroup.getObject(%cl);
        if (%recipient.team == %team)
            messageClient(%recipient, %msgType, %msgString, %a1, %a2, %a3, %a4,
                %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
    }
}

function messageTeamExcept(%client, %msgType, %msgString, %a1, %a2, %a3, %a4,
        %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
    %team = %client.team;
    %count = ClientGroup.getCount();
    for (%cl= 0; %cl < %count; %cl++)
    {
        %recipient = ClientGroup.getObject(%cl);
        if (%recipient.team == %team && %recipient != %client)
            messageClient(%recipient, %msgType, %msgString, %a1, %a2, %a3, %a4,
                %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
    }
}

function messageAll(%msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7,
        %a8, %a9, %a10, %a11, %a12, %a13)
{
    %count = ClientGroup.getCount();
    for (%cl = 0; %cl < %count; %cl++)
    {
        %client = ClientGroup.getObject(%cl);
        messageClient(%client, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5,
            %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
    }
}

function messageAllExcept(%client, %team, %msgtype, %msgString, %a1, %a2, %a3,
        %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
    // can exclude a client, a team or both. A -1 value in either field will
    // ignore that exclusion, so:
    //  messageAllExcept(-1, -1, $Mesblah, 'Blah!');
    // will message everyone (since there shouldn't be a client -1 or client on
    // team -1).
    %count = ClientGroup.getCount();
    for (%cl= 0; %cl < %count; %cl++)
    {
        %recipient = ClientGroup.getObject(%cl);
        if (%recipient != %client && %recipient.team != %team)
            messageClient(%recipient, %msgType, %msgString, %a1, %a2, %a3, %a4,
                %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
    }
}

//---------------------------------------------------------------------------
function teamRepairMessage(%client, %msgType, %msgString, %a1, %a2, %a3, %a4,
        %a5, %a6)
{
    %team = %client.team;

    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %recipient = ClientGroup.getObject(%i);
        if (%recipient.team == %team && %recipient != %client)
            commandToClient(%recipient, 'TeamRepairMessage', %msgType,
                %msgString, %a1, %a2, %a3, %a4, %a5, %a6);
    }
}

// -----------------------------------------------------------------------------
function teamDestroyMessage(%client, %msgType, %msgString, %a1, %a2, %a3, %a4,
        %a5, %a6)
{
    %team = %client.team;
    %count = ClientGroup.getCount();
    for (%i = 0; %i < %count; %i++)
    {
        %recipient = ClientGroup.getObject(%i);
        if (%recipient.team == %team && %recipient != %client)
        {
            commandToClient(%recipient, 'TeamDestroyMessage', %msgType,
                %msgString, %a1, %a2, %a3, %a4, %a5, %a6);
        }
    }
}

//---------------------------------------------------------------------------
function playAnim(%client, %anim)
{
    %player = %client.player;

    // don't play animations if player is in a vehicle (pilot/weapon operator)
    if (!isObject(%player) || %player.isPilot() || %player.isWeaponOperator())
        return;

    %weapon = (%player.getMountedImage($WeaponSlot) == 0 ? "" :
        %player.getMountedImage($WeaponSlot).getName().item);
    if (%weapon $= "MissileLauncher" || %weapon $= "SniperRifle")
    {
        %player.animResetWeapon = true;
        %player.lastWeapon = %weapon;
        %player.unmountImage($WeaponSlot);
        %player.setArmThread(look);
    }

    %player.setActionThread(%anim);
}

//---------------------------------------------------------------------------
function stripAudioStr(%var)
{
    %idx = strstr(%var, "~w");
    if (%idx != -1)
        %var = getSubStr(%var, 0, %idx);

    return %var;
}

//---------------------------------------------------------------------------
function stripTaggedVar(%var)
{
    return stripChars(detag(getTaggedString(%var)), "\cp\co\c6\c7\c8\c9");
}
