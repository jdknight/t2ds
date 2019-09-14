//------------------------------------------------------------------------------
//
// GameGui.cs
//
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
function getMissionTypeDisplayNames()
{
    %file = new FileObject();
    for (%type = 0; %type < $HostTypeCount; %type++)
    {
        $HostTypeDisplayName[%type] = $HostTypeName[%type];
        if (%file.openForRead("scripts/" @ $HostTypeName[%type] @ "Game.cs"))
        {
            while (!%file.isEOF())
            {
                %line = %file.readLine();
                if (getSubStr(%line, 0, 17) $= "// DisplayName = ")
                {
                    $HostTypeDisplayName[%type] = getSubStr(%line, 17, 1000);
                    break;
                }
            }
        }
    }

    %file.delete();
}

//------------------------------------------------------------------------------
function buildMissionList()
{
    %search = "missions/*.mis";
    %ct = 0;

    $HostTypeCount = 0;
    $HostMissionCount = 0;

    %fobject = new FileObject();

    for (%file = findFirstFile(%search); %file !$= ""; %file = findNextFile(%search))
    {
        %name = fileBase(%file); // get the name

        %idx = $HostMissionCount;
        $HostMissionCount++;

        $HostMissionFile[%idx] = %name;
        $HostMissionName[%idx] = %name;

        if (!%fobject.openForRead(%file))
            continue;

        %typeList = "None";

        while (!%fobject.isEOF())
        {
            %line = %fobject.readLine();
            if (getSubStr(%line, 0, 17) $= "// DisplayName = ")
            {
                // Override the mission name:
                $HostMissionName[%idx] = getSubStr(%line, 17, 1000);
            }
            else if (getSubStr(%line, 0, 18) $= "// MissionTypes = ")
            {
                %typeList = getSubStr(%line, 18, 1000);
                break;
            }
        }
        %fobject.close();

        // Don't include single player missions:
        if (strstr(%typeList, "SinglePlayer") != -1)
            continue;

        // Test to see if the mission is bot-enabled:
        %navFile = "terrains/" @ %name @ ".nav";
        $BotEnabled[%idx] = isFile(%navFile);

        for (%word = 0; (%misType = getWord(%typeList, %word)) !$= ""; %word++)
        {
            for (%i = 0; %i < $HostTypeCount; %i++)
                if ($HostTypeName[%i] $= %misType)
                    break;
            if (%i == $HostTypeCount)
            {
                $HostTypeCount++;
                $HostTypeName[%i] = %misType;
                $HostMissionCount[%i] = 0;
            }

            // add the mission to the type
            %ct = $HostMissionCount[%i];
            $HostMission[%i, $HostMissionCount[%i]] = %idx;
            $HostMissionCount[%i]++;
        }
    }

    getMissionTypeDisplayNames();

    %fobject.delete();
}

// One time only function call:
buildMissionList();

//------------------------------------------------------------------------------
function validateMissionAndType(%misName, %misType)
{
    for (%mis = 0; %mis < $HostMissionCount; %mis++)
        if ($HostMissionFile[%mis] $= %misName)
            break;
    if (%mis == $HostMissionCount)
        return false;

    for (%type = 0; %type < $HostTypeCount; %type++)
        if ($HostTypeName[%type] $= %misType)
            break;
    if (%type == $hostTypeCount)
        return false;

    $Host::Map = $HostMissionFile[%mis];
    $Host::MissionType = $HostTypeName[%type];
    return true;
}

//------------------------------------------------------------------------------
// This function returns the index of the next mission in the mission list.
//------------------------------------------------------------------------------
function getNextMission(%misName, %misType)
{
    // First find the index of the mission in the master list:
    for (%mis = 0; %mis < $HostMissionCount; %mis++)
        if ($HostMissionFile[%mis] $= %misName)
            break;
    if (%mis == $HostMissionCount)
        return "";

    // Now find the index of the mission type:
    for (%type = 0; %type < $HostTypeCount; %type++)
        if ($HostTypeName[%type] $= %misType)
            break;
    if (%type == $hostTypeCount)
        return "";

    // Now find the mission's index in the mission-type specific sub-list:
    for (%i = 0; %i < $HostMissionCount[%type]; %i++)
        if ($HostMission[%type, %i] == %mis)
            break;

    // Go BACKWARDS, because the missions are in reverse alphabetical order:
    if (%i == 0)
        %i = $HostMissionCount[%type] - 1;
    else
        %i--;

    // If there are bots in the game, don't switch to any maps without
    // a NAV file:
    if ($HostGameBotCount > 0)
    {
        for (%j = 0; %j < $HostMissionCount[%type] - 1; %j++)
        {
            if ($BotEnabled[$HostMission[%type, %i]])
                break;

            if (%i == 0)
                %i = $HostMissionCount[%type] - 1;
            else
                %i--;
        }
    }

    return $HostMission[%type, %i];
}
