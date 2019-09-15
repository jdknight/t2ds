//------------------------------------------------------------------------------
//
// Preference-related Capabilities
//
//------------------------------------------------------------------------------

if ($Host::Telnet $= "" && $Host::ClassicTelnet !$= "")
{
    $Host::Telnet = $Host::ClassicTelnet;
    $Host::TelnetPort = $Host::ClassicTelnetPort;
    $Host::TelnetPassword = $Host::ClassicTelnetPassword;
    $Host::TelnetListenPass = $Host::ClassicTelnetListenPass;
}
