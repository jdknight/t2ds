//------------------------------------------------------------------------------
//
// LoadingGui.cs
//
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
function clearLoadInfo()
{
    for (%line = 0; %line < $LoadQuoteLineCount; %line++)
        $LoadQuoteLine[%line] = "";
    $LoadQuoteLineCount = 0;

    for (%line = 0; %line < $LoadObjLineCount; %line++)
        $LoadObjLine[%line] = "";
    $LoadObjLineCount = 0;

    for (%line = 0; %line < $LoadRuleLineCount; %line++)
        $LoadRuleLine[%line] = "";
    $LoadRuleLineCount = 0;
}

//------------------------------------------------------------------------------
function buildLoadInfo(%mission, %missionType)
{
    clearLoadInfo();
    $CurrentMission = %mission;
    $MissionDisplayName = %mission;
    $MissionTypeDisplayName = %missionType;

    // Extract the map quote and objectives from the .mis file:
    %mapFile = "missions/" @ %mission @ ".mis";
    %file = new FileObject();
    if (%file.openForRead(%mapFile))
    {
        %state = "none";
        while (!%file.isEOF())
        {
            %line = %file.readLine();

            if (%state $= "none")
            {
                if (getSubStr(%line, 0, 17) $= "// DisplayName = ")
                    $MissionDisplayName = getSubStr(%line, 17, 1000);
                else if (%line $= "//--- MISSION QUOTE BEGIN ---")
                    %state = "quote";
                else if (%line $= "//--- MISSION STRING BEGIN ---")
                    %state = "objectives";
            }
            else if (%state $= "quote")
            {
                if (%line $= "//--- MISSION QUOTE END ---")
                    %state = "none";
                else
                {
                    $LoadQuoteLine[$LoadQuoteLineCount] = getSubStr(%line, 2, 1000);
                    $LoadQuoteLineCount++;
                }
            }
            else if (%state $= "objectives")
            {
                if (%line $= "//--- MISSION STRING END ---")
                {
                    // Once we've got the end of the mission string, we
                    // are through.
                    %state = "done";
                    break;
                }
                else
                {
                    %pos = strstr(%line, "]");
                    if (%pos == -1)
                    {
                        $LoadObjLine[$LoadObjLineCount] = getSubStr(%line, 2, 1000);
                        $LoadObjLineCount++;
                    }
                    else if (%pos > 3)
                    {
                        // Filter objective lines by mission type:
                        %typeList = getSubStr(%line, 3, %pos - 3);
                        // ------------------------------------------------------------------------
                        // z0dd - ZOD, 5/15/02. Add practice gametype so we get objectives printed.
                        if (%typeList $= "CTF")
                            %typeList = rtrim(%typeList) @ " PracticeCTF";
                        // ------------------------------------------------------------------------
                        if (strstr(%typeList, %missionType) != -1)
                        {
                            $LoadObjLine[$LoadObjLineCount] = getSubStr(%line, %pos + 1, 1000);
                            $LoadObjLineCount++;
                        }
                    }
                    else
                        error("invalid mission objective line - \"" @ %line @ "\"");
                }
            }
            else if (%state $= "blurb")
            {
                if (%line $= "//--- MISSION BLURB END ---")
                {
                    %state = "done";
                    break;
                }
                else
                {
                    $LoadRuleLine[$LoadRuleLineCount] = getSubStr(%line, 2, 1000);
                    $LoadRuleLineCount++;
                }
            }
        }
        %file.close();
    }

    // Extract the rules of engagement from the <mission type>Game.cs file:
    %gameFile = "scripts/" @ %missionType @ "Game.cs";
    if (%file.openForRead(%gameFile))
    {
        %state = "none";
        while (!%file.isEOF())
        {
            %line = %file.readLine();
            if (%state $= "none")
            {
                if (getSubStr(%line, 0, 17) $= "// DisplayName = ")
                    $MissionTypeDisplayName = getSubStr(%line, 17, 1000);
                if (%line $= "//--- GAME RULES BEGIN ---")
                    %state = "rules";
            }
            else if (%state $= "rules")
            {
                if (%line $= "//--- GAME RULES END ---")
                {
                    %state = "done";
                    break;
                }
                else
                {
                    $LoadRuleLine[$LoadRuleLineCount] = getSubStr(%line, 2, 1000);
                    $LoadRuleLineCount++;
                }
            }
        }
        %file.close();
    }

    %file.delete();
}

