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
                        if (strstr(%typeList, %missionType) != -1)
                        {
                            $LoadObjLine[$LoadObjLineCount] = getSubStr(%line, %pos + 1, 1000);
                            $LoadObjLineCount++;
                        }
                    }
                    else
                        error("Invalid mission objective line - \"" @ %line @ "\"");
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
function sendLoadInfoToClient(%client)
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
}
