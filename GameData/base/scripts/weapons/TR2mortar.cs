//--------------------------------------
// Mortar
//--------------------------------------

//--------------------------------------------------------------------------
// Force-Feedback Effects
//--------------------------------------
datablock EffectProfile(TR2MortarSwitchEffect)
{
    effectname = "weapons/mortar_activate";
    minDistance = 2.5;
    maxDistance = 2.5;
};

datablock EffectProfile(TR2MortarFireEffect)
{
    effectname = "weapons/mortar_fire";
    minDistance = 2.5;
    maxDistance = 5.0;
};

datablock EffectProfile(TR2MortarReloadEffect)
{
    effectname = "weapons/mortar_reload";
    minDistance = 2.5;
    maxDistance = 2.5;
};

datablock EffectProfile(TR2MortarDryFireEffect)
{
    effectname = "weapons/mortar_dryfire";
    minDistance = 2.5;
    maxDistance = 2.5;
};

datablock EffectProfile(TR2MortarExplosionEffect)
{
    effectname = "explosions/explosion.xpl03";
    minDistance = 30;
    maxDistance = 65;
};

//--------------------------------------------------------------------------
// Sounds
//--------------------------------------
datablock AudioProfile(TR2MortarSwitchSound)
{
    filename    = "fx/weapons/mortar_activate.wav";
    description = AudioClosest3d;
    preload = true;
    effect = TR2MortarSwitchEffect;
};

datablock AudioProfile(TR2MortarReloadSound)
{
    filename    = "fx/weapons/mortar_reload.wav";
    description = AudioClosest3d;
    preload = true;
    effect = TR2MortarReloadEffect;
};

datablock AudioProfile(TR2MortarFireSound)
{
    filename    = "fx/weapons/mortar_fire.wav";
    description = AudioDefault3d;
    preload = true;
    effect = TR2MortarFireEffect;
};

datablock AudioProfile(TR2MortarProjectileSound)
{
    filename    = "fx/weapons/mortar_projectile.wav";
    description = ProjectileLooping3d;
    preload = true;
};

datablock AudioProfile(TR2MortarExplosionSound)
{
    filename    = "fx/weapons/mortar_explode.wav";
    description = AudioBIGExplosion3d;
    preload = true;
    effect = TR2MortarExplosionEffect;
};

datablock AudioProfile(UnderwaterTR2MortarExplosionSound)
{
    filename    = "fx/weapons/mortar_explode_UW.wav";
    description = AudioBIGExplosion3d;
    preload = true;
    effect = TR2MortarExplosionEffect;
};

datablock AudioProfile(TR2MortarDryFireSound)
{
    filename    = "fx/weapons/mortar_dryfire.wav";
    description = AudioClose3d;
    preload = true;
    effect = TR2MortarDryFireEffect;
};

//----------------------------------------------------------------------------
// Bubbles
//----------------------------------------------------------------------------
datablock ParticleData(TR2MortarBubbleParticle)
{
    dragCoefficient      = 0.0;
    gravityCoefficient   = -0.25;
    inheritedVelFactor   = 0.0;
    constantAcceleration = 0.0;
    lifetimeMS           = 1500;
    lifetimeVarianceMS   = 600;
    useInvAlpha          = false;
    textureName          = "special/bubbles";

    spinRandomMin        = -100.0;
    spinRandomMax        =  100.0;

    colors[0]     = "0.7 0.8 1.0 0.4";
    colors[1]     = "0.7 0.8 1.0 0.4";
    colors[2]     = "0.7 0.8 1.0 0.0";
    sizes[0]      = 0.8;
    sizes[1]      = 0.8;
    sizes[2]      = 0.8;
    times[0]      = 0.0;
    times[1]      = 0.5;
    times[2]      = 1.0;
};

datablock ParticleEmitterData(TR2MortarBubbleEmitter)
{
    ejectionPeriodMS = 9;
    periodVarianceMS = 0;
    ejectionVelocity = 1.0;
    ejectionOffset   = 0.1;
    velocityVariance = 0.5;
    thetaMin         = 0;
    thetaMax         = 80;
    phiReferenceVel  = 0;
    phiVariance      = 360;
    overrideAdvances = false;
    particles = "TR2MortarBubbleParticle";
};

