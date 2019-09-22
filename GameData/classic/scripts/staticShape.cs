//******************************************************************************
//* Default StaticShape functions
//******************************************************************************

function StaticShapeData::onGainPowerEnabled(%data, %obj)
{
    if (%data.ambientThreadPowered)
        %obj.playThread($AmbientThread, "ambient");

    // if it's a deployed object, schedule the power thread; else play it immediately
    if (%data.deployAmbientThread)
        %obj.schedule(750, "playThread", $PowerThread, "Power");
    else
        %obj.playThread($PowerThread, "Power");

    // deployable objects get their recharge rate set right away -- don't set it again unless
    // the object has just been re-enabled
    if (%obj.initDeploy)
        %obj.initDeploy = false;
    else
    {
        if (%obj.getRechargeRate() <= 0)
        {
            %oldERate = %obj.getRechargeRate();
            %obj.setRechargeRate(%oldERate + %data.rechargeRate);
        }
    }

    if (%data.humSound !$= "")
        %obj.playAudio($HumSound, %data.humSound);
    %obj.setPoweredState(true);
}

function StaticShapeData::onLosePowerDisabled(%data, %obj)
{
    %client = %obj.getControllingClient();
    if (%client != 0)
        serverCmdResetControlObject(%client);

    if (%data.ambientThreadPowered)
        %obj.pauseThread($AmbientThread);

    if (!%data.alwaysAmbient)
    {
        %obj.stopThread($PowerThread);
        // MES -- drop shields and stop them from regenerating after power loss
        %obj.setRechargeRate(0.0);
        %obj.setEnergyLevel(0.0);
    }

    if (%data.humSound !$= "")
        %obj.stopAudio($HumSound);
    %obj.setPoweredState(false);
}

function StaticShapeData::gainPower(%data, %obj)
{
    if (%obj.isEnabled())
        %data.onGainPowerEnabled(%obj);

    Parent::gainPower(%data, %obj);
}

function StaticShapeData::losePower(%data, %obj)
{
    if (%obj.isEnabled())
        %data.onLosePowerDisabled(%obj);

    Parent::losePower(%data, %obj);
}

function ShapeBaseData::onEnabled()
{
}

function ShapeBaseData::onDisabled()
{
}

function StaticShapeData::onEnabled(%data, %obj, %prevState)
{
    if (%obj.isPowered())
        %data.onGainPowerEnabled(%obj);

    Parent::onEnabled(%data, %obj, %prevState);
}

function StaticShapeData::onDisabled(%data, %obj, %prevState)
{
    if (%obj.isPowered() || (%data.className $= "Generator"))
        %data.onLosePowerDisabled(%obj);

    Parent::onDisabled(%data, %obj, %prevState);
}

function StaticShape::deploy(%this)
{
   %this.playThread($DeployThread, "deploy");
}

function StaticShapeData::onEndSequence(%data, %obj, %thread)
{
    if (%thread == $DeployThread)
        %obj.setSelfPowered();

    Parent::onEndSequence(%data, %obj, %thread);
}

function ShapeBaseData::onEndSequence()
{
}

function ShapeBaseData::onPickup(%this, %obj, %player, %amount)
{
    // allows implementers to override an "onPickup" event
}

//******************************************************************************
//* Example explosion
//******************************************************************************

datablock EffectProfile(ShapeExplosionEffect)
{
    effectname = "explosions/explosion.xpl03";
    minDistance = 10;
    maxDistance = 50;
};

datablock AudioProfile(ShapeExplosionSound)
{
    filename = "fx/explosions/explosion.xpl03.wav";
    description = AudioExplosion3d;
    preload = true;
    effect = ShapeExplosionEffect;
};

datablock ExplosionData(ShapeExplosion)
{
    explosionShape = "disc_explosion.dts";
    soundProfile = ShapeExplosionSound;
    faceViewer = true;
};

//******************************************************************************
//*   Player Armors  -  Data Blocks (live players are now StaticTSObjects)
//******************************************************************************

