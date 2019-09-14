//--------------------------------------------------------------------------
function GameConnection::setVWeaponsHudActive(%client, %slot)
{
    %veh = %client.player.getObjectMount();
    %vehType = %veh.getDatablock().getName();
    commandToClient(%client, 'setVWeaponsHudActive', %slot, %vehType);
}

//----------------------------------------------------------------------------
function GameConnection::setVWeaponsHudClearAll(%client)
{
    commandToClient(%client, 'setVWeaponsHudClearAll');
}

//----------------------------------------------------------------------------
function GameConnection::setWeaponsHudBitmap(%client, %slot, %name, %bitmap)
{
    commandToClient(%client, 'setWeaponsHudBitmap', %slot, %name, %bitmap);
}

//----------------------------------------------------------------------------
function GameConnection::setWeaponsHudItem(%client, %name, %ammoAmount, %addItem)
{
    for (%i = 0; %i < $WeaponsHudCount; %i++)
    {
        if ($WeaponsHudData[%i, itemDataName] $= %name)
        {
            if ($WeaponsHudData[%i, ammoDataName] !$= "")
            {
                %ammoInv = %client.player.inv[$WeaponsHudData[%i, ammoDataName]];
                commandToClient(%client, 'setWeaponsHudItem', %i, %ammoInv, %addItem);
            }
            else
            {
                commandToClient(%client, 'setWeaponsHudItem', %i, -1, %addItem);
            }

            break;
        }
    }
}

//----------------------------------------------------------------------------
function GameConnection::setWeaponsHudAmmo(%client, %name, %ammoAmount)
{
    for (%i = 0; %i < $WeaponsHudCount; %i++)
        if ($WeaponsHudData[%i, ammoDataName] $= %name)
        {
            commandToClient(%client, 'setWeaponsHudAmmo', %i, %ammoAmount);
            break;
        }
}

//----------------------------------------------------------------------------
// z0dd - ZOD, 9/13/02. Serverside reticles, sever tells client what file to use.
function GameConnection::setWeaponsHudActive(%client, %name, %clearActive)
{
    if (%clearActive)
    {
        commandToClient(%client, 'setWeaponsHudActive', -1);
    }
    else
    {
        for (%i = 0; %i < $WeaponsHudCount; %i++)
        {
            if ($WeaponsHudData[%i, itemDataName] $= %name)
            {
                commandToClient(%client, 'setWeaponsHudActive', %i,
                    $WeaponsHudData[%i, reticle], $WeaponsHudData[%i, visible]);
                break;
            }
        }
    }
}

//----------------------------------------------------------------------------
function GameConnection::setWeaponsHudBackGroundBmp(%client, %name)
{
    commandToClient(%client, 'setWeaponsHudBackGroundBmp', %name);
}

//----------------------------------------------------------------------------
function GameConnection::setWeaponsHudHighLightBmp(%client, %name)
{
    commandToClient(%client, 'setWeaponsHudHighLightBmp', %name);
}

//----------------------------------------------------------------------------
function GameConnection::setWeaponsHudInfiniteAmmoBmp(%client, %name)
{
    commandToClient(%client, 'setWeaponsHudInfiniteAmmoBmp', %name);
}

//----------------------------------------------------------------------------
function GameConnection::setWeaponsHudClearAll(%client)
{
    commandToClient(%client, 'setWeaponsHudClearAll');
}

//----------------------------------------------------------------------------
//   Ammo Hud
//----------------------------------------------------------------------------
function GameConnection::setAmmoHudCount(%client, %amount)
{
    commandToClient(%client, 'setAmmoHudCount', %amount);
}

//----------------------------------------------------------------------------
//   Backpack Hud
//----------------------------------------------------------------------------

