//------------------------------------------------------------------------------
//
// scoreList.cs
//
//------------------------------------------------------------------------------

function updateScores()
{
   if ( !isObject( Game ) )
      return;

   %numTeams = Game.numTeams;

   // Initialize the team counts:
   for ( %teamIndex = 0; %teamIndex <= %numTeams; %teamIndex++ )
      Game.teamCount[%teamIndex] = 0;

   %count = ClientGroup.getCount();
   for ( %clientIndex = 0; %clientIndex < %count; %clientIndex++ )
   {
      %cl = ClientGroup.getObject( %clientIndex );
      %team = %cl.getSensorGroup();
      if ( %numTeams == 1 && %team != 0 )
         %team = 1;
      Game.teamScores[%team, Game.teamCount[%team], 0] = %cl.name;
      if ( %cl.score $= "" )
         Game.teamScores[%team, Game.teamCount[%team], 1] = 0;
      else
         Game.teamScores[%team, Game.teamCount[%team], 1] = %cl.score;
      Game.teamCount[%team]++;
   }
}

//------------------------------------------------------------------------------
function serverCmdGetScores( %client )
{
   // Client has requested the score list, so give it to 'em...
   if (isObject(Game))   
   {
      updateScores();
      %teamCount = Game.numTeams;

      if (%teamCount > 1)
      {
         // Send team messages:
         for (%team = 1; %team <= %teamCount; %team++)
            messageClient(%client, 'MsgTeamScore', "", %team, $teamScore[%team]);

			//send the player scores in order of their team rank...
			for (%team = 1; %team <= %teamCount; %team++)
			{
				for (%i = 0; %i < $TeamRank[%team, count]; %i++)
            {
               %cl = $TeamRank[%team, %i];
	            messageClient( %client, 'MsgPlayerScore', "", %cl, %cl.score, %cl.getPing(), %cl.getPacketLoss());
            }
			}
      }
		else
		{
			//send the player scores in order of their rank...
			for (%i = 0; %i < $TeamRank[0, count]; %i++)
         {
            %cl = $TeamRank[0, %i];
            messageClient( %client, 'MsgPlayerScore', "", %cl, %cl.score, %cl.getPing(), %cl.getPacketLoss());
         }
		}

		//now send the observers over
		%count = ClientGroup.getCount();
		for (%i = 0; %i < %count; %i++)
		{
			%cl = ClientGroup.getObject(%i);
			if (%cl.team <= 0)
            messageClient( %client, 'MsgPlayerScore', "", %cl, %cl.score, %cl.getPing(), %cl.getPacketLoss());
		}
   }
}