//--------------------------------------------------------------------------
// Splash
//--------------------------------------------------------------------------
datablock ParticleData(TR2MortarSplashParticle)
{
    dragCoefficient      = 1;
    gravityCoefficient   = 0.0;
    inheritedVelFactor   = 0.2;
    constantAcceleration = -1.4;
    lifetimeMS           = 300;
    lifetimeVarianceMS   = 0;
    textureName          = "special/droplet";
    colors[0]     = "0.7 0.8 1.0 1.0";
    colors[1]     = "0.7 0.8 1.0 0.5";
    colors[2]     = "0.7 0.8 1.0 0.0";
    sizes[0]      = 0.05;
    sizes[1]      = 0.2;
    sizes[2]      = 0.2;
    times[0]      = 0.0;
    times[1]      = 0.5;
    times[2]      = 1.0;
};

datablock ParticleEmitterData(TR2MortarSplashEmitter)
{
    ejectionPeriodMS = 4;
    periodVarianceMS = 0;
    ejectionVelocity = 3;
    velocityVariance = 1.0;
    ejectionOffset   = 0.0;
    thetaMin         = 0;
    thetaMax         = 50;
    phiReferenceVel  = 0;
    phiVariance      = 360;
    overrideAdvances = false;
    orientParticles  = true;
    lifetimeMS       = 100;
    particles = "TR2MortarSplashParticle";
};

datablock SplashData(TR2MortarSplash)
{
    numSegments = 10;
    ejectionFreq = 10;
    ejectionAngle = 20;
    ringLifetime = 0.4;
    lifetimeMS = 400;
    velocity = 3.0;
    startRadius = 0.0;
    acceleration = -3.0;
    texWrap = 5.0;

    texture = "special/water2";

    emitter[0] = TR2MortarSplashEmitter;

    colors[0] = "0.7 0.8 1.0 0.0";
    colors[1] = "0.7 0.8 1.0 1.0";
    colors[2] = "0.7 0.8 1.0 0.0";
    colors[3] = "0.7 0.8 1.0 0.0";
    times[0] = 0.0;
    times[1] = 0.4;
    times[2] = 0.8;
    times[3] = 1.0;
};

//---------------------------------------------------------------------------
// Mortar Shockwaves
//---------------------------------------------------------------------------
datablock ShockwaveData(UnderwaterTR2MortarShockwave)
{
    width = 6.0;
    numSegments = 32;
    numVertSegments = 6;
    velocity = 10;
    acceleration = 20.0;
    lifetimeMS = 900;
    height = 1.0;
    verticalCurve = 0.5;
    is2D = false;

    texture[0] = "special/shockwave4";
    texture[1] = "special/gradient";
    texWrap = 6.0;

    times[0] = 0.0;
    times[1] = 0.5;
    times[2] = 1.0;

    colors[0] = "0.4 0.4 1.0 0.50";
    colors[1] = "0.4 0.4 1.0 0.25";
    colors[2] = "0.4 0.4 1.0 0.0";

    mapToTerrain = true;
    orientToNormal = false;
    renderBottom = false;
};

datablock ShockwaveData(TR2MortarShockwave)
{
    width = 6.0;
    numSegments = 32;
    numVertSegments = 6;
    velocity = 30;
    acceleration = 20.0;
    lifetimeMS = 500;
    height = 1.0;
    verticalCurve = 0.5;
    is2D = false;

    texture[0] = "special/shockwave4";
    texture[1] = "special/gradient";
    texWrap = 6.0;

    times[0] = 0.0;
    times[1] = 0.5;
    times[2] = 1.0;

    colors[0] = "0.4 1.0 0.4 0.50";
    colors[1] = "0.4 1.0 0.4 0.25";
    colors[2] = "0.4 1.0 0.4 0.0";

    mapToTerrain = true;
    orientToNormal = false;
    renderBottom = false;
};