$BackpackHudData[0, itemDataName] = "AmmoPack";
$BackpackHudData[0, bitmapName] = "gui/hud_new_packammo";
$BackpackHudData[1, itemDataName] = "CloakingPack";
$BackpackHudData[1, bitmapName] = "gui/hud_new_packcloak";
$BackpackHudData[2, itemDataName] = "EnergyPack";
$BackpackHudData[2, bitmapName] = "gui/hud_new_packenergy";
$BackpackHudData[3, itemDataName] = "RepairPack";
$BackpackHudData[3, bitmapName] = "gui/hud_new_packrepair";
$BackpackHudData[4, itemDataName] = "SatchelCharge";
$BackpackHudData[4, bitmapName] = "gui/hud_new_packsatchel";
$BackpackHudData[5, itemDataName] = "ShieldPack";
$BackpackHudData[5, bitmapName] = "gui/hud_new_packshield";
$BackpackHudData[6, itemDataName] = "InventoryDeployable";
$BackpackHudData[6, bitmapName] = "gui/hud_new_packinventory";
$BackpackHudData[7, itemDataName] = "MotionSensorDeployable";
$BackpackHudData[7, bitmapName] = "gui/hud_new_packmotionsens";
$BackpackHudData[8, itemDataName] = "PulseSensorDeployable";
$BackpackHudData[8, bitmapName] = "gui/hud_new_packradar";
$BackpackHudData[9, itemDataName] = "TurretOutdoorDeployable";
$BackpackHudData[9, bitmapName] = "gui/hud_new_packturretout";
$BackpackHudData[10, itemDataName] = "TurretIndoorDeployable";
$BackpackHudData[10, bitmapName] = "gui/hud_new_packturretin";
$BackpackHudData[11, itemDataName] = "SensorJammerPack";
$BackpackHudData[11, bitmapName] = "gui/hud_new_packsensjam";
$BackpackHudData[12, itemDataName] = "AABarrelPack";
$BackpackHudData[12, bitmapName] = "gui/hud_new_packturret";
$BackpackHudData[13, itemDataName] = "FusionBarrelPack";
$BackpackHudData[13, bitmapName] = "gui/hud_new_packturret";
$BackpackHudData[14, itemDataName] = "MissileBarrelPack";
$BackpackHudData[14, bitmapName] = "gui/hud_new_packturret";
$BackpackHudData[15, itemDataName] = "PlasmaBarrelPack";
$BackpackHudData[15, bitmapName] = "gui/hud_new_packturret";
$BackpackHudData[16, itemDataName] = "ELFBarrelPack";
$BackpackHudData[16, bitmapName] = "gui/hud_new_packturret";
$BackpackHudData[17, itemDataName] = "MortarBarrelPack";
$BackpackHudData[17, bitmapName] = "gui/hud_new_packturret";
$BackpackHudData[18, itemDataName] = "SatchelUnarmed";
$BackpackHudData[18, bitmapName] = "gui/hud_satchel_unarmed";

// TR2
$BackpackHudData[19, itemDataName] = "TR2EnergyPack";
$BackpackHudData[19, bitmapName] = "gui/hud_new_packenergy";

$BackpackHudCount = 20;

function GameConnection::clearBackpackIcon(%client)
{
    commandToClient(%client, 'setBackpackHudItem', 0, 0);
}

function GameConnection::setBackpackHudItem(%client, %name, %addItem)
{
    for (%i = 0; %i < $BackpackHudCount; %i++)
        if ($BackpackHudData[%i, itemDataName] $= %name)
            commandToClient(%client, 'setBackpackHudItem', %i, %addItem);
}

function GameConnection::updateSensorPackText(%client, %num)
{
    commandToClient(%client, 'updatePackText', %num);
}

