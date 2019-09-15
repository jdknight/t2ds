//------------------------------------------------------------------------------
//
// Telnet-related Capabilities
//
//------------------------------------------------------------------------------

// flag to help suppress multiple telnet connections spamming users
$TelnetSpam = 0;

// hook to trigger when a telnet connection is made to the server
function onTelnetConnect(%ip, %access)
{
    // alert players if running in tournament mode
    if ($Host::TournamentMode && $TelnetSpam == 0)
    {
        %msg = '\c1Remote telnet connection established.%1';
        %snd = '~wfx/misc/diagnostic_on.wav';

        messageAll('MsgTelnetConnect', %msg, %snd);
        $TelnetSpam = 1;
        schedule(2000, 0, "clearTelnetSpam");
    }
}

// clear telnet connection spam flag
function clearTelnetSpam()
{
    $TelnetSpam = 0;
}

// configure telnet
function initializeTelnet()
{
    // check for host configuration overrides
    //
    // If telnet options have not been configured via command line arguments,
    // attempt to apply options from default host options.
    if ($telnetPort $= "" && $telnetPassword $= "" && $telnetListenPass $= "")
    {
        if ($Host::Telnet == 1)
        {
            $telnetPort = $Host::TelnetPort;
            $telnetPassword = $Host::TelnetPassword;
            $telnetListenPass = $Host::TelnetListenPass;
        }
    }

    if ($telnetPort !$= "")
    {
        echo("Telnet initialized on port " @ $telnetPort);
        telnetSetParameters($telnetPort, $telnetPassword, $telnetListenPass);
    }
}
