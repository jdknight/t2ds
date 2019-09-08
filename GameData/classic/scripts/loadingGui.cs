//------------------------------------------------------------------------------
//
// LoadingGui.cs
//
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
function LoadingGui::onAdd(%this)
{
   %this.qLineCount = 0;
}

//------------------------------------------------------------------------------
function LoadingGui::onWake(%this)
{
   if ( $HudHandle[shellScreen] !$= "" )
   {
      alxStop($HudHandle[shellScreen]);
      $HudHandle[shellScreen] = "";   
   }
   $HudHandle[loadingScreen] = alxPlay(LoadingScreenSound, 0, 0, 0);

	CloseMessagePopup();
}

//------------------------------------------------------------------------------
function LoadingGui::onSleep(%this)
{
   // Clear the load info:
   if ( %this.qLineCount !$= "" )
   {
      for ( %line = 0; %line < %this.qLineCount; %line++ )
         %this.qLine[%line] = "";
   }      
   %this.qLineCount = 0;

   LOAD_MapPic.setBitmap( "gui/Loading" );
   LOAD_MapName.setText( "" );
   LOAD_MapText.setText( "" );
   LOAD_MissionType.setText( "" );
   LOAD_GameText.setText( "" );
   LoadingProgress.setValue( 0 );

   alxStop($HudHandle[loadingScreen]);
}

//------------------------------------------------------------------------------
function clearLoadInfo()
{
   for ( %line = 0; %line < $LoadQuoteLineCount; %line++ )
      $LoadQuoteLine[%line] = "";
   $LoadQuoteLineCount = 0;

   for ( %line = 0; %line < $LoadObjLineCount; %line++ )
      $LoadObjLine[%line] = "";
   $LoadObjLineCount = 0;

   for ( %line = 0; %line < $LoadRuleLineCount; %line++ )
      $LoadRuleLine[%line] = "";
   $LoadRuleLineCount = 0;
}

//------------------------------------------------------------------------------
function buildLoadInfo( %mission, %missionType )
{
   clearLoadInfo();
   $CurrentMission = %mission;
   $MissionDisplayName = %mission;
   $MissionTypeDisplayName = %missionType;

   // Extract the map quote and objectives from the .mis file:
   %mapFile = "missions/" @ %mission @ ".mis";
   %file = new FileObject();
   if ( %file.openForRead( %mapFile ) )
   {
      %state = "none";
      while ( !%file.isEOF() )
      {
         %line = %file.readLine();

         if ( %state $= "none" )
         {
            if ( getSubStr( %line, 0, 17 ) $= "// DisplayName = " )
               $MissionDisplayName = getSubStr( %line, 17, 1000 );
            else if ( %line $= "//--- MISSION QUOTE BEGIN ---" )
               %state = "quote";
            else if ( %line $= "//--- MISSION STRING BEGIN ---" )
               %state = "objectives";
            else if ( %missionType $= "SinglePlayer" )
            {
               if ( getSubStr( %line, 0, 16 ) $= "// PlanetName = " )
                  $MissionTypeDisplayName = getSubStr( %line, 16, 1000 );
               else if ( %line $= "//--- MISSION BLURB BEGIN ---" )
                  %state = "blurb";
            }
         }
         else if ( %state $= "quote" )
         {
            if ( %line $= "//--- MISSION QUOTE END ---" )
               %state = "none";
            else
            {
               $LoadQuoteLine[$LoadQuoteLineCount] = getSubStr( %line, 2, 1000 );
               $LoadQuoteLineCount++;
            }
         }
         else if ( %state $= "objectives" )
         {
            if ( %line $= "//--- MISSION STRING END ---" )
            {
               if ( %missionType $= "SinglePlayer" )
                  %state = "none";
               else
               {
                  // Once we've got the end of the mission string, we are through.
                  %state = "done";
                  break;
               }
            }
            else
            {
               %pos = strstr( %line, "]" );
               if ( %pos == -1 )
               {
                  $LoadObjLine[$LoadObjLineCount] = getSubStr( %line, 2, 1000 );
                  $LoadObjLineCount++;
               }
               else if ( %pos > 3 )
               {
                  // Filter objective lines by mission type:
                  %typeList = getSubStr( %line, 3, %pos - 3 );
                  // ------------------------------------------------------------------------
                  // z0dd - ZOD, 5/15/02. Add practice gametype so we get objectives printed.
                  if(%typeList $= "CTF")
                     %typeList = rtrim(%typeList) @ " PracticeCTF";
                  // ------------------------------------------------------------------------
                  if ( strstr( %typeList, %missionType ) != -1 )
                  {
                     $LoadObjLine[$LoadObjLineCount] = getSubStr( %line, %pos + 1, 1000 );
                     $LoadObjLineCount++;
                  }
               }
               else
                  error( "Invalid mission objective line - \"" @ %line @ "\"" );
            }
         }
         else if ( %state $= "blurb" )
         {
            if ( %line $= "//--- MISSION BLURB END ---" )
            {
               %state = "done";
               break;
            }
            else
            {
               $LoadRuleLine[$LoadRuleLineCount] = getSubStr( %line, 2, 1000 );
               $LoadRuleLineCount++;
            }
         }
      }
      %file.close();
   }

   // Extract the rules of engagement from the <mission type>Game.cs file:
   if ( %missionType !$= "SinglePlayer" )
   {
      %gameFile = "scripts/" @ %missionType @ "Game.cs";
      if ( %file.openForRead( %gameFile ) )
      {
         %state = "none";
         while ( !%file.isEOF() )
         {
            %line = %file.readLine();
            if ( %state $= "none" )
            {
               if ( getSubStr( %line, 0, 17 ) $= "// DisplayName = " )
                  $MissionTypeDisplayName = getSubStr( %line, 17, 1000 );
               if ( %line $= "//--- GAME RULES BEGIN ---" )
                  %state = "rules";
            }
            else if ( %state $= "rules" )
            {
               if ( %line $= "//--- GAME RULES END ---" )
               {
                  %state = "done";
                  break;
               }
               else
               {
                  $LoadRuleLine[$LoadRuleLineCount] = getSubStr( %line, 2, 1000 );
                  $LoadRuleLineCount++;
               }
            }
         }
         %file.close();
      }
   }

   %file.delete();
}