//----------------------------------------------------------------------------
//----------------------------------------------------------------------------
$InventoryHudData[0, bitmapName]   = "gui/hud_handgren";
$InventoryHudData[0, itemDataName] = Grenade;
$InventoryHudData[0, ammoDataName] = Grenade;
$InventoryHudData[0, slot]         = 0;
$InventoryHudData[1, bitmapName]   = "gui/hud_mine";
$InventoryHudData[1, itemDataName] = Mine;
$InventoryHudData[1, ammoDataName] = Mine;
$InventoryHudData[1, slot]         = 1;
$InventoryHudData[2, bitmapName]   = "gui/hud_medpack";
$InventoryHudData[2, itemDataName] = RepairKit;
$InventoryHudData[2, ammoDataName] = RepairKit;
$InventoryHudData[2, slot]         = 3;
$InventoryHudData[3, bitmapName]   = "gui/hud_whiteout_gren";
$InventoryHudData[3, itemDataName] = FlashGrenade;
$InventoryHudData[3, ammoDataName] = FlashGrenade;
$InventoryHudData[3, slot]         = 0;
$InventoryHudData[4, bitmapName]   = "gui/hud_concuss_gren";
$InventoryHudData[4, itemDataName] = ConcussionGrenade;
$InventoryHudData[4, ammoDataName] = ConcussionGrenade;
$InventoryHudData[4, slot]         = 0;
$InventoryHudData[5, bitmapName]   = "gui/hud_handgren";
$InventoryHudData[5, itemDataName] = FlareGrenade;
$InventoryHudData[5, ammoDataName] = FlareGrenade;
$InventoryHudData[5, slot]         = 0;
$InventoryHudData[6, bitmapName]   = "gui/hud_handgren";
$InventoryHudData[6, itemDataName] = CameraGrenade;
$InventoryHudData[6, ammoDataName] = CameraGrenade;
$InventoryHudData[6, slot]         = 0;
$InventoryHudData[7, bitmapName]   = "gui/hud_beacon";
$InventoryHudData[7, itemDataName] = Beacon;
$InventoryHudData[7, ammoDataName] = Beacon;
$InventoryHudData[7, slot]         = 2;

// TR2
$InventoryHudData[8, bitmapName]   = "gui/hud_handgren";
$InventoryHudData[8, itemDataName] = TR2Grenade;
$InventoryHudData[8, ammoDataName] = TR2Grenade;
$InventoryHudData[8, slot]         = 0;

$InventoryHudCount = 9;


//----------------------------------------------------------------------------
//   Inventory Hud
//----------------------------------------------------------------------------
//-------------------------------------------------------------------------   ---
function GameConnection::setInventoryHudBitmap(%client, %slot, %name, %bitmap)
{
    commandToClient(%client, 'setInventoryHudBitmap', %slot, %name, %bitmap);
}

//----------------------------------------------------------------------------
function GameConnection::setInventoryHudItem(%client, %name, %amount, %addItem)
{
    for (%i = 0; %i < $InventoryHudCount; %i++)
        if ($InventoryHudData[%i, itemDataName] $= %name)
        {
            if ($InventoryHudData[%i, ammoDataName] !$= "")
                commandToClient(%client, 'setInventoryHudItem',
                    $InventoryHudData[%i, slot], %amount, %addItem);
            else
                commandToClient(%client, 'setInventoryHudItem',
                    $InventoryHudData[%i, slot], -1, %addItem);
            break;
        }
}

//----------------------------------------------------------------------------
function GameConnection::setInventoryHudAmount(%client, %name, %amount)
{
    for (%i = 0; %i < $InventoryHudCount; %i++)
        if ($InventoryHudData[%i, ammoDataName] $= %name)
        {
            commandToClient(%client, 'setInventoryHudAmount',
                $InventoryHudData[%i, slot], %amount);
            break;
        }
}

//----------------------------------------------------------------------------
function GameConnection::setInventoryHudBackGroundBmp(%client, %name)
{
    commandToClient(%client, 'setInventoryHudBackGroundBmp', %name);
}

//----------------------------------------------------------------------------
function GameConnection::setInventoryHudClearAll(%client)
{
    commandToClient(%client, 'setInventoryHudClearAll');
}

//----------------------------------------------------------------------------
function serverCmdTeamMessageSent(%client, %text)
{
    if (strlen(%text) >= $Host::MaxMessageLen)
        %text = getSubStr(%text, 0, $Host::MaxMessageLen);
    chatMessageTeam(%client, %client.team, '\c3%1: %2', %client.name, %text);
}