datablock StaticShapeData(HeavyMaleHuman_Dead)
{
    className = "deadArmor";
    catagory = "Player Armors";
    shapeFile = "heavy_male_dead.dts";
    isInvincible = true;
};

datablock StaticShapeData(MediumMaleHuman_Dead)
{
    className = "deadArmor";
    catagory = "Player Armors";
    shapeFile = "medium_male_dead.dts";
    isInvincible = true;
};

datablock StaticShapeData(LightMaleHuman_Dead)
{
    className = "deadArmor";
    catagory = "Player Armors";
    shapeFile = "light_male_dead.dts";
    isInvincible = true;
};

function deadArmor::onAdd(%data, %obj)
{
    Parent::onAdd(%data, %obj);
}

//*****************************************************************************
//*   Flagstands - Data Blocks
//*****************************************************************************
datablock StaticShapeData(InteriorFlagStand)
{
    className = "FlagIntStand";
    catagory = "Objectives";
    shapefile = "int_flagstand.dts";
    isInvincible = true;
    needsNoPower = true;
};

datablock StaticShapeData(ExteriorFlagStand)
{
    className = "FlagIntStand";
    catagory = "Objectives";
    shapefile = "ext_flagstand.dts";
    isInvincible = true;
    needsNoPower = true;
};

///////////////////////////////////////////
//flagIntStand::onAdd(%this, %obj)
//%this: objects datablock
//%obj:  the actual object being added
///////////////////////////////////////////

function ExteriorFlagStand::onAdd(%this, %obj)
{
    Parent::onAdd(%this, %obj);
    %obj.playThread($ActivateThread, "activate");
}

function ExteriorFlagStand::onFlagTaken(%this, %obj)
{
    %obj.setThreadDir($ActivateThread, 0);
}

function ExteriorFlagStand::onFlagReturn(%this, %obj)
{
    %obj.setThreadDir($ActivateThread, 1);
}

function ExteriorFlagStand::onCollision(%this, %obj, %colObj)
{
    game.flagStandCollision(%this, %obj, %colObj);
}

function InteriorFlagStand::onCollision(%this, %obj, %colObj)
{
    game.flagStandCollision(%this, %obj, %colObj);
}

///////////////////////////////////////////////
//end flag stand functions
///////////////////////////////////////////////

datablock StaticShapeData(FlipFlop)
{
    catagory = "Objectives";
    shapefile = "switch.dts";

    isInvincible = true;
    cmdCategory = "Objectives";
    cmdIcon = "CMDSwitchIcon";
    cmdMiniIconName = "commander/MiniIcons/com_switch_grey";
    targetTypeTag = 'Switch';
    alwaysAmbient = true;
    needsNoPower = true;
    emap = true;
};

function FlipFlop::onCollision(%data, %obj, %col)
{
    if (%col.getDataBlock().className $= Armor && %col.getState() !$= "Dead")
        %data.playerTouch(%obj, %col);
}

function FlipFlop::playerTouch(%data, %obj, %col)
{
    messageAll('MsgPlayerTouchSwitch', 'Player %1 touched switch %2', %col, %obj);
}

//--------------------------------------------------------------------------

function getTerrainAngle(%point)
{
    %up = "0 0 1";
    %angleRad = mACos(vectorDot(%point, %up));
    %angleDeg = mRadToDeg(%angleRad);
    return %angleDeg;
}

//******************************************************************************
//*   Pulse Sensor  -  Data Blocks                                             *
//******************************************************************************

datablock DebrisData(StaticShapeDebris)
{
    explodeOnMaxBounce = false;

    elasticity = 0.20;
    friction = 0.5;

    lifetime = 17.0;
    lifetimeVariance = 0.0;

    minSpinSpeed = 60;
    maxSpinSpeed = 600;

    numBounces = 10;
    bounceVariance = 0;

    staticOnMaxBounce = true;

    useRadiusMass = true;
    baseRadius = 0.4;

    velocity = 9.0;
    velocityVariance = 4.5;
};

