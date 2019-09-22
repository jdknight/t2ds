//------------------------------------------------------------------------------
//
// chatMenuHud.cs
//
//------------------------------------------------------------------------------

// Load in all of the installed chat items:
exec("scripts/cannedChatItems.cs");

//------------------------------------------------------------------------------
function serverCmdCannedChat(%client, %command, %fromAI)
{
    %cmdCode = getWord(%command, 0);
    %cmdId = getSubStr(%cmdCode, 1, strlen(%command) - 1);
    %cmdString = getWord(%command, 1);
    if (%cmdString $= "")
        %cmdString = getTaggedString(%cmdCode);

    if (!isObject($ChatTable[%cmdId]))
        return;

    %chatItem = $ChatTable[%cmdId];

    //if there is text
    if (%chatItem.text !$= "" || !%chatItem.play3D)
    {
        %message = %chatItem.text @ "~w" @ %chatItem.audioFile;

        if (%chatItem.teamOnly)
            cannedChatMessageTeam(%client, %client.team, '\c3%1: %2',
                %client.name, %message, %chatItem.defaultKeys);
        else
            cannedChatMessageAll(%client, '\c4%1: %2',
                %client.name, %message, %chatItem.defaultKeys);
    }
    //if no text, see if the audio is to be played in 3D...
    else if (%chatItem.play3D && %client.player)
        playTargetAudio(%client.target, addTaggedString(%chatItem.audioFile),
            AudioClosest3d, true);

    if (%chatItem.animation !$= "")
        playAnim(%client, %chatItem.animation);

    // Let the AI respond to the canned chat messages (from humans only)
    if (!%fromAI)
        CreateVoiceServerTask(%client, %cmdCode);
}
