//------------------------------------------------------------------------------
//
// Mine-Disk Support
//
//------------------------------------------------------------------------------

// override mine deployment to permit mine disks
function MineDeployed::damageObject(%data, %targetObject, %sourceObject,
        %position, %amount, %damageType)
{
    if (%targetObject.boom)
        return;

    %targetObject.damaged += %amount;

    if (%targetObject.damaged >= %data.maxDamage)
        %targetObject.setDamageState(Destroyed);
}