datablock DebrisData(SmallShapeDebris)
{
    explodeOnMaxBounce = false;

    elasticity = 0.20;
    friction = 0.5;

    lifetime = 17.0;
    lifetimeVariance = 0.0;

    minSpinSpeed = 60;
    maxSpinSpeed = 600;

    numBounces = 10;
    bounceVariance = 0;

    staticOnMaxBounce = true;

    useRadiusMass = true;
    baseRadius = 0.2;

    velocity = 5.0;
    velocityVariance = 2.5;
};

datablock AudioProfile(SensorHumSound)
{
    filename    = "fx/powered/sensor_hum.wav";
    description = CloseLooping3d;
    preload = true;
};

datablock SensorData(SensorLgPulseObj)
{
    detects = true;
    detectsUsingLOS = true;
    detectsPassiveJammed = false;
    detectsActiveJammed = false;
    detectsCloaked = false;
    detectionPings = true;
    detectRadius = 300;
};

datablock StaticShapeData(SensorLargePulse) : StaticShapeDamageProfile
{
    className = Sensor;
    catagory = "Sensors";
    shapeFile = "sensor_pulse_large.dts";
    maxDamage = 1.5;
    destroyedLevel = 1.5;
    disabledLevel = 0.85;
    explosion      = ShapeExplosion;
    expDmgRadius = 10.0;
    expDamage = 0.5;
    expImpulse = 2000.0;

    dynamicType = $TypeMasks::SensorObjectType;
    isShielded = true;
    energyPerDamagePoint = 33;
    maxEnergy = 110;
    rechargeRate = 0.31;
    ambientThreadPowered = true;
    humSound = SensorHumSound;

    cmdCategory = "Support";
    cmdIcon = CMDSensorIcon;
    cmdMiniIconName = "commander/MiniIcons/com_sensor_grey";
    targetNameTag = 'Large';
    targetTypeTag = 'Sensor';
    sensorData = SensorLgPulseObj;
    sensorRadius = SensorLgPulseObj.detectRadius;
    sensorColor = "255 194 9";

    debrisShapeName = "debris_generic.dts";
    debris = StaticShapeDebris;
};

datablock SensorData(SensorMedPulseObj)
{
    detects = true;
    detectsUsingLOS = true;
    detectsPassiveJammed = false;
    detectsActiveJammed = false;
    detectsCloaked = false;
    detectionPings = true;
    detectRadius = 175;
};

datablock StaticShapeData(SensorMediumPulse) : StaticShapeDamageProfile
{
    className = Sensor;
    catagory = "Sensors";
    shapeFile = "sensor_pulse_medium.dts";
    maxDamage = 1.2;
    destroyedLevel = 1.2;
    disabledLevel = 0.68;
    explosion      = ShapeExplosion;
    expDmgRadius = 7.0;
    expDamage = 0.4;
    expImpulse = 1500;

    dynamicType = $TypeMasks::SensorObjectType;
    isShielded = true;
    energyPerDamagePoint = 33;
    maxEnergy = 90;
    rechargeRate = 0.31;
    ambientThreadPowered = true;
    humSound = SensorHumSound;

    cmdCategory = "Support";
    cmdIcon = CMDSensorIcon;
    cmdMiniIconName = "commander/MiniIcons/com_sensor_grey";
    targetNameTag = 'Medium';
    targetTypeTag = 'Sensor';
    sensorData = SensorMedPulseObj;
    sensorRadius = SensorMedPulseObj.detectRadius;
    sensorColor = "255 194 9";

    debrisShapeName = "debris_generic.dts";
    debris = StaticShapeDebris;
};

function Sensor::onGainPowerEnabled(%data, %obj)
{
    setTargetSensorData(%obj.target, %data.sensorData);

    Parent::onGainPowerEnabled(%data, %obj);
}

function Sensor::onLosePowerDisabled(%data, %obj)
{
    setTargetSensorData(%obj.target, 0);

    Parent::onLosePowerDisabled(%data, %obj);
}

//******************************************************************************
//*   Generator  -  Data Blocks                                                *
//******************************************************************************

datablock AudioProfile(GeneratorHumSound)
{
    filename    = "fx/powered/generator_hum.wav";
    description = CloseLooping3d;
    preload = true;
};


