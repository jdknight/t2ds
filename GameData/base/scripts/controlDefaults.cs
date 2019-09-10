
function ServerCmdStartUseBackpack( %client, %data )
{
   %client.deployPack = false;
   %client.getControlObject().use( %data );
}  

function ServerCmdEndUseBackpack( %client )
{
   %client.deployPack = true;
}  

function serverCmdTestLOS(%client)
{
   %client.sendTargetTo(%client);
   %msg = 'This is a simple test.';
   messageClient(%client, 'TestMsg', %msg);
}

function serverCmdSetVehicleWeapon(%client, %num)
{
   %turret = %client.player.getControlObject();
   if(%turret.getDataBlock().numWeapons < %num)
      return;
   %turret.selectedWeapon = %num;
   
   //%hudNum = %turret.getDataBlock().getHudNum(%num);
   //%client.setVWeaponsHudActive(%hudNum);
   %client.setVWeaponsHudActive(%num);

   // set the active image on the client's obj
   if(%num == 1)
      %client.setObjectActiveImage(%turret, 2);
   else if(%num == 2)
      %client.setObjectActiveImage(%turret, 4);
   else
      %client.setObjectActiveImage(%turret, 6);

   // if firing then set the proper image trigger
   if(%turret.fireTrigger)
   {
      if(%num == 1)
      {
         %turret.setImageTrigger(4, false);
         if(%turret.getImageTrigger(6))
         {
            %turret.setImageTrigger(6, false);
            ShapeBaseImageData::deconstruct(%turret.getMountedImage(6), %turret);
         }
         %turret.setImageTrigger(2, true);
      }
      else if( %num == 2)
      {
         %turret.setImageTrigger(2, false);
         if(%turret.getImageTrigger(6))
         {
            %turret.setImageTrigger(6, false);
            ShapeBaseImageData::deconstruct(%turret.getMountedImage(6), %turret);
         }
         %turret.setImageTrigger(4, true);
      }            
      else
      {
         %turret.setImageTrigger(2, false);
         %turret.setImageTrigger(4, false);
      }                                   
   }   
}

function serverCmdSwitchVehicleWeapon(%client, %dir)
{
   %turret = %client.player.getControlObject();
   %weaponNum = %turret.selectedWeapon;
   if(%dir $= "next")
   {
      if(%weaponNum++ > %turret.getDataBlock().numWeapons)
         %weaponNum = 1;
   }
   else
   {
      if(%weaponNum-- < 1)
         %weaponNum = %turret.getDataBlock().numWeapons;
   }
   serverCmdSetVehicleWeapon(%client, %weaponNum);
}