//------------------------------------------------------------------------------
function dumpLoadInfo()
{
   echo( "Mission = \"" @ $MissionDisplayName @ "\", Mission Type = \"" @ $MissionTypeDisplayName @ "\"" );
   echo( "MISSION QUOTE: ( " @ $LoadQuoteLineCount @ " lines )" );
   for ( %line = 0; %line < $LoadQuoteLineCount; %line++ )
      echo( $LoadQuoteLine[%line] );

   echo( " " );

   echo( "MISSION STRING: ( " @ $LoadObjLineCount @ " lines )" );
   for ( %line = 0; %line < $LoadObjLineCount; %line++ )
      echo( $LoadObjLine[%line] );

   echo( " " );

   echo( "GAME RULES: ( " @ $LoadRuleLineCount @ " lines )" );
   for ( %line = 0; %line < $LoadRuleLineCount; %line++ )
      echo( $LoadRuleLine[%line] );
}

//------------------------------------------------------------------------------
// z0dd - ZOD, 5/12/02. Added another varible so we can send this twice
function sendLoadInfoToClient( %client, %second )
{
   //error( "** SENDING LOAD INFO TO CLIENT " @ %client @ "! **" );
   %singlePlayer = $CurrentMissionType $= "SinglePlayer";
   messageClient( %client, 'MsgLoadInfo', "", $CurrentMission, $MissionDisplayName, $MissionTypeDisplayName );

   // Send map quote:
   for ( %line = 0; %line < $LoadQuoteLineCount; %line++ )
   {
      if ( $LoadQuoteLine[%line] !$= "" )
         messageClient( %client, 'MsgLoadQuoteLine', "", $LoadQuoteLine[%line] );
   }

   // Send map objectives:
   if ( %singlePlayer )
   {
      switch ( $pref::TrainingDifficulty )
      {
         case 2:  %diff = "Medium";
         case 3:  %diff = "Hard";
         default: %diff = "Easy";
      }
      messageClient( %client, 'MsgLoadObjectiveLine', "", "<spush><font:" @ $ShellLabelFont @ ":" @ $ShellMediumFontSize @ ">DIFFICULTY: <spop>" @ %diff );
   }

   for ( %line = 0; %line < $LoadObjLineCount; %line++ )
   {
      if ( $LoadObjLine[%line] !$= "" )
         messageClient( %client, 'MsgLoadObjectiveLine', "", $LoadObjLine[%line], !%singlePlayer );
   }

   // Send rules of engagement:
   if ( !%singlePlayer )
      messageClient( %client, 'MsgLoadRulesLine', "", "<spush><font:Univers Condensed:18>RULES OF ENGAGEMENT:<spop>", false );

   for ( %line = 0; %line < $LoadRuleLineCount; %line++ )
   {
      if ( $LoadRuleLine[%line] !$= "" )
         messageClient( %client, 'MsgLoadRulesLine', "", $LoadRuleLine[%line], !%singlePlayer );
   }

   messageClient( %client, 'MsgLoadInfoDone' );

   // ----------------------------------------------------------------------------------------------
   // z0dd - ZOD, 5/12/02. Send the mod info screen if this isn't the second showing of mission info
   if(!%second)
      schedule(6000, 0, "sendModInfoToClient", %client);
   // ----------------------------------------------------------------------------------------------
}