datablock StaticShapeData(GeneratorLarge) : StaticShapeDamageProfile
{
    className      = Generator;
    catagory       = "Generators";
    shapeFile      = "station_generator_large.dts";
    explosion      = ShapeExplosion;
    maxDamage      = 1.50;
    destroyedLevel = 1.50;
    disabledLevel  = 0.85;
    expDmgRadius = 10.0;
    expDamage = 0.5;
    expImpulse = 1500.0;
    noIndividualDamage = true; //flag to make these invulnerable for certain mission types

    dynamicType = $TypeMasks::GeneratorObjectType;
    isShielded = true;
    energyPerDamagePoint = 30; // z0dd - ZOD, 9/27/02. was 30
    maxEnergy = 70; // z0dd - ZOD, 09/20/02. Was 50
    rechargeRate = 0.15; // z0dd - ZOD, 09/20/02. Was 0.05
    humSound = GeneratorHumSound;

    cmdCategory = "Support";
    cmdIcon = "CMDGeneratorIcon";
    cmdMiniIconName = "commander/MiniIcons/com_generator";
    targetTypeTag = 'Generator';

    debrisShapeName = "debris_generic.dts";
    debris = StaticShapeDebris;
};

datablock StaticShapeData(SolarPanel) : StaticShapeDamageProfile
{
    className = Generator;
    catagory = "Generators";
    shapeFile = "solarpanel.dts";
    explosion = ShapeExplosion;
    maxDamage = 1.00;
    destroyedLevel = 1.00;
    disabledLevel = 0.55;
    expDmgRadius = 5.0;
    expDamage = 0.3;
    expImpulse = 1000.0;
    noIndividualDamage = true; //flag to make these invulnerable for certain mission types
    emap = true;

    isShielded = true;
    energyPerDamagePoint = 30;
    rechargeRate = 0.1; // z0dd - ZOD, 09/20/02. Was 0.05

    dynamicType = $TypeMasks::GeneratorObjectType;
    maxEnergy = 30;
    humSound = GeneratorHumSound;

    cmdCategory = "Support";
    cmdIcon = CMDSolarGeneratorIcon;
    cmdMiniIconName = "commander/MiniIcons/com_solargen_grey";
    targetTypeTag = 'Solar Panel';

    debrisShapeName = "debris_generic.dts";
    debris = StaticShapeDebris;
};

function Generator::onDisabled(%data, %obj, %prevState)
{
    %obj.decPowerCount();

    Parent::onDisabled(%data, %obj, %prevState);
}

function Generator::onEnabled(%data, %obj, %prevState)
{
    %obj.incPowerCount();

    Parent::onEnabled(%data, %obj, %prevState);
}

//******************************************************************************
//Nexus Effect (Hunters)
//******************************************************************************

datablock StaticShapeData(Nexus_Effect)
{
    catagory = "Objectives";
    shapefile = "nexus_effect.dts";
    mass = 10;
    elasticity = 0.2;
    friction = 0.6;
    pickupRadius = 2;
};


datablock StaticShapeData(NexusBase)
{
    catagory = "Objectives";
    shapefile = "Nexusbase.dts";
    mass = 10;
    elasticity = 0.2;
    friction = 0.6;
    pickupRadius = 2;
};

datablock StaticShapeData(NexusCap)
{
    catagory = "Objectives";
    shapefile = "Nexuscap.dts";
    mass = 10;
    elasticity = 0.2;
    friction = 0.6;
    pickupRadius = 2;
};

//******************************************************************************
//*   Static Shape  -  Functions                                               *
//******************************************************************************

function StaticShapeData::create(%block)
{
    %obj = new StaticShape()
    {
        dataBlock = %block;
    };
    return(%obj);
}

function ShapeBase::damage(%this, %sourceObject, %position, %amount, %damageType)
{
    %this.getDataBlock().damageObject(%this, %sourceObject, %position,
        %amount, %damageType);
}

function ShapeBaseData::damageObject(%data, %targetObject, %position, %sourceObject, %amount, %damageType)
{
}