//--------------------------------------------------------------------------
// Mortar Explosion Particle effects
//--------------------------------------
datablock ParticleData(TR2MortarCrescentParticle)
{
    dragCoefficient      = 2;
    gravityCoefficient   = 0.0;
    inheritedVelFactor   = 0.2;
    constantAcceleration = -0.0;
    lifetimeMS           = 600;
    lifetimeVarianceMS   = 000;
    textureName          = "special/crescent3";
    colors[0]     = "0.7 1.0 0.7 1.0";
    colors[1]     = "0.7 1.0 0.7 0.5";
    colors[2]     = "0.7 1.0 0.7 0.0";
    sizes[0]      = 8.0;
    sizes[1]      = 16.0;
    sizes[2]      = 18.0;
    times[0]      = 0.0;
    times[1]      = 0.5;
    times[2]      = 1.0;
};

datablock ParticleEmitterData(TR2MortarCrescentEmitter)
{
    ejectionPeriodMS = 25;
    periodVarianceMS = 0;
    ejectionVelocity = 40;
    velocityVariance = 5.0;
    ejectionOffset   = 0.0;
    thetaMin         = 0;
    thetaMax         = 80;
    phiReferenceVel  = 0;
    phiVariance      = 360;
    overrideAdvances = false;
    orientParticles  = true;
    lifetimeMS       = 200;
    particles = "TR2MortarCrescentParticle";
};

datablock ParticleData(TR2MortarExplosionSmoke)
{
    dragCoeffiecient     = 0.4;
    gravityCoefficient   = -0.30;   // rises slowly
    inheritedVelFactor   = 0.025;

    lifetimeMS           = 1250;
    lifetimeVarianceMS   = 500;

    textureName          = "particleTest";

    useInvAlpha =  true;
    spinRandomMin = -100.0;
    spinRandomMax =  100.0;

    textureName = "special/Smoke/bigSmoke";

    colors[0]     = "0.7 0.7 0.7 0.0";
    colors[1]     = "0.4 0.4 0.4 0.5";
    colors[2]     = "0.4 0.4 0.4 0.5";
    colors[3]     = "0.4 0.4 0.4 0.0";
    sizes[0]      = 25.0;
    sizes[1]      = 28.0;
    sizes[2]      = 40.0;
    sizes[3]      = 56.0;
    times[0]      = 0.0;
    times[1]      = 0.333;
    times[2]      = 0.666;
    times[3]      = 1.0;
};

datablock ParticleEmitterData(TR2MortarExplosionSmokeEmitter)
{
    ejectionPeriodMS = 10;
    periodVarianceMS = 0;

    ejectionOffset = 8.0;


    ejectionVelocity = 7.0;//3.25;
    velocityVariance = 1.2;

    thetaMin         = 0.0;
    thetaMax         = 90.0;

    lifetimeMS       = 500;

    particles = "TR2MortarExplosionSmoke";
};

//---------------------------------------------------------------------------
// Underwater Explosion
//---------------------------------------------------------------------------
datablock ParticleData(TR2UnderwaterExplosionSparks)
{
    dragCoefficient      = 0;
    gravityCoefficient   = 0.0;
    inheritedVelFactor   = 0.2;
    constantAcceleration = 0.0;
    lifetimeMS           = 500;
    lifetimeVarianceMS   = 350;
    textureName          = "special/crescent3";
    colors[0]     = "0.4 0.4 1.0 1.0";
    colors[1]     = "0.4 0.4 1.0 1.0";
    colors[2]     = "0.4 0.4 1.0 0.0";
    sizes[0]      = 3.5;
    sizes[1]      = 3.5;
    sizes[2]      = 3.5;
    times[0]      = 0.0;
    times[1]      = 0.5;
    times[2]      = 1.0;
};

datablock ParticleEmitterData(TR2UnderwaterExplosionSparksEmitter)
{
    ejectionPeriodMS = 2;
    periodVarianceMS = 0;
    ejectionVelocity = 17;
    velocityVariance = 4;
    ejectionOffset   = 0.0;
    thetaMin         = 0;
    thetaMax         = 60;
    phiReferenceVel  = 0;
    phiVariance      = 360;
    overrideAdvances = false;
    orientParticles  = true;
    lifetimeMS       = 100;
    particles = "TR2UnderwaterExplosionSparks";
};

