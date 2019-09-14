//--------------------------------------------------------------------------
//
// Tribes 2 startup
//
//--------------------------------------------------------------------------

enableWinConsole(true);

// disable performance counters to prevent client-observed stuttering
setPerfCounterEnable(false);

// z0dd - ZOD - Founder (founder@mechina.com), 10/23/02. Fixes bug where by
// parent functions are lost when packages are deactivated.
package PackageFix
{
    function isActivePackage(%package)
    {
        for (%i = 0; %i < $TotalNumberOfPackages; %i++)
        {
            if ($Package[%i] $= %package)
            {
                return true;
                break;
            }
        }
        return false;
    }

    function ActivatePackage(%this)
    {
        Parent::ActivatePackage(%this);
        if ($TotalNumberOfPackages $= "")
            $TotalNumberOfPackages = 0;
        else
        {
            // This package name is already active, so lets not activate it again.
            if (isActivePackage(%this))
            {
                error("ActivatePackage called for a currently active package!");
                return;
            }
        }
        $Package[$TotalNumberOfPackages] = %this;
        $TotalNumberOfPackages++;
    }

    function DeactivatePackage(%this)
    {
        %count = 0;
        %counter = 0;
        // find the index number of the package to deactivate
        for (%i = 0; %i < $TotalNumberOfPackages; %i++)
        {
            if ($Package[%i] $= %this)
            %breakpoint = %i;
        }
        for (%j = 0; %j < $TotalNumberOfPackages; %j++)
        {
            if (%j < %breakpoint)
            {
                // go ahead and assign temp array, save code
                %tempPackage[%count] = $Package[%j];
                %count++;
            }
            else if (%j > %breakpoint)
            {
                %reactivate[%counter] = $Package[%j];
                $Package[%j] = "";
                %counter++;
            }
        }

        //deactivate all the packagess from the last to the current one
        for (%k = (%counter - 1); %k > -1; %k--)
            Parent::DeactivatePackage(%reactivate[%k]);

        //deactivate the package that started all this
        Parent::DeactivatePackage(%this);

        //don't forget this
        $TotalNumberOfPackages = %breakpoint;

        //reactivate all those other packages
        for (%l = 0; %l < %counter; %l++)
            ActivatePackage(%reactivate[%l]);
    }

    function listPackages()
    {
        echo("Activated Packages:");
        for (%i = 0; %i < $TotalNumberOfPackages; %i++)
            echo($Package[%i]);
    }
};
activatePackage(PackageFix);
// End z0dd - ZOD - Founder
//------------------------------------------------------------------------------

$serverprefs = "prefs/serverPrefs.cs";

//------------------------------------------------------------------------------

for ($i = 1; $i < $Game::argc ; $i++)
{
    $arg = $Game::argv[$i];
    $nextArg = $Game::argv[$i+1];
    $nextArg2 = $Game::argv[$i+2];
    $hasNextArg = $Game::argc - $i > 1;
    $has2NextArgs = $Game::argc - $i > 2;

    if ($arg $= "-mod" && $hasNextArg)
    {
        setModPaths($nextArg);
        $i += 2;
    }
    else if ($arg $= "-serverprefs" && $hasNextArg)
    {
        $i++;
        $serverprefs = $nextArg;
    }
    else if ($arg $= "-mission" && $has2NextArgs)
    {
        $i += 2;
        $mission = $nextArg;
        $missionType = $nextArg2;
    }
    else if ($arg $= "-telnetParams" && $has2NextArgs)
    {
        $i += 3;
        $telnetPort = $nextArg;
        $telnetPassword = $nextArg2;
        $telnetListenPass = $nextArg3;
        telnetSetParameters($telnetPort, $telnetPassword, $telnetListenPass);
    }
    else if ($arg $= "-bot" && $hasNextArg)
    {
        $i++;
        $CmdLineBotCount = $nextArg;
    }
    else if ($arg $= "-quit")
    {
        quit();
        return;
    }
}

//------------------------------------------------------------------------------

// load autoexec once for command-line overrides
exec("autoexec.cs", true);

// load defaults
exec("scripts/serverDefaults.cs", true);
if (isFile($serverprefs))
    exec($serverprefs, true, true);

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

setRandomSeed();

exec("console_end.cs");
