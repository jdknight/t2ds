//------------------------------------------------------------------------------
//
// GameGui.cs
//
//------------------------------------------------------------------------------

// z0dd - ZOD: Execute the mission and game type skip lists so that 
// arrays are put into memory for function buildMissionList.
exec("prefs/MissionSkip.cs", true);
exec("prefs/GameTypeSkip.cs", true);

//------------------------------------------------------------------------------
function getMissionTypeDisplayNames()
{
   %file = new FileObject();
   for ( %type = 0; %type < $HostTypeCount; %type++ )
   {
      $HostTypeDisplayName[%type] = $HostTypeName[%type];
      if ( %file.openForRead( "scripts/" @ $HostTypeName[%type] @ "Game.cs" ) )
      {
         while ( !%file.isEOF() )
         {
            %line = %file.readLine();
            if ( getSubStr( %line, 0, 17 ) $= "// DisplayName = " )
            {
               $HostTypeDisplayName[%type] = getSubStr( %line, 17, 1000 );
               break;
            }
         }
      }
   }

   %file.delete();
}

//------------------------------------------------------------------------------
function buildMissionList()
{
   %search = "missions/*.mis";
   %ct = 0;
   $HostTypeCount = 0;
   $HostMissionCount = 0;
   %fobject = new FileObject();
   for( %file = findFirstFile( %search ); %file !$= ""; %file = findNextFile( %search ) )
   {
      %name = fileBase( %file ); // get the name

      // ---------------------------------------------------------------------
      // z0dd - ZOD:
      // If skip list file exists, make sure this mission isn't in skip list.
      // If mission is on the skip list then remove it from rotation.
      if(isFile("prefs/MissionSkip.cs"))
      {
         %found = 0;
         for( %i = 0; $SkipMission::name[%i] !$= ""; %i++ ) {
            if( $SkipMission::name[%i] $= %name )
            {
               error("MISSION PULLED FROM ROTATION: " @ %name);
               %found = 1;
               break;
            }
         }
         if(%found)
            continue;
      }
      // ---------------------------------------------------------------------

      %idx = $HostMissionCount;
      $HostMissionCount++;
      $HostMissionFile[%idx] = %name;
      $HostMissionName[%idx] = %name;

      if ( !%fobject.openForRead( %file ) )
         continue;

      %typeList = "None";
      while ( !%fobject.isEOF() )
      {
         %line = %fobject.readLine();
         if ( getSubStr( %line, 0, 17 ) $= "// DisplayName = " )
         {
            // Override the mission name:
            $HostMissionName[%idx] = getSubStr( %line, 17, 1000 );
         }
         else if ( getSubStr( %line, 0, 18 ) $= "// MissionTypes = " )
         {
            %typeList = getSubStr( %line, 18, 1000 );
            break;
         }
      }
      %fobject.close();

      // Don't include single player missions:
      if ( strstr( %typeList, "SinglePlayer" ) != -1 )
         continue;

      // Test to see if the mission is bot-enabled:
      %navFile = "terrains/" @ %name @ ".nav";
      $BotEnabled[%idx] = isFile( %navFile );

      for( %word = 0; ( %misType = getWord( %typeList, %word ) ) !$= ""; %word++ )
      {
         //---------------------------------------------------------------------------------
         // z0dd - ZOD - Founder(founder@mechina.com): Append Tribe Practice to CTF missions
         if(%misType $= "CTF")
            %typeList = rtrim(%typeList) @ " PracticeCTF";

         // z0dd - ZOD: If skip list file exists, make sure this mission type isn't in 
         // skip list. If mission type is on the skip list then remove it from rotation. 
         if(isFile("prefs/GameTypeSkip.cs"))
         {
            %found = 0;
            for( %i = 0; $SkipType::name[%i] !$= ""; %i++ ) {
               if( $SkipType::name[%i] $= %misType )
               {
                  error("GAME TYPE REMOVED: " @ %misType);
                  %found = 1;
                  break;
               }
            }
            if(%found)
               continue;
         }
         //---------------------------------------------------------------------------------
         for ( %i = 0; %i < $HostTypeCount; %i++ )
            if ( $HostTypeName[%i] $= %misType )
               break;
         if ( %i == $HostTypeCount )
         {
            $HostTypeCount++;
            $HostTypeName[%i] = %misType;
            $HostMissionCount[%i] = 0;
         }
         // add the mission to the type
         %ct = $HostMissionCount[%i];
         $HostMission[%i, $HostMissionCount[%i]] = %idx;
         $HostMissionCount[%i]++;
      }
   }
   getMissionTypeDisplayNames();
   %fobject.delete();
}

// One time only function call:
buildMissionList();

/////////////////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD - Founder(founder@mechina.com):
// Functions to add and remove missions from $SkipMission::name (MissionSkip.cs).