datablock ParticleData(TR2MortarExplosionBubbleParticle)
{
    dragCoefficient      = 0.0;
    gravityCoefficient   = -0.25;
    inheritedVelFactor   = 0.0;
    constantAcceleration = 0.0;
    lifetimeMS           = 1500;
    lifetimeVarianceMS   = 600;
    useInvAlpha          = false;
    textureName          = "special/bubbles";

    spinRandomMin        = -100.0;
    spinRandomMax        =  100.0;

    colors[0]     = "0.7 0.8 1.0 0.0";
    colors[1]     = "0.7 0.8 1.0 0.4";
    colors[2]     = "0.7 0.8 1.0 0.0";
    sizes[0]      = 2.0;
    sizes[1]      = 2.0;
    sizes[2]      = 2.0;
    times[0]      = 0.0;
    times[1]      = 0.8;
    times[2]      = 1.0;
};

datablock ParticleEmitterData(TR2MortarExplosionBubbleEmitter)
{
    ejectionPeriodMS = 5;
    periodVarianceMS = 0;
    ejectionVelocity = 1.0;
    ejectionOffset   = 7.0;
    velocityVariance = 0.5;
    thetaMin         = 0;
    thetaMax         = 80;
    phiReferenceVel  = 0;
    phiVariance      = 360;
    overrideAdvances = false;
    particles = "TR2MortarExplosionBubbleParticle";
};

datablock DebrisData(UnderwaterTR2MortarDebris)
{
    emitters[0] = MortarExplosionBubbleEmitter;

    explodeOnMaxBounce = true;

    elasticity = 0.4;
    friction = 0.2;

    lifetime = 1.5;
    lifetimeVariance = 0.2;

    numBounces = 1;
};

datablock ExplosionData(UnderwaterTR2MortarSubExplosion1)
{
    explosionShape = "disc_explosion.dts";
    faceViewer           = true;
    delayMS = 100;
    offset = 3.0;
    playSpeed = 1.5;

    sizes[0] = "3.25 3.25 3.25";//"0.75 0.75 0.75";
    sizes[1] = "2.5 2.5 2.5";//"1.0  1.0  1.0";
    sizes[2] = "1.5 1.5 1.5";//"0.5 0.5 0.5";
    times[0] = 0.0;
    times[1] = 0.5;
    times[2] = 1.0;
};

datablock ExplosionData(UnderwaterTR2MortarSubExplosion2)
{
    explosionShape = "disc_explosion.dts";
    faceViewer           = true;
    delayMS = 50;
    offset = 3.0;
    playSpeed = 0.75;

    sizes[0] = "4.5 4.5 4.5";//"1.5 1.5 1.5";
    sizes[1] = "4.5 4.5 4.5";//"1.5 1.5 1.5";
    sizes[2] = "3.5 3.5 3.5";//"1.0 1.0 1.0";
    times[0] = 0.0;
    times[1] = 0.5;
    times[2] = 1.0;
};

datablock ExplosionData(UnderwaterTR2MortarSubExplosion3)
{
    explosionShape = "disc_explosion.dts";
    faceViewer           = true;
    delayMS = 0;
    offset = 0.0;
    playSpeed = 0.5;

    sizes[0] = "1.0 1.0 1.0";
    sizes[1] = "2.0 2.0 2.0";
    sizes[2] = "1.5 1.5 1.5";
    times[0] = 0.0;
    times[1] = 0.5;
    times[2] = 1.0;
};

datablock ExplosionData(UnderwaterTR2MortarExplosion)
{
    soundProfile   = UnderwaterTR2MortarExplosionSound;

    shockwave = UnderwaterTR2MortarShockwave;
    shockwaveOnTerrain = true;

    subExplosion[0] = UnderwaterTR2MortarSubExplosion1;
    subExplosion[1] = UnderwaterTR2MortarSubExplosion2;
    subExplosion[2] = UnderwaterTR2MortarSubExplosion3;

    emitter[0] = TR2MortarExplosionBubbleEmitter;
    emitter[1] = TR2UnderwaterExplosionSparksEmitter;

    shakeCamera = true;
    camShakeFreq = "8.0 9.0 7.0";
    camShakeAmp = "100.0 100.0 100.0";
    camShakeDuration = 1.3;
    camShakeRadius = 25.0;
};