function ShapeBaseData::onDestroyed(%data, %obj, %prevState)
{
}

function ShapeBaseData::checkShields(%data, %targetObject, %position, %amount, %damageType)
{
    %energy   = %targetObject.getEnergyLevel();
    %strength = %energy / %data.energyPerDamagePoint;
    %shieldScale = %data.shieldDamageScale[%damageType];
    if (%shieldScale $= "")
        %shieldScale = 1;

    if (%amount * %shieldScale <= %strength)
    {
        // Shield absorbs all
        %lost = %amount * %shieldScale * %data.energyPerDamagePoint;
        %energy -= %lost;
        %targetObject.setEnergyLevel(%energy);

        %normal = "0.0 0.0 1.0";
        %targetObject.playShieldEffect(%normal);

        return 0;
    }

    // Shield exhausted
    %targetObject.setEnergyLevel(0);
    return %amount - %strength / %shieldScale;
}

function StaticShapeData::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType)
{
    // if this is a non-team mission type and the object is "protected", don't damage it
    if (%data.noIndividualDamage && Game.allowsProtectedStatics())
        return;

    // if this is a Siege mission and this object shouldn't take damage (e.g. vehicle stations)
    if (%data.noDamageInSiege && Game.class $= "SiegeGame")
        return;

    if (%sourceObject && %targetObject.isEnabled())
    {
        if (%sourceObject.client)
        {
            %targetObject.lastDamagedBy = %sourceObject.client;
            %targetObject.lastDamagedByTeam = %sourceObject.client.team;
            %targetObject.damageTimeMS = GetSimTime();
        }
        else
        {
            %targetObject.lastDamagedBy = %sourceObject;
            %targetObject.lastDamagedByTeam = %sourceObject.team;
            %targetObject.damageTimeMS = GetSimTime();
        }
    }

    // Scale damage type & include shield calculations...
    if (%data.isShielded)
        %amount = %data.checkShields(%targetObject, %position, %amount, %damageType);

    %damageScale = %data.damageScale[%damageType];
    if (%damageScale !$= "")
        %amount *= %damageScale;

    //if team damage is off, cap the amount of damage so as not to disable the object...
    if (!$TeamDamage && !%targetObject.getDataBlock().deployedObject)
    {
        // -------------------------------------
        // z0dd - ZOD, 6/24/02. Console spam fix
        if (isObject(%sourceObject))
        {
            //see if the object is being shot by a friendly
            if (%sourceObject.getDataBlock().catagory $= "Vehicles")
                %attackerTeam = getVehicleAttackerTeam(%sourceObject);
            else
                %attackerTeam = %sourceObject.team;
        }

        if ((%targetObject.getTarget() != -1) && isTargetFriendly(%targetObject.getTarget(), %attackerTeam))
        {
            %curDamage = %targetObject.getDamageLevel();
            %availableDamage = %targetObject.getDataBlock().disabledLevel - %curDamage - 0.05;
            if (%amount > %availableDamage)
                %amount = %availableDamage;
        }
    }

    // if there's still damage to apply
    if (%amount > 0)
        %targetObject.applyDamage(%amount);
}

// little special casing for the above function
function getVehicleAttackerTeam(%vehicleId)
{
    %name = %vehicleId.getDataBlock().getName();
    if (%name $= "BomberFlyer" || %name $= "AssaultVehicle")
        %gunner = %vehicleId.getMountNodeObject(1);
    else
        %gunner = %vehicleId.getMountNodeObject(0);

    if (%gunner)
        return %gunner.team;

    return %vehicleId.team;
}

function StaticShapeData::onDamage(%this, %obj)
{
    // Set damage state based on current damage level
    %damage = %obj.getDamageLevel();
    if (%damage >= %this.destroyedLevel)
    {
        if (%obj.getDamageState() !$= "Destroyed")
        {
            %obj.setDamageState(Destroyed);
            // if object has an explosion damage radius associated with it, apply explosion damage
            if (%this.expDmgRadius)
                RadiusExplosion(%obj, %obj.getWorldBoxCenter(),
                    %this.expDmgRadius, %this.expDamage, %this.expImpulse,
                    %obj, $DamageType::Explosion);
            %obj.setDamageLevel(%this.maxDamage);
        }
    }
    else
    {
        if (%damage >= %this.disabledLevel)
        {
            if (%obj.getDamageState() !$= "Disabled")
                %obj.setDamageState(Disabled);
        }
        else
        {
            if (%obj.getDamageState() !$= "Enabled")
                %obj.setDamageState(Enabled);
        }
    }
}

