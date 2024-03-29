//------------------------------------------------------------------------------
//
// Tribes 2 startup
//
//------------------------------------------------------------------------------

function onExit() {}
for ($i = 1; $i < $Game::argc; $i++)
{
    $arg = $Game::argv[$i];
    if ($arg $= "-help" || $arg $= "--help")
    {
        enableWinConsole(true);

        echo("Tribes 2 Dedicated Server Options");
        echo(" ");
        echo("  --bind <address>            override the address to bind to");
        echo("  --bot <count>               override initial bot count");
        echo("  --help                      show this help");
        echo("  --mission <map> <type>      load a specific map on start");
        echo("  --port <port>               override the server port");
        echo("  --serverprefs <file>        override server preferences file");
        echo("                               (default prefs/serverPrefs.cs)");
        echo("  --telnetParams <port> <password> <listen-password>");
        echo("                              configure telnet options");
        quit();
        return;
    }
}

// cleanup pre-compiled dso's
//
// Utility method to delete script files to help reduce issues when introducing
// new script modifications.
function cleanupDsos()
{
    while((%f = findFirstFile("*.dso")) !$= "")
        deleteFile(%f);
}
cleanupDsos();

// disable asserts from generating a message box (fail for restart, if possible)
$FP::DisableAsserts = true;

// enable logging information to the console
enableWinConsole(true);

// disable performance counters to prevent client-observed stuttering
setPerfCounterEnable(false);

// configure a random seed based off the current time
setRandomSeed();

// package stacking deactivation corrections
//
// When a package overrides a method that is already overridden by another
// package, the deactivation of the package will result in the "Parent::" calls
// to not work as expected. This corrections will deactivate all packages that
// have been loaded after a specific package that is requested to be
// deactivated; an in turn reactivate the packages again.
$__PkgFix_TotalPkgs = 0;
package __PkgFix
{
    function isActivePackage(%pkg)
    {
        for (%i = 0; %i < $__PkgFix_TotalPkgs; %i++)
            if ($__PkgFix_Pkgs[%i] $= %pkg)
                return true;

        return false;
    }

    function activatePackage(%pkg)
    {
        Parent::activatePackage(%pkg);

        // package name is already active; internally ignore request
        if (isActivePackage(%pkg))
            return;

        $__PkgFix_Pkgs[$__PkgFix_TotalPkgs] = %pkg;
        $__PkgFix_TotalPkgs++;
    }

    function deactivatePackage(%pkg)
    {
        // find the index number of the package to deactivate
        for (%idx = 0; %idx < $__PkgFix_TotalPkgs; %idx++)
            if ($__PkgFix_Pkgs[%idx] $= %pkg)
                break;

        // track packages queued for reactivation
        %cnt = 0;
        for (%i = (%idx + 1); %i < $__PkgFix_TotalPkgs; %i++)
        {
            %reactivate[%cnt] = $__PkgFix_Pkgs[%i];
            $__PkgFix_Pkgs[%i] = "";
            %cnt++;
        }

        // deactivate all the packages from the last to the current one
        for (%k = (%cnt - 1); %k > -1; %k--)
            Parent::deactivatePackage(%reactivate[%k]);

        // deactivate the package that started all this
        Parent::deactivatePackage(%pkg);

        // reset tracked package count to existing activated packages
        $__PkgFix_TotalPkgs = %idx;

        // reactivate all those other packages
        for (%l = 0; %l < %cnt; %l++)
            activatePackage(%reactivate[%l]);
    }

    function listPackages()
    {
        for (%i = 0; %i < $__PkgFix_TotalPkgs; %i++)
            echo($__PkgFix_Pkgs[%i]);
    }
};
activatePackage(__PkgFix);

// hook invoked when the game is safely shutdown
function onExit()
{
    BanList::Export("prefs/banlist.cs");

    cleanupDsos();
}

//------------------------------------------------------------------------------

$serverprefs = "prefs/serverPrefs.cs";

//------------------------------------------------------------------------------

for ($i = 1; $i < $Game::argc; $i++)
{
    $arg = $Game::argv[$i];
    $nextArg = $Game::argv[$i+1];
    $nextArg2 = $Game::argv[$i+2];
    $hasNextArg = $Game::argc - $i > 1;
    $has2NextArgs = $Game::argc - $i > 2;
    $has3NextArgs = $Game::argc - $i > 3;

    if (($arg $= "-bind" || $arg $= "--bind") && $hasNextArg)
    {
        $i++;
        if ($nextArg !$= "")
            $ServerBind = $nextArg;
    }
    else if (($arg $= "-mod" || $arg $= "--mod") && $hasNextArg)
    {
        $i++;
        if ($nextArg !$= "")
        {
            setModPaths($nextArg);
        }
    }
    else if (($arg $= "-serverprefs" || $arg $= "--serverprefs") && $hasNextArg)
    {
        $i++;
        if ($nextArg !$= "")
        {
            $serverprefs = $nextArg;
        }
    }
    else if (($arg $= "-mission" || $arg $= "--mission") && $has2NextArgs)
    {
        $i += 2;
        if ($nextArg !$= "" && $nextArg2 !$= "")
        {
            $mission = $nextArg;
            $missionType = $nextArg2;
        }
    }
    else if (($arg $= "-port" || $arg $= "--port") && $hasNextArg)
    {
        $i++;
        if ($nextArg !$= "")
            $ServerPort = $nextArg;
    }
    else if (($arg $= "-telnetParams" || $arg $= "--telnetParams")
                && $has3NextArgs)
    {
        $i += 3;
        if ($nextArg !$= "" && $nextArg2 !$= "" && $nextArg3 !$= "")
        {
            $telnetPort = $nextArg;
            $telnetPassword = $nextArg2;
            $telnetListenPass = $nextArg3;
        }
    }
    else if (($arg $= "-bot" || $arg $= "--bot") && $hasNextArg)
    {
        $i++;
        if ($nextArg !$= "")
            $CmdLineBotCount = $nextArg;
    }
    else if ($arg $= "-quit" || $arg $= "--quit")
    {
        quit();
        return;
    }
}

//------------------------------------------------------------------------------

// load autoexec once for command-line overrides
if (isFile("autoexec.cs"))
    exec("autoexec.cs", true);

// load defaults
exec("scripts/serverDefaults.cs", true);
if (isFile($serverprefs))
    exec($serverprefs, true, true);
exec("scripts/preferences.cs");

// convert the team skin and name vars to tags
$index = 0;
while ($Host::TeamSkin[$index] !$= "")
{
    $TeamSkin[$index] = addTaggedString($Host::TeamSkin[$index]);
    $index++;
}

$index = 0;
while ($Host::TeamName[$index] !$= "")
{
    $TeamName[$index] = addTaggedString($Host::TeamName[$index]);
    $index++;
}

// initialize the hologram names
$index = 1;
while ($Host::holoName[$index] !$= "")
{
    $holoName[$index] = $Host::holoName[$index];
    $index++;
}

//------------------------------------------------------------------------------

exec("console_end.cs");