//---------------------------------------------------------------------------
// Explosion
//---------------------------------------------------------------------------

datablock ExplosionData(TR2MortarSubExplosion1)
{
    explosionShape = "mortar_explosion.dts";
    faceViewer           = true;

    delayMS = 100;

    offset = 5.0;

    playSpeed = 1.5;

    sizes[0] = "8.0 8.0 8.0";//"1.5 1.5 1.5";
    sizes[1] = "8.0 8.0 8.0";//"1.5 1.5 1.5";
    times[0] = 0.0;
    times[1] = 1.0;
};

datablock ExplosionData(TR2MortarSubExplosion2)
{
    explosionShape = "mortar_explosion.dts";
    faceViewer           = true;

    delayMS = 50;

    offset = 5.0;

    playSpeed = 1.0;

    sizes[0] = "12.0 12.0 12.0";//"3.0 3.0 3.0";
    sizes[1] = "12.0 12.0 12.0";//"3.0 3.0 3.0";
    times[0] = 0.0;
    times[1] = 1.0;
};

datablock ExplosionData(TR2MortarSubExplosion3)
{
    explosionShape = "mortar_explosion.dts";
    faceViewer           = true;
    delayMS = 0;
    offset = 0.0;
    playSpeed = 0.7;

    sizes[0] = "24.0 24.0 24.0";//"3.0 3.0 3.0";
    sizes[1] = "48.0 48.0 48.0";//"6.0 6.0 6.0";
    times[0] = 0.0;
    times[1] = 1.0;
};

datablock ExplosionData(TR2MortarExplosion)
{
    soundProfile   = TR2MortarExplosionSound;

    shockwave = MortarShockwave;
    shockwaveOnTerrain = true;

    subExplosion[0] = TR2MortarSubExplosion1;
    subExplosion[1] = TR2MortarSubExplosion2;
    subExplosion[2] = TR2MortarSubExplosion3;

    emitter[0] = TR2MortarExplosionSmokeEmitter;
    emitter[1] = TR2MortarCrescentEmitter;

    shakeCamera = true;
    camShakeFreq = "8.0 9.0 7.0";
    camShakeAmp = "100.0 100.0 100.0";
    camShakeDuration = 1.3;
    camShakeRadius = 40.0;//25.0;
};

//---------------------------------------------------------------------------
// Smoke particles
//---------------------------------------------------------------------------
datablock ParticleData(TR2MortarSmokeParticle)
{
    dragCoeffiecient     = 0.4;
    gravityCoefficient   = -0.3;   // rises slowly
    inheritedVelFactor   = 0.125;

    lifetimeMS           =  1200;
    lifetimeVarianceMS   =  200;
    useInvAlpha          =  true;
    spinRandomMin        = -100.0;
    spinRandomMax        =  100.0;

    animateTexture = false;

    textureName = "special/Smoke/bigSmoke";

    colors[0]     = "0.7 1.0 0.7 0.5";
    colors[1]     = "0.3 0.7 0.3 0.8";
    colors[2]     = "0.0 0.0 0.0 0.0";
    sizes[0]      = 4.0;//2.0;
    sizes[1]      = 8.0;//4.0;
    sizes[2]      = 17.0;//8.5;
    times[0]      = 0.0;
    times[1]      = 0.5;
    times[2]      = 1.0;
};


datablock ParticleEmitterData(TR2MortarSmokeEmitter)
{
    ejectionPeriodMS = 10;
    periodVarianceMS = 3;

    ejectionVelocity = 4.0;//2.25;
    velocityVariance = 0.55;

    thetaMin         = 0.0;
    thetaMax         = 40.0;

    particles = "TR2MortarSmokeParticle";
};