// --------------------------------------------------------------------
// Team logos - only the logo projector should be placed in a mission

datablock StaticShapeData(BaseLogo)  //storm logo
{
    className = Logo;
    shapeFile = "teamlogo_storm.dts";
    alwaysAmbient = true;
};

datablock StaticShapeData(BaseBLogo)  //Inferno Logo
{
    className = Logo;
    shapeFile = "teamlogo_inf.dts";
    alwaysAmbient = true;
};

datablock StaticShapeData(BiodermLogo)
{
    className = Logo;
    shapeFile = "teamlogo_bd.dts";
    alwaysAmbient = true;
};

datablock StaticShapeData(BEagleLogo)
{
    className = Logo;
    shapeFile = "teamlogo_be.dts";
    alwaysAmbient = true;
};

datablock StaticShapeData(DSwordLogo)
{
    className = Logo;
    shapeFile = "teamlogo_ds.dts";
    alwaysAmbient = true;
};

datablock StaticShapeData(COTPLogo)
{
    className = Logo;
    shapeFile = "teamlogo_hb.dts";
    alwaysAmbient = true;
};

datablock StaticShapeData(SwolfLogo)
{
    className = Logo;
    shapeFile = "teamlogo_sw.dts";
    alwaysAmbient = true;
};

datablock StaticShapeData(LogoProjector)
{
    className = Projector;
    catagory = "Objectives";
    shapeFile = "teamlogo_projector.dts";
    alwaysAmbient = true;
    isInvincible = true;
};

function Projector::onAdd(%data, %obj)
{
    Parent::onAdd(%data, %obj);

    %obj.holo = 0;
}

////////////////////////////////////////////
//  Tapestries
///////////////////////////////////////////
datablock StaticShapeData(Banner_Honor)
{
    catagory = "Eyecandy";
    shapefile = "banner_honor.dts";
};

datablock StaticShapeData(Banner_Strength)
{
    catagory = "Eyecandy";
    shapefile = "banner_strength.dts";
};

datablock StaticShapeData(Banner_Unity)
{
    catagory = "Eyecandy";
    shapefile = "banner_unity.dts";
};

////////////////////////////////////////////////////////////////////////////////
//

//--------------------------------------------------------------------------
// Totally static objects
//  The format of these strings are:
//  0: Catagory
//  1: Name
//  2: File
//  3:      MaxSlope             [ only used with the randomOrg function ]
//  4:      ZOffset              [ only used with the randomOrg function ]
//  5:      slopeWithTerrain     [ only used with the randomOrg function ]
//  6:      minScale             [ only used with the randomOrg function ]
//  7:      maxScale             [ only used with the randomOrg function ]

$StaticTSObjects[0]  = "Organics BiodermPlant3 xorg3.dts";
$StaticTSObjects[1]  = "Organics BiodermPlant4 xorg4.dts";
$StaticTSObjects[2]  = "Organics BiodermPlant5 xorg5.dts";
$StaticTSObjects[3]  = "Organics BiodermPlant20 xorg20.dts";
$StaticTSObjects[4]  = "Organics BiodermPlant21 xorg21.dts";
$StaticTSObjects[5]  = "Organics BiodermPlant22 xorg22.dts";

$StaticTSObjects[6]  = "Organics BEPlant1 borg1.dts 40 0.35 1 0.5 2";
$StaticTSObjects[7] = "Organics BEPlant5 borg5.dts 40 0.0 1 1 1.5";
$StaticTSObjects[8] = "Organics BEPlant6 borg6.dts";
$StaticTSObjects[9] = "Organics BEPlant7 borg7.dts";