// commandToServer('AddMap', MapFilename);
function serverCmdAddMap(%client, %map)
{
   %map = detag(%map);
   if(%client.isSuperAdmin)
      AddMapToList(%map, %client);
   else
      messageClient(%client, 'MsgNotSuperAdmin', '\c2Only Super Admins can use this command.');
}
// AddMapToList(MapFilename);
function AddMapToList(%map, %client)
{
   if(%map $="")
      return;

   %found = 0;
   for( %i = 0; $SkipMission::name[%i] !$= ""; %i++ ) {
      if($SkipMission::name[%i] $= %map) {
         %found = 1;
          break;
      }
   }
   if(%found)
   {
      error( "Mission " @ %map @ " allready exists in skip list!" );
      return;
   }
   if($MissionSkip::count $= "")
      $MissionSkip::count = 0;

   $SkipMission::name[$MissionSkip::count] = %map;
   $MissionSkip::count++;

   %val = 'removal from';
   writeMissionSkipList(%map, %val, %client);
}

// commandToServer('RemoveMap', MapFilename);
function serverCmdRemoveMap(%client, %map)
{
   %map = detag(%map);
   if(%client.isSuperAdmin)
      RemoveMapFromList(%map, %client);
   else
      messageClient(%client, 'MsgNotSuperAdmin', '\c2Only Super Admins can use this command.');
}

// RemoveMapFromList(MapFilename);
function RemoveMapFromList(%map, %client)
{
   if(%map $="")
      return;

   %count = 0;
   for( %i = 0; %i < $MissionSkip::count; %i++ )
   {
      if($SkipMission::name[%i] !$= %map)
      {
         %Temp[%count] = $SkipMission::name[%i];
         %count++;
      }
   }
   for( %j = 0; %j < %count; %j++ )
      $SkipMission::name[%j] = %Temp[%j];

   $MissionSkip::count = %count;
   //$MissionSkip::count --;

   %val = 'restoration to';
   writeMissionSkipList(%map, %val, %client);
}