//--------------------------------------------------------------------------
// Projectile
//--------------------------------------
datablock GrenadeProjectileData(TR2MortarShot)
{
    projectileShapeName = "mortar_projectile.dts";
    emitterDelay        = -1;
    directDamage        = 0.0;
    hasDamageRadius     = true;
    indirectDamage      = 0.2;
    damageRadius        = 50.0;
    radiusDamageType    = $DamageType::Mortar;
    kickBackStrength    = 9500;

    explosion           = "MortarExplosion";
    underwaterExplosion = "UnderwaterMortarExplosion";
    velInheritFactor    = 0.5;
    splash              = TR2MortarSplash;
    depthTolerance      = 10.0; // depth at which it uses underwater explosion

    baseEmitter         = TR2MortarSmokeEmitter;
    bubbleEmitter       = TR2MortarBubbleEmitter;

    grenadeElasticity = 0.15;
    grenadeFriction   = 0.4;
    armingDelayMS     = 1200;//2000;
    muzzleVelocity    = 120.0;//63.7;
    drag              = 0.1;
    gravityMod        = 1.5;

    sound			 = TR2MortarProjectileSound;

    hasLight    = true;
    lightRadius = 4;
    lightColor  = "0.05 0.2 0.05";

    hasLightUnderwaterColor = true;
    underWaterLightColor = "0.05 0.075 0.2";
};

//--------------------------------------------------------------------------
// Ammo
//--------------------------------------

datablock ItemData(TR2MortarAmmo)
{
    className = Ammo;
    catagory = "Ammo";
    shapeFile = "ammo_mortar.dts";
    mass = 1;
    elasticity = 0.2;
    friction = 0.6;
    pickupRadius = 2;
    pickUpName = "some mortar ammo";

    computeCRC = false;
};

//--------------------------------------------------------------------------
// Weapon
//--------------------------------------
datablock ItemData(TR2Mortar)
{
    className = Weapon;
    catagory = "Spawn Items";
    shapeFile = "TR2weapon_mortar.dts";
    image = TR2MortarImage;
    mass = 1;
    elasticity = 0.2;
    friction = 0.6;
    pickupRadius = 2;
    pickUpName = "a mortar gun";

    computeCRC = true;
    emap = true;
};

datablock ShapeBaseImageData(TR2MortarImage)
{
    className = WeaponImage;
    shapeFile = "TR2weapon_mortar.dts";
    item = TR2Mortar;
    ammo = TR2MortarAmmo;
    offset = "0 0 0";
    emap = true;

    projectile = TR2MortarShot;
    projectileType = GrenadeProjectile;

    stateName[0] = "Activate";
    stateTransitionOnTimeout[0] = "ActivateReady";
    stateTimeoutValue[0] = 0.5;
    stateSequence[0] = "Activate";
    stateSound[0] = TR2MortarSwitchSound;

    stateName[1] = "ActivateReady";
    stateTransitionOnLoaded[1] = "Ready";
    stateTransitionOnNoAmmo[1] = "NoAmmo";

    stateName[2] = "Ready";
    stateTransitionOnNoAmmo[2] = "NoAmmo";
    stateTransitionOnTriggerDown[2] = "Fire";
    //stateSound[2] = MortarIdleSound;

    stateName[3] = "Fire";
    stateSequence[3] = "Recoil";
    stateTransitionOnTimeout[3] = "Reload";
    stateTimeoutValue[3] = 0.8;
    stateFire[3] = true;
    stateRecoil[3] = LightRecoil;
    stateAllowImageChange[3] = false;
    stateScript[3] = "onFire";
    stateSound[3] = TR2MortarFireSound;

    stateName[4] = "Reload";
    stateTransitionOnNoAmmo[4] = "NoAmmo";
    stateTransitionOnTimeout[4] = "Ready";
    stateTimeoutValue[4] = 2.0;
    stateAllowImageChange[4] = false;
    stateSequence[4] = "Reload";
    stateSound[4] = TR2MortarReloadSound;

    stateName[5] = "NoAmmo";
    stateTransitionOnAmmo[5] = "Reload";
    stateSequence[5] = "NoAmmo";
    stateTransitionOnTriggerDown[5] = "DryFire";

    stateName[6]       = "DryFire";
    stateSound[6]      = TR2MortarDryFireSound;
    stateTimeoutValue[6]        = 1.5;
    stateTransitionOnTimeout[6] = "NoAmmo";
};