$StaticTSObjects[10] = "Organics BEPlant12 borg12.dts";
$StaticTSObjects[11] = "Organics BEPlant13 borg13.dts";
$StaticTSObjects[12] = "Organics BELgTree16 borg16.dts 20 -3.0 0 0.8 1.5";
$StaticTSObjects[13] = "Organics BESmTree17 borg17.dts 20 -3.0 1 0.8 1.5";
$StaticTSObjects[14] = "Organics BELgTree18 borg18.dts 20 -3.0 0 0.8 1.5";
$StaticTSObjects[15] = "Organics BELgTree19 borg19.dts 20 -3.0 0 0.8 1.5";
$StaticTSObjects[16] = "Organics BEPlant20 borg20.dts";

$StaticTSObjects[17] = "Organics BEPlant23 borg23.dts";
$StaticTSObjects[18] = "Organics BEPlant25 borg25.dts";

$StaticTSObjects[19] = "Organics BEPlant31 borg31.dts";
$StaticTSObjects[20] = "Organics BEPlant32 borg32.dts";
$StaticTSObjects[21] = "Organics BEPlant33 borg33.dts";
$StaticTSObjects[22] = "Organics BEPlant34 borg34.dts";

$StaticTSObjects[23] = "Organics PhoenixPlant1 porg1.dts";
$StaticTSObjects[24] = "Organics PhoenixPlant2 porg2.dts";
$StaticTSObjects[25] = "Organics PhoenixPlant3 porg3.dts";
$StaticTSObjects[26] = "Organics PhoenixPlant5 porg5.dts 25 -0.2 1 0.6 1.0";
$StaticTSObjects[27] = "Organics PhoenixPlant6 porg6.dts";
$StaticTSObjects[28] = "Organics PhoenixPlant20 porg20.dts";

$StaticTSObjects[29] = "Organics PhoenixPlant22 porg22.dts 25 0.1 1 0.8 1.4";
$StaticTSObjects[30] = "Organics SWTree20 sorg20.dts";
$StaticTSObjects[31] = "Organics SWShrub21 sorg21.dts";
$StaticTSObjects[32] = "Organics SWTree22 sorg22.dts";
$StaticTSObjects[33] = "Organics SWShrub23 sorg23.dts";
$StaticTSObjects[34] = "Organics SWShrub24 sorg24.dts";
$StaticTSObjects[35] = "Stackables Crate1 stackable1l.dts";
$StaticTSObjects[36] = "Stackables Crate2 stackable1m.dts";
$StaticTSObjects[37] = "Stackables Crate3 stackable1s.dts";
$StaticTSObjects[38] = "Stackables Crate4 stackable2l.dts";
$StaticTSObjects[39] = "Stackables Crate5 stackable2m.dts";
$StaticTSObjects[40] = "Stackables Crate6 stackable2s.dts";
$StaticTSObjects[41] = "Stackables Crate7 stackable3l.dts";
$StaticTSObjects[42] = "Stackables Crate8 stackable3m.dts";
$StaticTSObjects[43] = "Stackables Crate9 stackable3s.dts";
$StaticTSObjects[44] = "Stackables Crate10 stackable4l.dts";
$StaticTSObjects[45] = "Stackables Crate11 stackable4m.dts";
$StaticTSObjects[46] = "Stackables Crate12 stackable5l.dts";
$StaticTSObjects[47] = "Debris ScoutWreckageShape vehicle_air_scout_wreck.dts";
$StaticTSObjects[48] = "Debris TankWreckageShape vehicle_land_assault_wreck.dts";
$StaticTSObjects[49] = "Organics DSPlant16 dorg16.dts 20 -3.0 0 0.8 1.5";
$StaticTSObjects[50] = "Organics DSPlant17 dorg17.dts 20 -3.0 1 0.8 1.5";
$StaticTSObjects[51] = "Organics DSPlant18 dorg18.dts 20 -3.0 0 0.8 1.5";
$StaticTSObjects[52] = "Organics DSPlant19 dorg19.dts 20 -3.0 0 0.8 1.5";