//------------------------------------------------------------------------------
function serverCmdMessageSent(%client, %text)
{
    if (strlen(%text) >= $Host::MaxMessageLen)
        %text = getSubStr(%text, 0, $Host::MaxMessageLen);
    chatMessageAll(%client, '\c4%1: %2', %client.name, %text);
}

//------------------------------------------------------------------------------
function serverCmdShowHud(%client, %tag)
{
    %tagName = getWord(%tag, 1);
    %tag = getWord(%tag, 0);
    messageClient(%client, 'OpenHud', "", %tag);
    switch$ (%tag)
    {
    case 'inventoryScreen':
        %client.numFavsCount = 0;
        inventoryScreen::updateHud(1, %client, %tag);
    case 'vehicleHud':
        vehicleHud::updateHud(1, %client, %tag);
    case 'scoreScreen':
        updateScoreHudThread(%client, %tag);
    }
}

//------------------------------------------------------------------------------
function updateScoreHudThread(%client, %tag)
{
    Game.updateScoreHud(%client, %tag);
    cancel(%client.scoreHudThread);
    %client.scoreHudThread = schedule(3000,
        %client, "updateScoreHudThread", %client, %tag);
}

//------------------------------------------------------------------------------
function serverCmdHideHud(%client, %tag)
{
    %tag = getWord(%tag, 0);
    messageClient(%client, 'CloseHud', "", %tag);
    switch$ (%tag)
    {
    case 'scoreScreen':
        cancel(%client.scoreHudThread);
        %client.scoreHudThread = "";
    }
}

//------------------------------------------------------------------------------
function serverCmdSetClientFav(%client, %text)
{
    if (getWord(getField(%text, 0), 0) $= armor)
    {
        %client.curFavList = %text;
        %validList = checkInventory(%client, %text);
        %client.favorites[0] = getField(%text, 1);
        %armor = getArmorDatablock(%client,
            $NameToInv[getField(%validList, 1)]);
        %weaponCount = 0;
        %packCount = 0;
        %grenadeCount = 0;
        %mineCount = 0;
        %count = 1;
        %client.weaponIndex = "";
        %client.packIndex = "";
        %client.grenadeIndex = "";
        %client.mineIndex = "";

        for (%i = 3; %i < getFieldCount(%validList); %i = %i + 2)
        {
            %setItem = false;
            switch$ (getField(%validList, %i-1))
            {
            case weapon:
                if (%weaponCount < %armor.maxWeapons)
                {
                    if (!%weaponCount)
                        %client.weaponIndex = %count;
                    else
                        %client.weaponIndex = %client.weaponIndex TAB %count;
                    %weaponCount++;
                    %setItem = true;
                }
            case pack:
                if (%packCount < 1)
                {
                    %client.packIndex = %count;
                    %packCount++;
                    %setItem = true;
                }
            case grenade:
                if (%grenadeCount < %armor.maxGrenades)
                {
                    if (!%grenadeCount)
                        %client.grenadeIndex = %count;
                    else
                        %client.grenadeIndex = %client.grenadeIndex TAB %count;
                    %grenadeCount++;
                    %setItem = true;
                }
            case mine:
                if (%mineCount < %armor.maxMines)
                {
                    if (!%mineCount)
                        %client.mineIndex = %count;
                    else
                        %client.mineIndex = %client.mineIndex TAB %count;
                    %mineCount++;
                    %setItem = true;
                }
            }

            if (%setItem)
            {
                %client.favorites[%count] = getField(%validList, %i);
                %count++;
            }
        }

        %client.numFavs = %count;
        %client.numFavsCount = 0;
        inventoryScreen::updateHud(1, %client, 'inventoryScreen');
    }
}

//------------------------------------------------------------------------------
function displayObserverHud(%client, %targetClient, %potentialClient)
{
    if (%targetClient > 0)
        bottomPrint(%client,
            "\nYou are now observing: " @ getTaggedString(%targetClient.name), 0, 3);
    else if (%potentialClient > 0)
        bottomPrint(%client,
            "\nObserver Fly Mode\n" @ getTaggedString(%potentialClient.name), 0, 3);
    else
        bottomPrint(%client, "\nObserver Fly Mode", 0, 3);
}