//------------------------------------------------------------------------------
// z0dd - ZOD, 5/12/02. Added another variable so we can send this twice
function sendLoadInfoToClient(%client, %second)
{
    messageClient(%client, 'MsgLoadInfo', "",
        $CurrentMission, $MissionDisplayName, $MissionTypeDisplayName);

    // Send map quote:
    for (%line = 0; %line < $LoadQuoteLineCount; %line++)
    {
        if ($LoadQuoteLine[%line] !$= "")
            messageClient(%client, 'MsgLoadQuoteLine', "", $LoadQuoteLine[%line]);
    }

    for (%line = 0; %line < $LoadObjLineCount; %line++)
    {
        if ($LoadObjLine[%line] !$= "")
            messageClient(%client, 'MsgLoadObjectiveLine', "", $LoadObjLine[%line], true);
    }

    // Send rules of engagement:
    messageClient(%client, 'MsgLoadRulesLine', "",
        "<spush><font:Univers Condensed:18>RULES OF ENGAGEMENT:<spop>", false);

    for (%line = 0; %line < $LoadRuleLineCount; %line++)
    {
        if ($LoadRuleLine[%line] !$= "")
            messageClient(%client, 'MsgLoadRulesLine', "", $LoadRuleLine[%line], true);
    }

    messageClient(%client, 'MsgLoadInfoDone');

    // ----------------------------------------------------------------------------------------------
    // z0dd - ZOD, 5/12/02. Send the mod info screen if this isn't the second showing of mission info
    if (!%second)
        schedule(6000, 0, "sendModInfoToClient", %client);
    // ----------------------------------------------------------------------------------------------
}

function sendModInfoToClient(%client)
{
    %on = "On";
    %off = "Off";
    %time = "<color:556B2F>Time limit: <color:8FBC8F>" @ $Host::TimeLimit;
    %max = "<color:556B2F>Max players: <color:8FBC8F>" @ $Host::MaxPlayers;
    %td = "<color:556B2F>Team damage: <color:8FBC8F>" @ ($Host::TeamDamageOn ? %on : %off);
    %crc = "<color:556B2F>CRC checking: <color:8FBC8F>" @ ($Host::CRCTextures ? %on : %off);
    %pure = "<color:556B2F>Pure server: <color:8FBC8F>" @ ($Host::PureServer ? %on : %off);
    %smurf = "<color:556B2F>Refuse smurfs: <color:8FBC8F>" @ ($Host::NoSmurfs ? %on : %off);
    %random = "<color:556B2F>Random teams: <color:8FBC8F>" @ ($Host::ClassicRandomizeTeams ? %on : %off);
    if ($CurrentMissionType $= "PracticeCTF")
        %prac = "<color:556B2F>Practice Mode: <color:8FBC8F>On";
    else
        %prac = "<color:556B2F>Practice Mode: <color:8FBC8F>Off";

    if (!$Host::ClassicRandomMissions)
        %nmis = "<color:556B2F>Next mission: <color:8FBC8F>" @ findNextCycleMission();
    else
        %nmis = "<color:556B2F>Next mission: <color:8FBC8F>Randomly selected";

    %modName = "Classic  1.1-t2ds";
    %ModCnt = 1;
    %ModLine[0] = "<color:556B2F>Developers: <color:8FBC8F><a:PLAYER\tz0dd>z0dd</a> and <a:PLAYER\t-ZOD->ZOD</a>";

    %SpecialCnt = 3;
    %SpecialTextLine[0] = %prac;
    %SpecialTextLine[1] = %random;
    %SpecialTextLine[2] = %nmis;

    %ServerCnt = 6;
    %ServerTextLine[0] = %time;
    %ServerTextLine[1] = %max;
    %ServerTextLine[2] = %td;
    %serverTextLine[3] = %crc;
    %ServerTextLine[4] = %pure;
    %ServerTextLine[5] = %smurf;
    //%ServerTextLine[6] = %prac;
    //%ServerTextLine[7] = %nmis;

    %singlePlayer = $CurrentMissionType $= "SinglePlayer";
    messageClient(%client, 'MsgLoadInfo', "",
        $CurrentMission, %modName, $Host::GameName);

    // Send mod details (non bulleted list, small text):
    for (%line = 0; %line < %ModCnt; %line++)
    {
        if (%ModLine[%line] !$= "")
            messageClient(%client, 'MsgLoadQuoteLine', "", %ModLine[%line]);
    }

    // Send mod special settings (bulleted list, large text):
    for (%line = 0; %line < %SpecialCnt; %line++)
    {
        if (%SpecialTextLine[%line] !$= "")
            messageClient(%client, 'MsgLoadObjectiveLine', "",
                %SpecialTextLine[%line], !%singlePlayer);
    }

    // Send server info:
    if (!%singlePlayer)
        messageClient(%client, 'MsgLoadRulesLine', "",
            "<color:8FBC8F>" @ $Host::Info, false);

    for (%line = 0; %line < %ServerCnt; %line++)
    {
        if (%ServerTextLine[%line] !$= "")
            messageClient(%client, 'MsgLoadRulesLine', "",
                %ServerTextLine[%line], !%singlePlayer);
    }
    messageClient(%client, 'MsgLoadInfoDone');
    // z0dd - ZOD, 5/12/02. Send mission info again so as not to conflict with cs scripts.
    schedule(7000, 0, "sendLoadInfoToClient", %client, true);
}