$StaticTSObjects[53] = "PlayerArmors LightMaleHumanArmorImage light_male.dts";
$StaticTSObjects[54] = "PlayerArmors MediumMaleHumanArmorImage medium_male.dts";
$StaticTSObjects[55] = "PlayerArmors HeavyMaleHumanArmorImage heavy_male.dts";
$StaticTSObjects[56] = "PlayerArmors LightFemaleHumanArmorImage light_female.dts";
$StaticTSObjects[57] = "PlayerArmors MediumFemaleHumanArmorImage medium_female.dts";
$StaticTSObjects[58] = "PlayerArmors HeavyFemaleHumanArmorImage heavy_male.dts";
$StaticTSObjects[59] = "PlayerArmors LightMaleBiodermArmorImage bioderm_light.dts";
$StaticTSObjects[60] = "PlayerArmors MediumMaleBiodermArmorImage bioderm_medium.dts";
$StaticTSObjects[61] = "PlayerArmors HeavyMaleBiodermArmorImage bioderm_heavy.dts";

$StaticTSObjects[62] = "Organics BEGrass1 Borg6.dts";

$StaticTSObjects[63] = "Plugs bePlug bmiscf.dts";
$StaticTSObjects[64] = "Plugs dsPlug dmiscf.dts";
$StaticTSObjects[65] = "Plugs xPlug xmiscf.dts";
$StaticTSObjects[66] = "Plugs hPlug pmiscf.dts";
$StaticTSObjects[67] = "Plugs swPlug smiscf.dts";

$StaticTSObjects[68] = "Statues Base statue_base.dts";
$StaticTSObjects[69] = "Statues HeavyMaleStatue statue_hmale.dts";
$StaticTSObjects[70] = "Statues LightFemaleStatue statue_lfemale.dts";
$StaticTSObjects[71] = "Statues LightMaleStatue statue_lmale.dts";
$StaticTSObjects[72] = "Statues Plaque statue_plaque.dts";

$StaticTSObjects[73] = "Debris BomberDebris1 bdb1.dts";
$StaticTSObjects[74] = "Debris BomberDebris2 bdb2.dts";
$StaticTSObjects[75] = "Debris BomberDebris3 bdb3.dts";
$StaticTSObjects[76] = "Debris BomberDebris4 bdb4.dts";
$StaticTSObjects[77] = "Debris BomberDebris5 bdb5.dts";
$StaticTSObjects[78] = "Debris HavocDebris1 hdb1.dts";
$StaticTSObjects[79] = "Debris HavocDebris2 hdb2.dts";
$StaticTSObjects[80] = "Debris HavocDebris3 hdb3.dts";
$StaticTSObjects[81] = "Debris IDebris1  idb.dts";
$StaticTSObjects[82] = "Debris MPBDebris1 mpbdb1.dts";
$StaticTSObjects[83] = "Debris MPBDebris2 mpbdb2.dts";
$StaticTSObjects[84] = "Debris MPBDebris3 mpbdb3.dts";
$StaticTSObjects[85] = "Debris MPBDebris4 mpbdb4.dts";
$StaticTSObjects[86] = "Debris ScoutDebris1 sdb1.dts";
$StaticTSObjects[87] = "Debris TankDebris1 tdb1.dts";
$StaticTSObjects[88] = "Debris TankDebris2 tdb2.dts";
$StaticTSObjects[89] = "Debris TankDebris3 tdb3.dts";
$StaticTSObjects[90] = "Debris GraveMarker1 gravemarker1.dts";
$StaticTSObjects[91] = "Test Test1 test1.dts";
$StaticTSObjects[92] = "Test Test2 test2.dts";
$StaticTSObjects[93] = "Test Test3 test3.dts";
$StaticTSObjects[94] = "Test Test4 test4.dts";
$StaticTSObjects[95] = "Test Test5 test5.dts";

$NumStaticTSObjects = 96;

function TSStatic::create(%shapeName)
{
    %obj = new TSStatic()
    {
        shapeName = %shapeName;
    };
    return %obj;
}

function TSStatic::damage(%this)
{
}