function sendModInfoToClient(%client)
{
   %on = "On";
   %off = "Off";
   %time = "<color:556B2F>Time limit: <color:8FBC8F>" @ $Host::TimeLimit;
   %max = "<color:556B2F>Max players: <color:8FBC8F>" @ $Host::MaxPlayers;
   %td = "<color:556B2F>Team damage: <color:8FBC8F>" @ ($Host::TeamDamageOn ? %on : %off);
   %crc = "<color:556B2F>CRC checking: <color:8FBC8F>" @ ($Host::CRCTextures ? %on : %off);
   %pure = "<color:556B2F>Pure server: <color:8FBC8F>" @ ($Host::PureServer ? %on : %off);
   %smurf = "<color:556B2F>Refuse smurfs: <color:8FBC8F>" @ ($Host::NoSmurfs ? %on : %off);
   %random = "<color:556B2F>Random teams: <color:8FBC8F>" @ ($Host::ClassicRandomizeTeams ? %on : %off); 
   if($CurrentMissionType $= "PracticeCTF")
      %prac = "<color:556B2F>Practice Mode: <color:8FBC8F>On";
   else
      %prac = "<color:556B2F>Practice Mode: <color:8FBC8F>Off";

   if(!$Host::ClassicRandomMissions)
      %nmis = "<color:556B2F>Next mission: <color:8FBC8F>" @ findNextCycleMission();
   else
      %nmis = "<color:556B2F>Next mission: <color:8FBC8F>Randomly selected";

   %modName = "Classic  1.1";
   %ModCnt = 1;
   %ModLine[0] = "<color:556B2F>Developers: <color:8FBC8F><a:PLAYER\tz0dd>z0dd</a> and <a:PLAYER\t-ZOD->ZOD</a>";

   %SpecialCnt = 3;
   %SpecialTextLine[0] = %prac;
   %SpecialTextLine[1] = %random;
   %SpecialTextLine[2] = %nmis;

   %ServerCnt = 6;
   %ServerTextLine[0] = %time;
   %ServerTextLine[1] = %max;
   %ServerTextLine[2] = %td;
   %serverTextLine[3] = %crc;
   %ServerTextLine[4] = %pure;
   %ServerTextLine[5] = %smurf;
   //%ServerTextLine[6] = %prac;
   //%ServerTextLine[7] = %nmis;

   %singlePlayer = $CurrentMissionType $= "SinglePlayer";
   messageClient( %client, 'MsgLoadInfo', "", $CurrentMission, %modName, $Host::GameName );

   // Send mod details (non bulleted list, small text):
   for(%line = 0; %line < %ModCnt; %line++)
   {
      if(%ModLine[%line] !$= "")
         messageClient(%client, 'MsgLoadQuoteLine', "", %ModLine[%line]);
   }

   // Send mod special settings (bulleted list, large text):
   for (%line = 0; %line < %SpecialCnt; %line++)
   {
      if(%SpecialTextLine[%line] !$= "")
         messageClient( %client, 'MsgLoadObjectiveLine', "", %SpecialTextLine[%line], !%singlePlayer);
   }

   // Send server info:
   if ( !%singlePlayer )
      messageClient( %client, 'MsgLoadRulesLine', "", "<color:8FBC8F>" @ $Host::Info, false );

   for(%line = 0; %line < %ServerCnt; %line++)
   {
      if (%ServerTextLine[%line] !$= "")
         messageClient(%client, 'MsgLoadRulesLine', "", %ServerTextLine[%line], !%singlePlayer);
   }
   messageClient(%client, 'MsgLoadInfoDone');
   // z0dd - ZOD, 5/12/02. Send mission info again so as not to conflict with cs scripts.
   schedule(7000, 0, "sendLoadInfoToClient", %client, true);
}