function writeMissionSkipList(%name, %val, %client)
{
   %newfile = "prefs/MissionSkip.cs";
   if(isFile(%newfile))
   {
      deleteFile(%newfile);
      if(isFile("prefs/MissionSkip.cs.dso"))
         deleteFile("prefs/MissionSkip.cs.dso");
   }

   %listfile = new fileObject();
   %listfile.openForWrite(%newfile);
   %listfile.writeLine("// ------------------------- Mission Skip List -------------------------");
   %listfile.writeLine("// ----- Mission file names without file extension. Ex: BeggarsRun -----");
   %listfile.writeLine("// ------------ Missions on list are excluded from rotation ------------");
   %listfile.writeLine("");
   for( %k = 0; %k < $MissionSkip::count; %k++ ) {
      %listfile.writeLine("$SkipMission::name[" @ %k @ "] = \"" @ $SkipMission::name[%k] @ "\";");
   }
   %listfile.writeLine("$MissionSkip::count = " @ $MissionSkip::count @ ";");
   %listfile.close();
   %listfile.delete();

   if(%client !$= "")
      messageClient(%client, 'MsgAdmin', '\c3\"%1\"\c2 %2 mission rotation successful.', %name, %val);

   echo( "Mission " @ %name @ " " @ %val @ " mission rotation successful." );
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
// z0dd - ZOD - Founder(founder@mechina.com):
// Functions to add and remove mission types from $SkipType::name (GameTypeSkip.cs).

// commandToServer('AddType', Typename);
function serverCmdAddType(%client, %type)
{
   %type = detag(%type);
   if(%client.isSuperAdmin)
      AddTypeToList(%type, %client);
   else
      messageClient(%client, 'MsgNotSuperAdmin', '\c2Only Super Admins can use this command.');
}

// AddTypeToList(Typename);
function AddTypeToList(%type, %client)
{
   if(%type $="")
      return;

   %found = 0;
   for( %i = 0; $SkipType::name[%i] !$= ""; %i++ ) {
      if($SkipType::name[%i] $= %type) {
         %found = 1;
          break;
      }
   }
   if(%found)
   {
      error( "Game type " @ %type @ " allready exists in skip list!" );
      return;
   }
   if($TypeSkip::count $= "")
      $TypeSkip::Count = 0;

   $SkipType::name[$TypeSkip::count] = %type;
   $TypeSkip::count++;

   %val = 'removed';
   writeTypeSkipList(%type, %val, %client);
}

// commandToServer('RemoveType', Typename);
function serverCmdRemoveType(%client, %type)
{
   %type = detag(%type);
   if(%client.isSuperAdmin)
      RemoveTypeFromList(%type, %client);
   else
      messageClient(%client, 'MsgNotSuperAdmin', '\c2Only Super Admins can use this command.');
}

// RemoveTypeFromList(Typename);
function RemoveTypeFromList(%type, %client)
{
   if(%type $="")
      return;

   %count = 0;
   for( %i = 0; %i < $TypeSkip::count; %i++ )
   {
      if($SkipType::name[%i] !$= %type)
      {
         %Temp[%count] = $SkipType::name[%i];
         %count++;
      }
   }
   for( %j = 0; %j < %count; %j++ )
      $SkipType::name[%j] = %Temp[%j];

   $TypeSkip::count = %count;
   //$TypeSkip::count --;

   %val = 'restored';
   writeTypeSkipList(%type, %val, %client);
}

function writeTypeSkipList(%name, %val, %client)
{
   %newfile = "prefs/GameTypeSkip.cs";
   if(isFile(%newfile))
   {
      deleteFile(%newfile);
      if(isFile("prefs/GameTypeSkip.cs.dso"))
         deleteFile("prefs/GameTypeSkip.cs.dso");
   }
   %listfile = new fileObject();
   %listfile.openForWrite(%newfile);
   %listfile.writeLine("// ------------------------- Game Type Skip List -------------------------");
   %listfile.writeLine("// ----------------------- Game type names. Ex: CnH ----------------------");
   %listfile.writeLine("// ------------ Game types on list are excluded from rotation ------------");
   %listfile.writeLine("");
   for( %k = 0; %k < $TypeSkip::count; %k++ ) {
      %listfile.writeLine("$SkipType::name[" @ %k @ "] = \"" @ $SkipType::name[%k] @ "\";");
   }
   %listfile.writeLine("$TypeSkip::count = " @ $TypeSkip::count @ ";");
   %listfile.close();
   %listfile.delete();

   if(%client !$= "")
      messageClient(%client, 'MsgAdmin', '\c2Game type \c3\"%1\"\c2 %2 successfully.', %name, %val);

   echo( "Game type " @ %name @ " " @ %val @ " successfully." );
}

//------------------------------------------------------------------------------
function validateMissionAndType(%misName, %misType)
{
   for ( %mis = 0; %mis < $HostMissionCount; %mis++ )
      if( $HostMissionFile[%mis] $= %misName )
         break;
   if ( %mis == $HostMissionCount )
      return false;
   for ( %type = 0; %type < $HostTypeCount; %type++ )
      if ( $HostTypeName[%type] $= %misType )
         break;
   if(%type == $hostTypeCount)
      return false;
   $Host::Map = $HostMissionFile[%mis];
   $Host::MissionType = $HostTypeName[%type];

   return true;
}

//------------------------------------------------------------------------------
// This function returns the index of the next mission in the mission list.
//------------------------------------------------------------------------------
function getNextMission( %misName, %misType )
{
   // First find the index of the mission in the master list:
   for ( %mis = 0; %mis < $HostMissionCount; %mis++ )
      if( $HostMissionFile[%mis] $= %misName )
         break;

   if ( %mis == $HostMissionCount )
      return "";

   // Now find the index of the mission type:
   for ( %type = 0; %type < $HostTypeCount; %type++ )
      if ( $HostTypeName[%type] $= %misType )
         break;

   if ( %type == $hostTypeCount )
      return "";

   // Now find the mission's index in the mission-type specific sub-list:
   for ( %i = 0; %i < $HostMissionCount[%type]; %i++ )
      if ( $HostMission[%type, %i] == %mis )
         break;

   // --------------------------------------------------------------------
   // z0dd - ZOD: Enable random mission rotation for current mission type.
   if($Host::ClassicRandomMissions)
   {
      %i = mFloor(getRandom(0, ($HostMissionCount[%type] - 1)));

      // If its same as last map, go back 1
      if($HostMissionFile[$HostMission[%type, %i]] $= %misName)
         %i--;

      // If its greater then or equal to count, set to zero
      %i = %i >= $HostMissionCount[%type] ? 0 : %i;
   }
   else
   {
      // Go BACKWARDS, because the missions are in reverse alphabetical order:
      if ( %i == 0 )   
         %i = $HostMissionCount[%type] - 1;
      else
         %i--;
   }
   // --------------------------------------------------------------------

   // If there are bots in the game, don't switch to any maps without
   // a NAV file:
   if ( $HostGameBotCount > 0 )
   {
      for ( %j = 0; %j < $HostMissionCount[%type] - 1; %j++ )
      {
         if ( $BotEnabled[$HostMission[%type, %i]] )
            break;

         // --------------------------------------------------------------------
         // z0dd - ZOD: Enable random mission rotation for current mission type.
         if($Host::ClassicRandomMissions)
         {
            %i = mFloor(getRandom(0, ($HostMissionCount[%type] - 1)));

            // If its same as last map, go back 1
            if($HostMissionFile[$HostMission[%type, %i]] $= %misName)
               %i--;

            // If its greater then or equal to count, set to zero
            %i = %i >= $HostMissionCount[%type] ? 0 : %i;
         }
         else
         {
            if ( %i == 0 )
               %i = $HostMissionCount[%type] - 1;
            else
               %i--;
         }
         // --------------------------------------------------------------------
      }
   }
   
   return $HostMission[%type, %i];
}
