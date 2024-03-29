
// load default controls:
exec("scripts/controlDefaults.cs");

// ---------------------------------------------------------------------------------
// z0dd - ZOD, 5/8/02. Moved here so scripters can use the message callback feature.
// message.cs is loaded so autoexec can add new message callbacks
exec("scripts/message.cs");

//exec any user created .cs files found in scripts/autoexec (order is that returned by the OS)
function loadCustomScripts()
{
    %path = "scripts/autoexec/*.cs";
    for (%file = findFirstFile(%path); %file !$= ""; %file = findNextFile(%path))
        exec(%file);
}
loadCustomScripts();

// override settings from autoexec.cs
exec("autoexec.cs");

//TINMAN hack to add a command line option for starting a bot match...
if ($CmdLineBotCount !$= "")
{
    $Host::BotCount = $CmdLineBotCount;
}

// message.cs is loaded so autoexec can add new message callbacks
// z0dd - ZOD, 5/8/02. Moved so scripters can use the message callback feature.
//exec("scripts/message.cs");

//--------------------------------------------------------------------------

exec("scripts/GameGui.cs");
exec("scripts/scoreList.cs");
exec("scripts/server.cs");
exec("scripts/telnet.cs");
exec("scripts/hud.cs");
exec("scripts/inventoryHud.cs");
exec("scripts/chatMenuHud.cs");
exec("scripts/loadingGui.cs");
exec("scripts/voiceChat.cs");
exec("scripts/targetManager.cs");
exec("scripts/gameCanvas.cs");
exec("scripts/centerPrint.cs");
exec("scripts/obsolete.cs");

//------------------------------------------------------------------------------

function loadPostModScripts()
{
    %path = "scripts/post.d/*.cs";
    for (%f = findFirstFile(%path); %f !$= ""; %f = findNextFile(%path))
        exec(%f);
}
//loadPostModScripts(); -- interim; invoked in server.cs for now

//------------------------------------------------------------------------------

// see if the mission and type are valid
// if they are they will be assigned into $Host::Map and $Host::MissionType
if ($mission !$= "" && $missionType !$= "")
    validateMissionAndType($mission, $missionType);

if ($ServerBind !$= "")
{
    $Host::BindAddress = $ServerBind;
}

if ($ServerPort $= "")
{
    $ServerPort = $Host::Port;
}

$Host::Dedicated = true;
$ServerName = $Host::GameName;
initializeTelnet();
setNetPort($ServerPort);
CreateServer($Host::Map, $Host::MissionType);