//------------------------------------------------------------------------------
addMessageCallback( 'MsgLoadInfo', handleLoadInfoMessage );
addMessageCallback( 'MsgLoadQuoteLine', handleLoadQuoteLineMessage );
addMessageCallback( 'MsgLoadObjectiveLine', handleLoadObjectiveLineMessage );
addMessageCallback( 'MsgLoadRulesLine', handleLoadRulesLineMessage );
addMessageCallback( 'MsgLoadInfoDone', handleLoadInfoDoneMessage );

//------------------------------------------------------------------------------
function handleLoadInfoMessage( %msgType, %msgString, %bitmapName, %mapName, %missionType )
{
   // Clear all of the loading info lines:
   for ( %line = 0; %line < LoadingGui.qLineCount; %line++ )
      LoadingGui.qLine[%line] = "";
   LoadingGui.qLineCount = 0;

   for ( %line = 0; %line < LobbyGui.objLineCount; %line++ )
      LobbyGui.objLine[%line] = "";
   LobbyGui.objLineCount = 0;

   // ---------------------------------------------------
   // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
   %loadBmp = "gui/load_" @ %bitmapName @ ".png";
   // ---------------------------------------------------

   if ( !isFile( "textures/" @ %loadBmp ) )
      %loadBmp = "gui/loading";
   LOAD_MapPic.setBitmap( %loadBmp );
   LOAD_MapName.setText( %mapName );
   LOAD_MissionType.setText( %missionType );
   LOAD_MapText.setText( "" );
   LOAD_GameText.setText( "" );
}

//------------------------------------------------------------------------------
function handleLoadQuoteLineMessage( %msgType, %msgString, %line )
{
   LoadingGui.qLine[LoadingGui.qLineCount] = %line;
   LoadingGui.qLineCount++;

   %text = "<spush><color:dcdcdc><spush><font:Univers italic:16>";
   for ( %line = 0; %line < LoadingGui.qLineCount - 1; %line++ )
      %text = %text @ LoadingGui.qLine[%line] @ "\n";
   %text = %text @ "<spop><spush><font:" @ $ShellLabelFont @ ":" @ $ShellFontSize @ ">";
   %text = %text @ LoadingGui.qLine[%line] @ "<spop><spop>\n"; // tag line

   LOAD_MapText.setText( %text );
}

//------------------------------------------------------------------------------
function handleLoadObjectiveLineMessage( %msgType, %msgString, %line, %bulletStyle )
{
   LobbyGui.objLine[LobbyGui.objLineCount] = %line;
   LobbyGui.objLineCount++;

   if ( %bulletStyle )
      %line = "<bitmap:bullet_2><lmargin:24>" @ %line @ "<lmargin:0>";

   %newText = LOAD_MapText.getText();
   if ( %newText $= "" ) // In case there's no quote 
      %newText = %line;
   else
      %newText = %newText NL %line;
   LOAD_MapText.setText( %newText );
}

//------------------------------------------------------------------------------
function handleLoadRulesLineMessage( %msgType, %msgString, %line, %bulletStyle )
{
   if ( %bulletStyle )
      %line = "<bitmap:bullet_2><lmargin:24>" @ %line @ "<lmargin:0>";

   %newText = LOAD_GameText.getText();
   if ( %newText $= "" )
      %newText = %line;
   else
      %newText = %newText NL %line;
   LOAD_GameText.setText( %newText );
}

//------------------------------------------------------------------------------
function handleLoadInfoDoneMessage( %msgType, %msgString )
{
   LoadingGui.gotLoadInfo = true;
}