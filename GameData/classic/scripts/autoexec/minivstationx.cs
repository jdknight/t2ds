//
// xPack-T2-CTF-Maps.vl2 update file.
// MiniVStationX
// Adapted from the original station code by
// Tim 'Zear' Hammock
//
// This file created to address code changes in base++
// To handle issues with certain maps from the xPack Map Pack
// that move the vehicle stations and scale the vehicle pads.
//
// z0dd - ZOD, 4/25/02

package MiniVStationX
{
    function StationVehiclePad::createStationVehicle(%data, %obj)
    {
        Parent::createStationVehicle(%data, %obj);

        schedule(250, 0, "moveVStationX");
    }
};

function moveVStationX()
{
    if ($CurrentMission $= "Raindance_x")
    {
        nametoid(team1vehiclestation).station.setTransform("-180.737 264.173 73.9045 0 0 -0.999913 0.0206931");
        nametoid(team1vehiclestation).station.trigger.setTransform("-180.737 264.173 73.9045 0 0 -0.999913 0.0206931");
        nametoid(team2vehiclestation).station.setTransform("-44.2395 -559.427 62.578 0 0 1 3.13932");
        nametoid(team2vehiclestation).station.trigger.setTransform("-44.2395 -559.427 62.578 0 0 1 3.13932");
    }
    else if ($CurrentMission $= "Rollercoaster_x")
    {
        nametoid(team1vehiclestation).station.setTransform("357.822 57.4137 157.373 0 0 1 1.752");
        nametoid(team1vehiclestation).station.trigger.setTransform("357.822 57.4137 157.373 0 0 1 1.68703");
        nametoid(team2vehiclestation).station.setTransform("-401.698 7.81527 156.566 0 0 -1 1.48");
        nametoid(team2vehiclestation).station.trigger.setTransform("-401.698 7.81527 156.566 0 0 -1 1.54951");
        (nametoid(team1flag)+ 1).setTransform("0 0 0 0 0 0 0");
        (nametoid(team2flag)+ 1).setTransform("0 0 0 0 0 0 0");
    }
    else
        return;
}

activatePackage(MiniVStationX);
